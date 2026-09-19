using System.Collections.Generic;
using UnityEngine;

public class TraitMineralGenerator : TraitContainer
{
    const int EssenceCost = 100;

    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        owner.isOn = true;
    }

    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    public override bool HasCharges => true;

    public override bool ShowCharges => true;

    public override void Prespawn(int lv)
    {
    }

    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        OpenUI();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return;
        }
        p.TrySetAct("查看机器", delegate
        {
            OpenUI();
            return false;
        });
        base.TrySetAct(p);
    }

    LayerList CurLayer;

    void OpenUI()
    {
        CurLayer = EClass.ui.AddLayer<LayerList>();
        CurLayer.SetSize(640f, 480f);
        Refresh();
    }

    void Refresh()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<string> lines = new List<string>
        {
            owner.isOn ? "机器：开启（点击关闭）" : "机器：关闭（点击开启）",
            "配方：万能精华×100 → 目标矿石×1",
            "目标矿石样品不会消耗；仅支持原版矿石",
            "万能精华放相邻输入器，产物送相邻输出器",
            "每10分钟工作一次（每小时最多6次），耗电100",
        };
        int targetIndex = 0;
        foreach (Thing thing in owner.things)
        {
            if (IsOriginalOre(thing))
            {
                targetIndex++;
                lines.Insert(targetIndex, "目标矿石" + targetIndex + "：" + thing.source.GetName());
            }
        }
        if (targetIndex == 0)
        {
            lines.Insert(1, "目标矿石：未设置（点击打开容器）");
        }
        else
        {
            lines.Insert(targetIndex + 1, "点击打开容器可设置最多3种目标矿石");
        }
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                Toggle(!owner.isOn);
                Refresh();
            }
            else if (i == 1 || (targetIndex > 0 && i <= targetIndex + 1))
            {
                LayerInventory.CreateContainer(owner.Thing);
            }
        }, autoClose: false);
    }

    public static void ProcessTick()
    {
        if (!ModConfig.Instance.Enabled || EClass._map == null)
        {
            return;
        }
        foreach (Thing thing in EClass._map.things)
        {
            TraitMineralGenerator generator = thing.trait as TraitMineralGenerator;
            if (generator != null)
            {
                generator.ProcessOnce();
            }
        }
    }

    void ProcessOnce()
    {
        if (!owner.ExistsOnMap || owner.isBroken || !owner.isOn
            || EClass._map.isBreakerDown || EClass._zone.electricity < 0)
        {
            return;
        }
        TraitInputFeeder input = FindNeighbor<TraitInputFeeder>();
        TraitOutputFeeder output = FindNeighbor<TraitOutputFeeder>();
        if (input == null || output == null || !FeederUsable(input))
        {
            return;
        }
        foreach (Thing target in owner.things)
        {
            if (IsOriginalOre(target))
            {
                ProcessOnce(target, input, output);
            }
        }
    }

    bool ProcessOnce(Thing target, TraitInputFeeder input, TraitOutputFeeder output)
    {
        Thing essence = input.owner.things.Find((Thing t) => t.source.id == "ut_universal_essence" && t.Num >= EssenceCost);
        if (target == null || essence == null || output.owner.things.IsFull())
        {
            return false;
        }
        Thing product = ThingGen.Create(target.id, target.material.id).SetNum(1);
        output.owner.AddCard(product);
        essence.ModNum(-EssenceCost);
        Debug.Log("[UniversalTool] mineral generator: +1 " + target.id);
        return true;
    }

    static bool IsOriginalOre(Thing thing)
    {
        return thing != null && (thing.id == "ore" || thing.id == "ore_gem"
            || thing.source._origin == "ore" || thing.source._origin == "ore_gem");
    }

    static bool FeederUsable(TraitInputFeeder feeder)
    {
        int machines = 0;
        feeder.owner.pos.ForeachNeighbor(delegate (Point p)
        {
            if (p.FindThing<TraitAutoCondenser>() != null || p.FindThing<TraitAutoDuplicator>() != null
                || p.FindThing<TraitAutoGenerator>() != null
                || p.FindThing<TraitMineralGenerator>() != null || p.FindThing<TraitAutoSmelter>() != null)
            {
                machines++;
            }
        }, diagonal: false);
        return machines <= 1;
    }

    T FindNeighbor<T>() where T : Trait
    {
        T found = null;
        owner.pos.ForeachNeighbor(delegate (Point p)
        {
            if (found == null)
            {
                found = p.FindThing<T>();
            }
        }, diagonal: false);
        return found;
    }
}
