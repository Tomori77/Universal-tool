// 万能分解机：像凝华器一样的混合机（单材料槽位），把材料分解成万能精华
// 页面标题「分解」，配方见 Recipe 表（factory=Decomposer）
public class TraitDecomposer : TraitCrafter
{
    // Recipe 表 factory 列与此匹配
    public override string IdSource => "Decomposer";

    public override int numIng => 1; // 单材料槽位（树枝 → 精华）

    public override string CrafterTitle => "分解";

    public override AnimeID IdAnimeProgress => AnimeID.Shiver;

    public override string idSoundProgress => "cook_grind";

    // 总开关：关闭时禁用使用入口
    public override bool CanUse(Chara c) => ModConfig.Instance.Enabled;

    public override void TrySetAct(ActPlan p)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return;
        }
        base.TrySetAct(p);
    }

    // 放入判定与 GetSource（Patch_TraitDecomposer_GetSource）保持一致：
    // 垃圾/书/装备（含祝福、诅咒、远程、弹药、防具、盾）都可放入，按 rarity 分档回收。
    public override bool IsCraftIngredient(Card c, int idx)
    {
        if (c == null || c.category == null)
        {
            return false;
        }
        return c.category.IsChildOf("junk") || c.category.IsChildOf("book") || c.IsEquipmentOrRangedOrAmmo;
    }
}
