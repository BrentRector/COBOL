// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE'S LEG 1 HOLDS EVERY HOST-ONLY WITNESS AND EVERY CHEAP TREE-WIDE SWEEP, BY TRAIT (kb/Work PB3022;
/// DESIGN-test-build-ci.md §3.17.3): every Unit test class that drives the file-sharing layer's host types
/// (<c>src/Cobol.Net.Runtime/IO/Sharing/</c>, <c>HostFile</c>) from concurrent workers carries
/// <c>[Trait("Host", "ReFS")]</c>, so <c>scripts/gate_plan.py</c> puts it in leg 1 (tier 0h) at every gate; every drift
/// test that <c>scripts/spec/drift_rules.py --sweeps</c> classifies as a tree-wide sweep carries
/// <c>[Trait("Sweep", "tree")]</c> and no other class does (tier 0c); and the planner's source scan of the traits
/// agrees with the attributes the compiler sees.
/// </summary>
/// <remarks>
/// <para>
/// Why the host-only tier exists: <c>E:</c> is ReFS and CI's Windows runner is NTFS, and they delete differently — the
/// PB2917 sidecar-unlink race reproduced on this host in 20 of 7,200 probes and may never on NTFS. With the local
/// whole-population run gone (CI is the whole-population net, kb/Work PB3015), such a witness would run locally only
/// when a diff happened to reach it. Membership is DERIVED here — from the sharing layer's declared types and the
/// test's own concurrency — never listed, so a new race witness that lacks the trait is red by name.
/// </para>
/// <para>
/// Why the sweep tier exists (the operator's proposal 2, 2026-10-11): the bookkeeping reds a landing used to find
/// in its whole-population run (a stale index, an unregistered row) come from cheap tree-wide drift tests; run in
/// every gate's leg 1, the implementer finds them instead.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class HostWitnessTraitDriftTests
{
    /// <summary>A worker started beside the test's own thread: the shape of a race witness.</summary>
    private static readonly Regex ConcurrentWorker = new(@"\bnew Thread\(|\bnew Barrier\(|\bTask\.Run\(|\bParallel\.",
        RegexOptions.NonBacktracking);

    private static readonly Regex TypeDeclaration = new(
        @"^\s*(?:(?:public|internal|sealed|static|partial|abstract|readonly|ref|file)\s+)*(?:class|struct|interface|enum|record)\s+(?<name>\w+)",
        RegexOptions.Multiline | RegexOptions.NonBacktracking);

    private static IEnumerable<(string Name, string Value)> ClassTraits(Type t) =>
        t.GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(TraitAttribute))
            .Select(a => ((string)a.ConstructorArguments[0].Value!, (string)a.ConstructorArguments[1].Value!));

    private static readonly Type[] UnitTypes = typeof(HostWitnessTraitDriftTests).Assembly.GetTypes();

    [Fact]
    public void EveryFileSystemRaceWitness_CarriesTheHostTrait()
    {
        string[] sharingTypes =
        [
            .. Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime", "IO", "Sharing"), "*.cs")
                .SelectMany(f => TypeDeclaration.Matches(File.ReadAllText(f)).Select(m => m.Groups["name"].Value))
                .Distinct(StringComparer.Ordinal),
            "HostFile",
        ];
        var mentions = new Regex(@"\b(?:" + string.Join("|", sharingTypes) + @")\b", RegexOptions.NonBacktracking);
        var witnesses = Directory.EnumerateFiles(TestRepo.Tests("Cobol.Net.Tests.Unit"), "*.cs")
            .Select(f => (File: f, Text: File.ReadAllText(f)))
            .Where(x => x.File != Path.Combine(TestRepo.Tests("Cobol.Net.Tests.Unit"), nameof(HostWitnessTraitDriftTests) + ".cs")
                        && mentions.IsMatch(x.Text) && ConcurrentWorker.IsMatch(x.Text))
            .SelectMany(x => TypeDeclaration.Matches(x.Text).Select(m => m.Groups["name"].Value))
            .SelectMany(name => UnitTypes.Where(t => t.Name == name && t.DeclaringType is null))
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes<FactAttribute>().Any()))
            .Distinct()
            .ToList();

        // ⛔ THE SCAN ASSERTS ITS OWN POPULATION: the two witnesses the design names. A scan that stopped seeing them
        // would report every witness traited over nothing.
        Assert.Contains(witnesses, t => t.Name == nameof(SidecarColumnLockHostDriftTests));
        Assert.Contains(witnesses, t => t.Name == nameof(RegionLocksDriftTests));
        var bare = witnesses.Where(t => !ClassTraits(t).Contains(("Host", "ReFS"))).Select(t => t.Name).ToList();
        Assert.True(bare.Count == 0,
            "these test classes drive the file-sharing layer from concurrent workers — a file-system race witness, "
            + "whose race may reproduce only on this host's ReFS drive — but do not carry [Trait(\"Host\", \"ReFS\")], "
            + "so the gate would run them only when a diff reaches them (kb/Work PB3022):\n  " + string.Join("\n  ", bare));
    }

    [Fact]
    public void TheSweepTrait_IsExactlyDriftRulesTreeWideSweeps_AndThePlannerSeesEveryTrait()
    {
        ProcessObservation sweeps = PythonInstrument.Run(TestRepo.Scripts("spec", "drift_rules.py"), "--sweeps");
        Assert.True(sweeps.ExitCode == 0, $"drift_rules.py --sweeps failed (exit {sweeps.ExitCode}):\n{sweeps.Stdout}{sweeps.Stderr}");
        var expected = sweeps.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(HostWitnessTraitDriftTests), expected); // the population check: it reads scripts/

        ProcessObservation scan = PythonInstrument.Run(TestRepo.Scripts("gate_plan.py"), "--traits");
        Assert.True(scan.ExitCode == 0, $"gate_plan.py --traits failed (exit {scan.ExitCode}):\n{scan.Stdout}{scan.Stderr}");
        using var json = System.Text.Json.JsonDocument.Parse(scan.Stdout);
        var scanned = json.RootElement.EnumerateObject()
            .SelectMany(asm => asm.Value.EnumerateObject().Select(cls => (Assembly: asm.Name, Class: cls.Name, Traits: cls.Value)))
            .ToList();
        var traited = scanned.Where(x => x.Traits.TryGetProperty("Sweep", out _)).Select(x => x.Class)
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(traited.SetEquals(expected),
            "the classes carrying [Trait(\"Sweep\", \"tree\")] are not exactly drift_rules.py's tree-wide sweeps "
            + "(kb/Work PB3022: the planner's tier 0c reads the trait):\n  missing the trait: "
            + string.Join(", ", expected.Except(traited).Order(StringComparer.Ordinal))
            + "\n  trait on a non-sweep: " + string.Join(", ", traited.Except(expected).Order(StringComparer.Ordinal)));

        // The planner reads traits from the SOURCES (it plans before any build); in this assembly, what it read must be
        // what the compiler attached, class by class.
        var compiled = UnitTypes.Where(t => t.DeclaringType is null)
            .SelectMany(t => ClassTraits(t).Select(tr => $"{t.Name} {tr.Name}={tr.Value}"))
            .ToHashSet(StringComparer.Ordinal);
        var read = scanned.Where(x => x.Assembly == "Unit")
            .SelectMany(x => x.Traits.EnumerateObject()
                .SelectMany(tr => tr.Value.EnumerateArray().Select(v => $"{x.Class} {tr.Name}={v.GetString()}")))
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(read.SetEquals(compiled),
            "gate_plan.py's trait scan disagrees with the compiled [Trait] attributes of the Unit assembly:\n  compiled only: "
            + string.Join(", ", compiled.Except(read).Order(StringComparer.Ordinal))
            + "\n  scanned only: " + string.Join(", ", read.Except(compiled).Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void ThePlan_OrdersTheHostAndSweepTiers_AfterTheChangesOwnCases()
    {
        string planner = File.ReadAllText(TestRepo.Scripts("gate_plan.py"));
        Assert.Matches(@"(?m)^TIER_ORDER = \(""0a"", ""0h"", ""0c"", ""0u"", ""1"", ""2"", ""3""\)\r?$", planner);
        Assert.Matches(@"(?m)^HOST_ONLY = \{\r?$", planner);
    }
}
