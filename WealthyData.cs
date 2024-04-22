using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.Necropolis;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;
using GameOffsets;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static ExileCore.PoEMemory.FilesInMemory.ModsDat;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WealthyData;

public class WealthyData : BaseSettingsPlugin<WealthyDataSettings>
{
    public string filterText = "";
    public enum MonsterType
    {
        High = 0,
        Normal = 1,
        Low = 2,
        VeryLow = 3
    }

    public static WealthyData Main;

    public override bool Initialise()
    {
        Main = this;
        return true;
    }

    public override void DrawSettings()
    {
        if (Settings.LastSelectedIndex >= Settings.DataSets.Count)
        {
            Settings.LastSelectedIndex = -1;
        }

        if (ImGui.BeginChild("LeftSettings", new Vector2(180, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border, ImGuiWindowFlags.None))
        {
            if (ImGui.Button("Add Dataset"))
            {
                Settings.DataSets.Add(new DataSet());
                Settings.LastSelectedIndex = Settings.DataSets.Count - 1;
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var refString = filterText;
            ImGui.InputTextWithHint("", "Filter..", ref refString, 1024);
            filterText = refString;

            var allflameCost = Settings.AverageWealthCostInChaos;
            var containmentCost = Settings.AverageContainmentCostInChaos;
            var bestProfitIndex = -1;
            var worstProfitIndex = -1;

            if (Settings.DataSets is {Count: > 0})
            {
                var profitList = Settings.DataSets.Select((data, index) => new
                {
                    Index = index,
                    Profit = data.TotalHistoricalYield - ((Settings.OverrideCosts ? allflameCost : data.WealthCostInChaos ?? allflameCost) * data.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : data.ContainmentCostInChaos ?? containmentCost))
                }).ToList();
                bestProfitIndex = profitList.MaxBy(x => x.Profit)?.Index ?? -1;
                worstProfitIndex = profitList.MinBy(x => x.Profit)?.Index ?? -1;
            }

            if (Settings.DataSets.Count != 0)
            {
                for (var i = 0; i < Settings.DataSets.Count; i++)
                {
                    ImGui.PushID($"{i}_dataSetSelector");

                    string bestOrWorstMarker = "";
                    if (i == bestProfitIndex)
                        bestOrWorstMarker = " [BEST]";
                    else if (i == worstProfitIndex)
                        bestOrWorstMarker = " [WORST]";

                    var label = $"Dataset {(i+1).ToString().PadLeft(Settings.DataSets.Count.ToString().Length, '0')} [{(Settings.DataSets[i].LockedData ? "X" : " ")}]{bestOrWorstMarker}";
                    if (!label.Contains(filterText, StringComparison.CurrentCultureIgnoreCase)) continue;
                    if (ImGui.Selectable(label, Settings.LastSelectedIndex == i))
                    {
                        Settings.LastSelectedIndex = i;
                    }

                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();
        var contentRegionArea = ImGui.GetContentRegionAvail();
        if (ImGui.BeginChild("RightSettings", contentRegionArea, ImGuiChildFlags.Border, ImGuiWindowFlags.None))
        {
            if (Settings.DataSets.Count > Settings.LastSelectedIndex && Settings.LastSelectedIndex > -1)
            {
                DrawGlobalsWidget();

                ReturnsAndCostWidget();

                if (DrawDatasetWidget()) return;

                DrawValuesWidgets();

                ImGui.SeparatorText("Pack Data");
                // Table
                if (ImGui.Button("Add Pack") && !Settings.DataSets[Settings.LastSelectedIndex].LockedData)
                {
                    Settings.DataSets[Settings.LastSelectedIndex].Packs.Add(new PackData());
                }

                if (ImGui.Button("Copy Pack"))
                {
                    var necropolisMonsterPanel = GameController.IngameState.IngameUi.NecropolisMonsterPanel;
                    if (necropolisMonsterPanel is {IsVisible: true} && !Settings.DataSets[Settings.LastSelectedIndex].LockedData)
                    {
                        GetModelLists(necropolisMonsterPanel, out var packs);

                        Settings.DataSets[Settings.LastSelectedIndex].Packs = packs;
                    }
                }

                if (Settings.DataSets[Settings.LastSelectedIndex].Packs.Count <= 0)
                {
                    return;
                }

                if (DrawPackTable()) return;
            }
            else
            {
                ImGui.SeparatorText("No Dataset Selected");
            }
        }

        ImGui.EndChild();
    }

    private bool DrawDatasetWidget()
    {
        ImGui.SeparatorText($"Dataset ({Settings.LastSelectedIndex+1})");
        var refCheck = Settings.DataSets[Settings.LastSelectedIndex].LockedData;
        if (ImGui.Checkbox("Lock Dataset", ref refCheck))
        {
            Settings.DataSets[Settings.LastSelectedIndex].LockedData = !Settings.DataSets[Settings.LastSelectedIndex].LockedData;
        }

        ImGui.SameLine(0, 200);
        if (ImGui.Button($"{(Settings.DataSets[Settings.LastSelectedIndex].LockedData ? "Unlock to Dataset" : "Delete This Dataset")}") && !Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets.RemoveAt(Settings.LastSelectedIndex);
            Settings.LastSelectedIndex--;
            return true;
        }

        if (ImGui.Button($"Set Current Allflame & Containment Prices {(Settings.DataSets[Settings.LastSelectedIndex].LockedData ? "[Unlock to apply]" : string.Empty)}") &&
            !Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].WealthCostInChaos = Settings.AverageWealthCostInChaos;
            Settings.DataSets[Settings.LastSelectedIndex].ContainmentCostInChaos = Settings.AverageContainmentCostInChaos;
        }

        var allflameCost = Settings.AverageWealthCostInChaos;
        var containmentCost = Settings.AverageContainmentCostInChaos;
        var currentDataset = Settings.DataSets[Settings.LastSelectedIndex];
        var costValue = (Settings.OverrideCosts ? allflameCost : currentDataset.WealthCostInChaos ?? allflameCost) * currentDataset.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : currentDataset.ContainmentCostInChaos ?? containmentCost);
        var currentDatasetProfit = currentDataset.TotalHistoricalYield - costValue;

        if (ImGui.BeginTable("CurrentDatasetTable", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Data Type");
            ImGui.TableSetupColumn("Amount");
            ImGui.TableSetupColumn("Value Type");
            ImGui.TableHeadersRow();

            // Text Column
            ImGui.TableNextColumn();

            if (currentDataset.WealthCostInChaos != null)
            {
                ImGui.Text("Snapshot Wealth Cost");
            }

            if (currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.Text("Snapshot Containment Cost");
            }

            if (currentDataset.WealthCostInChaos != null || currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.NewLine();
            }

            ImGui.Text("Dataset Cost");
            ImGui.Text("Dataset Yield");
            ImGui.Text("Dataset Profit / Loss");

            ImGui.TableNextColumn();

            if (currentDataset.WealthCostInChaos != null)
            {
                ImGui.Text(currentDataset.WealthCostInChaos.ToString());
            }

            if (currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.Text(currentDataset.ContainmentCostInChaos.ToString());
            }

            if (currentDataset.WealthCostInChaos != null || currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.NewLine();
            }

            ImGui.Text($"{costValue:#,#}");
            ImGui.Text($"{currentDataset.TotalHistoricalYield:#,#}");
            ImGui.Text($"{currentDatasetProfit:#,#}");

            ImGui.TableNextColumn();

            if (currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.Text("Chaos");
            }

            if (currentDataset.WealthCostInChaos != null)
            {
                ImGui.Text("Chaos");
            }

            if (currentDataset.WealthCostInChaos != null || currentDataset.ContainmentCostInChaos != null)
            {
                ImGui.NewLine();
            }

            ImGui.Text("Chaos");
            ImGui.Text("Chaos");
            ImGui.Text("Chaos");

            ImGui.EndTable();
        }

        ImGui.NewLine();

        return false;
    }

    private void DrawGlobalsWidget()
    {
        ImGui.SeparatorText("Globals");
        var regionWidth = ImGui.GetContentRegionAvail().X / 3;

        ImGui.SetNextItemWidth(regionWidth);
        var refInt = Settings.DevotionModPctBonus;
        ImGui.InputInt("Global Devotion Bonus %", ref refInt);
        Settings.DevotionModPctBonus = refInt;

        ImGui.SetNextItemWidth(regionWidth);
        refInt = Settings.AverageWealthCostInChaos;
        ImGui.InputInt("Average cost of Allflame of Wealth", ref refInt);
        Settings.AverageWealthCostInChaos = refInt;

        ImGui.SetNextItemWidth(regionWidth);
        refInt = Settings.AverageContainmentCostInChaos;
        ImGui.InputInt("Average cost of Scarab of Containment", ref refInt);
        Settings.AverageContainmentCostInChaos = refInt;

        var refBool = Settings.OverrideCosts;
        if (ImGui.Checkbox("Global Override of Dataset Item Costs", ref refBool))
        {
            Settings.OverrideCosts = !Settings.OverrideCosts;
        }
        ImGui.NewLine();
    }

    private void ReturnsAndCostWidget()
    {
        ImGui.SeparatorText("Returns / Cost");

        var allflameCost = Settings.AverageWealthCostInChaos;
        var containmentCost = Settings.AverageContainmentCostInChaos;
        var costValue = Settings.DataSets.Sum(item => (Settings.OverrideCosts ? allflameCost : item.WealthCostInChaos ?? allflameCost) * item.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : item.ContainmentCostInChaos ?? containmentCost));
        var analysis = new MetricsCalculator();
        var meanGain = analysis.ProcessData("mean",
            Settings.DataSets,
            item => item.TotalHistoricalYield - ((Settings.OverrideCosts ? allflameCost : item.WealthCostInChaos ?? allflameCost) * item.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : item.ContainmentCostInChaos ?? containmentCost)));

        var medianGain = analysis.ProcessData("median",
            Settings.DataSets,
            item => item.TotalHistoricalYield - ((Settings.OverrideCosts ? allflameCost : item.WealthCostInChaos ?? allflameCost) * item.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : item.ContainmentCostInChaos ?? containmentCost)));

        var modeGain = analysis.ProcessData("mode",
            Settings.DataSets,
            item => item.TotalHistoricalYield - ((Settings.OverrideCosts ? allflameCost : item.WealthCostInChaos ?? allflameCost) * item.Packs.Count(x => x.AllFlameApplied) + (Settings.OverrideCosts ? containmentCost : item.ContainmentCostInChaos ?? containmentCost)));

        var historicalReturnValue = Settings.DataSets.Sum(item => item.TotalHistoricalYield);

        if (ImGui.BeginTable("ProfitReturnsTable", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Data Type");
            ImGui.TableSetupColumn("Amount");
            ImGui.TableSetupColumn("Value Type");
            ImGui.TableHeadersRow();

            // Text Column
            ImGui.TableNextColumn();

            ImGui.Text("Total Dataset Cost");
            ImGui.Text("Total Dataset Yield");
            ImGui.Text("Total Profit / Loss");
            ImGui.NewLine();
            ImGui.Text("Gains Mean");
            ImGui.Text("Gains Median");
            ImGui.Text("Gains Mode");

            ImGui.TableNextColumn();

            ImGui.Text($"{costValue:#,#}");
            ImGui.Text($"{historicalReturnValue:#,#}");
            ImGui.Text($"{historicalReturnValue - costValue:#,#}");
            ImGui.NewLine();
            ImGui.Text($"{meanGain:#,#}");
            ImGui.Text($"{medianGain:#,#}");
            ImGui.Text($"{modeGain:#,#}");

            ImGui.TableNextColumn();

            ImGui.Text("Chaos");
            ImGui.Text("Chaos");
            ImGui.Text("Chaos");
            ImGui.NewLine();
            ImGui.Text("Chaos");
            ImGui.Text("Chaos");
            ImGui.Text("Chaos");

            ImGui.EndTable();
        }

        ImGui.NewLine();
    }

    private bool DrawPackTable()
    {
        if (!ImGui.BeginTable("WeightingTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            return true;
        }

        var currentDataset = Settings.DataSets[Settings.LastSelectedIndex];

        ImGui.TableSetupColumn($"({currentDataset.Packs.Count})");
        ImGui.TableSetupColumn("Pack Size", ImGuiTableColumnFlags.None, 150);
        ImGui.TableSetupColumn($"All Flames Applied ({currentDataset.Packs.Count(x => x.AllFlameApplied)})");
        ImGui.TableSetupColumn(new StringBuilder().Append("MonsterType (H-").Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.High)).Append(", N-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.Normal)).Append(", L-").Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.Low)).Append(", VL-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.VeryLow)).Append(")").ToString(),
            ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableHeadersRow();

        var deletePackIndex = -1;
        // Weighting column
        ImGui.TableNextColumn();
        for (var i = 0; i < currentDataset.Packs.Count; i++)
        {
            ImGui.PushID($"{i}_packColumn");
            var pack = currentDataset.Packs[i];
            if (ImGui.Button("Delete") && !currentDataset.LockedData)
            {
                deletePackIndex = i;
            }

            ImGui.TableNextColumn();

            var usableSpace = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(usableSpace.X);

            var reftDouble = pack.PackSizeModifier;
            ImGui.InputDouble("", ref reftDouble);
            if (!currentDataset.LockedData)
            {
                pack.PackSizeModifier = reftDouble;
            }

            ImGui.TableNextColumn();

            var enabled = "Wealthy           ";
            var disabled = "Poor";
            var label = pack.AllFlameApplied
                ? enabled
                : disabled.PadRight(enabled.Length);

            if (ImGui.Button($"{label}") && !currentDataset.LockedData)
            {
                pack.AllFlameApplied = !pack.AllFlameApplied;
            }

            ImGui.TableNextColumn();

            usableSpace = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(usableSpace.X);

            var refPackType = (int)pack.MonsterType;

            if (ImGui.Combo("##", ref refPackType, Enum.GetNames(typeof(MonsterType)), GetEnumLength<MonsterType>()) && !currentDataset.LockedData)
            {
                pack.MonsterType = (MonsterType)refPackType;
            }

            if (i < currentDataset.Packs.Count - 1)
            {
                ImGui.TableNextColumn();
            }

            ImGui.PopID();
        }

        ImGui.EndTable();
        if (deletePackIndex != -1)
        {
            currentDataset.Packs.RemoveAt(deletePackIndex);
        }

        return false;
    }

