// 便携工作台：随身虚拟工作台，随时随地打开制作台/杂工桌（可扩展更多工作台）
using System.Collections.Generic;
using UnityEngine;

public class TraitPortableWorkbench : TraitCrafter
{
    // 全部工作台入口（id 与名称一一对应），按配置开关过滤后生成 tab
    public static readonly string[] AllWorkbenchIds = { "workbench", "factory_wood", "factory_tinker", "factory_stone", "workbench2" };
    public static readonly string[] AllWorkbenchNames = { "制作台", "木工的桌子", "杂工的桌子", "石工的桌子", "设计台" };

    public static string WorkbenchId = "workbench";

    public static bool IsOpen;

    public override bool IsFactory => true;

    public override bool CanUseFromInventory => true;

    // 关键：TraitCrafter 默认 HoldAsDefaultInteraction=true → 背包右键默认"拿起"而非"使用"。
    // 重写为 false，让右键默认执行"使用"（打开工作台）。
    public override bool HoldAsDefaultInteraction => false;

    // 背包右键"使用"出现的前提（显式重写，避免继承 TraitCrafter.CanUse 的 NPC 检查误判）
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override bool OnUse(Chara c)
    {
        Debug.Log("[UniversalTool] PortableWorkbench.OnUse");
        OpenCraft();
        return false;
    }

    public override void TrySetAct(ActPlan p)
    {
        p.TrySetAct("craft", delegate
        {
            OpenCraft();
            return false;
        });
    }

    // 按配置开关返回已启用的工作台 id（至少保留一个）
    public static string[] GetEnabledIds()
    {
        ModConfig cfg = ModConfig.Instance;
        bool[] flags = { cfg.WbWorkbench.Value, cfg.WbWood.Value, cfg.WbTinker.Value, cfg.WbStone.Value, cfg.WbDrafting.Value };
        List<string> ids = new List<string>();
        for (int i = 0; i < AllWorkbenchIds.Length; i++)
        {
            if (flags[i])
            {
                ids.Add(AllWorkbenchIds[i]);
            }
        }
        if (ids.Count == 0)
        {
            ids.Add("workbench");
        }
        return ids.ToArray();
    }

    // 与 GetEnabledIds 下标对齐的名称数组
    public static string[] GetEnabledNames()
    {
        ModConfig cfg = ModConfig.Instance;
        bool[] flags = { cfg.WbWorkbench.Value, cfg.WbWood.Value, cfg.WbTinker.Value, cfg.WbStone.Value, cfg.WbDrafting.Value };
        List<string> names = new List<string>();
        for (int i = 0; i < AllWorkbenchNames.Length; i++)
        {
            if (flags[i])
            {
                names.Add(AllWorkbenchNames[i]);
            }
        }
        if (names.Count == 0)
        {
            names.Add("制作台");
        }
        return names.ToArray();
    }

    public static void OpenCraft()
    {
        string[] ids = GetEnabledIds();
        if (System.Array.IndexOf(ids, WorkbenchId) < 0)
        {
            WorkbenchId = ids[0]; // 当前 tab 被配置关闭时回退到第一个可用工作台
        }
        Debug.Log("[UniversalTool] OpenCraft: " + WorkbenchId);
        IsOpen = true;
        try
        {
            Thing factory = ThingGen.Create(WorkbenchId);
            Debug.Log("[UniversalTool] factory created: " + (factory != null ? factory.id : "NULL"));
            LayerCraft layer = EClass.ui.AddLayer<LayerCraft>();
            layer.SetFactory(factory);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[UniversalTool] OpenCraft failed: " + e);
        }
    }
}
