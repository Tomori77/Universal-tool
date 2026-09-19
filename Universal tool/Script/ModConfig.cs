// 万能工具配置：26 个配置项。UI 由 ModSettingsUI 手动 xml 布局生成（阶段 C 起改输入框+分块），
// 字段上的 [ModCfg*] 特性仅作文档标注（不再用于自动绑定，Register 不传 configs）。
// ConfigEntry<T> 来自 BepInEx；Mod Options 缺失时字段仍作为 cfg 文件配置存在，惰性解析不报错。
using BepInEx.Configuration;

public class ModConfig
{
    public static ModConfig Instance = new ModConfig();

    // ===== 通用 =====
    [ModCfgToggle("Universal tool：启用", "总开关。关闭后本 mod 的交互功能与运行时补丁均不生效")]
    public ConfigEntry<bool> Enable;

    // ===== 凝华器 =====
    [ModCfgSlider("凝华器：产出倍率", 1f, 10f, 1f, true)]
    public ConfigEntry<int> CondenserMult;

    // ===== 技能训练器 =====
    [ModCfgSlider("训练器：大地结晶提升等级", 1f, 999f, 1f, true)]
    public ConfigEntry<int> TrainEarth;
    [ModCfgSlider("训练器：太阳结晶提升等级", 1f, 999f, 1f, true)]
    public ConfigEntry<int> TrainSun;
    [ModCfgSlider("训练器：魔力结晶提升等级", 1f, 999f, 1f, true)]
    public ConfigEntry<int> TrainMana;
    [ModCfgToggle("训练器：允许对同伴使用", "关闭后只能训练主角自己")]
    public ConfigEntry<bool> TrainAlly;

    // ===== 增幅器 =====
    [ModCfgSlider("增幅器：阶梯1单价（1-20个）", 1f, 100f, 1f, true)]
    public ConfigEntry<int> AmpTier1;
    [ModCfgSlider("增幅器：阶梯2单价（21-50个）", 1f, 100f, 1f, true)]
    public ConfigEntry<int> AmpTier2;
    [ModCfgSlider("增幅器：阶梯3单价（51-199个）", 1f, 100f, 1f, true)]
    public ConfigEntry<int> AmpTier3;
    [ModCfgSlider("增幅器：阶梯4单价（200个以上）", 1f, 100f, 1f, true)]
    public ConfigEntry<int> AmpTier4;
    [ModCfgToggle("增幅器：突破临时属性上限", "关闭则使用游戏默认 ModTempElement（受 (|基础值|+100)/4 上限截断）")]
    public ConfigEntry<bool> AmpBreakCap;

    // ===== 镐尖斧 =====
    [ModCfgSlider("镐尖斧：采集效率倍率", 1f, 10f, 1f, true)]
    public ConfigEntry<int> AxeEfficiency;
    [ModCfgSlider("镐尖斧：硬度倍率", 1f, 10f, 1f, true)]
    public ConfigEntry<int> AxeHardness;

    // ===== 便携工作台 =====
    [ModCfgToggle("便携工作台：制作台")]
    public ConfigEntry<bool> WbWorkbench;
    [ModCfgToggle("便携工作台：木工的桌子")]
    public ConfigEntry<bool> WbWood;
    [ModCfgToggle("便携工作台：杂工的桌子")]
    public ConfigEntry<bool> WbTinker;
    [ModCfgToggle("便携工作台：石工的桌子")]
    public ConfigEntry<bool> WbStone;
    [ModCfgToggle("便携工作台：设计台")]
    public ConfigEntry<bool> WbDrafting;

    // ===== 便携出货箱 =====
    [ModCfgToggle("便携出货箱：联通大出货箱", "关闭则作为独立容器使用")]
    public ConfigEntry<bool> ShippingLink;

    // ===== 分解机 =====
    [ModCfgSlider("分解机：产出倍率", 1f, 10f, 1f, true)]
    public ConfigEntry<int> DecomposerMult;

    // ===== 精华电池 =====
    [ModCfgSlider("精华电池：发电量", 1f, 100000f, 100f, true)]
    public ConfigEntry<int> BatteryPower;

    // ===== 物品收集器 =====
    [ModCfgSlider("收集器：收集半径", 1f, 5f, 1f, true)]
    public ConfigEntry<int> CollectorRadius;

    // ===== 属性灌注器 =====
    [ModCfgSlider("属性灌注器：比例（每 N 精华 +1 点）", 1f, 10000f, 1f, true)]
    public ConfigEntry<int> InfuseRatio;

    // ===== 万能修复器 =====
    [ModCfgSlider("修复器：鉴定消耗精华", 1f, 100000f, 10f, true)]
    public ConfigEntry<int> RepairIdentifyCost;
    [ModCfgSlider("修复器：高级鉴定消耗精华", 1f, 100000f, 10f, true)]
    public ConfigEntry<int> RepairIdentifySPCost;
    [ModCfgSlider("修复器：去诅咒消耗精华", 1f, 100000f, 10f, true)]
    public ConfigEntry<int> RepairUncurseCost;

