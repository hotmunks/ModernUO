using System;
using Server.Mobiles;

namespace Server;

public static class BossLoot
{
    // Loot items never carry more than five random properties.
    private const int MaxProperties = 5;

    public static int MinProperties { get; private set; }

    public static int MinIntensity { get; private set; }

    public static int PackMultiplier { get; private set; }

    public static void Configure()
    {
        MinProperties = Math.Clamp(ServerConfiguration.GetOrUpdateSetting("loot.boss.minProperties", 0), 0, MaxProperties);
        MinIntensity = Math.Clamp(ServerConfiguration.GetOrUpdateSetting("loot.boss.minIntensity", 0), 0, 100);
        PackMultiplier = Math.Max(ServerConfiguration.GetOrUpdateSetting("loot.boss.packMultiplier", 1), 1);
    }

    public static bool IsBoss(Mobile m) => m is BaseChampion or Harrower or HarrowerTentacles;
}
