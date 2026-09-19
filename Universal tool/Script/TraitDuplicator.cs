// 复制器：消耗万能精华复制物品（自定义 UI：列表 → 检查材料 → 消耗 → 产出）
// 复制配方可扩展（SrcIds/SrcNums/EssenceCosts/OutNums 数组一一对应）
using System.Collections.Generic;
using UnityEngine;

public class TraitDuplicator : Trait
{
    // 复制配方：大地结晶 ×1 + 精华 ×10 → ×2；太阳结晶 ×1 + 精华 ×30 → ×2；魔力结晶 ×1 + 精华 ×100 → ×2；金属锭 ×1 + 精华 ×100 → ×2
    public static readonly string[] SrcIds = { "crystal_earth", "crystal_sun", "crystal_mana", "ingot" };
    public static readonly int[] SrcNums = { 1, 1, 1, 1 };
    public static readonly int[] EssenceCosts = { 10, 30, 100, 100 };
    public static readonly int[] OutNums = { 2, 2, 2, 2 };

    public static LayerList CurLayer;

    // 背包右键"使用"出现的前提
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenDuplicator();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("actUse", delegate
        {
            OpenDuplicator();
            return false;
        });
    }

    // 复制列表（不自动关闭，复制后可原地刷新）
    public static void OpenDuplicator()
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
        List<string> opts = new List<string>();
        for (int i = 0; i < SrcIds.Length; i++)
        {
            string name = EClass.sources.things.map[SrcIds[i]].GetName();
            opts.Add(name + " ×" + SrcNums[i] + " → ×" + OutNums[i] + "（精华 ×" + EssenceCosts[i] + "）");
        }
        CurLayer.SetList(opts, (string s) => s, delegate(int i, string s)
        {
            AskCount(i);
        }, autoClose: false);
    }

    // 点击配方后弹输入框决定复制数量（用户定稿）：N 份材料/精华 → N 份产出
    static void AskCount(int idx)
    {
        Dialog.InputName("输入复制数量（1~999）", "1", delegate (bool cancel, string text)
        {
            if (cancel)
            {
                return;
            }
            int n = 0;
            foreach (char ch in (text ?? ""))
            {
                if (!char.IsDigit(ch))
                {
                    continue;
                }
                n = n * 10 + (ch - '0');
                if (n > 999)
                {
                    break;
                }
            }
            if (n < 1)
            {
                SE.Beep();
                return;
            }
            Duplicate(idx, n);
        });
    }

    public static void Duplicate(int idx, int count)
    {
        Thing src = EClass.pc.things.Find(SrcIds[idx]);
        Thing essence = EClass.pc.things.Find("ut_universal_essence");
        if (src == null || src.Num < SrcNums[idx] * count || essence == null || essence.Num < EssenceCosts[idx] * count)
        {
            SE.Beep();
            return;
        }
        src.ModNum(-SrcNums[idx] * count);
        essence.ModNum(-EssenceCosts[idx] * count);
        Thing result = ThingGen.Create(SrcIds[idx]);
        result.SetNum(OutNums[idx] * count);
        EClass.pc.Pick(result);
        EClass.Sound.Play("craft");
        Refresh(); // 原地刷新持有数
    }
}
