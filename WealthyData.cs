using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.Necropolis;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

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
                Settings.LastSelectedIndex = Settings.DataSets.Count-1;
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
        ImGui.TableSetupColumn(new StringBuilder()
                .Append("MonsterType (H-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.High))
                .Append(", N-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.Normal))
                .Append(", L-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.Low))
                .Append(", VL-")
                .Append(currentDataset.Packs.Count(x => x.MonsterType == MonsterType.VeryLow))
                .Append(")")
                .ToString(),
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

            var refFloat = pack.PackSizeModifier;
            ImGui.InputFloat("", ref refFloat);
            if (!currentDataset.LockedData)
            {
                pack.PackSizeModifier = refFloat;
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

    private void GetModelLists(NecropolisMonsterPanel necropolisTransitionWindow, out List<PackData> mods)
    {
        var associations = necropolisTransitionWindow.Associations;
        mods = new List<PackData>();
        for (var i = 0; i < associations.Count; i++)
        {
            var t = associations[i];
            var monsterModel = ConvertElementToMonster(associations.ElementAtOrDefault(i));
            if (monsterModel == null) continue;
            var item = new PackData
            {
                AllFlameApplied = monsterModel.Name == "Tumbling Wealth",
                MonsterType = monsterModel.Density,
                PackSizeModifier = monsterModel.PackSize
            };

            mods.Add(item);
        }
    }

    public MonsterModel ConvertElementToMonster(NecropolisMonsterPanelMonsterAssociation element)
    {
        if (element == null) return null;

        var model = new MonsterModel();
        model.MonsterAssociation = element;

        Main.LogMessage($"element.Pack.Name = \"{element.GetChildFromIndices(0, 2, 0).Text}\"", 10);
        model.Name = element.GetChildFromIndices(0, 2, 0).Text;

        if (element.PackFrequency == null)
        {
            Main.GameController.Area.ForceRefreshArea(false);
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
                break;
            }

            if (tooltipText.Contains("50% more Pack Size"))
            {
                model.PackSize = 1.5f;
                break;
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