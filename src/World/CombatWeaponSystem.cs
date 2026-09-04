namespace Murim.World;

public sealed class CombatWeaponSystem
{
    public double WeaponAttackBonus(WorldState world, Npc npc)
    {
        var equipped = npc.Inventory.Entries.FirstOrDefault(e =>
            world.Inventory.Items.TryGetValue(e.ItemId, out var item) &&
            item.Category is ItemCategory.Weapon or ItemCategory.Armor);
        if (equipped is null || !world.Inventory.Items.TryGetValue(equipped.ItemId, out var definition)) return 0;
        var condition = equipped.Durability <= 0 || definition.MaxDurability <= 0 ? 0 : (double)equipped.Durability / definition.MaxDurability;
        return definition.Category == ItemCategory.Weapon ? definition.BaseValue * (0.4 + condition * 0.6) : 0;
    }

    public double DefenseBonus(WorldState world, Npc npc)
    {
        var equipped = npc.Inventory.Entries.FirstOrDefault(e =>
            world.Inventory.Items.TryGetValue(e.ItemId, out var item) && item.Category == ItemCategory.Armor);
        if (equipped is null || !world.Inventory.Items.TryGetValue(equipped.ItemId, out var definition)) return 0;
        var condition = equipped.Durability <= 0 || definition.MaxDurability <= 0 ? 0 : (double)equipped.Durability / definition.MaxDurability;
        return definition.BaseValue * 0.15 * condition;
    }

    public bool DamageEquippedWeapon(WorldState world, Npc npc, int amount = 1)
    {
        var equipped = npc.Inventory.Entries.FirstOrDefault(e =>
            world.Inventory.Items.TryGetValue(e.ItemId, out var item) && item.Category is ItemCategory.Weapon or ItemCategory.Armor);
        if (equipped is null || equipped.Durability <= 0) return false;
        equipped.Durability = Math.Max(0, equipped.Durability - Math.Max(1, amount));
        return true;
    }
}
