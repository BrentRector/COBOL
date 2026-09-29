// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ EVERY FILE DESCRIPTION CLAUSE THE GRAMMAR PARSES HAS DECIDED WHICH §13.4.5.2 FORMATS CONTAIN IT (kb/Work PB1238).
///
/// <para><b>Why it exists.</b> Until PB1238 no code decided which of ISO §13.4.5.2's three formats an FD was, so
/// every clause bound against every organization: LINAGE on a relative file, CODE-SET on an indexed one and LINAGE
/// on a report FD compiled clean, and the five file-control-entry alternatives the grammar carries in the FD
/// position had no binder arm at all — parsed and silently DROPPED. The admissibility table
/// (<see cref="FileDescriptionFormats"/>) is only automatic for the NEXT clause if a new <c>fileDescriptionClause</c>
/// alternative cannot land without a row; these facts read the generated parser, not a list, so it cannot.</para>
/// </summary>
public sealed class FileDescriptionFormatDriftTests
{
    /// <summary>The alternatives of <c>fileDescriptionClause</c> as the GENERATED parser exposes them: its
    /// no-argument accessors returning a rule context, less the <c>unrecognizedClause</c> error production.</summary>
    private static IReadOnlyList<Type> Alternatives() =>
        typeof(Core.FileDescriptionClauseContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0
                        && typeof(ParserRuleContext).IsAssignableFrom(m.ReturnType)
                        && m.ReturnType != typeof(Core.UnrecognizedClauseContext))
            .Select(m => m.ReturnType)
            .Distinct()
            .ToList();

    /// <summary>Both directions: every alternative has a row (a new clause is decided, never bound by default)
    /// and every row names a live alternative (a deleted clause leaves no stale row behind).</summary>
    [Fact]
    public void TheAdmissibilityTable_IsTotalOverTheGrammarsAlternatives()
    {
        var alternatives = Alternatives();
        Assert.True(alternatives.Count >= 15,
            $"only {alternatives.Count} fileDescriptionClause alternative(s) found — the reflection probe is broken "
            + "and the obligations below would hold vacuously");
        var missing = alternatives.Where(t => !FileDescriptionFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0, "fileDescriptionClause alternative(s) with no FileDescriptionFormats row, so no "
            + "§13.4.5.2 format decides them: " + string.Join(", ", missing));
        var stale = FileDescriptionFormats.ByContext.Keys.Except(alternatives).Select(t => t.Name).ToList();
        Assert.True(stale.Count == 0, "FileDescriptionFormats row(s) naming no fileDescriptionClause alternative: "
            + string.Join(", ", stale));
        Assert.Equal(FileDescriptionFormats.Rows.Count, FileDescriptionFormats.ByContext.Count);
    }

    /// <summary>A row either names the formats that contain its clause or names the entry the clause belongs to —
    /// never neither (a clause no format admits and no message can redirect) and never both.</summary>
    [Fact]
    public void EveryRow_IsAdmittedSomewhere_OrNamesItsHomeEntry()
    {
        foreach (var row in FileDescriptionFormats.Rows)
            Assert.True((row.Formats != FileDescriptionFormat.None) ^ (row.HomeEntry is not null),
                $"{row.Name}: Formats={row.Formats}, HomeEntry={row.HomeEntry ?? "null"}");
    }

    /// <summary>The format is decided from the file control entry first: §9.1.22 makes a report file SEQUENTIAL,
    /// so a relative or indexed file is Format 2 even when its FD writes a REPORT clause (§13.4.5.3 SR7).</summary>
    [Theory]
    [InlineData(FileOrganization.Sequential, false, "Sequential")]
    [InlineData(FileOrganization.LineSequential, false, "Sequential")]
    [InlineData(FileOrganization.Sequential, true, "Report")]
    [InlineData(FileOrganization.LineSequential, true, "Report")]
    [InlineData(FileOrganization.Relative, false, "RelativeOrIndexed")]
    [InlineData(FileOrganization.Indexed, true, "RelativeOrIndexed")]
    public void TheFormat_IsDecidedByOrganizationThenReportClause(
        FileOrganization organization, bool hasReport, string expected) =>
        Assert.Equal(Enum.Parse<FileDescriptionFormat>(expected), FileDescriptionFormats.Of(organization, hasReport));
}
