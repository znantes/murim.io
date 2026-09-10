using Murim.Simulation;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var runtime = LivingWorldFactory.CreateRuntime(seed: 190724, npcPopulation: 10_000);
var world = runtime.World;
var content = runtime.Content;

Require(content.Techniques.Count == 10_000, "Technique count must be 10,000.");
Require(content.Techniques.Count(t => t.Rarity == Rarity.Divine) == 50, "Divine technique count must be 50.");
Require(content.Monsters.Count == 5_000, "Monster/form count must be 5,000.");
Require(content.Pills.Count == 1_260, "Pill count must be 1,260.");
Require(content.Artifacts.Count == 1_000, "Artifact count must be 1,000.");
Require(content.Professions.Count >= 60, "Profession catalogue is too small.");
Require(content.Tournaments.Any(t => t.Code == "dragon_phoenix_gathering"), "Dragon/Phoenix gathering missing.");
Require(world.Npcs.Count == 10_000, "Initial NPC population must be 10,000.");
Require(world.PlayerNpc is not null, "Player must control a real NPC.");

var dayBefore = world.Clock.Day;
runtime.AdvanceMinutes(7 * 1440);
Require(world.Clock.Day == dayBefore + 7, "One-week action did not advance world time by seven days.");

var player = world.PlayerNpc!;
runtime.Injuries.Amputate(player, BodyRegion.Hand, BodySide.Right, world.Clock.Day, "smoke-test fictional accident");
runtime.AdvanceMinutes(40 * 1440);
Require(player.Injuries.Any(i => i.Kind == InjuryKind.Amputation && i.Permanent && i.Region == BodyRegion.Hand && i.Side == BodySide.Right), "Permanent amputation was lost during healing.");
Require(InjurySystem.HandFunction(player, BodySide.Right) < .5, "Permanent hand loss must affect function.");

var adults = world.Npcs.Values.Where(n => n.IsAlive && n.Id != player.Id && n.AgeYears(world.Clock) >= 18).Take(2).ToArray();
Require(adults.Length == 2, "Need two adults for obligation test.");
var request = runtime.Obligations.Request(world, adults[0], adults[1], ObligationKind.Delivery, "Livrer un colis familial avant demain soir", 1440, .8);
runtime.Obligations.Accept(world, request.Id);
var trustBefore = runtime.BaseEngine.Relationships.GetOrCreate(adults[0], adults[1], world.Clock.Day).Trust;
runtime.AdvanceMinutes(5 * 1440);
Require(request.Status == ObligationStatus.Failed && request.RequesterKnowsOutcome, "Missed accepted obligation should fail and eventually be discovered.");
var trustAfter = runtime.BaseEngine.Relationships.GetOrCreate(adults[0], adults[1], world.Clock.Day).Trust;
Require(trustAfter < trustBefore, "A broken accepted promise should reduce requester trust.");

var masteryNpc = adults[0];
masteryNpc.Body.Fatigue = 0;
var rare = new TechniqueDefinition(900001, "TEST-RARE", "Rare perfectionnée", TechniqueDomain.Sword, Rarity.Rare, 1.2, .5, 1, 1, .01, MartialRealm.FirstRate, ["martial"]);
var mythic = new TechniqueDefinition(900002, "TEST-MYTHIC", "Mythique débutante", TechniqueDomain.Sword, Rarity.Mythic, 3.2, .9, 4, 4, .08, MartialRealm.Transformation, ["martial"]);
masteryNpc.Techniques[rare.Id] = new TechniqueProgress { TechniqueId = rare.Id, LearnedDay = world.Clock.Day - 3650, LastPracticedDay = world.Clock.Day, PracticePoints = 720, Comprehension = 100, ExecutionConsistency = 100, PersonalAdaptation = 20 };
masteryNpc.Techniques[mythic.Id] = new TechniqueProgress { TechniqueId = mythic.Id, LearnedDay = world.Clock.Day, LastPracticedDay = world.Clock.Day, PracticePoints = 1, Comprehension = 2, ExecutionConsistency = 1 };
var rarePower = runtime.Techniques.EffectivePotency(masteryNpc, rare);
var mythicPower = runtime.Techniques.EffectivePotency(masteryNpc, mythic);
Require(rarePower > mythicPower, $"Mastery rule failed: Rare={rarePower:0.###}, Mythic={mythicPower:0.###}.");

