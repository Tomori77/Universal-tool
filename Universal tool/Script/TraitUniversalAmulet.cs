// 万能护身符：背包右键打开 3×3 共 9 格容器
// 背包"打开"入口由 InvOwner 对容器物品自动提供（冷藏箱同款），格子宽高走 trait 参数（trait 列 "UniversalAmulet,3,3"）
// 特殊机制待定，后续版本再加
public class TraitUniversalAmulet : TraitContainer
{
    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    // 新造出的护身符不预生成内容（基类 Prespawn 会随机塞杂物/奖牌进容器）
    public override void Prespawn(int lv)
    {
    }
}
