using System.Reflection;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Zone), nameof(Zone.OnSimulateHour))]
public static class Patch_Zone_OnSimulateHour_Collector
{
    public static void Postfix(VirtualDate date)
    {
        if (!date.IsRealTime)
        {
            TraitAutomationScheduler.ProcessOfflineHour(GetHourToken(date), date);
        }
    }

    static int GetHourToken(Date date)
    {
        return (((date.year * 12 + date.month) * 30 + date.day) * 24) + date.hour;
    }
}

[HarmonyPatch(typeof(GameDate), nameof(GameDate.AdvanceMin))]
public static class Patch_GameDate_AdvanceMin_Automation
{
    public static void Prefix(ref long __state)
    {
        __state = GetAbsoluteMinute(EClass.world.date);
    }

    public static void Postfix(GameDate __instance, int a, long __state)
    {
        if (!ModConfig.Instance.Enabled || a <= 0)
        {
            return;
        }

        long endMinute = GetAbsoluteMinute(__instance);
        long nextTick = ((__state / 10) + 1) * 10;
        while (nextTick <= endMinute)
        {
            TraitAutomationScheduler.ProcessRealtimeTick((int)(nextTick / 60));
            nextTick += 10;
        }
    }

    static long GetAbsoluteMinute(Date date)
    {
        return (((long)date.year * 12 + date.month) * 30 + date.day) * 24 * 60
            + date.hour * 60 + date.min;
    }
}

// 便携工作台：LayerCraft 打开时注入工作台切换 tab（中文，且只显示工作台标签、隐藏配方分类标签）
[HarmonyPatch(typeof(LayerCraft), nameof(LayerCraft.OnAfterInit))]
public static class Patch_LayerCraft_OnAfterInit
{
    public static void Postfix(LayerCraft __instance)
    {
        if (!ModConfig.Instance.Enabled || !TraitPortableWorkbench.IsOpen)
        {
            return;
        }
        // 阻止配方分类 tab（tabBuilt=true → RefreshCategory 不再添加"武器/防具"等分类标签）
        FieldInfo tabBuilt = typeof(LayerCraft).GetField("tabBuilt", BindingFlags.Instance | BindingFlags.NonPublic);
        if (tabBuilt != null)
        {
            tabBuilt.SetValue(__instance, true);
        }
        string[] ids = TraitPortableWorkbench.GetEnabledIds();
        string[] names = TraitPortableWorkbench.GetEnabledNames();
        for (int i = 0; i < ids.Length; i++)
        {
            string _id = ids[i];
            __instance.windowList.AddTab(names[i], null, delegate
            {
                TraitPortableWorkbench.WorkbenchId = _id;
                __instance.SetFactory(ThingGen.Create(_id));
            });
        }
        __instance.windowList.BuildTabs(0);
    }
}

// 关闭 LayerCraft 时清除便携模式标志
[HarmonyPatch(typeof(LayerCraft), nameof(LayerCraft.OnKill))]
public static class Patch_LayerCraft_OnKill
{
    public static void Postfix()
    {
        TraitPortableWorkbench.IsOpen = false;
    }
}

// 凝华器（混合机）配方列表：在产物图标后显示生成量（×num）
[HarmonyPatch(typeof(UIDragGridInfo), nameof(UIDragGridInfo.Init))]
public static class Patch_UIDragGridInfo_Init
{
    public static void Postfix(UIDragGridInfo __instance)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return;
        }
        try
        {
            UIList list = __instance.list;
            if (list == null || list.buttons == null)
            {
                return;
            }
            foreach (UIList.ButtonPair pair in list.buttons)
            {
                SourceRecipe.Row row = pair.obj as SourceRecipe.Row;
                if (row == null || row.num == null)
                {
                    continue;
                }
                int num;
                if (!int.TryParse(row.num, out num))
                {
                    continue;
                }
                Util.Instantiate(__instance.moldCat, pair.component.transform)
                    .GetComponentInChildren<UIText>().SetText("×" + num);
            }
        }
        catch (System.Exception)
        {
        }
    }
}

