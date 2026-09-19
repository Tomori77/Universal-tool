// 技能训练器：装置，可重复使用；消耗结晶按等级提升已有技能（自己/队友/宠物）
// 界面：目标列表（LayerList）→ 技能列表（LayerList 滚动，不自动关闭）→ 结晶浮层（UIContextMenu）
using System.Collections.Generic;
using UnityEngine;

public class TraitSkillTrainer : Trait
{
    // 结晶需求（读配置）：大地/太阳/魔力结晶各自提升的等级
    public static readonly string[] CrystalIds = { "crystal_earth", "crystal_sun", "crystal_mana" };
    public static readonly string[] CrystalNames = { "大地结晶", "太阳结晶", "魔力结晶" };

    public static int[] GetCrystalLv()
    {
        ModConfig cfg = ModConfig.Instance;
        return new int[] { cfg.TrainEarth.Value, cfg.TrainSun.Value, cfg.TrainMana.Value };
    }

    public static Chara CurTarget;
    public static LayerList CurLayer;

    // 背包右键"使用"出现的前提（基类 CanUse 默认 false → 无使用项）
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenTrainer();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("actUse", delegate
        {
            OpenTrainer();
            return false;
        });
    }

    // 第一层：选目标（自己 + 队伍成员；配置关闭同伴时只显示自己）
    public static void OpenTrainer()
    {
        List<Chara> targets = new List<Chara> { EClass.pc };
        if (ModConfig.Instance.TrainAlly.Value)
        {
            foreach (Chara m in EClass.pc.party.members)
            {
                if (m != null && !m.isDestroyed && m.uid != EClass.pc.uid)
                {
                    targets.Add(m);
                }
            }
        }
        if (targets.Count == 1)
        {
            SelectSkill(targets[0]);
            return;
        }
        EClass.ui.AddLayer<LayerList>().SetList(targets, (Chara c) => c.Name, delegate(int i, string s)
        {
            SelectSkill(targets[i]);
        });
    }

    // 第二层：选已有技能（滚动列表，不自动关闭，可连续训练）
    public static void SelectSkill(Chara target)
    {
        CurTarget = target;
        List<Element> skills = GetSkills(target);
        CurLayer = EClass.ui.AddLayer<LayerList>();
        CurLayer.SetSize(640f, 480f); // 调宽窗口，完整显示技能名+等级
        CurLayer.SetList(skills, (Element e) => e.source.GetName() + " Lv" + target.elements.Base(e.id), delegate(int i, string s)
        {
            ShowCrystalMenu(skills[i]);
        }, autoClose: false);
    }

    public static List<Element> GetSkills(Chara target)
    {
        List<Element> skills = new List<Element>();
        foreach (Element e in target.elements.dict.Values)
        {
            if (e.source.category == "skill" && target.elements.HasBase(e.id))
            {
                skills.Add(e);
            }
        }
        return skills;
    }

    // 结晶浮层（悬停在技能列表上方，不打断页面）
    public static void ShowCrystalMenu(Element skill)
    {
        UIContextMenu menu = EClass.ui.CreateContextMenuInteraction();
        int[] lv = GetCrystalLv();
        bool debug = ModConfig.Instance.DebugId.Value;
        for (int i = 0; i < CrystalIds.Length; i++)
        {
            int idx = i;
            Thing t = EClass.pc.things.Find(CrystalIds[i]);
            int num = (t != null) ? t.Num : 0;
            string label = CrystalNames[i] + " ×" + num + " → +" + lv[i] + "级" + (debug ? " <" + CrystalIds[i] + ">" : "");
            menu.AddButton(() => label, () => Train(skill, idx));
        }
        menu.Show();
    }

    public static void Train(Element skill, int idx)
    {
        Thing t = EClass.pc.things.Find(CrystalIds[idx]);
        if (t == null || t.Num < 1)
        {
            SE.Beep();
            return;
        }
        t.ModNum(-1);
        CurTarget.elements.ModBase(skill.id, GetCrystalLv()[idx]);
        Msg.Say("trainSkill", CurTarget, skill.source.GetName());
        // 原地刷新技能列表（等级已更新，可继续训练其它技能）
        if (CurLayer != null && CurLayer.gameObject != null)
        {
            List<Element> skills = GetSkills(CurTarget);
            CurLayer.SetList(skills, (Element e) => e.source.GetName() + " Lv" + CurTarget.elements.Base(e.id), delegate(int i, string s)
            {
                ShowCrystalMenu(skills[i]);
            }, autoClose: false);
        }
    }
}
