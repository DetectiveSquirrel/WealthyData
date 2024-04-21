﻿using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.Necropolis;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static ExileCore.PoEMemory.FilesInMemory.ModsDat;

namespace WealthyData;

public class WealthyData : BaseSettingsPlugin<WealthyDataSettings>
{
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

        if (ImGui.BeginChild("LeftSettings", new Vector2(150, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border, ImGuiWindowFlags.None))
        {
            if (ImGui.Button("Add Dataset"))
            {
                Settings.DataSets.Add(new DataSet());
                Settings.LastSelectedIndex = Settings.DataSets.Count - 1;
            }

            if (Settings.DataSets.Count != 0)
            {
                for (var i = 0; i < Settings.DataSets.Count; i++)
                {
                    ImGui.PushID($"{i}_dataSetSelector");
                    if (ImGui.Selectable($"Data Set {i}", Settings.LastSelectedIndex == i))
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
                ImGui.SeparatorText("Globals");
                var refDevotionBonus = Settings.DevotionModPctBonus;
                ImGui.InputInt("Global Devotion Bonus %", ref refDevotionBonus);
                Settings.DevotionModPctBonus = refDevotionBonus;

                ImGui.SeparatorText("Data Set");
                var refCheck = Settings.DataSets[Settings.LastSelectedIndex].LockedData;
                if (ImGui.Checkbox("Lock Dataset", ref refCheck))
                {
                    Settings.DataSets[Settings.LastSelectedIndex].LockedData = !Settings.DataSets[Settings.LastSelectedIndex].LockedData;
                }

                ImGui.SameLine(0, 200);
                if (ImGui.Button("Delete This Dataset") && !Settings.DataSets[Settings.LastSelectedIndex].LockedData)
                {
                    Settings.DataSets.RemoveAt(Settings.LastSelectedIndex);
                    Settings.LastSelectedIndex--;
                    return;
                }

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

        var refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalStrongboxes;
        ImGui.InputInt("Strongboxes", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalStrongboxes = refInt;
        }

        var refDouble = Settings.DataSets[Settings.LastSelectedIndex].TotalHistoricalYield;
        ImGui.InputDouble("Total Historical Yield", ref refDouble);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalHistoricalYield = refDouble;
        }

        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalChaos;
        ImGui.InputInt("Total Chaos", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalChaos = refInt;
        }

        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalExalt;
        ImGui.InputInt("Total Exalt", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalExalt = refInt;
        }

        refInt = Settings.DataSets[Settings.LastSelectedIndex].TotalVaal;
        ImGui.InputInt("Total Vaal", ref refInt);
        if (!Settings.DataSets[Settings.LastSelectedIndex].LockedData)
        {
            Settings.DataSets[Settings.LastSelectedIndex].TotalVaal = refInt;
        }
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

            LogMessage($"associations.ElementAtOrDefault({i})");
            if (monsterModel == null) continue;
            var item = new PackData
            {
                AllFlameApplied = monsterModel.Name == "Tumbling Wealth",
                MonsterType = monsterModel.Density,
                PackSizeModifier = CalculatePackSize(SumModStats(associations.ElementAtOrDefault(i).Mod != null
                        ? [GetDiffTieredMod(associations.ElementAtOrDefault(i).Mod, monsterModel.TierChange)]
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
            -1 => GetLowerTierMod(necropolisPackMods, modRecord),
            1 => modRecord.Upgrade?.Mod ?? modRecord.Mod,
            0 => modRecord.Mod,
            _ => modRecord.Mod
        };
    }

    private ModRecord GetLowerTierMod(UniversalFileWrapper<NecropolisPackMod> necroPackMods, NecropolisPackMod currentModRecord)
    {
        var lowestTierId = GetLowestTierId(necroPackMods, currentModRecord.Mod.Group);
        if (!int.TryParse(currentModRecord.Tier.Id, out var currentModValue) || currentModValue == lowestTierId)
        {
            return currentModRecord.Mod;
        }

        var desiredModValue = currentModValue - 1;
        foreach (var modEntry in necroPackMods.EntriesList)
        {
            if (modEntry.Mod.Key != currentModRecord.Mod.Key && modEntry.Mod.Group == currentModRecord.Mod.Group && int.TryParse(modEntry.Tier.Id, out var newModValue) &&
                newModValue == desiredModValue)
            {
                LogMessage($"Grabbed Lower tier mod for {currentModRecord.Mod.Key} => {modEntry.Mod.Key}");
                return modEntry.Mod;
            }
        }

        return currentModRecord.Mod;
    }

    private int GetLowestTierId(UniversalFileWrapper<NecropolisPackMod> necroPackMods, string group)
    {
        return necroPackMods.EntriesList.Where(m => m.Mod.Group == group).Select(m => int.TryParse(m.Tier.Id, out var tierId)
            ? tierId
            : int.MaxValue).Min();
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
            else if (tooltipText.Contains("50% more Pack Size"))
            {
                model.PackSize = 1.5f;
            }
            else if (tooltipText.Contains("+1"))
            {
                model.TierChange = 1;
            }
            else if (tooltipText.Contains("-1"))
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