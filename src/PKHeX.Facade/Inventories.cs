using System.Collections.Immutable;
using PKHeX.Core;

namespace PKHeX.Facade;

public class Inventories
{
    private readonly Game _game;
    private readonly PlayerBag _bag;

    public Inventories(Game game)
    {
        _game = game;
        _bag = _game.SaveFile.Inventory;

        InventoryTypes = GetInventoryTypes();
        InventoryItems = GetInventories();
    }

    public Inventory this[string key]
    {
        get
        {
            if (InventoryItems.ContainsKey(key))
            {
                return InventoryItems[key];
            }
            else
            {
                throw new KeyNotFoundException($"The inventory '{key}' does not exist.");
            }
        }
    }


    public ImmutableHashSet<string> InventoryTypes { get; init; }
    public ImmutableDictionary<string, Inventory> InventoryItems { get; init; }

    private ImmutableHashSet<string> GetInventoryTypes()
        => _bag.Pouches.Select(i => i.Type.ToString()).ToImmutableHashSet();

    private ImmutableDictionary<string, Inventory> GetInventories() => InventoryTypes.ToImmutableDictionary(
        type => type,
        type => new Inventory(type, _game, _bag)
    );
}

public static class InventoriesExtensions
{
    public static IEnumerable<Inventory.Item> AllExceptNone(this Inventory inventory)
    {
        return inventory.Where(i => !i.IsNone);
    }
}