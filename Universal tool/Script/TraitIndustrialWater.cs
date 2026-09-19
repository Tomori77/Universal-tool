// 工业用水复用原版水的饮用效果，并在创建时复制原版水的实际材质。
public class TraitIndustrialWater : TraitDrink
{
    public override EffectId IdEffect => EffectId.DrinkWater;

    public override void OnCreate(int lv)
    {
        base.OnCreate(lv);
        Thing water = ThingGen.Create("water");
        if (water != null && water.material != null)
        {
            owner.ChangeMaterial(water.material.id);
        }
    }
}
