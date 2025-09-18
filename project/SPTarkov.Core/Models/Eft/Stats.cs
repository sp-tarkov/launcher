namespace SPTarkov.Core.Models;

public class Stats
{
    public Counters OverallCounters // TODO: rework as they are different
    {
        get;
        set;
    } = new();

    public Counters PmcCounters // Custom counter Type: { key: Raids, value: 69 }
    {
        get;
        set;
    } = new();

    public Counters ScavCounters // Custom counter Type: { key: Raids, value: 69 }
    {
        get;
        set;
    } = new();

    public string SurvivorClass { get; set; } = "Unknown";
}
