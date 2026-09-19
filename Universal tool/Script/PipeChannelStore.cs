// 管道频道持久化：游戏 Card 没有可安全复用的自由文本字段（c_refText 会参与命名显示），
// 改为 mod 自建存储：按物品 uid 存「频道|工作模式|类别筛选|轮询计数|传输速率」，随 Game.Save 写盘（BepInEx 目录）、首次访问懒加载
using System.Collections.Generic;
using System.IO;

public static class PipeChannelStore
{
    public const int ModeSequential = 0; // 顺序
    public const int ModeRoundRobin = 1; // 轮询

    static readonly Dictionary<int, string> channels = new Dictionary<int, string>();
    static readonly Dictionary<int, int> modes = new Dictionary<int, int>();
    static readonly Dictionary<int, string> categories = new Dictionary<int, string>();
    static readonly Dictionary<int, int> rrCounts = new Dictionary<int, int>(); // 轮询模式跨小时累计传输次数
    static readonly Dictionary<int, int> rates = new Dictionary<int, int>(); // 0=整个格子，其余为每次搬运数量
    static bool loaded;

    static string StorePath => Path.Combine(BepInEx.Paths.BepInExRootPath, "UniversalTool_pipes.csv");

    public static string GetChannel(Card c)
    {
        Load();
        string s;
        return channels.TryGetValue(c.uid, out s) ? s : "";
    }

    public static void SetChannel(Card c, string channel)
    {
        Load();
        channels[c.uid] = channel ?? "";
    }

    public static int GetMode(Card c)
    {
        Load();
        int m;
        return modes.TryGetValue(c.uid, out m) ? m : ModeSequential;
    }

    public static void SetMode(Card c, int mode)
    {
        Load();
        modes[c.uid] = mode;
    }

    public static string GetCategory(Card c)
    {
        Load();
        string s;
        return categories.TryGetValue(c.uid, out s) ? s : "";
    }

    public static void SetCategory(Card c, string id)
    {
        Load();
        categories[c.uid] = id ?? "";
    }

    // 轮询计数：跨小时持续累加（下次从下一台继续），输出器减少时取模自然回退
    public static int GetRRCount(Card c)
    {
        Load();
        int v;
        return rrCounts.TryGetValue(c.uid, out v) ? v : 0;
    }

    public static void SetRRCount(Card c, int v)
    {
        Load();
        rrCounts[c.uid] = v;
    }

    public static int GetRate(Card c)
    {
        Load();
        int v;
        return rates.TryGetValue(c.uid, out v) && IsValidRate(v) ? v : 0;
    }

    public static void SetRate(Card c, int v)
    {
        Load();
        rates[c.uid] = IsValidRate(v) ? v : 0;
    }

    static bool IsValidRate(int v)
    {
        return v == 0 || v == 1 || v == 2 || v == 5 || v == 10 || v == 50 || v == 100 || v == 200 || v == 500 || v == 1000;
    }

    static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        try
        {
            if (!File.Exists(StorePath))
            {
                return;
            }
            foreach (string line in File.ReadAllLines(StorePath))
            {
                string[] p = line.Split(',');
                if (p.Length < 2)
                {
                    continue;
                }
                int uid;
                if (!int.TryParse(p[0], out uid))
                {
                    continue;
                }
                channels[uid] = p[1];
                int m;
                if (p.Length >= 3 && int.TryParse(p[2], out m))
                {
                    modes[uid] = m;
                }
                if (p.Length >= 4)
                {
                    categories[uid] = p[3];
                }
                int rr;
                if (p.Length >= 5 && int.TryParse(p[4], out rr))
                {
                    rrCounts[uid] = rr;
                }
                int rate;
                if (p.Length >= 6 && int.TryParse(p[5], out rate) && IsValidRate(rate))
                {
                    rates[uid] = rate;
                }
            }
        }
        catch (System.Exception)
        {
        }
    }

    public static void Save()
    {
        try
        {
            List<string> lines = new List<string>();
            HashSet<int> uids = new HashSet<int>(channels.Keys);
            foreach (int uid in rates.Keys)
            {
                uids.Add(uid);
            }
            foreach (int uid in uids)
            {
                lines.Add(uid + "," + (channels.ContainsKey(uid) ? channels[uid] : "") + ","
                    + (modes.ContainsKey(uid) ? modes[uid] : ModeSequential) + ","
                    + (categories.ContainsKey(uid) ? categories[uid] : "") + ","
                    + (rrCounts.ContainsKey(uid) ? rrCounts[uid] : 0) + ","
                    + (rates.ContainsKey(uid) ? rates[uid] : 0));
            }
            File.WriteAllLines(StorePath, lines);
        }
        catch (System.Exception)
        {
        }
    }
}
