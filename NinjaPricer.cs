using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using System;
using System.Threading;

namespace WealthyData;

internal class NinjaPricer
{
    public static double GetValue(Entity item, bool returnSinglePrice = true)
    {
        var _estimatedValue = new Lazy<double>(() =>
            {
                var value = WealthyData.Main.GameController.PluginBridge.GetMethod<Func<Entity, double>>("NinjaPrice.GetValue")?.Invoke(item);

                return value ?? 0;
            },
            LazyThreadSafetyMode.PublicationOnly);

        if (returnSinglePrice)
        {
            item.TryGetComponent<Stack>(out var stackComp);
            if (stackComp != null)
            {
                return _estimatedValue.Value / stackComp.Size;
            }
        }

        return _estimatedValue.Value;
    }
}