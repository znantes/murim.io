namespace Murim.World;

public enum ItemCategory
{
    Food,
    Medicine,
    Material,
    Clothing,
    Tool,
    Weapon,
    Armor,
    Book,
    Currency,
    Artifact,
    Miscellaneous
}

public sealed class ItemDefinition
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "Objet inconnu";
    public ItemCategory Category { get; init; }
    public double WeightKg { get; init; }
    public double BaseValue { get; init; }
    public bool Consumable { get; init; }
    public int MaxDurability { get; init; }
}

public sealed class InventoryEntry
{
    public Guid ItemId { get; init; }
    public int Quantity { get; set; }
    public int Durability { get; set; }
}

public sealed class Inventory
{
    public List<InventoryEntry> Entries { get; } = new();

    public void Add(ItemDefinition item, int quantity = 1)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var entry = Entries.FirstOrDefault(e => e.ItemId == item.Id);
        if (entry is null)
        {
            Entries.Add(new InventoryEntry { ItemId = item.Id, Quantity = quantity, Durability = item.MaxDurability });
            return;
        }
        entry.Quantity += quantity;
    }

    public bool Remove(Guid itemId, int quantity = 1)
    {
        if (quantity <= 0) return false;
        var entry = Entries.FirstOrDefault(e => e.ItemId == itemId);
        if (entry is null || entry.Quantity < quantity) return false;
        entry.Quantity -= quantity;
        if (entry.Quantity == 0) Entries.Remove(entry);
        return true;
    }
}

public sealed class InventorySystem
{
    private readonly Dictionary<Guid, ItemDefinition> _items = new();
    public IReadOnlyDictionary<Guid, ItemDefinition> Items => _items;

    public ItemDefinition Register(string name, ItemCategory category, double weightKg, double baseValue, bool consumable = false, int maxDurability = 0)
    {
        var item = new ItemDefinition
        {
            Name = name,
            Category = category,
            WeightKg = Math.Max(0, weightKg),
            BaseValue = Math.Max(0, baseValue),
            Consumable = consumable,
            MaxDurability = Math.Max(0, maxDurability)
        };
        _items[item.Id] = item;
        return item;
    }

    public double WeightOf(Inventory inventory) => inventory.Entries.Sum(e => _items.TryGetValue(e.ItemId, out var item) ? item.WeightKg * e.Quantity : 0);
    public double ValueOf(Inventory inventory) => inventory.Entries.Sum(e => _items.TryGetValue(e.ItemId, out var item) ? item.BaseValue * e.Quantity : 0);

    public ItemDefinition? FindByName(string name) => _items.Values.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
}
