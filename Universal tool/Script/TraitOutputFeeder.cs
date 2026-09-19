// 输出器：自动设备的收货容器。与自动凝华器上下左右相邻放置，自动接收其产物
// 格子 4×3（trait 列 "OutputFeeder,4,3"，参数 1/2 = 宽高）
public class TraitOutputFeeder : TraitContainer
{
    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    // 新造出的容器不预生成内容（基类会随机塞杂物/奖牌）
    public override void Prespawn(int lv)
    {
    }
}
