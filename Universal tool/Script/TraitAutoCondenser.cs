// 自动凝华器（内置催化剂槽版，用户改定）：
// - 催化剂放进机器自身的储料格（3×1，打开容器），结晶放相邻输入器
// - 「使用」打开配方页（配方 + 开关机），地图右键另有「打开容器」放催化剂；不允许手动制作
// - 每小时结算 6 次（=10分钟一次）：开机每小时耗 1 催化剂，配方每次耗 1 结晶
// - 配方优先级 魔力(1000) > 太阳(300) > 大地(100)，结晶不足 6 个按实际数量；产物送相邻输出器
// 耗电 20（Thing 表 electricity=-20）；关机（isOn，持久化）不加工不耗电
using System.Collections.Generic;
using UnityEngine;

public class TraitAutoCondenser : TraitContainer
{
    // [0] 结晶 id、[1] 每次产出精华数（数组顺序即优先级）
    static readonly string[][] Recipes = new string[][]
    {
        new string[] { "crystal_mana", "1000" },
        new string[] { "crystal_sun", "300" },
        new string[] { "crystal_earth", "100" },
    };

    const int FuelHourKey = 876543;

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
            "配方：魔力结晶×1（每次）→ 精华×1000",
            "配方：太阳结晶×1（每次）→ 精华×300",
            "配方：大地结晶×1（每次）→ 精华×100",
            "结晶放相邻输入器；每10分钟加工一次（每小时最多6次）",
            "产物送相邻输出器；输入器相邻两台自动机器时不供料",
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
                LayerInventory.CreateContainer(owner.Thing); // 打开催化剂储料格
            }
        }, autoClose: false);
    }

    public static void ProcessTick(int hourToken)
    {
        if (EClass._map == null)
        {
            return;
        }
        List<TraitAutoCondenser> machines = new List<TraitAutoCondenser>();
        foreach (Thing thing in EClass._map.things)
        {
            TraitAutoCondenser condenser = thing.trait as TraitAutoCondenser;
            if (condenser != null)
            {
                machines.Add(condenser);
            }
        }
        foreach (TraitAutoCondenser condenser in machines)
        {
            condenser.ProcessOnce(hourToken);
        }
    }

    void ProcessOnce(int hourToken)
    {
        if (!ModConfig.Instance.Enabled || !owner.ExistsOnMap || owner.isBroken || !owner.isOn)
        {
            return;
        }
        if (!ConsumeHourlyCatalyst(hourToken))
        {
            return;
        }
        if (EClass._map.isBreakerDown || EClass._zone.electricity < 0)
        {
            return;
        }
        TraitInputFeeder feeder = FindNeighbor<TraitInputFeeder>();
        TraitOutputFeeder output = FindNeighbor<TraitOutputFeeder>();
        if (feeder == null || output == null || !FeederUsable(feeder) || output.owner.things.IsFull())
        {
            return;
        }
        foreach (string[] recipe in Recipes)
        {
            Thing crystal = feeder.owner.things.Find((Thing t) => t.source.id == recipe[0]);
            if (crystal == null || crystal.Num < 1)
            {
                continue;
            }
            crystal.ModNum(-1);
            Deliver(output, int.Parse(recipe[1]) * ModConfig.Instance.CondenserMult.Value);
            return;
        }
    }

    bool ConsumeHourlyCatalyst(int hourToken)
    {
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

    // 输入器相邻 ≥2 台自动机器时不供料（用户定稿；自动机器=凝华器/复制机/生成器）
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

    void Deliver(TraitOutputFeeder output, int num)
    {
        Thing product = ThingGen.Create("ut_universal_essence");
        product.SetNum(num);
        ModConfig.NormalizeUniversalMaterial(product);
        output.owner.AddCard(product);
        Debug.Log("[UniversalTool] auto condenser: +" + num + " essence");
    }
}
