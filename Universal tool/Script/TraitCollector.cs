// 物品收集器：每 10 分钟（每小时结算 6 次）收集以自身为中心、半径 R（全局设置 1~5）内
// 掉落在地上的物品（未安装、非容器、非隐藏），存入自身 6×6（36 格）容器
// 开关机在打开的页面切换（isOn 持久化，放置即开启）；不耗电
using System.Collections.Generic;
using UnityEngine;

public class TraitCollector : TraitContainer
{
    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        owner.isOn = true; // 放置即开始收集
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
        p.TrySetAct("actUse", delegate
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
            "储存空间：6×6（36格，点击打开）",
            "收集半径：" + ModConfig.Instance.CollectorRadius.Value + "（设置页可调 1~5）",
            "每10分钟收集一次范围内掉落在地上的物品",
            "收集到的物品存入自身容器（6×6，共36格）",
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
        if (EClass._map == null)
        {
            return;
        }
        List<TraitCollector> machines = new List<TraitCollector>();
        foreach (Thing thing in EClass._map.things)
        {
            TraitCollector collector = thing.trait as TraitCollector;
            if (collector != null)
            {
                machines.Add(collector);
            }
        }
        foreach (TraitCollector collector in machines)
        {
            if (ModConfig.Instance.Enabled && collector.owner.ExistsOnMap && !collector.owner.isBroken && collector.owner.isOn)
            {
                collector.CollectOnce();
            }
        }
    }

    void CollectOnce()
    {
        if (owner.things.IsFull())
        {
            return;
        }
        int radius = Mathf.Clamp(ModConfig.Instance.CollectorRadius.Value, 1, 5);
        // 先收集候选再搬运（避免迭代中修改格子物品列表）
        List<Thing> loose = new List<Thing>();
        Point center = owner.pos;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                Point p = new Point(center.x + dx, center.z + dz);
                if (!p.IsValid)
                {
                    continue;
                }
                foreach (Thing t in p.Things)
                {
                    if (!t.IsInstalled && !t.isHidden && !t.isRoofItem && !t.isMasked && !t.trait.IsContainer)
                    {
                        loose.Add(t);
                    }
                }
            }
        }
        foreach (Thing t in loose)
        {
            if (owner.things.IsFull())
            {
                break; // 容器满则剩余物品留在原地
            }
            owner.AddCard(t); // AddCard 自动从地上（zone）摘除
        }
        if (loose.Count > 0)
        {
            Debug.Log("[UniversalTool] collector: picked " + loose.Count + " stack(s)");
        }
    }

}
