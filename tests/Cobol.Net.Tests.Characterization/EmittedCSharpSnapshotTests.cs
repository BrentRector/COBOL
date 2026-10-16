// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// Gate 3 (rearchitecture Phase 0): snapshot the GENERATED C# for every positive corpus program — advisory/reviewed
/// proof that a pure-reorg refactor left emission byte-identical. An intentional emit change re-baselines with review
/// (while gate 1 proves it behavior-preserving). Negative programs emit no C# and are covered by gate 2.
/// Seed/re-baseline with <c>COBOLNET_UPDATE_SNAPSHOTS=1</c> (local only).
/// <para>The runtime version a module records (<c>[assembly: CobolRepository(…)]</c> and
/// <c>__CobolModule.EnsureRegistered</c>) is the package version, which every runtime major bump raises
/// (RuntimeAbiPinDriftTests). It is snapshotted as <see cref="RuntimeVersionToken"/>, substituted only where it equals
/// <see cref="RuntimeAbi.Version"/>: a version bump re-records nothing, while an emitter that stamps any other version
/// still diffs. The exact text of both stamps is RuntimeAbiPinDriftTests.TheEmittedRecord_StatesTheVersionsThisRuntimeDeclares'
/// to hold. Before this, every major bump left all of these snapshots red under a gate whose leg 1 never planned them
/// (a version change reaches no test through code impact), and only the landing's whole Linux run saw it (PB1099's
/// finisher, 2026-10-16).</para>
/// </summary>
public sealed class EmittedCSharpSnapshotTests
{
    /// <summary>What a snapshot holds in place of the runtime's own version.</summary>
    private const string RuntimeVersionToken = "{RUNTIME-VERSION}";

    /// <summary>The two stamps that record the runtime version, each followed by this runtime's own version literal.</summary>
    private static readonly Regex VersionStamp = new(
        $"""(CobolRepositoryAttribute\(\d+, |RegisterModule\("[^"]*", )"{Regex.Escape(RuntimeAbi.Version.ToString())}"(?=, )""",
        RegexOptions.CultureInvariant);

    public static IEnumerable<object[]> Positive() => CharacterizationCorpus.Positive();

    [Theory]
    [MemberData(nameof(Positive))]
    public void EmittedCSharp_MatchesSnapshot(string name, int edition)
    {
        string path = CharacterizationCorpus.SourcePath(name, negative: false);
        var probe = CompilerProbe.Probe(path, edition, emit: true);
        Assert.True(probe.Ok, $"positive characterization program '{name}' must compile: "
            + string.Join("; ", probe.Diagnostics));
        Assert.NotNull(probe.EmittedCSharp);
        string file = System.IO.Path.Combine(CharacterizationCorpus.SnapshotDir, $"{name}.{edition}.g.cs.txt");
        Snapshot.Assert(file, VersionStamp.Replace(probe.EmittedCSharp!, $"$1\"{RuntimeVersionToken}\""));
    }
}
