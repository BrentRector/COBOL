// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY WHOLE-GROUP PARSE GOES THROUGH THE ONE §8.9 GATE LOOP (kb/Work PB655; ISO §8.3.2.1 1)).
/// A reservation-gated word is not a <c>cobolWord</c> alternative, so a front end that calls
/// <c>compilationUnit()</c> WITHOUT <c>ReservationGateRewriter.ParseToFixpoint</c> can never read a word §8.9
/// leaves free at its edition as a name: a program naming a table <c>COL</c> at COBOL-85 dies with "no viable
/// alternative". This test makes the next front end automatic: a source file that starts a whole-group parse must
/// also call the shared loop.
/// </summary>
public sealed class ReservationGateParseEntryDriftTests
{
    private static readonly Regex WholeGroupParse = new(@"\.compilationUnit\(\)", RegexOptions.Compiled);
    private const string GateLoop = "ReservationGateRewriter.ParseToFixpoint(";

    [Fact]
    public void EveryWholeGroupParse_RunsTheSharedReservationGateLoop()
    {
        var parsers = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => WholeGroupParse.IsMatch(File.ReadAllText(f)))
            .ToList();

        // The population is asserted, never assumed: the known front end must be found, or the scan is blind.
        var names = parsers.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Frontend.cs", names);

        var offenders = parsers.Where(f => !File.ReadAllText(f).Contains(GateLoop, StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(TestRepo.Root, f))
            .ToList();
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} source file(s) parse a whole compilation group without the §8.9 gate loop "
            + $"({GateLoop}…): {string.Join(", ", offenders)}. Route the parse through the shared loop "
            + "(kb/Work PB655) so a word §8.9 leaves free at the edition reads as a user-defined word.");
    }
}