// 凝华器产出倍率 / 分解机产出数量：在混合产物生成后按配置调整（仅 EssenceExtractor / Decomposer）
[HarmonyPatch(typeof(TraitCrafter), nameof(TraitCrafter.Craft))]
public static class Patch_TraitCrafter_Craft
{
    public static void Postfix(TraitCrafter __instance, ref Thing __result)
    {
        if (!ModConfig.Instance.Enabled || __result == null)
        {
            return;
        }
        if (__instance is TraitEssenceExtractor)
        {
            int mult = ModConfig.Instance.CondenserMult.Value;
            if (mult > 1)
            {
                __result.SetNum(__result.Num * mult);
            }
        }
        else if (__instance is TraitDecomposer)
        {
            int mult = ModConfig.Instance.DecomposerMult.Value;
            if (mult > 1)
            {
                __result.SetNum(__result.Num * mult); // 产出按倍率放大（各配方 num 不同：垃圾1/书5/装备1/2/3）
            }
        }
        ModConfig.NormalizeUniversalMaterial(__result);
    }
}

// 镐尖斧：按配置放大采集硬度与采集效率（重算 toolLv / efficiency / difficulty / maxProgress）
[HarmonyPatch(typeof(BaseTaskHarvest), nameof(BaseTaskHarvest.SetTarget))]
public static class Patch_BaseTaskHarvest_SetTarget
{
    public static void Postfix(BaseTaskHarvest __instance, Chara c)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return;
        }
        Thing tool = __instance.tool;
        if (tool == null || !(tool.trait is TraitAxePickaxe))
        {
            return;
        }
        int hMult = ModConfig.Instance.AxeHardness.Value;
        int eMult = ModConfig.Instance.AxeEfficiency.Value;
        if (hMult == 1 && eMult == 1)
        {
            return;
        }
        // 反推原硬度贡献：toolLv = hardness*num2/100 + encLV + blessedState + c.Evalue(idEle)*3/2
        int hardness = tool.material.hardness;
        if (hardness > 0 && hMult != 1)
        {
            int skill = c.Evalue(__instance.idEle) * 3 / 2;
            int encBlessed = tool.encLV + (int)tool.blessedState;
            int num2 = (__instance.toolLv - skill - encBlessed) * 100 / hardness;
            if (num2 < 0)
            {
                num2 = 100;
            }
            __instance.toolLv = hardness * hMult * num2 / 100 + encBlessed + skill;
        }
        int oldEfficiency = __instance.efficiency;
        int newEfficiency = (__instance.toolLv + 5) * 100 / ((__instance.reqLv + 5) * 140 / 100);
        if (newEfficiency < 50)
        {
            newEfficiency = 50;
        }
        newEfficiency *= eMult;
        __instance.efficiency = newEfficiency;
        if (__instance.IsTooHard)
        {
            __instance.difficulty = 3;
        }
        else if (newEfficiency > 150)
        {
            __instance.difficulty = 0;
        }
        else if (newEfficiency > 100)
        {
            __instance.difficulty = 1;
        }
        else
        {
            __instance.difficulty = 2;
        }
        if (oldEfficiency > 0 && oldEfficiency != newEfficiency)
        {
            __instance.maxProgress = (int)Mathf.Clamp((float)__instance.maxProgress * oldEfficiency / newEfficiency, 2f, 1000f);
        }
    }
}

