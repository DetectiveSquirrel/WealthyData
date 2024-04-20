using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace WealthyData;

public static class StashHandler
{
    public static InventoryType GetTypeOfCurrentVisibleStash() =>
        WealthyData.Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.InvType ?? InventoryType.InvalidInventory;

    public static IList<NormalInventoryItem> GetVisibleStashInventory() =>
        WealthyData.Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems.Where(x => !string.IsNullOrWhiteSpace(x.Item.Metadata)).ToList();

    public static bool IsStashPanelOpenCondition()
    {
        return ElementHandler.IsInGameUiElementVisibleCondition(ui => ui.StashElement);
    }

    public static bool IsVisibleStashValidCondition() =>
        GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory;

    public static bool TryGetVisibleStashInventory(out IList<NormalInventoryItem> inventoryItems)
    {
        inventoryItems = IsVisibleStashValidCondition()
            ? GetVisibleStashInventory()
            : null;

        return inventoryItems != null;
    }
}