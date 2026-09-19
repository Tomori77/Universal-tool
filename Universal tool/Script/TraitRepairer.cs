// 万能修复器：鉴定（普通/高级/全部）+ 去诅咒，消耗万能精华。
// 鉴定方式对齐魔法师 NPC（TraitAppraiser.CanIdentify）：
//   普通鉴定 = ActEffect.Proc(EffectId.Identify)（rarity < Mythical）
//   高级鉴定 = ActEffect.Proc(EffectId.GreaterIdentify)（Mythical/Artifact 神器）
//   鉴定全部 = Thing.Identify(false) 批量普通鉴定
using System.Collections.Generic;
using UnityEngine;

public class TraitRepairer : Trait
{
    public static LayerList CurLayer;

    // 背包右键"使用"出现的前提
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenRepairer();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("actUse", delegate
        {
            OpenRepairer();
            return false;
        });
    }

    // 主菜单：鉴定（普通）/ 高级鉴定 / 鉴定全部 / 去诅咒
    public static void OpenRepairer()
    {
        CurLayer = EClass.ui.AddLayer<LayerList>();
        CurLayer.SetSize(640f, 480f);
        ShowMainMenu();
    }

    public static void ShowMainMenu()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<string> opts = new List<string>
        {
            "鉴定（普通）",
            "高级鉴定（神器）",
            "鉴定全部",
            "去诅咒",
        };
        CurLayer.SetList(opts, (string s) => s, delegate(int i, string s)
        {
            switch (i)
            {
                case 0:
                    ShowIdentifyList(false);
                    break;
                case 1:
                    ShowIdentifyList(true);
                    break;
                case 2:
                    IdentifyAll();
                    break;
                case 3:
                    ShowUncurseList();
                    break;
            }
        }, autoClose: false);
    }

    // 鉴定列表（superior=false 普通 / true 高级神器）
    public static void ShowIdentifyList(bool superior)
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<Thing> items = EClass.pc.things.List(delegate(Thing t)
        {
            if (t == null || t.isDestroyed || t.IsIdentified)
            {
                return false;
            }
            if (superior)
            {
                return t.rarity >= Rarity.Mythical;
            }
            return t.rarity < Rarity.Mythical;
        });
        int cost = superior ? ModConfig.Instance.RepairIdentifySPCost.Value : ModConfig.Instance.RepairIdentifyCost.Value;
        CurLayer.SetList(items, delegate(Thing t)
        {
            return t.Name + "（" + cost + " 精华）";
        }, delegate(int i, string s)
        {
            Identify(items[i], superior);
        }, autoClose: false);
    }

    // 去诅咒列表
    public static void ShowUncurseList()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<Thing> items = EClass.pc.things.List(delegate(Thing t)
        {
            return t != null && !t.isDestroyed && t.blessedState <= BlessedState.Cursed;
        });
        int cost = ModConfig.Instance.RepairUncurseCost.Value;
        CurLayer.SetList(items, delegate(Thing t)
        {
            return t.Name + "（" + cost + " 精华）";
        }, delegate(int i, string s)
        {
            Uncurse(items[i]);
        }, autoClose: false);
    }

    public static void Identify(Thing t, bool superior)
    {
        int cost = superior ? ModConfig.Instance.RepairIdentifySPCost.Value : ModConfig.Instance.RepairIdentifyCost.Value;
        Thing essence = EClass.pc.things.Find("ut_universal_essence");
        if (essence == null || essence.Num < cost)
        {
            SE.Beep();
            return;
        }
        essence.ModNum(-cost);
        ActEffect.Proc(superior ? EffectId.GreaterIdentify : EffectId.Identify, EClass.pc, t);
        ShowIdentifyList(superior); // 原地刷新剩余未鉴定物品
    }

    // 批量普通鉴定：弹输入框让用户输入鉴定个数，判定精华是否足够，不足时整体拒绝
    public static void IdentifyAll()
    {
        List<Thing> items = EClass.pc.things.List(delegate(Thing t)
        {
            return t != null && !t.isDestroyed && !t.IsIdentified && t.rarity < Rarity.Mythical;
        });
        if (items.Count == 0)
        {
            SE.Beep();
            return;
        }
        int cost = ModConfig.Instance.RepairIdentifyCost.Value;
        Dialog.InputName("鉴定数量（可鉴定 " + items.Count + " 件，每件 " + cost + " 精华）", "", delegate(bool cancel, string text)
        {
            if (cancel)
            {
                return;
            }
            int n;
            if (!int.TryParse(text, out n) || n <= 0)
            {
                SE.Beep();
                return;
            }
            if (n > items.Count)
            {
                SE.Beep();
                return;
            }
            int total = n * cost;
            Thing essence = EClass.pc.things.Find("ut_universal_essence");
            if (essence == null || essence.Num < total)
            {
                SE.Beep(); // 精华不足，整体拒绝
                return;
            }
            essence.ModNum(-total);
            for (int i = 0; i < n; i++)
            {
                items[i].Identify(false);
            }
            Msg.Say("identified", EClass.pc, n.ToString() + " 件");
            ShowMainMenu();
        });
    }

    public static void Uncurse(Thing t)
    {
        int cost = ModConfig.Instance.RepairUncurseCost.Value;
        Thing essence = EClass.pc.things.Find("ut_universal_essence");
        if (essence == null || essence.Num < cost)
        {
            SE.Beep();
            return;
        }
        essence.ModNum(-cost);
        ActEffect.Proc(EffectId.Uncurse, EClass.pc, t); // tc 是装备 → 直接去诅咒
        ShowUncurseList(); // 原地刷新剩余诅咒物品
    }
}
