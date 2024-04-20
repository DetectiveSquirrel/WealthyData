using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using System;

namespace WealthyData;

public static class ElementHandler
{
    public static bool IsInGameUiElementVisibleCondition(Func<IngameUIElements, Element> panelSelector) =>
        panelSelector(WealthyData.Main.GameController?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;
}