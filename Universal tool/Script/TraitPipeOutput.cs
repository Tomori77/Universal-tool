// 物品输出器（频道制）：设置频道（数字/字母）后，接收同频道物品输入器送来的物品，存入贴近的容器
// 类别筛选（用户定稿）：未选择 = 接收全部；选择后 = 只接收该类物品（筛选存于输出器，由输入器搬运会时匹配）
// 传输与分配逻辑在 TraitPipeInput
public class TraitPipeOutput : Trait
{
    // 可选类别（id, 中文名），均为游戏大类，IsChildOf 自动涵盖子类
    public static readonly string[][] CategoryOptions = new string[][]
    {
        new string[] { "weapon", "武器" },
        new string[] { "armor", "防具" },
        new string[] { "ranged", "射击武器" },
        new string[] { "ammo", "弹药" },
        new string[] { "food", "食品" },
        new string[] { "drink", "饮料" },
        new string[] { "drug", "药" },
        new string[] { "book", "书本" },
        new string[] { "resource", "资源" },
        new string[] { "junk", "废品" },
        new string[] { "furniture", "家具" },
        new string[] { "tool", "工具" },
    };

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
        CurLayer.SetSize(640f, 640f);
        Refresh();
    }

    void Refresh()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        string channel = PipeChannelStore.GetChannel(owner);
        string filterName = CategoryName(PipeChannelStore.GetCategory(owner));
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>
        {
            channel.Length > 0 ? "频道：" + channel + "（点击修改）" : "频道：未设置（点击设置）",
            (filterName.Length > 0 ? "类别筛选：" + filterName : "类别筛选：未设置（接收全部）") + "（点击选择）",
            "接收同频道物品输入器送来的物品，存入贴近的容器",
            "设置类别后只接收该类物品",
            "无频道或频道内没有输入器时不工作",
        };
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                AskChannel();
            }
            else if (i == 1)
            {
                ShowCategorySelect();
            }
        }, autoClose: false);
    }

    // 类别筛选选择页（用户定稿：点击后选择显示）
    void ShowCategorySelect()
    {
        if (CurLayer == null || CurLayer.gameObject == null)
        {
            return;
        }
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>
        {
            "清除筛选（接收全部）",
        };
        foreach (string[] opt in CategoryOptions)
        {
            lines.Add(opt[1]);
        }
        lines.Add("返回");
        CurLayer.SetList(lines, (string s) => s, delegate (int i, string s)
        {
            if (i == 0)
            {
                PipeChannelStore.SetCategory(owner, "");
                Refresh();
            }
            else if (i >= 1 && i <= CategoryOptions.Length)
            {
                PipeChannelStore.SetCategory(owner, CategoryOptions[i - 1][0]);
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
                PipeChannelStore.SetChannel(owner, TraitPipeInput.FilterChannel(text));
            }
            Refresh();
        });
    }

    static string CategoryName(string id)
    {
        foreach (string[] opt in CategoryOptions)
        {
            if (opt[0] == id)
            {
                return opt[1];
            }
        }
        return "";
    }
}
