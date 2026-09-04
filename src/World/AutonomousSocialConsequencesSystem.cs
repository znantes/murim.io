namespace Murim.World;

/// <summary>
/// Turns ordinary autonomous world activity into social consequences that can be
/// witnessed, remembered, spread as information, and reflected in reputation.
/// This system is deliberately lightweight: it never grants the player special protection.
/// </summary>
public sealed class AutonomousSocialConsequencesSystem
{
    private readonly HashSet<(long Day, Guid EventId)> _processed = new();

    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        LastEvents.Clear();
        ProcessCommerce(world);
        ProcessExistingInjuries(world);
        CreateAutonomousIncidents(world);
        CleanupProcessed(world.Time.Day - 30);
    }

    private void ProcessCommerce(WorldState world)
    {
        foreach (var transaction in world.Commerce.Transactions.Where(t => t.Day == world.Time.Day))
        {
            if (!world.Npcs.TryGetValue(transaction.BuyerNpcId, out var buyer) ||
                !world.Npcs.TryGetValue(transaction.SellerNpcId, out var seller))
                continue;

            var business = world.Commerce.Businesses.GetValueOrDefault(transaction.BusinessId);
            if (business is null) continue;

            var eventId = transaction.BusinessId;
            if (!_processed.Add((world.Time.Day, eventId))) continue;

            var subject = seller;
            world.Reputation.Apply(world, seller.Id, 0.25, "Local", 1);
            AdjustRelationship(buyer, seller, 0.025, 0.02, 0.02);
            Publish(world, buyer, "Commerce", $"{buyer.Identity.Name} a acheté auprès de {seller.Identity.Name} à {business.Name}.", seller.Id, business.LocationId, InformationReliability.Verified, 0.15);
            Publish(world, seller, "Commerce", $"{seller.Identity.Name} a vendu à {buyer.Identity.Name} à {business.Name}.", seller.Id, business.LocationId, InformationReliability.Verified, 0.15);
            buyer.History.Add("Commerce", buyer.AgeYears, $"Achète à {seller.Identity.Name} dans {business.Name}.");
            seller.History.Add("Commerce", seller.AgeYears, $"Vend à {buyer.Identity.Name} dans {business.Name}.");
            LastEvents.Add($"Commerce : {buyer.Identity.DisplayName} traite avec {subject.Identity.DisplayName}.");
        }
    }

    private void ProcessExistingInjuries(WorldState world)
    {
        foreach (var patient in world.Npcs.Values.Where(n => n.IsAlive && n.Conditions.Count > 0))
        {
            foreach (var condition in patient.Conditions.Where(c => c.SourceNpcId is not null && c.OnsetDay == world.Time.Day).ToList())
            {
                if (condition.SourceNpcId is not Guid sourceId || !world.Npcs.TryGetValue(sourceId, out var source)) continue;
                var eventId = condition.Id;
                if (!_processed.Add((world.Time.Day, eventId))) continue;

                var hostile = condition.Type is ConditionType.Fracture;
                var polarity = hostile ? -0.65 : -0.20;
                var text = hostile
                    ? $"{patient.Identity.Name} a été blessé lors d'un incident impliquant {source.Identity.Name}."
                    : $"{patient.Identity.Name} a subi un incident impliquant {source.Identity.Name}.";
                world.Reputation.Apply(world, source.Id, polarity * 2.5, "Local", 1);
                AdjustRelationship(patient, source, polarity * 0.08, polarity * 0.05, polarity * 0.04);
                Publish(world, patient, "Incident", text, source.Id, patient.CurrentLocationId, InformationReliability.Unverified, polarity);
                patient.History.Add("Incident", patient.AgeYears, text);
                source.History.Add("Incident", source.AgeYears, $"Est impliqué dans un incident avec {patient.Identity.Name}.");
                LastEvents.Add($"Incident : {patient.Identity.DisplayName} est blessé.");
            }
        }
    }

    private static void CreateAutonomousIncidents(WorldState world)
    {
        var groups = world.Npcs.Values
            .Where(n => n.IsAlive && n.AgeYears >= 12 && n.CurrentLocationId is not null)
            .GroupBy(n => n.CurrentLocationId!.Value);

        foreach (var group in groups)
        {
            var people = group.OrderBy(n => n.Id).Take(12).ToList();
            if (people.Count < 2) continue;

            var seed = DeterministicRandomSeed.Create(world.WorldSeed + 97, world.Time.Day, group.Key);
            var random = new Random(seed);
            if (random.NextDouble() > Math.Min(0.30, people.Count * 0.025)) continue;

            var first = people[random.Next(people.Count)];
            var second = people.Where(n => n.Id != first.Id).ElementAt(random.Next(people.Count - 1));
            var roll = random.NextDouble();

            if (roll < 0.27)
                Help(world, first, second);
            else if (roll < 0.48)
                FairDealing(world, first, second);
            else if (roll < 0.68)
                Theft(world, first, second);
            else if (roll < 0.84)
                Accident(world, first, second, random);
            else
                Exploit(world, first, second);
        }
    }

    private static void Help(WorldState world, Npc helper, Npc recipient)
    {
        var amount = Math.Min(2.5 + helper.Wealth * 0.04, helper.Wealth);
        helper.ApplyWealthChange(-amount);
        recipient.ApplyWealthChange(amount);
        AdjustRelationship(helper, recipient, 0.12, 0.08, 0.06);
        world.Reputation.Apply(world, helper.Id, 2.0, "Local", 2);
        var text = $"{helper.Identity.Name} aide {recipient.Identity.Name} spontanément.";
        helper.History.Add("Aide", helper.AgeYears, text);
        recipient.History.Add("Aide", recipient.AgeYears, $"Reçoit l'aide de {helper.Identity.Name}.");
        Publish(world, helper, "Aide", text, helper.Id, helper.CurrentLocationId, InformationReliability.Verified, 0.85);
    }

    private static void FairDealing(WorldState world, Npc first, Npc second)
    {
        AdjustRelationship(first, second, 0.06, 0.05, 0.04);
        world.Reputation.Apply(world, first.Id, 0.8, "Local", 2);
        world.Reputation.Apply(world, second.Id, 0.4, "Local", 2);
        var text = $"{first.Identity.Name} et {second.Identity.Name} concluent un échange honnête.";
        first.History.Add("Commerce", first.AgeYears, text);
        second.History.Add("Commerce", second.AgeYears, text);
        Publish(world, first, "Commerce", text, first.Id, first.CurrentLocationId, InformationReliability.Verified, 0.45);
    }

    private static void Theft(WorldState world, Npc thief, Npc victim)
    {
        var amount = Math.Min(1.5 + victim.Wealth * 0.03, victim.Wealth);
        if (amount <= 0.01) return;
        victim.ApplyWealthChange(-amount);
        thief.ApplyWealthChange(amount);
        AdjustRelationship(thief, victim, -0.20, -0.18, -0.12);
        world.Reputation.Apply(world, thief.Id, -4.5, "Local", 2);
        var text = $"{thief.Identity.Name} vole une partie des biens de {victim.Identity.Name}.";
        thief.History.Add("Crime", thief.AgeYears, text);
        victim.History.Add("Crime", victim.AgeYears, $"Subit un vol commis par {thief.Identity.Name}.");
        Publish(world, victim, "Crime", text, thief.Id, victim.CurrentLocationId, InformationReliability.Unverified, -0.95);
    }

    private static void Accident(WorldState world, Npc first, Npc second, Random random)
    {
        var patient = random.NextDouble() < 0.5 ? first : second;
        var bystander = patient.Id == first.Id ? second : first;
        var severity = 0.12 + random.NextDouble() * 0.20;
        var condition = world.Medicine.Inflict(patient, ConditionType.Fracture, "Blessure accidentelle", severity, severity, world.Time.Day, false, 14, bystander.Id);
        AdjustRelationship(bystander, patient, -0.02, 0.01, 0.0);
        var text = $"Un accident blesse {patient.Identity.Name} en présence de {bystander.Identity.Name}.";
        patient.History.Add("Accident", patient.AgeYears, text);
        Publish(world, patient, "Accident", text, patient.Id, patient.CurrentLocationId, InformationReliability.Plausible, -0.15);
    }

    private static void Exploit(WorldState world, Npc exploiter, Npc target)
    {
        var amount = Math.Min(3.0 + target.Wealth * 0.06, target.Wealth);
        if (amount <= 0.01) return;
        target.ApplyWealthChange(-amount);
        exploiter.ApplyWealthChange(amount);
        AdjustRelationship(exploiter, target, -0.12, -0.10, -0.08);
        world.Reputation.Apply(world, exploiter.Id, -2.5, "Local", 2);
        var text = $"{exploiter.Identity.Name} profite de la vulnérabilité de {target.Identity.Name}.";
        exploiter.History.Add("Abus", exploiter.AgeYears, text);
        target.History.Add("Abus", target.AgeYears, $"Est exploité par {exploiter.Identity.Name}.");
        Publish(world, target, "Abus", text, exploiter.Id, target.CurrentLocationId, InformationReliability.Unverified, -0.75);
    }

    private static void AdjustRelationship(Npc first, Npc second, double affinity, double trust, double respect)
    {
        var a = first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.IsActive);
        var b = second.Relationships.FirstOrDefault(r => r.ToNpcId == first.Id && r.IsActive);
        if (a is not null) a.Shift(affinity, trust, respect);
        if (b is not null) b.Shift(affinity, trust, respect);
    }

    private static void Publish(WorldState world, Npc source, string topic, string content, Guid subjectId, Guid? locationId, InformationReliability reliability, double polarity)
    {
        world.Information.Publish(world, source, topic, content, subjectId, locationId, reliability, polarity);
    }

    private void CleanupProcessed(long beforeDay)
    {
        if (beforeDay <= 0) return;
        _processed.RemoveWhere(x => x.Day < beforeDay);
    }
}
