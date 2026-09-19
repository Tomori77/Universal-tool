// 设置页 UI（阶段 C 修订）：学习 Instant Fishing 的分块布局（hlayout 两列 + vlayout border 分组）。
// 数字类 → <slider> 滑块（下方 <text> 为可调区间注释）；开关类 → <toggle>；通用组含「恢复默认」按钮。
// OnBuildUI 里用 GetPreBuild<T>(id) 拿控件做双向绑定（控件 ↔ ConfigEntry），恢复默认后 SettingChanged 自动刷新控件。
using System;
using System.Reflection;
using BepInEx.Configuration;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;
using UnityEngine;

public static class ModSettingsUI
{
    private const string XML = @"
<config>
  <hlayout>
    <vlayout>
      <vlayout border='true'><title>通用</title>
        <toggle id='enable' checked='true'><contentId>启用 Universal tool</contentId></toggle>
        <button id='reset_btn'><contentId>恢复默认</contentId></button>
      </vlayout>
      <vlayout border='true'><title>凝华器</title>
        <slider id='condenser_mult' min='1' max='10' buttons='true'>产出倍率</slider>
        <text>可调范围：1~10</text>
      </vlayout>
      <vlayout border='true'><title>分解机</title>
        <slider id='decomposer_mult' min='1' max='10' buttons='true'>产出倍率</slider>
        <text>可调范围：1~10</text>
      </vlayout>
      <vlayout border='true'><title>精华电池</title>
        <slider id='battery_power' min='1' max='10000' buttons='true'>发电量</slider>
        <text>可调范围：1~10000</text>
      </vlayout>
      <vlayout border='true'><title>物品收集器</title>
        <slider id='collector_radius' min='1' max='5' buttons='true'>收集半径</slider>
        <text>可调范围：1~5</text>
      </vlayout>
      <vlayout border='true'><title>技能训练器</title>
        <slider id='train_earth' min='10' max='100' buttons='true'>大地结晶等级</slider>
        <text>可调范围：10~100</text>
        <slider id='train_sun' min='20' max='200' buttons='true'>太阳结晶等级</slider>
        <text>可调范围：20~200</text>
        <slider id='train_mana' min='50' max='500' buttons='true'>魔力结晶等级</slider>
        <text>可调范围：50~500</text>
        <toggle id='train_ally' checked='true'><contentId>允许对同伴使用</contentId></toggle>
      </vlayout>
      <vlayout border='true'><title>增幅器</title>
        <slider id='amp_tier1' min='1' max='100' buttons='true'>阶梯1单价（1-20个）</slider>
        <text>可调范围：1~100</text>
        <slider id='amp_tier2' min='1' max='100' buttons='true'>阶梯2单价（21-50个）</slider>
        <text>可调范围：1~100</text>
        <slider id='amp_tier3' min='1' max='100' buttons='true'>阶梯3单价（51-199个）</slider>
        <text>可调范围：1~100</text>
        <slider id='amp_tier4' min='1' max='100' buttons='true'>阶梯4单价（200个以上）</slider>
        <text>可调范围：1~100</text>
        <toggle id='amp_break_cap' checked='true'><contentId>突破临时属性上限</contentId></toggle>
      </vlayout>
    </vlayout>
    <vlayout>
      <vlayout border='true'><title>属性灌注器</title>
        <slider id='infuse_ratio' min='1' max='100' buttons='true'>比例（每 N 精华 +1 点）</slider>
        <text>可调范围：1~100</text>
      </vlayout>
      <vlayout border='true'><title>万能修复器</title>
        <slider id='repair_identify' min='50' max='1000' buttons='true'>鉴定消耗精华</slider>
        <text>可调范围：50~1000</text>
        <slider id='repair_identify_sp' min='100' max='2000' buttons='true'>高级鉴定消耗精华</slider>
        <text>可调范围：100~2000</text>
        <slider id='repair_uncurse' min='100' max='2000' buttons='true'>去诅咒消耗精华</slider>
        <text>可调范围：100~2000</text>
      </vlayout>
      <vlayout border='true'><title>镐尖斧</title>
        <slider id='axe_efficiency' min='1' max='10' buttons='true'>采集效率倍率</slider>
        <text>可调范围：1~10</text>
        <slider id='axe_hardness' min='1' max='10' buttons='true'>硬度倍率</slider>
        <text>可调范围：1~10</text>
      </vlayout>
      <vlayout border='true'><title>便携工作台</title>
        <toggle id='wb_workbench' checked='true'><contentId>制作台</contentId></toggle>
        <toggle id='wb_wood' checked='true'><contentId>木工的桌子</contentId></toggle>
        <toggle id='wb_tinker' checked='true'><contentId>杂工的桌子</contentId></toggle>
        <toggle id='wb_stone' checked='true'><contentId>石工的桌子</contentId></toggle>
        <toggle id='wb_drafting' checked='true'><contentId>设计台</contentId></toggle>
      </vlayout>
      <vlayout border='true'><title>便携出货箱</title>
        <toggle id='shipping_link' checked='true'><contentId>联通大出货箱</contentId></toggle>
      </vlayout>
      <vlayout border='true'><title>调试</title>
        <toggle id='debug_id' checked='false'><contentId>显示物品ID</contentId></toggle>
      </vlayout>
    </vlayout>
  </hlayout>
</config>";

