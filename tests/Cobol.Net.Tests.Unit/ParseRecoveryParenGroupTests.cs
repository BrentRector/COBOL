// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE SYNTAX ERROR IN A PARENTHESIZED GROUP IS ONE DIAGNOSTIC (kb/Work PB2152). ISO §8.3.5 4): "Except in
/// pseudo-text, parentheses may appear only in balanced pairs of left and right parentheses", so the parser's error
/// recovery consumes a group it skips WHOLE (<c>CobolErrorStrategy.ConsumeUntil</c>) and never deletes one half of a
/// pair (<c>CobolErrorStrategy.SingleTokenDeletion</c>).
/// <para>The behavior-neutrality oracle caught the regression these pin: after the reference paren became its own
/// token (kb/Work PB2113) each of these sources reported a SECOND error, "unexpected ')'", for the one fault it holds,
/// while the negative fixtures that carry them stayed green, because a fixture asserts that its diagnostic is
/// PRESENT, never that it is the only one. These assert the count.</para>
/// </summary>
public sealed class ParseRecoveryParenGroupTests
{
    public static TheoryData<string, string> OneFaultSources() => new()
    {
        // §13.18.20.3 SR1: the entry's data-name is subscripted — the '1' inside the group used to resync as a level-number.
        { "entry-name-subscripted", "       01 G.\n          05 A(1) PIC X(3).\n       PROCEDURE DIVISION.\n           STOP RUN.\n" },
        // §8.3.5 4): a group around a DISPLAY operand delimits nothing — single-token deletion used to drop the '('.
        { "display-operand-group", "       01 W-S PIC X(5).\n       PROCEDURE DIVISION.\n           DISPLAY (W-S).\n           STOP RUN.\n" },
        // §7.3.10.4 GR3 (>>COBOL-WORDS UNDEFINE "PICTURE"): `X(3)` is no picture string — the '3' resynced as a level-number.
        { "picture-withdrawn", "       01 X PICTURE X(3).\n       PROCEDURE DIVISION.\n           STOP RUN.\n" },
    };

    [Theory]
    [MemberData(nameof(OneFaultSources))]
    public void ParenthesizedGroupSkippedInRecovery_ReportsOneError(string name, string body)
    {
        string prefix = name == "picture-withdrawn" ? "       >>COBOL-WORDS UNDEFINE \"PICTURE\"\n" : "";
        string source = prefix + "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PRPG.\n       DATA DIVISION.\n"
            + "       WORKING-STORAGE SECTION.\n" + body;
        var errors = Compile(source);
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("'('", errors[0]);
    }

    private static IReadOnlyList<string> Compile(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB2152_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prpg.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prpg.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.False(r.Success, "the source holds one syntax error and must be rejected");
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
