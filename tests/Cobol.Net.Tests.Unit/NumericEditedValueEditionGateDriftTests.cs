// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE RULE, EVERY FORMAT: ISO §13.18.63.3 SR6's edition reaches every general format of the VALUE clause
/// that can carry a numeric literal on a numeric-edited subject.
///
/// <para>SR6 — "If the item is of category numeric-edited, then, subject to Syntax rules 2 and 3, literals in
/// formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their numeric-edited
/// forms according to the rules for the MOVE statement" — is the only rule that admits a numeric literal on a
/// numeric-edited subject, and Annex E.3.3 item 43 dates it as a COBOL-2023 addition. §13.18.63.4 GR19 and
/// §14.9.39.4 GR6 carry it onto the format-3 (condition-name) spelling as well; the determination is on
/// kb/Work PB921.</para>
///
/// <para><b>Why a property and not only a golden.</b> The rule was written down TWICE — its literal-FORM half in
/// <c>DataBinder.ValidateValueCategory</c>, which the item VALUE, the level-88 set and the group VALUE all funnel
/// through, and its EDITION half inline in <c>DataBinder.ScreenValueLiteral</c>, which only the item VALUE
/// reaches. The two places covered different formats, so at <c>--std 85</c> the format-1 spelling
/// <c>01 X PIC ZZ9.99 VALUE 10.</c> was refused while the format-3 spelling
/// <c>01 X PIC ZZ9.99. 88 X-TEN VALUE 10.</c> was ACCEPTED and stored the COBOL-2023 edited image, and a
/// format-4 report entry <c>03 COLUMN 1 PIC ZZ9.99 VALUE 10.</c> was accepted and printed it. A corpus fixture
/// pins each of those programs; this pins the PROPERTY across the formats, so a fifth site — or a fourth one
/// that stops consulting the screen — fails here rather than in a user's program at the wrong edition.</para>
/// </summary>
public sealed class NumericEditedValueEditionGateDriftTests
{
    /// <summary>Every general format of the VALUE clause that §13.18.63.3 SR6 reaches, each carrying a NON-ZERO
    /// numeric literal on a numeric-edited subject — the exact construct Annex E.3.3 item 43 introduced.</summary>
    public static TheoryData<string, string> GatedFormats() => new()
    {
        { "FORMAT1-item", Data("       01  X  PIC ZZ9.99 VALUE 10.\r\n") },
        { "FORMAT2-table", Data("       01  T.\r\n"
            + "           05  E  PIC ZZ9.99 OCCURS 2 VALUE 10 FROM (1) TO (2).\r\n") },
        { "FORMAT3-condition-name", Data("       01  X  PIC ZZ9.99.\r\n           88  X-TEN  VALUE 10.\r\n") },
        { "FORMAT3-condition-name-range", Data("       01  X  PIC ZZ9.99.\r\n"
            + "           88  X-RANGE  VALUE 1 THRU 9.\r\n") },
        { "FORMAT4-report-section", Report("           03  COLUMN 1 PIC ZZ9.99 VALUE 10.\r\n") },
    };

    /// <summary>Below the edition that introduced it (Annex E.3.3 item 43), EVERY format is refused by name.</summary>
    [Theory]
    [MemberData(nameof(GatedFormats))]
    public void NumericLiteralOnANumericEditedSubject_IsRefusedBelow2023_InEveryFormat(string label, string src)
    {
        foreach (int edition in (int[])[85, 2002, 2014])
            Assert.True(Bind(label + edition, src, edition).Any(d => d.Contains("COBOLNET0900", StringComparison.Ordinal)),
                $"{label} at --std {edition}: a numeric literal on a numeric-edited subject drew no COBOLNET0900. "
                + "ISO §13.18.63.3 SR6's permission is a COBOL-2023 addition (Annex E.3.3 item 43), so every "
                + "format it reaches shall be refused below that edition — this format's arm stopped consulting "
                + "the one screen (kb/Work PB921).");
    }

