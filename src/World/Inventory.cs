namespace Murim.World;

public enum ItemCategory
{
    Food,
    Water,
    Medicine,
    Material,
    Tool,
    Weapon,
    Armor,
    Book,
    Artifact,
    Clothing,
    Miscellaneous
}

public sealed class ItemDefinition
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "Objet";
    public ItemCategory Category { get; init; } = ItemCategory.Miscellaneous;
    public double UnitWeightKg { get; init; }
    public double BaseValue { get; init; }
    public double Durability { get; init; } = 1;
}

public sealed class InventoryEntry
{
    public ItemDefinition Item { get; init; } = new();
    public int Quantity { get; set; }
    public double Condition { get; set; } = 1;
}

public sealed class Inventory
{
    public List<InventoryEntry> Items { get; } = new();

    public void Add(ItemDefinition item, int quantity = 1)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var entry = Items.FirstOrDefault(x => x.Item.Id == item.Id);
        if (entry is null) Items.Add(new InventoryEntry { Item = item, Quantity = quantity });
        else entry.Quantity += quantity;
    }

    public bool Remove(Guid itemId, int quantity = 1)
    {
        if (quantity <= 0) return false;
        var entry = Items.FirstOrDefault(x => x.Item.Id == itemId);
        if (entry is null || entry.Quantity < quantity) return false;
        entry.Quantity -= quantity;
        if (entry.Quantity == 0) Items.Remove(entry);
        return true;
    }

    public int Count(Guid itemId) => Items.FirstOrDefault(x => x.Item.Id == itemId)?.Quantity ?? 0;
    public double TotalWeightKg => Items.Sum(x => x.Item.UnitWeightKg * x.Quantity);
    public double EstimatedValue => Items.Sum(x => x.Item.BaseValue * x.Quantity * Math.Clamp(x.Condition, 0, 1));
}
