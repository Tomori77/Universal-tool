public static class TraitAutomationScheduler
{
    public static int CurrentHourToken { get; private set; }

    public static void ProcessRealtimeTick(int hourToken)
    {
        ProcessTick(hourToken, CreateRealtimeDate());
    }

    public static void ProcessOfflineHour(int hourToken, VirtualDate date)
    {
        for (int i = 0; i < 6; i++)
        {
            ProcessTick(hourToken, date);
        }
    }

    static void ProcessTick(int hourToken, VirtualDate date)
    {
        CurrentHourToken = hourToken;
        TraitAutoCondenser.ProcessTick(hourToken);
        TraitAutoDuplicator.ProcessTick();
        TraitAutoGenerator.ProcessTick();
        TraitCollector.ProcessTick();
        TraitPipeInput.ProcessTick();
        TraitFluidExtractor.ProcessTick();
        TraitAutoFarming.ProcessTick(date);
        TraitMineralGenerator.ProcessTick();
        TraitAutoSmelter.ProcessTick();
    }

    static VirtualDate CreateRealtimeDate()
    {
        VirtualDate date = new VirtualDate();
        date.IsRealTime = true;
        return date;
    }
}
