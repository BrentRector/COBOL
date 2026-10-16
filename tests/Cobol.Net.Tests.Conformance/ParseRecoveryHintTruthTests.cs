// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The parse-error recovery hints say only what is TRUE (kb/Work PB2770). A hint keyed on a word's spelling used to win
/// the message wherever the parse happened to fail at that word, and each such hint claimed a limitation the compiler
/// does not have: <c>MOVE 1 SET X TO 2</c> (MOVE lacks its TO) answered "This SET form may not be supported",
/// <c>MOVE 1 SEARCH …</c> "SEARCH statement may not be fully supported", and <c>EVALUATE X ALSO ALSO X …</c>
/// "EVALUATE with ALSO (multi-subject) may not be fully supported". Those hints are deleted, so each row is refused
/// with no "may not be" claim. The misplaced-scope-terminator hint (ISO §14.5.3.2: an explicit scope terminator closes
/// the most recent unterminated statement having its statement-name) is keyed on the token TYPE and on the statement
/// rules the ATN says can match it: END-SEARCH inside SEARCH ALL has its matching statement, so the hint is silent; an
/// END-IF or END-SEARCH outside any IF or SEARCH names the statement it lacks; and a user-defined word spelled END-…
/// is no terminator, so it is never told it lacks a statement named after its spelling.
/// </summary>
public sealed class ParseRecoveryHintTruthTests
{
    [Theory]
    [InlineData("PBHT01", "MOVE 1 SET W-N TO 2")]
    [InlineData("PBHT02", "MOVE 1 SEARCH T-E WHEN T-E (IX) = \"A\" DISPLAY 1 END-SEARCH")]
    [InlineData("PBHT03", "EVALUATE W-N ALSO ALSO W-N WHEN 1 ALSO 2 DISPLAY 1 END-EVALUATE")]
    [InlineData("PBHT04", "SEARCH ALL T-E END-SEARCH")]
    [InlineData("PBHT05", "SEARCH T-E END-SEARCH")]
    [InlineData("PBHT06", "MOVE 1 END-FLAG")]
    public void ARefusedStatement_IsNeverToldItsConstructIsUnsupported_OrUnmatched(string pid, string statement)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' is outside its general format, and it compiled");
        EditionHarness.AssertNoDiagnostic(diagnostics, "may not be");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOL0306");
    }

    /// <summary>
    /// A literal whose text spells a verb after a period is a literal, not a paragraph name run into a statement: no COBOL
    /// word can carry a period (ISO §8.3.2.1), so the deleted COBOL0304 hint, keyed on a token's text containing ".MOVE"
    /// and its siblings, could only ever fire on a literal or a PICTURE string, and there it was false.
    /// </summary>
    [Theory]
    [InlineData("PBHT21", "MOVE W-N \"A.MOVE\"")]
    [InlineData("PBHT22", "GO TO \"PARA.PERFORM\"")]
    public void ALiteralSpellingPeriodVerb_IsNeverToldAParagraphPeriodIsMissing(string pid, string statement)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' is outside its general format, and it compiled");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOL0304");
        EditionHarness.AssertNoDiagnostic(diagnostics, "paragraph name");
        EditionHarness.AssertNoDiagnostic(diagnostics, "qualified reference");
    }

    [Theory]
    [InlineData("PBHT11","MOVE 1 TO W-N END-IF", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT12", "MOVE 1 TO W-N END-SEARCH", "END-SEARCH appears without a matching SEARCH statement.")]
    public void AScopeTerminatorOutsideItsStatement_NamesTheStatement(string pid, string statement, string message)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' closes a statement that is not open, and it compiled");
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL0306");
        EditionHarness.AssertHasDiagnostic(diagnostics, message);
    }

    private static string Program(string pid, string statement) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(4) VALUE 0.
        01 END-FLAG PIC 9.
        01 T.
           05 T-E PIC X OCCURS 5 ASCENDING KEY T-E INDEXED BY IX.
        PROCEDURE DIVISION.
        MAIN-PARA.
            {statement}
            STOP RUN.

        """;
}
