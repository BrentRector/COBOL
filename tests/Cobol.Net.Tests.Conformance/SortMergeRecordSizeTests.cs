// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The record-size syntax rules between a sort-merge file and the files of a SORT or MERGE statement's USING and
/// GIVING phrases (kb/Work PB995): ISO §14.9.40.3 SR5 and SR11, §14.9.24.3 SR3 and SR12. One program per leg, ONE
/// statement in it, so each leg proves one rule arm drew exactly one refusal, and its legal twin drew none.
///
/// <para>⛔ WHY FACTS AND NOT ONLY CORPUS ENTRIES: the negative corpus matches its <c>.err</c> as a substring of the
/// whole diagnostic stream, so a program of several statements passes when ANY ONE of them is refused. The shapes
/// here are legs of one table, each its own compile, which also pins the two directions the rules run in
/// (USING bounded by the SD; the SD bounded by GIVING) and both arms of each (variable-length bounds both ends,
/// fixed-length only the upper one). The legal edge cases are the control arms that prove the screen can pass.</para>
/// </summary>
public sealed class SortMergeRecordSizeTests
{
    /// <summary>One file of the statement: <c>null</c> sizes are a fixed-length file of <paramref name="max"/>
    /// bytes, otherwise a RECORD IS VARYING file of <paramref name="min"/> to <paramref name="max"/>.</summary>
    private static string Describe(string kind, string name, int? min, int max) =>
        (kind, min) switch
        {
            ("SD", null) => $"       SD {name}.\n       01 {name}-REC.\n          05 {name}-K PIC XX.\n          05 {name}-X PIC X({max - 2}).\n",
            ("SD", _) => $"       SD {name}\n           RECORD IS VARYING IN SIZE FROM {min} TO {max} CHARACTERS.\n       01 {name}-REC.\n          05 {name}-K PIC XX.\n          05 {name}-X PIC X({max - 2}).\n",
            (_, null) => $"       FD {name}.\n       01 {name}-REC PIC X({max}).\n",
            _ => $"       FD {name}\n           RECORD IS VARYING IN SIZE FROM {min} TO {max} CHARACTERS.\n       01 {name}-REC PIC X({max}).\n",
        };

    private static string Program(string id, bool merge, (int? Min, int Max) sd, (int? Min, int Max)[] usings, (int? Min, int Max) giving)
    {
        var select = new System.Text.StringBuilder();
        var data = new System.Text.StringBuilder();
        select.Append($"           SELECT SW ASSIGN TO \"{id}sw.tmp\".\n");
        data.Append(Describe("SD", "SW", sd.Min, sd.Max));
        var names = new List<string>();
        for (int i = 0; i < usings.Length; i++)
        {
            string n = $"U{i + 1}";
            names.Add(n);
            select.Append($"           SELECT {n} ASSIGN TO \"{id}{n}.dat\".\n");
            data.Append(Describe("FD", n, usings[i].Min, usings[i].Max));
        }
        select.Append($"           SELECT G ASSIGN TO \"{id}g.dat\".\n");
        data.Append(Describe("FD", "G", giving.Min, giving.Max));
        string verb = merge ? "MERGE" : "SORT";
        return $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {id}.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
        {select}       DATA DIVISION.
               FILE SECTION.
        {data}       PROCEDURE DIVISION.
               MAIN.
                   {verb} SW ASCENDING KEY SW-K USING {string.Join(' ', names)} GIVING G
                   STOP RUN.
        """;
    }

    private static int CountRule(IEnumerable<string> errors, string rule) =>
        errors.Count(e => e.Contains("COBOLNET1757", StringComparison.Ordinal) && e.Contains(rule, StringComparison.Ordinal));

    // (SD, USING files, GIVING) → the rule that must refuse it, or null when the statement is legal.
    // Sizes are in bytes; the key (two bytes) always lies inside the SD's smallest record (§14.9.40.3 SR6 g).
    [Theory]
    // SORT, USING bounded by the SD (SR5)
    [InlineData(false, null, 6, new[] { 7 }, null, 8, "§14.9.40.3 SR5")]      // fixed SD 6, USING 7 > 6
    [InlineData(false, null, 6, new[] { 6 }, null, 8, null)]                    // equal is legal
    [InlineData(false, null, 6, new[] { 3 }, null, 8, null)]                    // a smaller USING record is legal
    [InlineData(false, 3, 6, new[] { 2 }, null, 8, "§14.9.40.3 SR5")]          // variable SD 3..6: USING 2 < smallest
    [InlineData(false, 3, 6, new[] { 7 }, null, 8, "§14.9.40.3 SR5")]          // variable SD 3..6: USING 7 > largest
    [InlineData(false, 3, 6, new[] { 3 }, null, 8, null)]                       // the smallest edge is legal
    [InlineData(false, 3, 6, new[] { 6 }, null, 8, null)]                       // the largest edge is legal
    // SORT, SD bounded by GIVING (SR11)
    [InlineData(false, null, 6, new[] { 6 }, null, 5, "§14.9.40.3 SR11")]      // fixed GIVING 5 < SD 6
    [InlineData(false, null, 6, new[] { 6 }, null, 6, null)]                    // equal is legal
    [InlineData(false, 3, 6, new[] { 4 }, 4, 8, "§14.9.40.3 SR11")]            // variable GIVING 4..8: SD smallest 3 < 4
    [InlineData(false, 3, 6, new[] { 4 }, 2, 5, "§14.9.40.3 SR11")]            // variable GIVING 2..5: SD largest 6 > 5
    [InlineData(false, 3, 6, new[] { 4 }, 3, 6, null)]                          // equal ranges are legal
    [InlineData(false, 3, 6, new[] { 4 }, 2, 9, null)]                          // a containing range is legal
    // MERGE, every USING file (SR3) and GIVING (SR12)
    [InlineData(true, null, 6, new[] { 6, 7 }, null, 8, "§14.9.24.3 SR3")]    // the SECOND USING file is larger
    [InlineData(true, null, 6, new[] { 6, 6 }, null, 8, null)]
    [InlineData(true, 3, 6, new[] { 6, 2 }, null, 8, "§14.9.24.3 SR3")]
    [InlineData(true, null, 6, new[] { 6, 6 }, null, 5, "§14.9.24.3 SR12")]
    [InlineData(true, 3, 6, new[] { 6, 4 }, 4, 8, "§14.9.24.3 SR12")]
    [InlineData(true, 3, 6, new[] { 6, 4 }, 3, 6, null)]
    public void RecordSizeRules_RefuseExactlyTheirViolations(
        bool merge, int? sdMin, int sdMax, int[] usingMax, int? giveMin, int giveMax, string? rule)
    {
        // A USING / GIVING variable file keeps integer-3 > integer-2 (§13.18.43.3 SR5): the table never states one
        // whose range is a single size, so a variable GIVING min of null means "fixed".
        var usings = usingMax.Select(m => ((int?)null, m)).ToArray();
        string id = $"PB995T{(merge ? "M" : "S")}{sdMin}{sdMax}{string.Join("", usingMax)}{giveMin}{giveMax}";
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program(id, merge, (sdMin, sdMax), usings, (giveMin, giveMax)), 2002);
        if (rule is null)
        {
            Assert.True(ok, "a legal statement was refused:\n" + string.Join("\n", errors));
            return;
        }
        Assert.False(ok, $"{rule}: the statement describes records the rule forbids");
        Assert.Equal(1, CountRule(errors, rule));
    }
}