    /// <summary>And at the edition that introduced it, NONE of them is — the over-rejection half of the same
    /// property, which is what a gate widened to more formats can get wrong.</summary>
    [Theory]
    [MemberData(nameof(GatedFormats))]
    public void NumericLiteralOnANumericEditedSubject_IsAcceptedAt2023_InEveryFormat(string label, string src)
        => Assert.DoesNotContain(Bind(label + "2023", src, 2023),
            d => d.Contains("COBOLNET0900", StringComparison.Ordinal));

    /// <summary>The literals SR6 does NOT gate, at the LOWEST edition — the screen's complement. SR6 exempts
    /// "the figurative constant ZERO or ZEROES and the integer and decimal forms of the literal zero" at all
    /// editions, and §13.18.63.3 SR7's alphanumeric/national edited image was always the pre-2023 spelling, so
    /// none of these may be refused at COBOL-85.</summary>
    [Theory]
    [InlineData("EDITED-IMAGE", "       01  X  PIC ZZ9.99 VALUE \" 10.00\".\r\n")]
    [InlineData("COND-EDITED-IMAGE", "       01  X  PIC ZZ9.99.\r\n           88  X-TEN  VALUE \" 10.00\".\r\n")]
    [InlineData("LITERAL-ZERO", "       01  X  PIC ZZ9 VALUE 0.\r\n")]
    [InlineData("COND-LITERAL-ZERO", "       01  X  PIC ZZ9.\r\n           88  X-ZERO  VALUE 0.\r\n")]
    [InlineData("FIGURATIVE-ZERO", "       01  X  PIC ZZ9 VALUE ZERO.\r\n")]
    [InlineData("COND-FIGURATIVE-ZERO", "       01  X  PIC ZZ9.\r\n           88  X-ZERO  VALUE ZERO.\r\n")]
    [InlineData("NOT-NUMERIC-EDITED", "       01  X  PIC 9(3) VALUE 10.\r\n")]
    public void TheLiteralsSr6DoesNotGate_AreNotRefusedAtTheLowestEdition(string label, string ws)
        => Assert.DoesNotContain(Bind(label, Data(ws), 85),
            d => d.Contains("COBOLNET0900", StringComparison.Ordinal));

    // ── harness ─────────────────────────────────────────────────────────────────────────────────────────────

    private static string Data(string ws) =>
        "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. NEVGATE.\r\n"
        + "       DATA DIVISION.\r\n"
        + "       WORKING-STORAGE SECTION.\r\n"
        + ws
        + "       PROCEDURE DIVISION.\r\n"
        + "       MAIN-PARA.\r\n"
        + "           STOP RUN.\r\n";

    private static string Report(string entry) =>
        "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. NEVGATER.\r\n"
        + "       ENVIRONMENT DIVISION.\r\n"
        + "       INPUT-OUTPUT SECTION.\r\n"
        + "       FILE-CONTROL.\r\n"
        + "           SELECT PRT ASSIGN TO \"nevgater.txt\".\r\n"
        + "       DATA DIVISION.\r\n"
        + "       FILE SECTION.\r\n"
        + "       FD  PRT REPORT IS R-NE.\r\n"
        + "       REPORT SECTION.\r\n"
        + "       RD  R-NE PAGE LIMIT 20 LINES.\r\n"
        + "       01  DET TYPE DE LINE PLUS 1.\r\n"
        + entry
        + "       PROCEDURE DIVISION.\r\n"
        + "       MAIN-PARA.\r\n"
        + "           STOP RUN.\r\n";

    private static List<string> Bind(string label, string src, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(),
            "cn_nevgate_" + new string(label.Where(char.IsLetterOrDigit).ToArray())
            + "_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend() { InitialFormat = InitialReferenceFormat.Auto }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var binder = new DataBinder(new EditionContext(edition));
            binder.Bind(program);
            return binder.Edition.Diagnostics.ToList();
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
        }
    }
}
