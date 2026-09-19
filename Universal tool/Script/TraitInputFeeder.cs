// 输入器：自动设备的供料容器。与自动凝华器上下左右相邻放置，内容物会被其每小时自动取用
// 格子 4×3（trait 列 "InputFeeder,4,3"，参数 1/2 = 宽高）
public class TraitInputFeeder : TraitContainer
{
    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    // 新造出的容器不预生成内容（基类会随机塞杂物/奖牌）
    public override void Prespawn(int lv)
    {
    }
}
