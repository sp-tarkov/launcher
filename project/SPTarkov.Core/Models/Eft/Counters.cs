namespace SPTarkov.Core.Models;

// Each stat shows something similar to what is expected to be displayed
public class Counters
{
    // 2
    public string Raids { get; set; }

    // 2
    public string Survived { get; set; }

    // 2
    public string Kia { get; set; }

    // 2
    public string Mia { get; set; }

    // 2
    public string Awol { get; set; }

    // 2
    public string Kills { get; set; }

    // 0H00M
    public string Online { get; set; }

    // 2
    public string RunThrough { get; set; }

    // 100%
    public string SurvivalRate { get; set; }

    // 00:00
    public string AverageLifeSpan { get; set; }

    // 2
    public string SurvivalsInARow { get; set; }

    // 0%
    public string LeaveRate { get; set; }

    // 2
    public string KillDeath { get; set; }

    // 0H00M
    public string AccountLifetime { get; set; }
}
