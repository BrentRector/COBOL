// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// THE ARMS OF THIS CLASS are numbered as docs/rearchitecture/DESIGN-test-build-ci.md §3.14.4 numbers them, and each
// is ONE self-contained test method whose name starts with its arm, so the mechanisms that land them separately
// (DESIGN §3.14.9) merge by union:
//   (1) LegOf is total and deterministic; NameKey strips exactly the partition suffix ........... M12
//   (2) each orderer's output is a permutation of its input ...................................... M12
//   (3) run_gate_legs.py --self-test (M13) and test_population.py --self-test (M14) fire every arm ..... M13, M14
//   (4) every test project under tests/ that the gate runs links GateLegs.cs .................... M12
//   (5) no discovered display name in a gated assembly contains the repository root ............. M12
//   (6) every `dotnet test` caller outside the driver scrubs COBOLNET_GATE_* ..................... M14

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Gate;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ORDERED GATE IS SOUND BY CONSTRUCTION ONLY IF EVERY WHOLE-ASSEMBLY RUN PROVES ITS POPULATION AND NO RUN BUT
/// THE GATE'S LEGS CAN BE NARROWED (kb/Work PB1708, PB1718; DESIGN-test-build-ci.md §3.14.3–3.14.4): the one
/// population check (<c>scripts/test_population.py</c>) fires on a dropped, a duplicated and a foreign case, and every
/// <c>dotnet test</c> caller under <c>scripts/</c> or <c>.github/workflows/</c> scrubs the leg handshake and the
/// MSBuild VSTest properties from its environment.
///
/// ⛔ THE GATE'S LEG FILTER IS SOUND BY CONSTRUCTION (kb/Work PB1719; DESIGN-test-build-ci.md sections 3.14.3 and
/// 3.14.4): the leg function (tests/_shared/GateLegs.cs) is total, deterministic and a function of the display name
/// alone, and its name key mirrors scripts/gate_plan.py exactly; the handshake runs a leg only when all three
/// COBOLNET_GATE_* variables are set and consistent, and refuses every other combination; an orderer's output is
/// checked to be a permutation; every test project under tests/ links GateLegs.cs and names GateTestFramework; and no
/// discovered case carries the repository root.
/// </summary>
/// <remarks>
/// <para>
/// A run narrowed through the ENVIRONMENT is invisible to its own verdict line: <c>VSTestTestCaseFilter</c> (MSBuild
/// imports every environment variable as a property) narrowed Characterization from 33 cases to 31 in the run AND
/// in <c>--list-tests</c>, exit 0 — measured 2026-09-28, and a runsettings file named by <c>RunSettingsFilePath</c>
/// did the same. So the population is always listed SCRUBBED, and every other caller runs scrubbed too; the gate's
/// own three-variable handshake (M12) is the one narrowing that is ever meant to reach a test host.
/// </para>
/// <para>
/// The arms are numbered as in section 3.14.4: (1) the leg function and the name key, (2) the orderers, (3) the
/// population check's self-test, (4) the links, (5) no repository root in a name, (6) the scrubbed callers. Arm (4)'s and (5)'s per-assembly halves also run in the Conformance and
/// Characterization assemblies (their own GateLegDriftTests), because each audits the assembly it runs in.
/// </para>
/// </remarks>
public sealed class GateLegDriftTests
{
    private const string PopulationTool = "test_population.py";
    private const string DriverTool = "run_gate_legs.py";

