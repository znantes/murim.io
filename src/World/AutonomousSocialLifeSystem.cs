namespace Murim.World;

public sealed class AutonomousSocialLifeSystem
{
    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        LastEvents.Clear();

        var livingAdults = world.Npcs.Values
            .Where(n => n.IsAlive && n.AgeYears >= 18)
            .ToList();

        foreach (var npc in livingAdults)
        {
            var random = DeterministicRandom(world.WorldSeed, npc.Id, world.Time.Day);
            if (random.NextDouble() < 0.08)
                SimulateMovement(world, npc, random);
        }

        CreateAutonomousEncounters(world, livingAdults);
    }

    private void SimulateMovement(WorldState world, Npc npc, Random random)
    {
        var destinations = new[]
        {
            "marché local",
            "lieu de travail",
            "quartier voisin",
            "maison d'un proche",
            "lieu d'étude",
            "lieu de culte"
        };
        var destination = destinations[random.Next(destinations.Length)];
        var duration = 15 + random.Next(46);

        npc.History.Add("Déplacement", npc.AgeYears,
            $"Se rend au {destination} pendant environ {duration} minutes.");
        LastEvents.Add($"{npc.Identity.DisplayName} se déplace vers le {destination}.");
    }

    private void CreateAutonomousEncounters(WorldState world, List<Npc> adults)
    {
        if (adults.Count < 2) return;

        var attempts = Math.Min(adults.Count / 2, 4);
        for (var i = 0; i < attempts; i++)
        {
            var first = adults[(int)((world.Time.Day + i * 17) % adults.Count)];
            var random = DeterministicRandom(world.WorldSeed + 23, first.Id, world.Time.Day + i);
            var second = adults[random.Next(adults.Count)];
            if (first.Id == second.Id) continue;

            var relationship = first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.IsActive);
            if (relationship is null)
            {
                var affinity = -0.10 + random.NextDouble() * 0.40;
                AddBidirectional(first, second, RelationshipType.Acquaintance, affinity, 0.20, 0.15);
                first.History.Add("Rencontre", first.AgeYears, $"Rencontre {second.Identity.DisplayName}.");
                second.History.Add("Rencontre", second.AgeYears, $"Rencontre {first.Identity.DisplayName}.");
                world.Information.Publish(
                    world,
                    first,
                    "Rencontre",
                    $"{first.Identity.DisplayName} a rencontré {second.Identity.DisplayName}.",
                    second.Id,
                    first.CurrentLocationId,
                    InformationReliability.Verified,
                    0);
                world.Information.Publish(
                    world,
                    second,
                    "Rencontre",
                    $"{second.Identity.DisplayName} a rencontré {first.Identity.DisplayName}.",
                    first.Id,
                    second.CurrentLocationId,
                    InformationReliability.Verified,
                    0);
                LastEvents.Add($"Rencontre entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
            }
            else if (relationship.Type == RelationshipType.Acquaintance && random.NextDouble() < 0.12)
            {
                relationship.Shift(0.05, 0.04, 0.03);
                first.History.Add("Lien social", first.AgeYears, $"Le lien avec {second.Identity.DisplayName} se renforce.");
                world.Information.Publish(
                    world,
                    first,
                    "Relation",
                    $"{first.Identity.DisplayName} semble entretenir un lien cordial avec {second.Identity.DisplayName}.",
                    second.Id,
                    first.CurrentLocationId,
                    InformationReliability.Unverified,
                    0.25);
            }
        }
    }

    private static void AddBidirectional(Npc first, Npc second, RelationshipType type, double affinity, double trust, double respect)
    {
        if (!first.Relationships.Any(r => r.ToNpcId == second.Id && r.IsActive))
            first.Relationships.Add(new Relationship { FromNpcId = first.Id, ToNpcId = second.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
        if (!second.Relationships.Any(r => r.ToNpcId == first.Id && r.IsActive))
            second.Relationships.Add(new Relationship { FromNpcId = second.Id, ToNpcId = first.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
    }

    private static Random DeterministicRandom(int worldSeed, Guid npcId, long day)
    {
        var bytes = npcId.ToByteArray();
        var guidHash = BitConverter.ToInt32(bytes, 0) ^ BitConverter.ToInt32(bytes, 8);
        return new Random(HashCode.Combine(worldSeed, guidHash, day));
    }
}