var host = world.Locations.Values.First();
var gathering = runtime.DragonPhoenix.Schedule(world, host.Id, 120, 12);
Require(world.Tournaments.ContainsKey(gathering.Id) && gathering.ArchetypeCode == "dragon_phoenix_gathering", "Dragon/Phoenix tournament scheduling failed.");

var portraitsA = new PortraitGeneticsSystem();
var portraitsB = new PortraitGeneticsSystem();
portraitsA.InitializeWorld(world);
portraitsB.InitializeWorld(world);
var faceA = portraitsA.GetOrCreate(player);
var faceB = portraitsB.GetOrCreate(player);
Require(Math.Abs(faceA.FaceWidth - faceB.FaceWidth) < 0.0000001 && Math.Abs(faceA.NoseWidth - faceB.NoseWidth) < 0.0000001, "Portrait genome must be deterministic for the same NPC.");
var familyChild = world.Npcs.Values.FirstOrDefault(n => n.ParentIds.Count >= 2 && world.Npcs.ContainsKey(n.ParentIds[0]) && world.Npcs.ContainsKey(n.ParentIds[1]));
if (familyChild is not null)
{
    var parentA = world.Npcs[familyChild.ParentIds[0]];
    var parentB = world.Npcs[familyChild.ParentIds[1]];
    var childFace = portraitsA.Inherit(familyChild, parentA, parentB);
    var p1 = portraitsA.GetOrCreate(parentA);
    var p2 = portraitsA.GetOrCreate(parentB);
    var parentalMin = Math.Min(p1.FaceWidth, p2.FaceWidth) - .16;
    var parentalMax = Math.Max(p1.FaceWidth, p2.FaceWidth) + .16;
    Require(childFace.FaceWidth >= parentalMin && childFace.FaceWidth <= parentalMax, "Child portrait should remain plausibly related to parental facial traits.");
}

var generations = new MartialGenerationSystem();
var sampleBirthYear = world.Npcs.Values.Select(n => (int)Math.Floor((n.Identity.BirthDay - 1) / 365.0) + 1).GroupBy(y => y).OrderByDescending(g => g.Count()).First().Key;
var generation = generations.Evaluate(world, sampleBirthYear, 12);
Require(!string.IsNullOrWhiteSpace(generation.CanonicalName), "Martial generation must receive an emergent name.");
var regionalGenerationName = generations.NameForRegion(generation, "Plaine centrale", world.Seed);
Require(!string.IsNullOrWhiteSpace(regionalGenerationName), "Martial generation must support regional historical names.");

AppearanceSmokeChecks.Run(world);
DeepWorldSmokeChecks.Run(world, content.Techniques);
InstitutionDomainSmokeChecks.Run(world);
UrbanVisualSmokeChecks.Run(runtime);
ContentValidator.Validate(content);
WorldIntegrity.NormalizeAndValidate(world);

Console.WriteLine("MURIM SMOKE TEST OK");
Console.WriteLine($"Techniques={content.Techniques.Count}; Divine={content.Techniques.Count(t => t.Rarity == Rarity.Divine)}; Monsters={content.Monsters.Count}; Pills={content.Pills.Count}; Artifacts={content.Artifacts.Count}");
Console.WriteLine($"NPCs={world.Npcs.Count}; Day={world.Clock.Day}; Events={world.Events.Count}; Rumors={world.Rumors.Count}; Goals={world.Goals.Count}");
Console.WriteLine($"Rare perfected={rarePower:0.###} > Mythic learned={mythicPower:0.###}");
Console.WriteLine($"Generation={generation.CanonicalName}; Regional={regionalGenerationName}");
