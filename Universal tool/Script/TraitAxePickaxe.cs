// 镐斧：斧+镐合一的采集工具。
// 采集能力（挖矿220/伐木225/挖掘230）在创建时直接注入，不依赖材料元素映射；
// 采集硬度由制作材料决定（BaseTaskHarvest：toolLv += tool.material.hardness * efficiency / 100）。
public class TraitAxePickaxe : TraitTool
{
    public override void OnCreate(int lv)
    {
        if (!ModConfig.Instance.Enabled)
        {
            return; // 总开关关闭时不注入采集能力
        }
        owner.elements.SetBase(220, 1); // 挖矿 mining
        owner.elements.SetBase(225, 1); // 伐木 lumberjack
        owner.elements.SetBase(230, 1); // 挖掘 digging
    }
}
