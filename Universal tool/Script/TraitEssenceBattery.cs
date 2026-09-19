// 精华电池：放置时为家园提供电力（发电量读配置，默认 200）
// 参考 TraitGenerator：发电量 = Electricity 属性（IsOn 时才供电，坏掉/断电时归零）
public class TraitEssenceBattery : TraitGenerator
{
    public override bool Waterproof => true;

    public override int Electricity
    {
        get
        {
            if (IsOn)
            {
                return ModConfig.Instance.BatteryPower.Value;
            }
            return 0;
        }
    }
}
