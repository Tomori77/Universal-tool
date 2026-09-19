// 精华提取机：像石磨一样的混合机（左侧配方列表），将材料提炼成万能精华
public class TraitEssenceExtractor : TraitCrafter
{
    // Recipe 表 factory 列与此匹配
    public override string IdSource => "EssenceExtractor";

    public override int numIng => 2;

    public override string CrafterTitle => "凝华";

    public override AnimeID IdAnimeProgress => AnimeID.Shiver;

    public override string idSoundProgress => "cook_grind";

    // 总开关：关闭时禁用使用入口（配方数据仍在 xlsx，仅此处的交互入口受控）
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override void TrySetAct(ActPlan p)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return;
        }
        base.TrySetAct(p);
    }
}
