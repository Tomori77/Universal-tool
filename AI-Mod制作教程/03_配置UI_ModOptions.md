# 03 · 配置 UI（Mod Options）

> 本页负责游戏内设置菜单。Mod Options 来自工坊 3381182341，GUID 为 `evilmask.elinplugins.modoptions`。配置流程查 `Mod制作规范.md`，引用查 `工具与依赖.md`。

## 1. 选择方案

| 方案 | 适用 |
|---|---|
| 自动绑定 | 一组开关、滑块或下拉框；实现最简单 |
| 手动 XML | 需要分组、横排、输入框、按钮或自定义顺序 |

优先使用自动绑定；只有布局需求超过自动绑定能力时才使用 XML。

## 2. 自动绑定

Mod Options 通过反射查找配置对象中的实例字段。字段必须是 `ConfigEntry<T>`，并带对应特性；静态字段不会被枚举。

- `[ModCfgToggle(titleId, tooltipId)]` → `ConfigEntry<bool>`。
- `[ModCfgSlider(titleId, min, max, step, buttons)]` → `ConfigEntry<int/long/float>`。
- `[ModCfgDropdown(titleId)]` → 枚举配置。

中文标题可直接传入；找不到翻译时会回退原文。配置对象使用单例，但字段本身必须是实例字段。

最小结构：

```csharp
public class ModConfig
{
    public static ModConfig Instance = new ModConfig();

    [ModCfgToggle("启用", "总开关")]
    public ConfigEntry<bool> Enable;

    [ModCfgSlider("产出倍率", 1f, 10f, 1f, true)]
    public ConfigEntry<int> Multiplier;

    public void Init(ConfigFile config)
    {
        Enable = config.Bind("General", "Enable", true, "Master switch.");
        Multiplier = config.Bind("General", "Multiplier", 1, "Output multiplier.");
    }
}
```

## 3. 注册与软依赖

在插件启动时先初始化 BepInEx 配置，再尝试注册 Mod Options。注册必须用 try-catch 包裹：未安装 Mod Options 时，cfg 文件仍应可用，Mod 功能不能因此崩溃。

```csharp
try
{
    ModOptionController c = ModOptionController.Register("作者.模块名", null, ModConfig.Instance);
    c?.SetTranslation("作者.模块名", "Mod Name", "模组名称", "模组名称");
}
catch (System.Exception e)
{
    Logger.LogWarning("Mod Options unavailable: " + e.Message);
}
```

Mod Options 的加载优先级应早于本 Mod；不要把它作为无法降级的硬依赖。

## 4. 手动 XML

需要分组、恢复默认、输入框等控件时使用：

1. `SetPreBuildWithXml(xml)` 定义布局。
2. `OnBuildUI` 中用 `GetPreBuild<T>(id)` 取得控件。
3. 手动把控件值与 `ConfigEntry.Value` 双向绑定。

常用标签：`config`、`vlayout`、`hlayout`、`toggle`、`slider`、`input`、`button`、`text`。常用控件属性：

- `OptToggle.Checked` / `OnValueChanged`。
- `OptSlider.Value` / `Title` / `OnValueChanged` / `Step`。
- `OptInput.Text` / `OnValueChanged`。
- `OptButton.OnClicked`。

注意：XML 的 slider `step` 可能不会应用到控件，需在 `OnBuildUI` 手动设置；手动布局的标题也要自行刷新当前值。

## 5. 多语言与运行时读取

- 用 `SetTranslation(guid, en, jp, cn)` 设置 Mod 列表名。
- 未注册的标题可直接显示原文。
- 业务代码直接读取 `ModConfig.Instance.Xxx.Value`，配置修改后通常立即生效。
- 恢复默认按钮可将各 `ConfigEntry` 的 `Value` 设为 `DefaultValue`。

## 6. 验证

1. 编译无错误。
2. 已安装 Mod Options 时，设置菜单能显示本 Mod。
3. 修改开关、滑块后对应功能立即变化。
4. 未安装 Mod Options 时 Mod 仍能加载，配置仍写入 `BepInEx\config\`。
