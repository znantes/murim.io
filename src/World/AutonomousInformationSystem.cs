namespace Murim.World;

/// <summary>
/// Lets autonomous NPCs transmit small pieces of information they personally
/// observed. Trust affects how reliable the transmitted information remains.
/// </summary>
public sealed class AutonomousInformationSystem
{
    private readonly HashSet<long> _processedDays = new();

    public void AdvanceDay(WorldState world)
    {
        if (!_processedDays.Add(world.Time.Day))
            return;

        foreach (var source in world.Npcs.Values.Where(n => n.IsAlive && n.CurrentLocationId is not null).OrderBy(n => n.Id))
        {
            var candidates = world.Npcs.Values
                .Where(n => n.IsAlive && n.Id != source.Id && n.CurrentLocationId == source.CurrentLocationId)
                .OrderBy(n => n.Id)
                .Take(6)
                .ToArray();

            if (candidates.Length == 0)
                continue;

            var random = new Random(DeterministicRandomSeed.Create(world.WorldSeed + 211, world.Time.Day, source.Id));
            if (random.NextDouble() >= Math.Clamp(0.12 + source.Personality.Sociability * 0.30, 0.05, 0.45))
                continue;

            var target = candidates[random.Next(candidates.Length)];
            var known = FindBestInformation(world, source, target);

            if (known is not null)
            {
                world.Information.Spread(world, source, target, known);
                source.History.Add("Rumeur", source.AgeYears, $"Transmet une information à {target.Identity.DisplayName}.");
                target.History.Add("Rumeur", target.AgeYears, $"Entend une information transmise par {source.Identity.DisplayName}.");
                continue;
            }

            var usefulPerson = FindUsefulPerson(world, source, target);
            if (usefulPerson is not null && usefulPerson.CurrentLocationId is Guid usefulLocationId && world.Geography.Locations.TryGetValue(usefulLocationId, out var usefulLocation))
            {
                var personInfo = world.Information.Publish(
                    world,
                    source,
                    "Personne",
                    $"{source.Identity.DisplayName} connaît {usefulPerson.Identity.DisplayName} à {usefulLocation.Name} et sait qu'il peut être utile.",
                    subjectNpcId: usefulPerson.Id,
                    locationId: usefulLocationId,
                    reliability: InformationReliability.Verified);
                world.Information.Spread(world, source, target, personInfo);
                source.History.Add("Information", source.AgeYears, $"Indique à {target.Identity.DisplayName} une personne utile : {usefulPerson.Identity.DisplayName}.");
                target.History.Add("Information", target.AgeYears, $"Apprend l'existence de {usefulPerson.Identity.DisplayName} grâce à {source.Identity.DisplayName}.");
                continue;
            }

            var locationId = FindUsefulLocation(world, source, target);
            if (locationId == Guid.Empty || !world.Geography.Locations.TryGetValue(locationId, out var location))
                continue;

            var locationInfo = world.Information.Publish(
                world,
                source,
                "Lieu",
                $"{source.Identity.DisplayName} connaît {location.Name} et peut indiquer comment y aller.",
                locationId: locationId,
                reliability: InformationReliability.Verified);
            world.Information.Spread(world, source, target, locationInfo);
            source.History.Add("Information", source.AgeYears, $"Indique à {target.Identity.DisplayName} comment rejoindre {location.Name}.");
            target.History.Add("Information", target.AgeYears, $"Apprend l'existence de {location.Name} grâce à {source.Identity.DisplayName}.");
        }
    }

    private static InformationItem? FindBestInformation(WorldState world, Npc source, Npc target)
    {
        var urgentNeed = target.Needs.Thirst >= 85 || target.Needs.Hunger >= 85 || target.Conditions.Any(c => c.Treatable && c.Severity >= 0.55);
        return world.Information.HeardBy(source)
            .Where(i => !i.HeardByNpcIds.Contains(target.Id) && i.SubjectNpcId != source.Id)
            .OrderByDescending(i => InformationPriority(world, source, target, i, urgentNeed))
            .ThenByDescending(i => i.CreatedDay)
            .ThenBy(i => i.Id)
            .FirstOrDefault();
    }

