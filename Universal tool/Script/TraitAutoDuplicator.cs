// 自动复制机 / 自动生成器：与自动凝华器同款工作方式（用户定稿）——
// 催化剂放机器自身储料格（3×1，每小时耗 1 个），材料放相邻输入器（每次 5 个），
// 每10分钟结算一次：材料×5 → 材料×10，产物送相邻输出器。
// 耗电 20；按输入器顺序取第一个符合配方的材料。
using System.Collections.Generic;
using UnityEngine;

public class TraitAutoDuplicator : TraitContainer
{
    // 自动配方：材料 id
    public virtual string[] RecipeIds => new string[] { "crystal_earth", "crystal_sun", "crystal_mana", "ingot" };

    const int FuelHourKey = 876544;
    const int NeedNum = 5;
    const int OutNum = 10;

    protected virtual int GetOutputNum(string id)
    {
        return OutNum;
    }

    public override void OnCreate(int lv)
    {
        base.OnCreate(lv); // 基类设容器格子 + Prespawn
        owner.isOn = true; // 放置即开始工作
    }

    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    // 新造出的机器不预生成内容
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
        base.TrySetAct(p); // 注册「打开容器」（放催化剂）
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
        Thing catalyst = owner.things.Find((Thing t) => t.source.id == "ut_catalyst");
        int num = catalyst != null ? catalyst.Num : 0;
        List<string> lines = new List<string>
        {
            owner.isOn ? "机器：开启（点击关闭）" : "机器：关闭（点击开启）",
            "催化剂储料格：" + num + " 个（点击打开，每小时开机消耗1个）",
        };
        foreach (string id in RecipeIds)
        {
            string name = EClass.sources.things.map[id].GetName();
            lines.Add("配方：" + name + "×" + NeedNum + "（每次）→ " + name + "×" + GetOutputNum(id));
        }
        lines.Add("材料放相邻输入器；每10分钟自动工作一次");
        lines.Add("产物送相邻输出器；按输入器格子顺序处理");
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                Toggle(!owner.isOn);
                Refresh();
            }
            else if (i == 1)
            {
                LayerInventory.CreateContainer(owner.Thing); // 打开催化剂储料格
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
            TraitAutoDuplicator machine = thing.trait as TraitAutoDuplicator;
            if (machine != null)
            {
                machine.ProcessOnce();
            }
        }
    }

    void ProcessOnce()
    {
        if (!ModConfig.Instance.Enabled || !owner.ExistsOnMap || owner.isBroken || !owner.isOn)
        {
            return;
        }
        if (!ConsumeHourlyCatalyst())
        {
            return;
        }
        if (EClass._map.isBreakerDown || EClass._zone.electricity < 0)
        {
            return;
        }
        TraitInputFeeder feeder = FindNeighbor<TraitInputFeeder>();
        if (feeder == null || !FeederUsable(feeder))
        {
            return;
        }
        TraitOutputFeeder output = FindNeighbor<TraitOutputFeeder>();
        if (output == null || output.owner.things.IsFull())
        {
            return;
        }
        foreach (Thing material in feeder.owner.things)
        {
            string id = material.source.id;
            if (!ContainsRecipe(id) || material.Num < NeedNum)
            {
                continue;
            }
            material.ModNum(-NeedNum);
            Deliver(output, id, GetOutputNum(id));
            break;
        }
    }

    bool ContainsRecipe(string id)
    {
        foreach (string recipeId in RecipeIds)
        {
            if (recipeId == id)
            {
                return true;
            }
        }
        return false;
    }

    bool ConsumeHourlyCatalyst()
    {
        int hourToken = TraitAutomationScheduler.CurrentHourToken;
        if (owner.GetInt(FuelHourKey) == hourToken)
        {
            return true;
        }
        Thing catalyst = owner.things.Find((Thing t) => t.source.id == "ut_catalyst" && t.Num > 0);
        if (catalyst != null)
        {
            catalyst.ModNum(-1);
            owner.SetInt(FuelHourKey, hourToken);
            return true;
        }
        return false;
    }

    public override void TryToggle()
    {
    }

    // 输入器相邻 ≥2 台自动机器时不供料（与自动凝华器同规则）
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

    // 相邻四格（上下左右）找指定 trait 的设备
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

    void Deliver(TraitOutputFeeder output, string id, int outputNum)
    {
        Thing product = ThingGen.Create(id);
        product.SetNum(outputNum);
        ModConfig.NormalizeUniversalMaterial(product);
        output.owner.AddCard(product);
        Debug.Log("[UniversalTool] auto duplicator: +" + outputNum + " " + id);
    }
}

// 自动生成器：无内部存储，直接处理相邻输入器中的材料。
public class TraitAutoGenerator : Trait
{
    static readonly string[] RecipeIds = { "ut_catalyst", "fertilizer" };

    public override void OnCreate(int lv)
    {
        owner.isOn = true;
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
            "配方：万能催化剂×5（每次）→ 万能催化剂×10",
            "配方：肥料×5（每次）→ 万能肥料×30",
            "材料放相邻输入器，产物送相邻输出器；每10分钟运行一次",
            "无内部存储；持续占用1000电力；机器只由玩家关闭",
        };
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                Toggle(!owner.isOn);
                Refresh();
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
            TraitAutoGenerator generator = thing.trait as TraitAutoGenerator;
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
        TraitInputFeeder feeder = FindNeighbor<TraitInputFeeder>();
        TraitOutputFeeder output = FindNeighbor<TraitOutputFeeder>();
        if (feeder == null || output == null || !FeederUsable(feeder) || output.owner.things.IsFull())
        {
            return;
        }
        foreach (Thing material in feeder.owner.things)
        {
            string id = material.source.id;
            if (!ContainsRecipe(id) || material.Num < 5)
            {
                continue;
            }
            material.ModNum(-5);
            string outputId = id == "fertilizer" ? "ut_fertilizer" : id;
            int outputNum = id == "fertilizer" ? 30 : 10;
            Thing product = ThingGen.Create(outputId).SetNum(outputNum);
            ModConfig.NormalizeUniversalMaterial(product);
            output.owner.AddCard(product);
            Debug.Log("[UniversalTool] auto generator: +" + outputNum + " " + outputId);
            return;
        }
    }

    static bool ContainsRecipe(string id)
    {
        foreach (string recipeId in RecipeIds)
        {
            if (recipeId == id)
            {
                return true;
            }
        }
        return false;
    }

    static bool FeederUsable(TraitInputFeeder feeder)
    {
        int machines = 0;
        feeder.owner.pos.ForeachNeighbor(delegate (Point p)
        {
            if (p.FindThing<TraitAutoCondenser>() != null || p.FindThing<TraitAutoDuplicator>() != null
                || p.FindThing<TraitAutoGenerator>() != null || p.FindThing<TraitMineralGenerator>() != null
                || p.FindThing<TraitAutoSmelter>() != null)
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

    public override void TryToggle()
    {
    }
}
