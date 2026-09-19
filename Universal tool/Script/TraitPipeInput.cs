// 输入终端（频道制）：设置频道（数字/字母）后，每 10 分钟（每小时结算 6 次）按设定速率搬运贴近容器的物品，
// 送到同频道输出终端贴近的容器。频道配对与距离无关（删除物品管道后以频道取代物理连通）；
// 频道至少要有一对输入器+输出器才工作
// 工作模式（用户定稿）：
//   顺序：按输入器→输出器距离从近到远，装满第一个容器后装第二个
//   轮询：每小时 6 次依次轮流分给各输出器（2台：1,3,5/2,4,6；3台：1,4/2,5/3,6；4台：1,5/2,6/3/4）
using System.Collections.Generic;
using UnityEngine;

public class TraitPipeInput : Trait
{
    static readonly int[] RateOptions = { 1, 2, 5, 10, 50, 100, 200, 500, 1000, 0 }; // 0=整个格子

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
        string channel = PipeChannelStore.GetChannel(owner);
        bool roundRobin = PipeChannelStore.GetMode(owner) == PipeChannelStore.ModeRoundRobin;
        int rate = PipeChannelStore.GetRate(owner);
        List<string> lines = new List<string>
        {
            channel.Length > 0 ? "频道：" + channel + "（点击修改）" : "频道：未设置（点击设置）",
            (roundRobin ? "工作模式：轮询" : "工作模式：顺序") + "（点击选择）",
            "传输速率：" + RateName(rate) + "（点击选择）",
            "每10分钟按传输速率搬运一次，送到同频道输出终端贴近的容器",
            "顺序：按距离装满第一个容器，再装第二个",
            "轮询：每小时6次依次轮流分给各输出器",
            "无频道或频道内没有输出器时不工作",
        };
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                AskChannel();
            }
            else if (i == 1)
            {
                ShowModeSelect();
            }
            else if (i == 2)
            {
                ShowRateSelect();
            }
        }, autoClose: false);
    }

    void ShowRateSelect()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<string> lines = new List<string>();
        foreach (int rate in RateOptions)
        {
            lines.Add(RateName(rate));
        }
        lines.Add("返回");
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i >= 0 && i < RateOptions.Length)
            {
                PipeChannelStore.SetRate(owner, RateOptions[i]);
            }
            Refresh();
        }, autoClose: false);
    }

    static string RateName(int rate)
    {
        return rate == 0 ? "整个格子" : rate + " 个/次";
    }

    // 工作模式选择页（用户定稿：点击后选择显示，非直接切换）
    void ShowModeSelect()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        List<string> lines = new List<string>
        {
            "顺序模式：按距离装满第一个容器，再装第二个",
            "轮询模式：每小时6次依次轮流分给各输出器",
            "返回",
        };
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                PipeChannelStore.SetMode(owner, PipeChannelStore.ModeSequential);
                Refresh();
            }
            else if (i == 1)
            {
                PipeChannelStore.SetMode(owner, PipeChannelStore.ModeRoundRobin);
                Refresh();
            }
            else
            {
                Refresh();
            }
        }, autoClose: false);
    }

    void AskChannel()
    {
        Dialog.InputName("输入频道（数字或字母）", PipeChannelStore.GetChannel(owner), delegate (bool cancel, string text)
        {
            if (!cancel)
            {
                PipeChannelStore.SetChannel(owner, FilterChannel(text));
            }
            Refresh();
        });
    }

    // 频道只允许数字和字母
    public static string FilterChannel(string text)
    {
        if (text == null)
        {
            return "";
        }
        string s = "";
        foreach (char ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                s += ch;
            }
        }
        return s;
    }

    public static void ProcessTick()
    {
        if (EClass._map == null)
        {
            return;
        }
        List<TraitPipeInput> machines = new List<TraitPipeInput>();
        foreach (Thing thing in EClass._map.things)
        {
            TraitPipeInput input = thing.trait as TraitPipeInput;
            if (input != null)
            {
                machines.Add(input);
            }
        }
        foreach (TraitPipeInput input in machines)
        {
            input.ProcessOnce();
        }
    }

    void ProcessOnce()
    {
        if (!ModConfig.Instance.Enabled || !owner.ExistsOnMap || owner.isBroken)
        {
            return;
        }
        string channel = PipeChannelStore.GetChannel(owner);
        if (channel.Length == 0)
        {
            return;
        }
        List<TraitPipeOutput> outputs = FindChannelOutputs(channel);
        if (outputs.Count > 0)
        {
            TransferOnce(outputs, PipeChannelStore.GetRate(owner));
        }
    }

    void TransferOnce(List<TraitPipeOutput> outputs, int rate)
    {
        Thing src = FindNeighborContainer(owner.pos);
        if (src == null)
        {
            return;
        }
        // 目标输出器带类别筛选时，只搬该类物品（筛选在输出器上设置，由输入器执行时匹配）
        if (PipeChannelStore.GetMode(owner) == PipeChannelStore.ModeSequential)
        {
            // 顺序：按距离从近到远找第一个「容器有空间 + 有可搬物品」的输出器
            foreach (TraitPipeOutput o in outputs)
            {
                Thing c = FindNeighborContainer(o.owner.pos);
                if (c == null || c == src || c.things.IsFull())
                {
                    continue;
                }
                Thing stack = FindStack(src, PipeChannelStore.GetCategory(o.owner));
                if (stack == null)
                {
                    continue;
                }
                MoveStack(c, stack, rate);
                return;
            }
        }
        else
        {
            // 轮询（用户改定）：跨小时记忆轮到哪台——持久计数器取模，下一小时从下一台继续；
            // 输出器消失时列表缩短，取模自然向后轮询（容器满/缺容器/无可搬物品则本次跳过但计数仍前进）
            int cursor = PipeChannelStore.GetRRCount(owner);
            PipeChannelStore.SetRRCount(owner, cursor + 1);
            TraitPipeOutput o = outputs[cursor % outputs.Count];
            Thing c = FindNeighborContainer(o.owner.pos);
            if (c != null && c != src && !c.things.IsFull())
            {
                Thing stack = FindStack(src, PipeChannelStore.GetCategory(o.owner));
                if (stack != null)
                {
                    MoveStack(c, stack, rate);
                }
            }
        }
    }

    static void MoveStack(Thing destination, Thing stack, int rate)
    {
        Thing moved = rate == 0 || stack.Num <= rate ? stack : stack.Split(rate);
        destination.AddCard(moved); // 整堆时自动从源容器摘除；拆分时加入新堆
    }

    // 按类别筛选取源容器里第一个可搬的整堆（未设筛选 = 第一堆；类别按 IsChildOf 匹配子类）
    static Thing FindStack(Thing src, string category)
    {
        if (category.Length == 0)
        {
            return src.things.Find((Thing t) => true);
        }
        return src.things.Find((Thing t) => t.category != null && t.category.IsChildOf(category));
    }

    // 当前地图内同频道的全部输出器，按与输入器的距离升序
    List<TraitPipeOutput> FindChannelOutputs(string channel)
    {
        List<TraitPipeOutput> list = new List<TraitPipeOutput>();
        foreach (Thing t in EClass._map.things)
        {
            TraitPipeOutput o = t.trait as TraitPipeOutput;
            if (o != null && t.IsInstalled && PipeChannelStore.GetChannel(t) == channel)
            {
                list.Add(o);
            }
        }
        Point center = owner.pos;
        list.Sort(delegate (TraitPipeOutput a, TraitPipeOutput b)
        {
            int da = Mathf.Max(Mathf.Abs(a.owner.pos.x - center.x), Mathf.Abs(a.owner.pos.z - center.z));
            int db = Mathf.Max(Mathf.Abs(b.owner.pos.x - center.x), Mathf.Abs(b.owner.pos.z - center.z));
            return da - db;
        });
        return list;
    }

    // 四邻格找已放置容器
    static Thing FindNeighborContainer(Point center)
    {
        Thing found = null;
        center.ForeachNeighbor(delegate (Point p)
        {
            if (found == null)
            {
                foreach (Thing t in p.Things)
                {
                    if (t.IsInstalled && t.trait.IsContainer)
                    {
                        found = t;
                        break;
                    }
                }
            }
        }, diagonal: false);
        return found;
    }
}
