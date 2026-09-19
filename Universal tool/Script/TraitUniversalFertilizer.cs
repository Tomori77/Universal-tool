// 万能肥料：放到作物格上立即催熟到可收获的阶段并自毁；
// 空格/无作物的格子不生效（肥料留在原地可拾回）
// 机制（✅反编译 GrowSystem）：使用作物实际 HarvestStage，避免最后阶段为枯萎阶段
public class TraitUniversalFertilizer : TraitFertilizer
{
    public override void OnChangePlaceState(PlaceState state)
    {
        if (!ModConfig.Instance.Enabled || state != PlaceState.installed)
        {
            return;
        }
        GrowSystem growth = owner.pos.growth;
        if (growth == null || growth.stages == null || growth.stages.Length < 2)
        {
            return; // 该格没有可生长的作物
        }
        int harvestStage = growth.HarvestStage;
        if (harvestStage < 0 || harvestStage >= growth.stages.Length || !growth.stages[harvestStage].harvest)
        {
            harvestStage = -1;
            for (int i = 0; i < growth.stages.Length; i++)
            {
                if (growth.stages[i].harvest)
                {
                    harvestStage = i;
                }
            }
        }
        if (harvestStage < 0)
        {
            return;
        }
        growth.SetStage(harvestStage);
        owner.PlaySound("mutation");
        owner.PlayEffect("mutation");
        owner.Destroy();
    }

    // 原版 TraitFertilizer 会在每小时结算并消耗肥料；万能肥料已在放置时立即完成催熟，避免重复处理。
    public override void OnSimulateHour(VirtualDate date)
    {
    }
}
