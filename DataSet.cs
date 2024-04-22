using System;
using System.Collections.Generic;
using static WealthyData.WealthyData;

namespace WealthyData;

public class DataSet
{
    public bool LockedData { get; set; } = false;
    public int TotalStrongboxes { get; set; } = 0;
    public double TotalHistoricalYield { get; set; } = 0.0;
    public int TotalChaos { get; set; } = 0;
    public int TotalExalt { get; set; } = 0;
    public int TotalVaal { get; set; } = 0;
    public int? WealthCostInChaos { get; set; } = null;
    public int? ContainmentCostInChaos { get; set; } = null;
    public List<PackData> Packs { get; set; } = [];
}

public class PackData
{
    public MonsterType MonsterType { get; set; } = MonsterType.Normal;
    public bool AllFlameApplied { get; set; } = false;
    public double PackSizeModifier { get; set; } = 1.0;
}