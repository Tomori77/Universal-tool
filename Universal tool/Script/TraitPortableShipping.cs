// 便携出货箱：直接打开，与最大的出货箱（container_shippingBig）联通、格子相同
public class TraitPortableShipping : TraitShippingChest
{
    public static Thing bigShipping;

    public override bool CanOpenContainer => ModConfig.Instance.Enabled;

    public static Thing GetBigShipping()
    {
        if (bigShipping == null || bigShipping.isDestroyed)
        {
            string id = EClass.sources.things.map.ContainsKey("container_shippingBig") ? "container_shippingBig" : "container_shipping";
            bigShipping = ThingGen.Create(id);
        }
        return bigShipping;
    }

    public override void Open()
    {
        if (ModConfig.Instance.ShippingLink.Value)
        {
            LayerInventory.CreateContainer(GetBigShipping()); // 联通大出货箱（共享格子）
        }
        else
        {
            base.Open(); // 独立容器（自身格子）
        }
    }
}
