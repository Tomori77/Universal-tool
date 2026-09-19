using System.Collections.Generic;
using UnityEngine;

// 自动农业机：机器自身储存种子，按半径矩形逐行种植。
public class TraitAutoFarming : TraitContainer
{
    const int RadiusKey = 240;
    static readonly int[] RadiusOptions = { 2, 4, 6, 8, 10 };
    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        owner.isOn = true;
        if (owner.GetInt(RadiusKey) == 0)
        {
            owner.SetInt(RadiusKey, 2);
        }
    }

    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

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

    int Radius => GetRadius();

    void OpenUI()
    {
        CurLayer = EClass.ui.AddLayer<LayerList>();
        CurLayer.SetSize(640f, 520f);
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
            "储物格：6×6（36格，点击打开）",
            "种植半径：" + Radius + "（范围为" + (Radius * 2 + 1) + "×" + (Radius * 2 + 1) + "，点击选择）",
            "每10分钟寻找第一个可种地块并横向种植一行；种子来自机器储物格",
            "每10分钟为范围内有种子的地块浇水，每格消耗1个工业用水",
            "机器耗电50，可跨过；不受输入器/输出器限制",
        };
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                Toggle(!owner.isOn);
                Refresh();
            }
            else if (i == 1)
            {
                LayerInventory.CreateContainer(owner.Thing);
            }
            else if (i == 2)
            {
                ShowRadiusSelect();
            }
        }, autoClose: false);
    }

    void ShowRadiusSelect()
    {
        List<string> lines = new List<string>();
        foreach (int radius in RadiusOptions)
        {
            lines.Add("半径 " + radius + "（" + (radius * 2 + 1) + "×" + (radius * 2 + 1) + "）");
        }
        lines.Add("返回");
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i >= 0 && i < RadiusOptions.Length)
            {
                owner.SetInt(RadiusKey, RadiusOptions[i]);
            }
            Refresh();
        }, autoClose: false);
    }

    int GetRadius()
    {
        int radius = owner.GetInt(RadiusKey, 2);
        foreach (int option in RadiusOptions)
        {
            if (option == radius)
            {
                return radius;
            }
        }
        owner.SetInt(RadiusKey, 2);
        return 2;
    }

    public static void ProcessTick(VirtualDate date)
    {
        if (!ModConfig.Instance.Enabled || EClass._map == null)
        {
            return;
        }
        foreach (Thing thing in EClass._map.things)
        {
            TraitAutoFarming machine = thing.trait as TraitAutoFarming;
            if (machine != null)
            {
                machine.ProcessOnce(date);
            }
        }
    }

    void ProcessOnce(VirtualDate date)
    {
        if (!owner.ExistsOnMap || owner.isBroken || !owner.isOn)
        {
            return;
        }
        if (EClass._map.isBreakerDown || EClass._zone.electricity < 0)
        {
            return;
        }
        PlantOneRow(date, Radius);
        WaterSeeds(Radius);
    }

    void PlantOneRow(VirtualDate date, int radius)
    {
        int firstRow = 0;
        int firstPlantable = -radius;
        for (int dz = -radius; dz <= radius; dz++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                Point target = new Point(owner.pos.x + dx, owner.pos.z + dz);
                if (target.IsValid && target.IsInBounds && CanPlant(target, date))
                {
                    firstRow = dz;
                    firstPlantable = dx;
                    break;
                }
            }
            if (firstPlantable >= -radius)
            {
                break;
            }
        }
        if (firstPlantable < -radius)
        {
            return;
        }
        int z = owner.pos.z + firstRow;
        for (int dx = firstPlantable; dx <= radius; dx++)
        {
            Point target = new Point(owner.pos.x + dx, z);
            if (target.IsValid && target.IsInBounds)
            {
                TryPlant(target, date);
            }
        }
    }

    bool CanPlant(Point target, VirtualDate date)
    {
        Thing seed = owner.things.Find((Thing t) => t.trait is TraitSeed && t.Num > 0);
        if (seed == null || target.Equals(owner.pos) || target.HasObj || target.Installed != null || !target.IsFarmField)
        {
            return false;
        }
        TraitSeed traitSeed = seed.trait as TraitSeed;
        return traitSeed != null && target.cell.CanGrow(traitSeed.row, date);
    }

    bool TryPlant(Point target, VirtualDate date)
    {
        if (!CanPlant(target, date))
        {
            return false;
        }
        Thing seed = owner.things.Find((Thing t) => t.trait is TraitSeed && t.Num > 0);
        Thing planted = seed.Split(1);
        EClass._zone.AddCard(planted, target).Install();
        TraitSeed plantedTrait = planted.trait as TraitSeed;
        if (plantedTrait == null)
        {
            owner.AddCard(planted);
            return false;
        }
        plantedTrait.TrySprout(force: true, sucker: false, date);
        return target.cell.HasObj;
    }

    void WaterSeeds(int radius)
    {
        Thing water = owner.things.Find((Thing t) => t.source.id == "ut_industrial_water" && t.Num > 0);
        if (water == null)
        {
            return;
        }
        for (int dz = -radius; dz <= radius && water.Num > 0; dz++)
        {
            for (int dx = -radius; dx <= radius && water.Num > 0; dx++)
            {
                Point target = new Point(owner.pos.x + dx, owner.pos.z + dz);
                if (target.IsValid && target.IsInBounds && HasSeed(target))
                {
                    water.ModNum(-1);
                    target.cell.isWatered = true;
                }
            }
        }
    }

    bool HasSeed(Point target)
    {
        if (target.Equals(owner.pos) || !target.IsFarmField || target.cell.isWatered
            || target.cell.IsTopWater || target.cell.IsSnowTile || target.cell.detail == null)
        {
            return false;
        }
        foreach (Thing thing in target.Things)
        {
            if (thing.trait is TraitSeed)
            {
                return true;
            }
        }
        return false;
    }
}