// 分解机回收配方：GetSource 非 virtual，用 Harmony prefix 接管（Recipe 表 ing1 只作配方列表显示）
// 实际匹配按物品类型 + 稀有度选择对应 tag 的配方（装备无统一 category，判定靠 IsEquipmentOrRangedOrAmmo）
[HarmonyPatch(typeof(TraitCrafter), nameof(TraitCrafter.GetSource))]
public static class Patch_TraitDecomposer_GetSource
{
    public static bool Prefix(TraitCrafter __instance, AI_UseCrafter ai, ref SourceRecipe.Row __result)
    {
        if (!(__instance is TraitDecomposer))
        {
            return true; // 非分解机走原方法
        }
        if (ai.ings == null || ai.ings.Count == 0)
        {
            __result = null;
            return false;
        }
        Thing ing = ai.ings[0];
        string tag = null;
        if (ing != null && ing.category != null)
        {
            if (ing.category.IsChildOf("junk"))
            {
                tag = "junk";
            }
            else if (ing.category.IsChildOf("book"))
            {
                tag = "book";
            }
            else if (ing.IsEquipmentOrRangedOrAmmo || ing.IsEquipment)
            {
                // 装备（含祝福/诅咒/防具/盾/远程/弹药）：按 rarity 分档
                if (ing.rarity >= Rarity.Mythical)
                {
                    tag = "equip_high";
                }
                else if (ing.rarity >= Rarity.Superior)
                {
                    tag = "equip_rare";
                }
                else
                {
                    tag = "equip_normal";
                }
            }
        }
        if (tag == null)
        {
            __result = null;
            return false;
        }
        foreach (SourceRecipe.Row row in EClass.sources.recipes.rows)
        {
            if (row.factory == __instance.IdSource && row.tag != null && System.Array.IndexOf(row.tag, tag) >= 0)
            {
                __result = row;
                return false;
            }
        }
        __result = null;
        return false;
    }
}

// 万能生成器：给游戏已有物品「水」添加制作配方
// xlsx 整行覆盖已有 id 会破坏原数据（空列回退本表默认值，官方文档确认），改用运行时改源行（runtime_editing_sources.md 推荐做法）
[HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
public static class Patch_SourceManager_WaterRecipe
{
    public static void Postfix()
    {
        SourceThing.Row row = EClass.sources.things.rows.Find((SourceThing.Row x) => x.id == "water");
        if (row == null)
        {
            return;
        }
        row.factory = new string[] { "ut_universal_generator" }; // 在万能生成器制造
        row.components = new string[] { "plank/1", "glass/3", "ut_universal_essence/1" }; // 木板×1 + 玻璃×3 + 万能精华×1
        row.recipeKey = new string[] { "*" }; // 初始习得
        Debug.Log("[UniversalTool] water recipe patched to ut_universal_generator");
    }
}

// 管道频道/终端设置数据：随游戏保存写盘（自建持久化，物品 uid 索引）
[HarmonyPatch(typeof(Game), nameof(Game.Save))]
public static class Patch_Game_Save_Stores
{
    public static void Postfix()
    {
        PipeChannelStore.Save();
    }
}

// 万能护身符：免疫死亡（用户定稿）。持有者（背包内有护身符）死亡时，
// 若护身符格内 催化剂≥100 且 精华≥1000：消耗 100+1000，状态全满、移除全部减益、3 回合无敌，取消死亡
// 机制（✅反编译）：Chara.Die 为死亡入口（Prefix 返回 false 取消）；ConInvulnerable.value=剩余回合（Tick 每回合 -1）
[HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
public static class Patch_Chara_Die_Amulet
{
    public static bool Prefix(Chara __instance)
    {
        if (!ModConfig.Instance.Enabled || __instance.things == null)
        {
            return true;
        }
        Thing amulet = __instance.things.Find((Thing t) => t.trait is TraitUniversalAmulet);
        if (amulet == null)
        {
            return true;
        }
        Thing catalyst = amulet.things.Find((Thing t) => t.source.id == "ut_catalyst");
        Thing essence = amulet.things.Find((Thing t) => t.source.id == "ut_universal_essence");
        if (catalyst == null || catalyst.Num < 100 || essence == null || essence.Num < 1000)
        {
            return true; // 材料不足 → 正常死亡
        }
        catalyst.ModNum(-100);
        essence.ModNum(-1000);
        __instance.hp = __instance.MaxHP; // 状态全满
        __instance.mana.value = __instance.mana.max;
        __instance.stamina.value = __instance.stamina.max;
        for (int i = __instance.conditions.Count - 1; i >= 0; i--) // 移除全部减益
        {
            if (__instance.conditions[i] is BaseDebuff)
            {
                __instance.conditions[i].Kill();
            }
        }
        Condition invulnerable = __instance.AddCondition<ConInvulnerable>(); // 3 回合无敌
        invulnerable.value = 3;
        __instance.PlaySound("mutation");
        __instance.PlayEffect("mutation");
        Debug.Log("[UniversalTool] amulet: death prevented");
        return false; // 取消本次死亡
    }
}