    private static int InformationPriority(WorldState world, Npc source, Npc target, InformationItem item, bool urgentNeed)
    {
        var score = (int)item.Reliability * 10;

        if (item.LocationId is Guid locationId && world.Geography.Locations.ContainsKey(locationId))
        {
            if (urgentNeed && IsUsefulLocation(world, target, locationId))
                score += 100;
            else if (!target.KnownLocationIds.Contains(locationId))
                score += 20;
        }

        if (item.SubjectNpcId is Guid subjectId && world.Npcs.TryGetValue(subjectId, out var subject) && subject.IsAlive)
        {
            if (urgentNeed && IsUsefulPerson(world, target, subject))
                score += 120;
            else if (subjectId != target.Id)
                score += 15;
        }

        var trust = source.Relationships.FirstOrDefault(r => r.ToNpcId == target.Id && r.IsActive)?.Trust ?? 0.2;
        score += (int)Math.Round(Math.Clamp(trust, 0, 1) * 10);
        return score;
    }

    private static bool IsUsefulPerson(WorldState world, Npc target, Npc person)
    {
        if (target.Conditions.Any(c => c.Treatable && c.Severity >= 0.55))
            return person.Profession.Type == ProfessionType.Healer && person.Profession.Skill >= 20;

        var need = target.Needs.Thirst >= 85 ? "eau" : target.Needs.Hunger >= 85 ? "nourriture" : null;
        return need is not null && FindHelpItem(world, person, need) is not null;
    }

    private static bool IsUsefulLocation(WorldState world, Npc target, Guid locationId)
    {
        if (target.Needs.Thirst >= 85 && world.Commerce.Businesses.Values.Any(b => b.Active && b.LocationId == locationId && b.OwnerNpcId != target.Id && b.IsOpen(world.Time.Period) && b.Stock.Any(s => s.Quantity > 0 && world.Inventory.Items.TryGetValue(s.ItemId, out var item) && item.Consumable && item.Category == ItemCategory.Food && string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))))
            return true;