    public static void Register()
    {
        try
        {
            ModOptionController controller = ModOptionController.Register("Charlotte.UniversalTool", null);
            if (controller == null)
            {
                return;
            }
            controller.SetTranslation("Charlotte.UniversalTool", "Universal tool", "万能工具", "万能工具");
            controller.SetPreBuildWithXml(XML);
            controller.OnBuildUI += BuildUI;
        }
        catch (Exception e)
        {
            Debug.LogWarning("UniversalTool: Mod Options unavailable, config UI disabled: " + e.Message);
        }
    }

    private static void BuildUI(OptionUIBuilder b)
    {
        ModConfig cfg = ModConfig.Instance;

        // 恢复默认按钮
        OptButton reset = b.GetPreBuild<OptButton>("reset_btn");
        if (reset != null)
        {
            reset.OnClicked += ResetAll;
        }

        // 开关类（双向绑定）
        BindToggle(b, "enable", cfg.Enable);
        BindToggle(b, "train_ally", cfg.TrainAlly);
        BindToggle(b, "amp_break_cap", cfg.AmpBreakCap);
        BindToggle(b, "wb_workbench", cfg.WbWorkbench);
        BindToggle(b, "wb_wood", cfg.WbWood);
        BindToggle(b, "wb_tinker", cfg.WbTinker);
        BindToggle(b, "wb_stone", cfg.WbStone);
        BindToggle(b, "wb_drafting", cfg.WbDrafting);
        BindToggle(b, "shipping_link", cfg.ShippingLink);
        BindToggle(b, "debug_id", cfg.DebugId);

        // 数字类（滑块，双向绑定）
        BindSlider(b, "condenser_mult", cfg.CondenserMult, 1f);
        BindSlider(b, "decomposer_mult", cfg.DecomposerMult, 1f);
        BindSlider(b, "battery_power", cfg.BatteryPower, 50f);
        BindSlider(b, "collector_radius", cfg.CollectorRadius, 1f);
        BindSlider(b, "train_earth", cfg.TrainEarth, 5f);
        BindSlider(b, "train_sun", cfg.TrainSun, 5f);
        BindSlider(b, "train_mana", cfg.TrainMana, 5f);
        BindSlider(b, "amp_tier1", cfg.AmpTier1, 1f);
        BindSlider(b, "amp_tier2", cfg.AmpTier2, 1f);
        BindSlider(b, "amp_tier3", cfg.AmpTier3, 1f);
        BindSlider(b, "amp_tier4", cfg.AmpTier4, 1f);
        BindSlider(b, "infuse_ratio", cfg.InfuseRatio, 1f);
        BindSlider(b, "repair_identify", cfg.RepairIdentifyCost, 10f);
        BindSlider(b, "repair_identify_sp", cfg.RepairIdentifySPCost, 10f);
        BindSlider(b, "repair_uncurse", cfg.RepairUncurseCost, 10f);
        BindSlider(b, "axe_efficiency", cfg.AxeEfficiency, 1f);
        BindSlider(b, "axe_hardness", cfg.AxeHardness, 1f);
    }

    private static void BindToggle(OptionUIBuilder b, string id, ConfigEntry<bool> cfg)
    {
        OptToggle t = b.GetPreBuild<OptToggle>(id);
        if (t == null)
        {
            return;
        }
        t.Checked = cfg.Value;
        bool busy = false;
        t.OnValueChanged += delegate(bool v)
        {
            if (!busy)
            {
                busy = true;
                cfg.Value = v;
                busy = false;
            }
        };
        cfg.SettingChanged += delegate
        {
            if (!busy && t.Checked != cfg.Value)
            {
                busy = true;
                t.Checked = cfg.Value;
                busy = false;
            }
        };
    }

    private static void BindSlider(OptionUIBuilder b, string id, ConfigEntry<int> cfg, float step)
    {
        OptSlider s = b.GetPreBuild<OptSlider>(id);
        if (s == null)
        {
            return;
        }
        s.Step = step;
        s.Value = cfg.Value;
        string baseTitle = s.Title; // xml 里的滑块标题（如「产出倍率」）
        s.Title = baseTitle + "（" + cfg.Value + "）"; // 标题旁显示当前数值
        bool busy = false;
        s.OnValueChanged += delegate(float v)
        {
            if (busy)
            {
                return;
            }
            int iv = (int)Math.Round(v);
            s.Title = baseTitle + "（" + iv + "）";
            if (cfg.Value != iv)
            {
                busy = true;
                cfg.Value = iv;
                busy = false;
            }
        };
        cfg.SettingChanged += delegate
        {
            if (!busy && (int)Math.Round(s.Value) != cfg.Value)
            {
                busy = true;
                s.Value = cfg.Value;
                s.Title = baseTitle + "（" + cfg.Value + "）";
                busy = false;
            }
        };
    }

    // 恢复所有配置项为默认值（反射遍历 ConfigEntry 字段，触发 SettingChanged 刷新控件）
    private static void ResetAll()
    {
        ModConfig cfg = ModConfig.Instance;
        FieldInfo[] fields = typeof(ModConfig).GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (FieldInfo f in fields)
        {
            Type ft = f.FieldType;
            if (!ft.IsGenericType || ft.GetGenericTypeDefinition() != typeof(ConfigEntry<>))
            {
                continue;
            }
            object entry = f.GetValue(cfg);
            if (entry == null)
            {
                continue;
            }
            PropertyInfo valueProp = ft.GetProperty("Value");
            PropertyInfo defProp = ft.GetProperty("DefaultValue");
            object def = defProp.GetValue(entry, null);
            valueProp.SetValue(entry, def, null);
        }
    }
}