    /// <summary>
    /// Arm (3), the population half: <c>test_population.py --self-test</c> passes and still drives every arm by
    /// name — a self-test quietly reduced to its happy path also exits 0.
    /// </summary>
    [Fact]
    public void Arm3_ThePopulationCheck_FiresEveryArmOnPlantedInputs()
    {
        string path = TestRepo.Scripts(PopulationTool);
        Assert.True(File.Exists(path), $"the population check is missing: {path}");

        ProcessObservation r = PythonInstrument.Run(path, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("SELF-TEST: PASS", StringComparison.Ordinal),
            $"`{PopulationTool} --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "short: a dropped case is NEVER RAN",
                     "over: a case twice is RAN TWICE",
                     "shard overlap that KEEPS THE COUNT",
                     "skipped: a NotExecuted-only definition is counted skipped",
                     "definitions, not result names",
                     "a definition the listing never printed is NOT IN THE POPULATION",
                     "stopped early",
                     "two listings that disagree are an ERROR",
                     "an EMPTY population is an ERROR",
                     "`scrubbed <command>` runs the command without a planted handshake",
                     "audit-callers flags every unscrubbed shape",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`{PopulationTool} --self-test` no longer drives '{arm}' — a check that has never been seen to fail "
                + $"is not evidence.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// Arm (3), the driver half: <c>run_gate_legs.py --self-test</c> passes and still drives every arm by name — the
    /// fail-fast stop and its named remainder, the population's dropped, duplicated and skipped cases, the identity
    /// mismatches, the empty leg, the no-plan fallback, the scrubbed all-or-none handshake, the lander's single leg,
    /// the refused second gate (kb/Work PB1721; DESIGN-test-build-ci.md section 3.14.4), and the implementer scope
    /// <c>leg1</c> of the batched-gating trial (kb/Work PB2515): leg 1 only, its verdict never a plain GREEN; and the
    /// audits' fail-fast (kb/Work PB2523): a red audit stops an implementer gate before the build, never a lander's.
    /// </summary>
    [Fact]
    public void Arm3_TheGateDriver_FiresEveryArmOnPlantedInputs()
    {
        string path = TestRepo.Scripts(DriverTool);
        Assert.True(File.Exists(path), $"the gate driver is missing: {path}");

        ProcessObservation r = PythonInstrument.Run(path, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("run_gate_legs SELF-TEST: PASS", StringComparison.Ordinal),
            $"`{DriverTool} --self-test` failed (exit {r.ExitCode}):\n{r.Stdout}{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "a whole planted population in two legs is GREEN",
                     "an assembly whose leg 1 is empty is not invoked for leg 1",
                     "every leg host is handed all three handshake variables",
                     "a dropped case is NEVER RAN",
                     "a case run in both legs is RAN TWICE",
                     "a skipped case is counted skipped, never as ran",
                     "a leg host that read another plan digest is an IDENTITY MISMATCH",
                     "a binary changed between the legs is an IDENTITY MISMATCH",
                     "a red in leg 1 STOPS the gate",
                     "the stopped gate NAMES its remainder",
                     "a red leg's whole output, its failure message included",
                     "no plan (the planner failed): ONE leg in the plain order",
                     "a leg host's environment is scrubbed",
                     "scope leg1 (the batched-gating trial): leg 1 only, every leg-2 case named NOT RUN",
                     "scope leg1: a red in leg 1 is LEG 1 ONLY: RED",
                     "scope leg1 with no plan: the one leg IS the whole population",
                     "malformed shared gate settings: the implementer gate is NOT RUN",
                     "a red audit (a stale drift-rules index) STOPS the implementer gate before the build",
                     "a red audit in -Mode lander is RED and the legs still run",
                     "-Mode lander: ONE leg, every assembly, no plan, no handshake, no slot, and no fail-fast",
                     "a second gate in the same worktree is REFUSED",
                     "a defect in the driver still ends in ONE verdict line",
                     "the gate lock is per worktree",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`{DriverTool} --self-test` no longer drives '{arm}' — a check that has never been seen to fail "
                + $"is not evidence.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// Arm (6): every <c>dotnet test</c> caller under <c>scripts/</c> runs scrubbed, and no CI workflow sets a
    /// variable that narrows a test run.
    /// </summary>
    [Fact]
    public void Arm6_EveryDotnetTestCaller_ScrubsTheGateEnvironment()
    {
        ProcessObservation r = PythonInstrument.Run(TestRepo.Scripts(PopulationTool), "audit-callers");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== CALLER AUDIT: CLEAN", StringComparison.Ordinal),
            "A `dotnet test` caller can run with a gate leg handshake (COBOLNET_GATE_*) or a VSTest*/"
            + "RunSettingsFilePath variable in its environment, which narrows the run to a PART of its assembly while "
            + "it exits 0 (kb/Work PB1718). Run it as `python scripts/test_population.py scrubbed dotnet test …` "
            + "(a python caller passes `env=scrubbed_env()`), and never set one of those variables in a workflow.\n"
            + $"(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");

        // ⛔ THE SCAN MUST ASSERT ITS OWN POPULATION: the callers §3.14.3 names, plus the gate twins. A scan that
        // stopped seeing them would report a clean tree over nothing.
        foreach (string site in new[]
                 {
                     "scripts/battery.sh", "scripts/run_gate_legs.py", "scripts/gen-vcr.ps1",
                     "scripts/gen-diagnostics-doc.ps1", "scripts/spec/record_verdicts.py",
                     "scripts/spec/record_impact_map.py", ".github/workflows/build-and-test.yml",
                 })
        {
            Assert.True(r.Stdout.Contains("  site  " + site + "\n", StringComparison.Ordinal)
                        || r.Stdout.Contains("  site  " + site + "\r\n", StringComparison.Ordinal),
                $"the caller audit no longer sees {site} — the scan is broken, not the tree clean.\n{r.Stdout}");
        }
    }

    private const string Ns = "CobolNet.Tests.Conformance";

    // ── Arm (1): the name key and the leg function ────────────────────────────────────────────────────────────────

    /// <summary>The name-key cases, each with the key the contract gives it.</summary>
    private static readonly (string Display, string Key)[] KeyCases =
    [
        ($"{Ns}.CorpusRunnerTests_P3.Positive(edition: \"2023\", name: \"a\")",
            $"{Ns}.CorpusRunnerTests.Positive(edition: \"2023\", name: \"a\")"),
        ($"{Ns}.VersionMatrixTests_P11.Cobol85Program_StillCompilesAtLaterEdition",
            $"{Ns}.VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition"),
        ($"{Ns}.Family_P1_P2.M", $"{Ns}.Family_P1.M"),                          // greedy: only the LAST suffix
        ($"{Ns}.Plain.M(x: \"Foo_P3.Bar_P4\")", $"{Ns}.Plain.M(x: \"Foo_P3.Bar_P4\")"), // arguments untouched
        ($"{Ns}.Outer_P2.M(a.b_P3.c)", $"{Ns}.Outer.M(a.b_P3.c)"),
        ($"{Ns}.M_P3.Method_P4", $"{Ns}.M.Method_P4"),                          // the method segment is never touched
        ("Class_P1.M", "Class_P1.M"),                                            // under three segments: untouched
        ($"{Ns}._P1.M", $"{Ns}._P1.M"),                                          // the family must be non-empty
        ($"{Ns}.C_Px.M", $"{Ns}.C_Px.M"),
        ($"{Ns}.C_P.M", $"{Ns}.C_P.M"),
        ($"{Ns}.C_P12x.M", $"{Ns}.C_P12x.M"),
        ($"{Ns}.Dup.Long(s: \"0123456789012345678901234567890123456789012345678\"···)",
            $"{Ns}.Dup.Long(s: \"0123456789012345678901234567890123456789012345678\"···)"),
        ("", ""),
        ("NoDots", "NoDots"),
    ];

    [Fact]
    public void Arm1_NameKey_StripsExactlyThePartitionSuffix()
    {
        var wrong = KeyCases.Where(c => GateNameKey.Of(c.Display) != c.Key)
            .Select(c => $"{c.Display} -> {GateNameKey.Of(c.Display)} (expected {c.Key})").ToList();
        Assert.True(wrong.Count == 0, "GateNameKey.Of broke the contract:\n  " + string.Join("\n  ", wrong));
    }

    [Fact]
    public void Arm1_LegOf_IsTotalAndDeterministic_OverRepeatedUnknownAndTruncatedNames()
    {
        const string truncated = $"{Ns}.Dup.Long(s: \"0123456789012345678901234567890123456789012345678\"···)";
        var plan = new GateAssemblyPlan("Conformance", "c0ffee",
            [$"{Ns}.A.M", truncated], [$"{Ns}.Corpus.Run(n: 1)", $"{Ns}.B.M"]);
        (string Display, int Leg)[] cases =
        [
            ($"{Ns}.A.M", 1),
            ($"{Ns}.A.M", 1),                        // a repeated name: the same leg every time
            (truncated, 1), (truncated, 1),          // two rows behind one truncated name: both in its leg
            ($"{Ns}.Corpus_P4.Run(n: 1)", 2),        // a partition row, by its key
            ($"{Ns}.Corpus_P0.Run(n: 1)", 2),        // the same key from another partition: the same leg
            ($"{Ns}.B.M", 2),
            ($"{Ns}.NeverPlanned.M", 1),             // unknown to the plan: leg 1, never nowhere
            ("", 1),
        ];
        foreach (var (display, leg) in cases)
        {
            Assert.True(plan.LegOf(display) == leg, $"LegOf({display}) = {plan.LegOf(display)}, expected {leg}");
            Assert.Equal(plan.LegOf(display), plan.LegOf(display));
        }

        Assert.True(plan.RankOf($"{Ns}.A.M") < plan.RankOf(truncated), "leg 1's list is in rank order");
        Assert.True(plan.RankOf(truncated) < plan.RankOf($"{Ns}.Corpus_P9.Run(n: 1)"), "leg 1 ranks before leg 2");
        Assert.Equal(int.MaxValue, plan.RankOf($"{Ns}.NeverPlanned.M"));

        var empty = new GateAssemblyPlan("Conformance", "c0ffee", [], []);
        Assert.All(cases, c => Assert.Equal(1, empty.LegOf(c.Display)));
    }

    /// <summary>The test hosts' key and leg function are one contract with scripts/gate_plan.py: the plan is built
    /// by one and read by the other, and a disagreement is a case the plan put in one leg and the host ran in the
    /// other — or in neither.</summary>
    [Fact]
    public void Arm1_NameKeyAndLegOf_MirrorGatePlanPy()
    {
        string[] names = [.. KeyCases.Select(c => c.Display), $"{Ns}.Corpus_P7.Run(n: 1)", $"{Ns}.Other.M"];
        var planJson = new
        {
            schema = 1, sha256 = "c0ffee",
            assemblies = new { Conformance = new { legs = new[] { 1, 2 }, leg1 = new[] { $"{Ns}.A.M" },
                leg2 = new[] { $"{Ns}.Corpus.Run(n: 1)", KeyCases[0].Key } } },
        };
        DirectoryInfo dir = Directory.CreateTempSubdirectory("gate-legs-");
        try
        {
            string namesFile = Path.Combine(dir.FullName, "names.json");
            string planFile = Path.Combine(dir.FullName, "plan.json");
            File.WriteAllText(namesFile, JsonSerializer.Serialize(names), new UTF8Encoding(false));
            File.WriteAllBytes(planFile, JsonSerializer.SerializeToUtf8Bytes(planJson));
            string code = string.Join(Environment.NewLine,
                "import json, sys",
                "sys.path.insert(0, 'scripts')",
                "import gate_plan",
                "names = json.load(open(sys.argv[1], encoding='utf-8'))",
                "plan = json.load(open(sys.argv[2], encoding='utf-8'))",
                "print(json.dumps([[gate_plan.name_key(n), gate_plan.leg_of(plan, 'Conformance', n)] for n in names]))");
            var r = PythonInstrument.Run("-c", code, namesFile, planFile);
            Assert.True(r.ExitCode == 0, $"gate_plan.py could not be driven:\n{r.Stdout}{r.Stderr}");
            var python = JsonSerializer.Deserialize<JsonElement[][]>(r.Stdout.Trim())!;
            GateAssemblyPlan plan = GatePlan.Parse(File.ReadAllBytes(planFile), "Conformance");
            Assert.Equal(names.Length, python.Length);
            for (int i = 0; i < names.Length; i++)
            {
                Assert.True(python[i][0].GetString() == GateNameKey.Of(names[i]),
                    $"name key of '{names[i]}': Python '{python[i][0].GetString()}', C# '{GateNameKey.Of(names[i])}'");
                Assert.True(python[i][1].GetInt32() == plan.LegOf(names[i]),
                    $"leg of '{names[i]}': Python {python[i][1].GetInt32()}, C# {plan.LegOf(names[i])}");
            }
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    // ── The handshake: a leg runs only on a complete, consistent environment ──────────────────────────────────────

    private static readonly byte[] PlanBytes = Encoding.UTF8.GetBytes(
        "{\"assemblies\":{\"Unit\":{\"leg1\":[\"N.C.A\"],\"leg2\":[\"N.C.B\"],\"legs\":[1,2]}},\"schema\":1,"
        + "\"sha256\":\"c0ffee\"}");

    private static GateHandshake Read(string? plan, string? leg, string? digest, string assembly = "Unit")
    {
        var env = new Dictionary<string, string?>
        {
            [GateHandshake.PlanVariable] = plan, [GateHandshake.LegVariable] = leg,
            [GateHandshake.DigestVariable] = digest,
        };
        return GateHandshake.Read(name => env.GetValueOrDefault(name), assembly);
    }

    private static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [Fact]
    public void Handshake_NoneSet_RunsEveryCase_AndAConsistentOneRunsTheLeg()
    {
        Assert.IsType<GateHandshake.None>(Read(null, null, null));
        Assert.IsType<GateHandshake.None>(Read("", "", ""));        // an empty value is unset

        DirectoryInfo dir = Directory.CreateTempSubdirectory("gate-legs-");
        try
        {
            string plan = Path.Combine(dir.FullName, "plan.json");
            File.WriteAllBytes(plan, PlanBytes);
            var leg = Assert.IsType<GateHandshake.Leg>(Read(plan, "2", Digest(PlanBytes).ToUpperInvariant()));
            Assert.Equal(2, leg.Number);
            Assert.Equal("c0ffee", leg.Plan.ContentSha256);
            Assert.Equal(2, leg.Plan.LegOf("N.C.B"));
            Assert.Equal(1, leg.Plan.LegOf("N.C.A"));
            Assert.Equal(Path.Combine(dir.FullName, "leg-2-Unit.json"), GateIdentityRecord.PathFor(leg));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Every partial or stale handshake refuses — on a real host each case then becomes an execution error
    /// and the run is RED (section 3.14.3; evidence b3: a constructor throw would run every case green).</summary>
    [Fact]
    public void Handshake_EveryPartialOrStaleEnvironment_Refuses()
    {
        DirectoryInfo dir = Directory.CreateTempSubdirectory("gate-legs-");
        try
        {
            string plan = Path.Combine(dir.FullName, "plan.json");
            File.WriteAllBytes(plan, PlanBytes);
            string digest = Digest(PlanBytes);
            string Planted(string name, string text)
            {
                string p = Path.Combine(dir.FullName, name);
                File.WriteAllText(p, text, new UTF8Encoding(false));
                return p;
            }

            string notJson = Planted("not-json.json", "not json");
            string schema2 = Planted("schema2.json", "{\"schema\":2,\"sha256\":\"x\",\"assemblies\":{}}");
            string badKey = Planted("badkey.json",
                "{\"schema\":1,\"sha256\":\"x\",\"assemblies\":{\"Unit\":{\"leg1\":[1],\"leg2\":[]}}}");
            string bothLegs = Planted("both.json",
                "{\"schema\":1,\"sha256\":\"x\",\"assemblies\":{\"Unit\":{\"leg1\":[\"N.C.A\"],\"leg2\":[\"N.C.A\"]}}}");
            var arms = new (string Arm, GateHandshake Result, string Says)[]
            {
                ("plan only", Read(plan, null, null), "COBOLNET_GATE_PLAN is set but COBOLNET_GATE_LEG and COBOLNET_GATE_PLAN_SHA256 are not"),
                ("leg only", Read(null, "1", null), "COBOLNET_GATE_LEG is set but"),
                ("digest only", Read(null, null, digest), "COBOLNET_GATE_PLAN_SHA256 is set but"),
                ("plan + leg", Read(plan, "1", null), "COBOLNET_GATE_PLAN and COBOLNET_GATE_LEG are set but COBOLNET_GATE_PLAN_SHA256 is not"),
                ("plan + digest", Read(plan, null, digest), "but COBOLNET_GATE_LEG is not"),
                ("leg + digest", Read(null, "2", digest), "but COBOLNET_GATE_PLAN is not"),
                ("leg 3", Read(plan, "3", digest), "COBOLNET_GATE_LEG is '3', not 1 or 2"),
                ("leg ' 1'", Read(plan, " 1", digest), "not 1 or 2"),
                ("missing file", Read(Path.Combine(dir.FullName, "absent.json"), "1", digest), "cannot be read"),
                ("digest mismatch", Read(plan, "1", Digest([1, 2, 3])), "hashes to " + digest),
                ("unparsable", Read(notJson, "1", Digest(File.ReadAllBytes(notJson))), "the plan is not JSON"),
                ("schema 2", Read(schema2, "1", Digest(File.ReadAllBytes(schema2))), "the plan is not schema 1"),
                ("no such assembly", Read(plan, "1", digest, "Conformance"), "the plan names no assembly 'Conformance'"),
                ("a key not a string", Read(badKey, "1", Digest(File.ReadAllBytes(badKey))), "holds a Number"),
                ("a key in both legs", Read(bothLegs, "1", Digest(File.ReadAllBytes(bothLegs))),
                    "the plan puts 'N.C.A' in both legs"),
            };
            var wrong = arms.Where(a => a.Result is not GateHandshake.Refused r || !r.Cause.Contains(a.Says,
                    StringComparison.Ordinal))
                .Select(a => $"{a.Arm}: {a.Result}").ToList();
            Assert.True(wrong.Count == 0, "a partial or stale handshake was not refused as expected:\n  "
                                          + string.Join("\n  ", wrong));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    // ── Arm (2): the orderers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A case with a planted display name; only the orderers see it, and nothing serializes it.</summary>
    private sealed class PlantedCase : LongLivedMarshalByRefObject, ITestCase
    {
        /// <summary>xunit's serialization contract (xUnit3001); a planted case is never deserialized.</summary>
        public PlantedCase() => throw new NotSupportedException("a planted case is never deserialized");

        public PlantedCase(string displayName, ITestMethod testMethod)
        {
            DisplayName = displayName;
            TestMethod = testMethod;
        }

        public string DisplayName { get; }
        public string? SkipReason => null;
        public ISourceInformation? SourceInformation { get; set; }
        public ITestMethod TestMethod { get; }
        public object[]? TestMethodArguments => null;
        public Dictionary<string, List<string>> Traits { get; } = new();
        public string UniqueID { get; } = Guid.NewGuid().ToString("N");
        public void Deserialize(IXunitSerializationInfo info) => throw new NotSupportedException();
        public void Serialize(IXunitSerializationInfo info) => throw new NotSupportedException();
    }

    internal void PlantedMethod() { }

    private static ITestMethod MethodIn(ITestCollection collection) =>
        new TestMethod(new TestClass(collection, Reflector.Wrap(typeof(GateLegDriftTests))),
            Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod),
                BindingFlags.Instance | BindingFlags.NonPublic)!));

    [Fact]
    public void Arm2_EachOrderer_ReturnsAPermutation_InPlanOrder()
    {
        var assembly = new TestAssembly(Reflector.Wrap(typeof(GateLegDriftTests).Assembly));
        var early = new TestCollection(assembly, null, "early");
        var late = new TestCollection(assembly, null, "late");
        var unplanned = new TestCollection(assembly, null, "unplanned");
        var plan = new GateAssemblyPlan("Unit", "c0ffee", ["N.E.First", "N.L.Second"], ["N.E.Third"]);
        var cases = new List<ITestCase>
        {
            new PlantedCase("N.U.Never", MethodIn(unplanned)),
            new PlantedCase("N.E.Third", MethodIn(early)),
            new PlantedCase("N.L.Second", MethodIn(late)),
            new PlantedCase("N.E.First", MethodIn(early)),
            new PlantedCase("N.E.First", MethodIn(early)),   // a repeated name: both kept
        };

        var orderedCases = new GateCaseOrderer(plan).OrderTestCases(cases).ToList();
        Assert.Equal(cases.Count, orderedCases.Count);
        Assert.True(cases.All(c => orderedCases.Count(o => ReferenceEquals(o, c)) == 1), "not a permutation");
        Assert.Equal(new[] { "N.E.First", "N.E.First", "N.L.Second", "N.E.Third", "N.U.Never" },
            orderedCases.Select(c => c.DisplayName));

        var collections = new List<ITestCollection> { unplanned, late, early };
        var orderedCollections = new GateCollectionOrderer(plan.CollectionRanks(cases))
            .OrderTestCollections(collections).ToList();
        Assert.Equal(new ITestCollection[] { early, late, unplanned }, orderedCollections);
    }

    /// <summary>xunit trusts an orderer's output: a dropped case would silently never run. The permutation check
    /// must THROW on every way an order can lose or invent a case (xunit then keeps its own complete order).</summary>
    [Fact]
    public void Arm2_APlantedNonPermutation_Throws()
    {
        object a = new(), b = new(), c = new();
        List<object> input = [a, b, c, a];
        var planted = new (string Arm, Func<IReadOnlyList<object>, IEnumerable<object>> Order)[]
        {
            ("drops one", xs => xs.Skip(1)),
            ("repeats one", xs => xs.Append(b)),
            ("swaps one for a stranger", xs => xs.Take(3).Append(new object())),
            ("collapses a repeat", xs => xs.Distinct().Append(c)),
            ("empty", _ => []),
        };
        foreach (var (arm, order) in planted)
        {
            var ex = Record.Exception(() => GateOrder.Permutation(input, order));
            Assert.True(ex is InvalidOperationException, $"the permutation check let '{arm}' through");
        }

        Assert.Equal(new[] { c, a, b, a }, GateOrder.Permutation(input, xs => [xs[2], xs[0], xs[1], xs[3]]));
    }

    // ── Arm (4): every test project links GateLegs.cs and names the gate's framework ──────────────────────────────

    [Fact]
    public void Arm4_EveryTestProject_LinksGateLegs_AndNamesTheGateFramework()
    {
        string props = File.ReadAllText(TestRepo.At("tests", "Directory.Build.props"));
        Assert.Contains(@"<Compile Include=""$(MSBuildThisFileDirectory)_shared\*.cs""", props, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"<AssemblyAttribute Include=""Xunit\.TestFrameworkAttribute"">\s*"
                                 + @"<_Parameter1>CobolNet\.Gate\.GateTestFramework</_Parameter1>\s*"
                                 + @"<_Parameter2>\$\(AssemblyName\)</_Parameter2>"), props);
        Assert.True(File.Exists(TestRepo.At("tests", "_shared", "GateLegs.cs")), "tests/_shared/GateLegs.cs is gone");

        var testProjects = Directory.EnumerateFiles(TestRepo.At("tests"), "*.csproj", SearchOption.AllDirectories)
            .Where(p => File.ReadAllText(p).Contains("<IsTestProject>true</IsTestProject>", StringComparison.Ordinal))
            .ToList();
        foreach (string gated in new[] { "Cobol.Net.Tests.Conformance", "Cobol.Net.Tests.Unit",
                     "Cobol.Net.Tests.Characterization" })
        {
            Assert.True(testProjects.Any(p => Path.GetFileNameWithoutExtension(p) == gated),
                $"the gated test project {gated} is not found as a test project under tests/");
        }

        var unlinked = testProjects.Where(p => Regex.IsMatch(File.ReadAllText(p),
                @"<Compile\s+Remove=""[^""]*(_shared|GateLegs)", RegexOptions.IgnoreCase))
            .Select(p => Path.GetRelativePath(TestRepo.Root, p)).ToList();
        Assert.True(unlinked.Count == 0, "a test project drops the gate's leg filter (tests/_shared/GateLegs.cs): "
                                         + string.Join(", ", unlinked));

        var ownFramework = Directory.EnumerateFiles(TestRepo.At("tests"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\[\s*assembly\s*:\s*(Xunit\.)?TestFramework(Attribute)?\s*\("))
            .Select(f => Path.GetRelativePath(TestRepo.Root, f)).ToList();
        Assert.True(ownFramework.Count == 0, "a test source names its own xunit framework, so its assembly would "
                                             + "name two: " + string.Join(", ", ownFramework));

        GateLegAudit.AssertNamesTheGateFramework(typeof(GateLegDriftTests).Assembly);
    }

    // ── Arm (5): no discovered case carries the repository root ──────────────────────────────────────────────────

    [Fact]
    public void Arm5_NoDiscoveredCase_CarriesTheRepositoryRoot() =>
        GateLegAudit.AssertNoCaseCarriesTheRoot(typeof(GateLegDriftTests).Assembly);

    /// <summary>The audit's witness, on EVERY host for EVERY root shape (kb/Work PB1719): a Windows root and a POSIX
    /// root, both matched as text on either OS, and <c>host</c>, this run's own <see cref="TestRepo.Root"/> — the
    /// form that is real where the test runs. It first planted only a Windows root, so on Linux it proved nothing
    /// and went red in CI (run 36533008383) while every Windows gate was green. The host root is resolved inside
    /// the test: as an argument it would put the root in this case's own name, which arm (5) forbids.</summary>
    [Theory]
    [InlineData("windows")]
    [InlineData("posix")]
    [InlineData("host")]
    public void Arm5_TheRootAudit_FiresOnPlantedOffenders(string shape)
    {
        string root = shape switch
        {
            "windows" => @"E:\COBOL-wt\battery87",
            "posix" => "/home/runner/work/COBOL/COBOL",
            "host" => TestRepo.Root,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "no such root shape"),
        };
        char sep = root.Contains('\\') ? '\\' : '/';
        string path = string.Join(sep, root, "src", "Parsing", "X.cs");
        static string Escaped(string s) => s.Replace("\\", @"\\", StringComparison.Ordinal); // as xunit shows it
        string escapedName = $"N.C.M(path: \"{Escaped(path)}\")";
        string slashName = $"N.C.M(path: \"{path.Replace('\\', '/')}\")";
        string truncatedName = $"N.C.T(path: \"{Escaped(root)[..3]}···\")";   // the root survives only in the argument
        const string cleanName = "N.C.Clean(rel: \"Parsing/X.cs\")";
        var planted = new (string, object[]?)[]
        {
            (escapedName, null),
            (slashName, null),
            (truncatedName, [path]),
            (cleanName, ["Parsing/X.cs"]),
        };

        var offenders = GateLegAudit.RootCarrying(planted, root);

        Assert.Equal(new[] { escapedName, slashName, truncatedName }, offenders);
        Assert.Throws<ArgumentException>(() => GateLegAudit.RootCarrying(planted, sep.ToString()));
    }
}