        if (target.Needs.Hunger >= 85 && world.Commerce.Businesses.Values.Any(b => b.Active && b.LocationId == locationId && b.OwnerNpcId != target.Id && b.IsOpen(world.Time.Period) && b.Stock.Any(s => s.Quantity > 0 && world.Inventory.Items.TryGetValue(s.ItemId, out var item) && item.Consumable && item.Category == ItemCategory.Food && !string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))))
            return true;

        return target.Conditions.Any(c => c.Treatable && c.Severity >= 0.55) && world.Npcs.Values.Any(n => n.IsAlive && n.Profession.Type == ProfessionType.Healer && n.Profession.Skill >= 20 && n.CurrentLocationId == locationId);
    }

    private static Npc? FindUsefulPerson(WorldState world, Npc source, Npc target)
    {
        var knownLocations = source.KnownLocationIds
            .Where(id => id != source.CurrentLocationId && !target.KnownLocationIds.Contains(id) && world.Geography.Locations.ContainsKey(id) && !double.IsInfinity(world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id)))
            .ToArray();
        if (knownLocations.Length == 0)
            return null;

        if (target.Conditions.Any(c => c.Treatable && c.Severity >= 0.55))
        {
            var healer = world.Npcs.Values
                .Where(n => n.IsAlive && n.Id != target.Id && n.Profession.Type == ProfessionType.Healer && n.Profession.Skill >= 20 && n.CurrentLocationId is Guid id && knownLocations.Contains(id))
                .OrderByDescending(n => n.Profession.Skill)
                .ThenBy(n => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, n.CurrentLocationId!.Value))
                .ThenBy(n => n.Id)
                .FirstOrDefault();
            if (healer is not null)
                return healer;
        }

        var need = target.Needs.Thirst >= 85 ? "eau" : target.Needs.Hunger >= 85 ? "nourriture" : null;
        if (need is null)
            return null;

        return world.Npcs.Values
            .Where(n => n.IsAlive && n.Id != target.Id && n.CurrentLocationId is Guid id && knownLocations.Contains(id) && FindHelpItem(world, n, need) is not null)
            .OrderByDescending(n => n.Relationships.FirstOrDefault(r => r.ToNpcId == target.Id && r.IsActive)?.Trust ?? 0)
            .ThenBy(n => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, n.CurrentLocationId!.Value))
            .ThenBy(n => n.Id)
            .FirstOrDefault();
    }

    private static ItemDefinition? FindHelpItem(WorldState world, Npc helper, string need)
    {
        foreach (var entry in helper.Inventory.Entries.Where(e => e.Quantity > 0))
        {
            if (!world.Inventory.Items.TryGetValue(entry.ItemId, out var item) || !item.Consumable || item.Category != ItemCategory.Food)
                continue;
            if (need == "eau" && string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))
                return item;
            if (need == "nourriture" && !string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null;
    }

    private static Guid FindUsefulLocation(WorldState world, Npc source, Npc target)
    {
        var knownLocations = source.KnownLocationIds
            .Where(id => id != source.CurrentLocationId && !target.KnownLocationIds.Contains(id) && world.Geography.Locations.ContainsKey(id) && !double.IsInfinity(world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id)))
            .ToArray();

        if (knownLocations.Length == 0)
            return Guid.Empty;

        var urgentNeed = target.Needs.Thirst >= 85 || target.Needs.Hunger >= 85 || target.Conditions.Any(c => c.Treatable && c.Severity >= 0.55);

        if (!urgentNeed && world.Employment.Contracts.TryGetValue(target.Id, out var contract) && contract.BuildingId is Guid buildingId && world.Buildings.Buildings.TryGetValue(buildingId, out var workplace))
        {
            if (knownLocations.Contains(workplace.LocationId))
                return workplace.LocationId;
        }

        if (target.Needs.Thirst >= 70)
        {
            var waterLocation = knownLocations
                .Where(id => world.Commerce.Businesses.Values.Any(b => b.Active && b.LocationId == id && b.OwnerNpcId != target.Id && b.IsOpen(world.Time.Period) && b.Stock.Any(s => s.Quantity > 0 && world.Inventory.Items.TryGetValue(s.ItemId, out var item) && item.Consumable && item.Category == ItemCategory.Food && string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))))
                .OrderBy(id => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id))
                .ThenBy(id => id)
                .FirstOrDefault();
            if (waterLocation != Guid.Empty)
                return waterLocation;
        }

        if (target.Needs.Hunger >= 70)
        {
            var foodLocation = knownLocations
                .Where(id => world.Commerce.Businesses.Values.Any(b => b.Active && b.LocationId == id && b.OwnerNpcId != target.Id && b.IsOpen(world.Time.Period) && b.Stock.Any(s => s.Quantity > 0 && world.Inventory.Items.TryGetValue(s.ItemId, out var item) && item.Consumable && item.Category == ItemCategory.Food && !string.Equals(item.Name, "Eau", StringComparison.OrdinalIgnoreCase))))
                .OrderBy(id => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id))
                .ThenBy(id => id)
                .FirstOrDefault();
            if (foodLocation != Guid.Empty)
                return foodLocation;
        }

        var healerLocation = knownLocations
            .Where(id => world.Npcs.Values.Any(n => n.IsAlive && n.Profession.Type == ProfessionType.Healer && n.Profession.Skill >= 20 && n.CurrentLocationId == id))
            .OrderBy(id => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id))
            .ThenBy(id => id)
            .FirstOrDefault();
        if (healerLocation != Guid.Empty)
            return healerLocation;

        return knownLocations
            .OrderBy(id => world.Geography.GetRouteDistance(source.CurrentLocationId!.Value, id))
            .ThenBy(id => id)
            .FirstOrDefault();
    }
}
