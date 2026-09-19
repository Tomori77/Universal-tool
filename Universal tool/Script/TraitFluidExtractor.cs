using System.Collections.Generic;

public class TraitFluidExtractor : TraitContainer
{
    const int BaseOutput = 24;

    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        owner.isOn = true;
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
            "储物格：6×6（36格，点击打开）",
            "配方：每10分钟产出工业用水×24",
            "使用条件：任意方向紧贴水方格",
            "3×3范围内有另一台流体提取器时，产出提升至×1.5",
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
            TraitFluidExtractor extractor = thing.trait as TraitFluidExtractor;
            if (extractor != null)
            {
                extractor.ProcessOnce();
            }
        }
    }

    void ProcessOnce()
    {
        if (!ModConfig.Instance.Enabled || !owner.ExistsOnMap || owner.isBroken || !owner.isOn)
        {
            return;
        }
        if (EClass._map.isBreakerDown || EClass._zone.electricity < 0
            || owner.things.IsFull() || !HasAdjacentWater())
        {
            return;
        }
        int output = HasNearbyExtractor() ? BaseOutput * 3 / 2 : BaseOutput;
        Thing product = ThingGen.Create("ut_industrial_water");
        product.SetNum(output);
        owner.AddCard(product);
    }

    bool HasAdjacentWater()
    {
        bool found = false;
        owner.pos.ForeachNeighbor(delegate (Point p)
        {
            if (!found && p.IsValid && p.cell.IsTopWater)
            {
                found = true;
            }
        }, diagonal: false);
        return found;
    }

    bool HasNearbyExtractor()
    {
        Point center = owner.pos;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0)
                {
                    continue;
                }
                Point p = new Point(center.x + dx, center.z + dz);
                if (p.IsValid && p.FindThing<TraitFluidExtractor>() != null)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
