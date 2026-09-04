namespace Murim.World;

public sealed class ReputationBehaviorSystem
{
    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        LastEvents.Clear();
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.AgeYears >= 18))
        {
            foreach (var relationship in npc.Relationships.Where(r => r.IsActive).ToList())
            {
                if (!world.Npcs.TryGetValue(relationship.ToNpcId, out var other) || !other.IsAlive) continue;
                var reputation = world.Reputation.GetValue(other.Id, "Local");
                var targetAffinity = Math.Clamp(reputation / 100.0 * 0.08, -0.08, 0.08);
                var targetTrust = Math.Clamp(reputation / 100.0 * 0.05, -0.05, 0.05);
                relationship.Shift(targetAffinity, targetTrust, targetTrust * 0.7);

                if (reputation <= -20 && relationship.Type == RelationshipType.Acquaintance && relationship.Trust < 0.12)
                {
                    relationship.Shift(-0.03, -0.04, -0.02);
                    LastEvents.Add($"{npc.Identity.DisplayName} devient méfiant envers {other.Identity.DisplayName}.");
                }
                else if (reputation >= 20 && relationship.Type == RelationshipType.Acquaintance && relationship.Trust > 0.45)
                {
                    relationship.Shift(0.02, 0.03, 0.02);
                    LastEvents.Add($"{npc.Identity.DisplayName} accorde davantage sa confiance à {other.Identity.DisplayName}.");
                }
            }
        }
    }
}