    private void DrawValuesWidgets()
    {
        ImGui.SeparatorText("Values");
        var regionWidth = ImGui.GetContentRegionAvail().X / 3;
        if (ImGui.Button("Copy Stash Tab Totals"))
        {
            if (GetStashValue(out var tabValue, out var chaosOrbs, out var exaltedOrbs, out var vaalOrbs))
            {
                if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
                {
                    Settings.DataSets[Settings.LastSelectedIndex].TotalHistoricalYield = tabValue;
                    Settings.DataSets[Settings.LastSelectedIndex].TotalChaos = chaosOrbs;
                    Settings.DataSets[Settings.LastSelectedIndex].TotalExalt = exaltedOrbs;
                    Settings.DataSets[Settings.LastSelectedIndex].TotalVaal = vaalOrbs;
                }
            }
        }

        ImGui.SetNextItemWidth(regionWidth);
        var refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalStrongboxes;
        ImGui.InputInt("Strongboxes", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalStrongboxes = refInt;
        }

        ImGui.SetNextItemWidth(regionWidth);
        var refDouble = Settings.DataSets[Settings.LastSelectedIndex].TotalHistoricalYield;
        ImGui.InputDouble("Total Historical Yield", ref refDouble);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalHistoricalYield = refDouble;
        }

