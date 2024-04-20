using ExileCore.PoEMemory.Elements.Necropolis;
using System;

namespace WealthyData;

public class MonsterModel
{
    public string Name { get; set; } = "NoName";
    public float PackSize { get; set; } = 1.0f;
    public WealthyData.MonsterType Density { get; set; } = WealthyData.MonsterType.Normal;

    public NecropolisMonsterPanelMonsterAssociation MonsterAssociation { get; set; }

    public static WealthyData.MonsterType MonsterDensityFromId(string str)
    {
        WealthyData.Main.LogMessage($"MonsterDensityFromId = \"{str}\"", 10);
        if (str != null)
        {
            str = str.ToLower();

            if ("rare".Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                return WealthyData.MonsterType.VeryLow;
            }

            if ("uncommon".Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                return WealthyData.MonsterType.Low;
            }

            if ("common".Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                return WealthyData.MonsterType.Normal;
            }

            if ("verycommon".Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                return WealthyData.MonsterType.High;
            }
        }

        // Lets assume this monster has a normal pack density?
        return WealthyData.MonsterType.Normal;
    }
}