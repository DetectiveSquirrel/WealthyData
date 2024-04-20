using System.Collections.Generic;
using static WealthyData.WealthyData;

namespace WealthyData;

public class DataSet
{
    public bool LockedData { get; set; } = false;
    public int TotalStrongboxes { get; set; }
    public double TotalHistoricalYield { get; set; }
    public int TotalChaos { get; set; }
    public int TotalExalt { get; set; }
    public int TotalVaal { get; set; }
    public List<PackData> Packs { get; set; } = [];
}

public class PackData
{
    public MonsterType MonsterType { get; set; } = MonsterType.Normal;
    public bool AllFlameApplied { get; set; }
    public float PackSizeModifier { get; set; } = 1.0f;
}