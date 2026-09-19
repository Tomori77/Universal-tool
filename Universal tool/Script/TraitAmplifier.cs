// 增幅器：消耗万能精华获取属性临时增益（ModTempElement，游戏自然衰减）
// 界面：属性列表 → 任意数量输入框（含阶梯公式提示）→ 按阶梯单价计算增益
using System.Collections.Generic;
using UnityEngine;

public class TraitAmplifier : Trait
{
    // 属性元素 id（Card.cs 确认）：70=力量 71=体质 72=灵巧 73=感知 74=学习 75=意志 76=魔力 77=魅力
    public static readonly string[] ItemIds = { "ut_universal_essence", "ut_universal_essence", "ut_universal_essence", "ut_universal_essence", "ut_universal_essence", "ut_universal_essence", "ut_universal_essence", "ut_universal_essence" };
    public static readonly int[] EleIds = { 70, 71, 72, 73, 74, 75, 76, 77 };
    public static readonly string[] EleNames = { "力量", "体质", "灵巧", "感知", "学习", "意志", "魔力", "魅力" };

    public static LayerList CurLayer;

    // 阶梯单价（单价 = 每个万能精华可兑换的属性点数，读配置）
    public static int GetTierPrice(int n)
    {
        if (n <= 20)
        {
            return ModConfig.Instance.AmpTier1.Value;
        }
        if (n <= 50)
        {
            return ModConfig.Instance.AmpTier2.Value;
        }
        if (n <= 199)
        {
            return ModConfig.Instance.AmpTier3.Value;
        }
        return ModConfig.Instance.AmpTier4.Value;
    }

    // 阶梯公式文案（显示在输入框旁，随配置动态生成）
    public static string TierText
    {
        get
        {
            ModConfig cfg = ModConfig.Instance;
            return "阶梯：1-20个" + cfg.AmpTier1.Value + "点/个；21-50个" + cfg.AmpTier2.Value + "点/个；51-199个" + cfg.AmpTier3.Value + "点/个；200个以上" + cfg.AmpTier4.Value + "点/个";
        }
    }

    // 背包右键"使用"出现的前提
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenAmplifier();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("actUse", delegate
        {
            OpenAmplifier();
            return false;
        });
    }

    // 阶梯单价：按消耗数量分档（整体单价）
    public static int GetBuffValue(int n)
    {
        return n * GetTierPrice(n);
    }

    // 属性列表（不自动关闭，点选后弹数量输入框）
    public static void OpenAmplifier()
    {
        CurLayer = EClass.ui.AddLayer<LayerList>();
        CurLayer.SetSize(640f, 480f);
        Refresh();
    }

    public static void Refresh()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        Thing essence = EClass.pc.things.Find("ut_universal_essence");
        int have = (essence != null) ? essence.Num : 0;
        bool debug = ModConfig.Instance.DebugId.Value;
        List<string> opts = new List<string>();
        for (int i = 0; i < EleNames.Length; i++)
        {
            opts.Add(EleNames[i] + "（万能精华持有 " + have + "）" + (debug ? " <ele:" + EleIds[i] + ">" : ""));
        }
        CurLayer.SetList(opts, (string s) => s, delegate(int i, string s)
        {
            PromptAmount(i);
        }, autoClose: false);
    }

    // 弹出任意数量输入框（含阶梯公式提示）
    public static void PromptAmount(int idx)
    {
        Dialog.InputName("万能精华数量 " + TierText, "", delegate(bool cancel, string text)
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
            Apply(idx, n);
        });
    }

    // 绕过 ModTempElement 的"临时属性上限"（(|基础值|+100)/4 截断），直接累加到 tempElements
    public static void AddTempElement(Chara c, int ele, int v)
    {
        if (c.tempElements == null)
        {
            c.tempElements = new ElementContainer();
            c.tempElements.SetParent(c);
        }
        c.tempElements.ModBase(ele, v);
    }

    public static void Apply(int idx, int n)
    {
        Thing t = EClass.pc.things.Find(ItemIds[idx]);
        if (t == null || t.Num < n)
        {
            SE.Beep();
            return;
        }
        int eleId = EleIds[idx];
        int value = GetBuffValue(n);
        t.ModNum(-n);
        if (ModConfig.Instance.AmpBreakCap.Value)
        {
            AddTempElement(EClass.pc, eleId, value); // 直接累加临时属性，突破上限
        }
        else
        {
            EClass.pc.ModTempElement(eleId, value); // 游戏默认：受 (|基础值|+100)/4 上限截断
        }
        Msg.Say("trainSkill", EClass.pc, EleNames[idx] + " +" + value);
        Refresh(); // 原地刷新持有数
    }
}