    // ===== 调试 =====
    [ModCfgToggle("调试：显示物品ID", "在本 mod 的列表中追加物品内部 id")]
    public ConfigEntry<bool> DebugId;

    // 总开关快捷属性（未绑定前视为启用，避免启动早期误判）
    public bool Enabled => Enable == null || Enable.Value;

    // 万能材料的标准材质（defMat）：所有产出统一为对应物品的基准材质，保证可堆叠。
    public static string EssenceMat => GetUniversalMaterial("ut_universal_essence");
    public static string CatalystMat => GetUniversalMaterial("ut_catalyst");

    public static void NormalizeUniversalMaterial(Thing thing)
    {
        if (thing == null)
        {
            return;
        }
        string mat = thing.source.id == "ut_catalyst" ? CatalystMat
            : (thing.source.id == "ut_universal_essence" ? EssenceMat : null);
        if (!string.IsNullOrEmpty(mat))
        {
            thing.ChangeMaterial(mat);
        }
    }

    static string GetUniversalMaterial(string id)
    {
        SourceThing.Row row;
        if (EClass.sources.things.map.TryGetValue(id, out row) && !string.IsNullOrEmpty(row.defMat))
        {
            return row.defMat;
        }
        return "oak";
    }

    // 绑定全部配置项（在 Awake 中调用一次）
    public void Init(ConfigFile config)
    {
        Enable = config.Bind("General", "Enable", true, "Master switch for Universal tool.");

        CondenserMult = config.Bind("Condenser", "OutputMultiplier", 1, "Condenser essence output multiplier.");

        TrainEarth = config.Bind("Trainer", "EarthLevel", 10, "Skill levels gained per earth crystal.");
        TrainSun = config.Bind("Trainer", "SunLevel", 20, "Skill levels gained per sun crystal.");
        TrainMana = config.Bind("Trainer", "ManaLevel", 50, "Skill levels gained per mana crystal.");
        TrainAlly = config.Bind("Trainer", "AllowCompanion", true, "Allow training companions/pets.");

        AmpTier1 = config.Bind("Amplifier", "Tier1Price", 5, "Essence price per point (1-20 essences).");
        AmpTier2 = config.Bind("Amplifier", "Tier2Price", 6, "Essence price per point (21-50 essences).");
        AmpTier3 = config.Bind("Amplifier", "Tier3Price", 8, "Essence price per point (51-199 essences).");
        AmpTier4 = config.Bind("Amplifier", "Tier4Price", 10, "Essence price per point (200+ essences).");
        AmpBreakCap = config.Bind("Amplifier", "BreakCap", true, "Bypass the temporary-element cap.");

        AxeEfficiency = config.Bind("AxePickaxe", "EfficiencyMultiplier", 1, "Harvest efficiency multiplier.");
        AxeHardness = config.Bind("AxePickaxe", "HardnessMultiplier", 1, "Tool material hardness multiplier.");

        WbWorkbench = config.Bind("PortableWorkbench", "Workbench", true, "Show 制作台 tab.");
        WbWood = config.Bind("PortableWorkbench", "Wood", true, "Show 木工的桌子 tab.");
        WbTinker = config.Bind("PortableWorkbench", "Tinker", true, "Show 杂工的桌子 tab.");
        WbStone = config.Bind("PortableWorkbench", "Stone", true, "Show 石工的桌子 tab.");
        WbDrafting = config.Bind("PortableWorkbench", "Drafting", true, "Show 设计台 tab.");

        ShippingLink = config.Bind("PortableShipping", "LinkBig", true, "Link to the big shipping chest.");

        DecomposerMult = config.Bind("Decomposer", "OutputMultiplier", 1, "Decomposer essence output multiplier.");

        BatteryPower = config.Bind("Battery", "Power", 200, "Electricity supplied by the essence battery.");

        CollectorRadius = config.Bind("Collector", "Radius", 3, "Item collector pickup radius (1-5).");

        InfuseRatio = config.Bind("Infuser", "Ratio", 5, "Essence per permanent attribute point.");

        RepairIdentifyCost = config.Bind("Repairer", "IdentifyCost", 50, "Essence cost to identify an item.");
        RepairIdentifySPCost = config.Bind("Repairer", "IdentifySPCost", 100, "Essence cost for superior identify (mythical artifact).");
        RepairUncurseCost = config.Bind("Repairer", "UncurseCost", 100, "Essence cost to remove a curse.");

        DebugId = config.Bind("Debug", "ShowId", false, "Append internal item id in mod lists.");
    }
}