        ImGui.SetNextItemWidth(regionWidth);
        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalChaos;
        ImGui.InputInt("Total Chaos", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalChaos = refInt;
        }

        ImGui.SetNextItemWidth(regionWidth);
        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalExalt;
        ImGui.InputInt("Total Exalt", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalExalt = refInt;
        }

        ImGui.SetNextItemWidth(regionWidth);
        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalVaal;
        ImGui.InputInt("Total Vaal", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalVaal = refInt;
        }

        ImGui.NewLine();
    }

    public static IReadOnlyDictionary<GameStat, int> SumModStats(IEnumerable<ModRecord> mods)
    {
        return new DefaultDictionary<GameStat, int>(mods.SelectMany(x => x.StatNames.Zip(x.StatRange, (name, value) => (name.MatchingStat, value)))
                .GroupBy(x => x.MatchingStat, x => x.value.Min, (stat, values) => (stat, values.Sum())).ToDictionary(x => x.stat, x => x.Item2),
            0);
    }

    private void GetModelLists(NecropolisMonsterPanel necropolisTransitionWindow, out List<PackData> mods)
    {
        var associations = necropolisTransitionWindow.AssociationsWithMods;
        mods = new List<PackData>();
        for (var i = 0; i < associations.Count; i++)
        {
            var monsterModel = ConvertElementToMonster(associations.ElementAtOrDefault(i).Association);

            if (monsterModel == null) continue;
            var item = new PackData
            {
                AllFlameApplied = monsterModel.Name == "Tumbling Wealth",
                MonsterType = monsterModel.Density,
                PackSizeModifier = CalculatePackSize(SumModStats(associations.ElementAtOrDefault(i).Mod != null
                        ? new List<ModRecord> {GetDiffTieredMod(associations.ElementAtOrDefault(i).Mod, monsterModel.TierChange)}
                        : new List<ModRecord>())[GameStat.PackSizePct],
                    Settings.DevotionModPctBonus,
                    monsterModel.PackSize)
            };

            mods.Add(item);
        }
    }

    public ModRecord GetDiffTieredMod(ModRecord modKey, int tierChange)
    {
        var necropolisPackMods = GameController.Files.NecropolisPackMods;
        if (necropolisPackMods == null || necropolisPackMods.Address == 0 || !necropolisPackMods.EntriesList.Any())
        {
            Main.GameController.Game.ReloadFiles();
        }

        var modRecord = necropolisPackMods?.EntriesList.FirstOrDefault(x => x.Mod.Key == modKey.Key);
        if (modRecord == null)
        {
            return modKey;
        }

        return tierChange switch
        {
            -1 => GetLowerTierMod(modRecord),
            1 => GetHigherTierMod(modRecord),
            0 => modRecord.Mod,
            _ => modRecord.Mod
        };
    }

    private ModRecord GetHigherTierMod(NecropolisPackMod modRecord)
    {
        var upgradeMod = modRecord.Upgrade?.Mod;
        if (upgradeMod != null)
        {
            LogMessage($"Grabbed Higher tier mod for {modRecord.Mod.Key} => {upgradeMod.Key}");
            return upgradeMod;
        }

        LogMessage($"{modRecord.Mod.Key} is the highest tier version");
        return modRecord.Mod;
    }

    private ModRecord GetLowerTierMod(NecropolisPackMod modRecord)
    {
        var downgradeMod = modRecord.Downgrade?.Mod;
        if (downgradeMod != null)
        {
            LogMessage($"Grabbed Lower tier mod for {modRecord.Mod.Key} => {downgradeMod.Key}");
            return downgradeMod;
        }

        LogMessage($"{modRecord.Mod.Key} is the lowest tier version");
        return modRecord.Mod;
    }

    public double CalculatePackSize(double increasedPct, int devotionBonus, double moreOrLessPct) => 1.0 * moreOrLessPct * (1 + 0.01 * increasedPct * (1 + 0.01 * devotionBonus));

    public MonsterModel ConvertElementToMonster(NecropolisMonsterPanelMonsterAssociation element)
    {
        if (element == null) return null;

        var model = new MonsterModel();
        model.MonsterAssociation = element;

        //Main.LogMessage($"element.Pack.Name = \"{element.GetChildFromIndices(0, 2, 0).Text}\"", 10);
        model.Name = element.GetChildFromIndices(0, 2, 0).Text;

        if (element.PackFrequency == null)
        {
            Main.GameController.Game.ReloadFiles();
        }

        var packFrequencyId = element.PackFrequency?.Id;
        model.Density = MonsterModel.MonsterDensityFromId(packFrequencyId ?? null);

        var monsterModifiers = element.MonsterPortrait.GetChildAtIndex(1);
        if (monsterModifiers is not {ChildCount: > 0}) return model;

        foreach (var child in monsterModifiers.Children)
        {
            var tooltipText = child.Tooltip?.Text ?? null;

            if (tooltipText == null) continue;
            if (tooltipText.Contains("50% less Pack Size", StringComparison.CurrentCultureIgnoreCase))
            {
                model.PackSize = 0.5f;
            }
            else if (tooltipText.Contains("50% more Pack Size", StringComparison.CurrentCultureIgnoreCase))
            {
                model.PackSize = 1.5f;
            }
            else if (tooltipText.Contains("+1", StringComparison.CurrentCultureIgnoreCase))
            {
                model.TierChange = 1;
            }
            else if (tooltipText.Contains("-1", StringComparison.CurrentCultureIgnoreCase))
            {
                model.TierChange = -1;
            }
        }

        return model;
    }

    public static bool GetStashValue(out double TabValue, out int ChaosOrbs, out int ExaltedOrbs, out int VaalOrbs)
    {
        TabValue = 0.0;
        ChaosOrbs = 0;
        ExaltedOrbs = 0;
        VaalOrbs = 0;

        if (!StashHandler.TryGetVisibleStashInventory(out var itemList))
        {
            return false;
        }

        foreach (var item in itemList)
        {
            var currentStackSize = item.Item.TryGetComponent<Stack>(out var stackComponent)
                ? stackComponent.Size
                : 1;

            switch (item.Item.Metadata)
            {
                case "Metadata/Items/Currency/CurrencyRerollRare":
                    ChaosOrbs += currentStackSize;
                    break;
                case "Metadata/Items/Currency/CurrencyAddModToRare":
                    ExaltedOrbs += currentStackSize;
                    break;
                case "Metadata/Items/Currency/CurrencyCorrupt":
                    VaalOrbs += currentStackSize;
                    break;
            }

            TabValue += NinjaPricer.GetValue(item.Item, false);
        }

        return true;
    }

    private static int GetEnumLength<T>() => Enum.GetNames(typeof(T)).Length;
}