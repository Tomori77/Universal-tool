using System.Collections.Generic;
using UnityEngine;

public class TraitAutoSmelter : TraitContainer
{
    const int FuelHourKey = 876545;
    const int MaxProcessNum = 10;
    static readonly TraitSmelter Smelter = new TraitSmelter();

    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        owner.isOn = true;
        SyncFuelCharges();
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
        SyncFuelCharges();
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
        Thing fuel = owner.things.Find((Thing t) => t.source.id == "ut_catalyst");
        List<string> lines = new List<string>
        {
            owner.isOn ? "机器：开启（点击关闭）" : "机器：关闭（点击开启）",
            "万能催化剂：" + (fuel == null ? 0 : fuel.Num) + " 个（点击打开，每小时开机消耗1个）",
            "配方：与原版熔炉一致，每10分钟最多处理10个物品",
            "矿物放相邻输入器，产物送相邻输出器",
            "每10分钟工作一次（每小时最多6次），耗电20；按输入器顺序处理",
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
        SyncFuelCharges();
    }

    public override void SetMainText(UIText t, bool hotitem)
    {
        SyncFuelCharges();
        base.SetMainText(t, hotitem);
    }

    public static void ProcessTick()
    {
        if (!ModConfig.Instance.Enabled || EClass._map == null)
        {
            return;
        }
        foreach (Thing thing in EClass._map.things)
        {
            TraitAutoSmelter smelter = thing.trait as TraitAutoSmelter;
            if (smelter != null)
            {
                smelter.ProcessOnce();
            }
        }
    }

    bool ProcessOnce()
    {
        if (!owner.ExistsOnMap || owner.isBroken || !owner.isOn)
        {
            return false;
        }
        if (!ConsumeHourlyCatalyst())
        {
            return false;
        }
        if (EClass._map.isBreakerDown || EClass._zone.electricity < 0)
        {
            return false;
        }
        TraitInputFeeder input = FindNeighbor<TraitInputFeeder>();
        TraitOutputFeeder output = FindNeighbor<TraitOutputFeeder>();
        if (input == null || output == null || !FeederUsable(input))
        {
            return false;
        }
        int processed = 0;
        List<Thing> materials = new List<Thing>();
        foreach (Thing material in input.owner.things)
        {
            materials.Add(material);
        }
        foreach (Thing material in materials)
        {
            SourceRecipe.Row recipe = FindRecipe(material);
            if (recipe == null)
            {
                continue;
            }
            int amount = System.Math.Min(material.Num, MaxProcessNum - processed);
            for (int i = 0; i < amount; i++)
            {
                if (output.owner.things.IsFull())
                {
                    return processed > 0;
                }
                int outputNum = recipe.num.Calc();
                if (outputNum <= 0)
                {
                    continue;
                }
                string[] productData = recipe.thing.Split('%');
                int materialId = productData.Length > 1
                    ? EClass.sources.materials.alias[productData[1]].id
                    : material.material.id;
                Thing product = ThingGen.Create(productData[0], materialId).SetNum(outputNum);
                output.owner.AddCard(product);
                material.ModNum(-1);
                processed++;
                Debug.Log("[UniversalTool] auto smelter: " + material.id + " -> " + productData[0]);
                if (processed >= MaxProcessNum)
                {
                    return true;
                }
            }
        }
        return processed > 0;
    }

    bool ConsumeHourlyCatalyst()
    {
        int hourToken = TraitAutomationScheduler.CurrentHourToken;
        if (owner.GetInt(FuelHourKey) == hourToken)
        {
            return true;
        }
        Thing catalyst = owner.things.Find((Thing t) => t.source.id == "ut_catalyst" && t.Num > 0);
        if (catalyst == null)
        {
            return false;
        }
        catalyst.ModNum(-1);
        owner.SetInt(FuelHourKey, hourToken);
        SyncFuelCharges();
        return true;
    }

    void SyncFuelCharges()
    {
        Thing fuel = owner.things.Find((Thing t) => t.source.id == "ut_catalyst");
        owner.c_charges = fuel == null ? 0 : fuel.Num;
    }

    static SourceRecipe.Row FindRecipe(Thing material)
    {
        if (material == null)
        {
            return null;
        }
        foreach (SourceRecipe.Row row in EClass.sources.recipes.rows)
        {
            if (row.factory == "Smelter" && Smelter.IsIngredient(0, row, material))
            {
                return row;
            }
        }
        return null;
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

    public override void TryToggle()
    {
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
