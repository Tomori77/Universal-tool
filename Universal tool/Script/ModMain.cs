using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;

// 注意：所有类使用全局命名空间（自定义 trait 类要求，Type.GetType 只查全局）

[BepInPlugin("UniversalTool", "Universal tool", "0.1.0")]
[BepInProcess("Elin.exe")]
public class ModMain : BaseUnityPlugin
{
    public static ModMain Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ModConfig.Instance.Init(Config);
        RegisterModOptions();
        RegisterCustomTypes();
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo("Universal tool loaded");
    }

    // 注册 Mod Options 配置 UI（手动 xml 布局 + 输入框，见 ModSettingsUI）
    private static void RegisterModOptions()
    {
        ModSettingsUI.Register();
    }

    // ClassCache 程序集注册：自定义 trait 生效前提（照抄 EnchantingTable 方案）
    private static void RegisterCustomTypes()
    {
        FieldInfo field = typeof(ClassCache).GetField("assemblies", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        HashSet<string> set = field?.GetValue(null) as HashSet<string>;
        if (set != null)
        {
            set.Add(Assembly.GetExecutingAssembly().GetName().Name);
            Instance?.Logger.LogInfo("Custom types assembly registered");
        }
        else
        {
            Instance?.Logger.LogWarning("ClassCache assemblies field not found");
        }
    }
}
