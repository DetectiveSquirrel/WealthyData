using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Collections.Generic;

namespace WealthyData;

public class WealthyDataSettings : ISettings
{
    public List<DataSet> DataSets = new(new List<DataSet>());
    public int LastSelectedIndex { get; set; } = -1;
    public ToggleNode Enable { get; set; } = new(false);
}