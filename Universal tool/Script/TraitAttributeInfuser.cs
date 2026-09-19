// 属性灌注器：消耗万能精华永久提升属性（elements.ModBase，不衰减）
// 界面与增幅器一致：属性列表 → 任意数量输入框 → 按比例换算永久属性
// 比例读配置（默认每 5 精华 +1 点，向下取整）
using System.Collections.Generic;
using UnityEngine;

public class TraitAttributeInfuser : Trait
{
    public static readonly int[] EleIds = { 70, 71, 72, 73, 74, 75, 76, 77 };
    public static readonly string[] EleNames = { "力量", "体质", "灵巧", "感知", "学习", "意志", "魔力", "魅力" };

    public static LayerList CurLayer;

    // 背包右键"使用"出现的前提
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenInfuser();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("actUse", delegate
        {
            OpenInfuser();
            return false;
        });
    }

    // 比例文案（显示在输入框旁，随配置动态生成）
    public static string RatioText
    {
        get
        {
            return "比例：每 " + ModConfig.Instance.InfuseRatio.Value + " 个万能精华永久 +1 点属性（向下取整）";
        }
    }

    // 属性列表（不自动关闭，点选后弹数量输入框）
    public static void OpenInfuser()
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

    // 弹出任意数量输入框（含比例提示）
    public static void PromptAmount(int idx)
    {
        Dialog.InputName("万能精华数量 " + RatioText, "", delegate(bool cancel, string text)
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

    public static void Apply(int idx, int n)
    {
        Thing t = EClass.pc.things.Find("ut_universal_essence");
        if (t == null || t.Num < n)
        {
            SE.Beep();
            return;
        }
        int ratio = ModConfig.Instance.InfuseRatio.Value;
        if (ratio <= 0)
        {
            ratio = 1;
        }
        int points = n / ratio; // 向下取整
        if (points <= 0)
        {
            SE.Beep();
            return;
        }
        t.ModNum(-n);
        EClass.pc.elements.ModBase(EleIds[idx], points); // 永久提升基础属性
        Msg.Say("trainSkill", EClass.pc, EleNames[idx] + " +" + points);
        Refresh(); // 原地刷新持有数
    }
}
