using System;
using System.Collections.Generic;
using System.Linq;

namespace WealthyData;

public class MetricsCalculator
{
    public static double CalculateStats(List<double> values, string statType)
    {
        switch (statType.ToLower())
        {
            case "mean":
            case "average":
                return values.Average();
            case "median":
                values.Sort();
                var midIndex = values.Count / 2;
                if (values.Count % 2 == 0)
                {
                    return (values[midIndex - 1] + values[midIndex]) / 2.0;
                }

                return values[midIndex];
            case "mode":
                return values.GroupBy(n => n).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
            default:
                return 0;
        }
    }

    public double ProcessData(string statType, List<DataSet> dataSets)
    {
        var extractedData = dataSets.Select(item => item.TotalHistoricalYield).ToList();

        return CalculateStats(extractedData, statType);
    }

    public double ProcessData(string statType, List<DataSet> dataSets, Func<DataSet, double> dataSelector)
    {
        var extractedData = dataSets.Select(dataSelector).ToList();
        return CalculateStats(extractedData, statType);
    }
}