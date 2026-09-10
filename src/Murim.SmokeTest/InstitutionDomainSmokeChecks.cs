using Murim.Simulation;

public static class InstitutionDomainSmokeChecks
{
    public static void Run(WorldState world)
    {
        var system = new InstitutionDomainSystem();
        var domains = system.BuildAll(world);
        Check(domains.Count > 100, "Institution domains should exist across the living world.");

        var poor = new Household { Id = Guid.NewGuid(), FamilyName = "TestPauvre", HomeLocationId = world.Locations.Keys.First(), Wealth = 5, Prestige = 3 };
        var small = new Household { Id = Guid.NewGuid(), FamilyName = "TestPetit", HomeLocationId = world.Locations.Keys.First(), Wealth = 30, Prestige = 25 };
        var medium = new Household { Id = Guid.NewGuid(), FamilyName = "TestMoyen", HomeLocationId = world.Locations.Keys.First(), Wealth = 58, Prestige = 52 };
        var rich = new Household { Id = Guid.NewGuid(), FamilyName = "TestGrand", HomeLocationId = world.Locations.Keys.First(), Wealth = 92, Prestige = 85 };
        Check(system.FamilyScale(poor) == InstitutionScale.Poor, "Poor household scale classification failed.");
        Check(system.FamilyScale(small) == InstitutionScale.Small, "Small household scale classification failed.");
        Check(system.FamilyScale(medium) == InstitutionScale.Medium, "Medium household scale classification failed.");
        Check(system.FamilyScale(rich) == InstitutionScale.Great, "Great household scale classification failed.");

        var namgung = FactionCatalog.ByCode("namgung")!;
        var namgungHousehold = world.Households.Values.First(h => h.FactionId == namgung.Id);
        var greatFamily = system.ForHousehold(namgungHousehold.Id);
        Check(greatFamily is not null && greatFamily.CurrentScale == InstitutionScale.Great, "Great-family household must generate a great estate.");
        Check(greatFamily!.ActiveFacilities.Count() >= 45, "Great family estate needs a genuinely large number of usable places.");
        Check(greatFamily.ActiveFacilities.Any(f => f.Kind == FacilityKind.MartialArchive), "Great family must have a martial archive.");
        Check(greatFamily.ActiveFacilities.Any(f => f.Kind == FacilityKind.Tombs), "Great family must preserve ancestral tomb space.");
        Check(greatFamily.ActiveFacilities.Any(f => f.Kind == FacilityKind.AccountingOffice), "Great family must include non-martial administration/economy.");

        var shaolin = FactionCatalog.ByCode("shaolin")!;
        var greatSect = system.ForFaction(shaolin.Id);
        Check(greatSect is not null && greatSect.CurrentScale == InstitutionScale.Great, "High-prestige sect must generate a great sect domain.");
        Check(greatSect!.ActiveFacilities.Count() >= 50, "Great sect should feel like a mountain settlement, not five rooms.");
        Check(greatSect.ActiveFacilities.Any(f => f.Kind == FacilityKind.CoreDisciplesCourt), "Great sect must distinguish core disciples.");
        Check(greatSect.ActiveFacilities.Any(f => f.Kind == FacilityKind.MeditationCave), "Great sect must support specialized secluded training places.");
        Check(greatSect.ActiveFacilities.Any(f => f.Kind == FacilityKind.ForbiddenGround), "Great sect must be able to contain forbidden areas.");

        var smallSectDefinition = FactionCatalog.ByCode("silent_river")!;
        var smallSect = system.ForFaction(smallSectDefinition.Id);
        Check(smallSect is not null && smallSect.CurrentScale == InstitutionScale.Small, "Low-prestige sect must stay physically small.");
        Check(smallSect!.ActiveFacilities.Count() >= 8 && smallSect.ActiveFacilities.Count() < 20, "Small sect should have enough places to live but remain compact.");

        var formerGreatCount = greatFamily.ActiveFacilities.Count();
        system.Reevaluate(greatFamily, 7, 8);
        Check(greatFamily.CurrentScale == InstitutionScale.Poor && greatFamily.HistoricalPeakScale == InstitutionScale.Great, "A fallen great family must retain its historical peak.");
        Check(greatFamily.Facilities.Any(f => f.MinimumScale == InstitutionScale.Great && f.State == FacilityState.Abandoned), "Decline should leave abandoned high-status buildings instead of deleting them.");
        Check(greatFamily.ActiveFacilities.Count() < formerGreatCount, "A collapsed family should no longer operate every former facility.");

        var gate = greatSect.ActiveFacilities.First(f => f.Access == FacilityAccess.Public);
        var restricted = greatSect.ActiveFacilities.First(f => f.Access == FacilityAccess.Restricted);
        var outsider = world.Npcs.Values.First(n => n.PrimaryFactionId != greatSect.FactionId);
        Check(system.CanEnter(outsider, greatSect, gate), "Public sect space should allow outsiders.");
        Check(!system.CanEnter(outsider, greatSect, restricted), "Restricted sect space must not be freely accessible.");
        outsider.Knowledge.Add($"access:{restricted.Id}");
        Check(system.CanEnter(outsider, greatSect, restricted), "Explicit special access should allow entry into a restricted facility.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
