// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A STAGE-OWNED DIRECTIVE OPERAND ENDS WHERE ITS GENERAL FORMAT ENDS (kb/Work PB1365). ISO §7.3.3 SR3 (fixed form) and
/// SR4 (free form): a compiler directive "may be followed only by space characters and an optional inline comment".
/// <see cref="CompilerDirectiveCatalog.CheckOperand"/> enforces that centrally for every closed-word row, but a
/// <see cref="DirectiveOperandForm.Stage"/> row is skipped by it BY DESIGN, so the stage that owns the operand must consume it
/// whole — and <c>TurnDirectiveProcessor</c> did not: its loop <c>break</c>s at CHECKING and a word after ON or OFF took
/// effect in silence. The rule is written here over the rows, not over the one stage that failed: EVERY directive word of
/// EVERY Stage row must reject one word past its conforming operand, in both reference formats, through the real front end,
/// and must still accept its conforming operand with and without an inline comment. A new Stage row without a witness
/// line below is itself a red test, so the next Stage directive cannot ship unchecked.
/// </summary>
public sealed class StageOwnedDirectiveOperandDriftTests : CobolNetTestBase
{
    /// <summary>One conforming program fragment per directive word of a Stage row, written so <c>{0}</c> sits at the end of
    /// the line that carries the word. The fragment is the smallest legal context for that word (an <c>&gt;&gt;ELSE</c> needs
    /// its <c>&gt;&gt;IF</c> / <c>&gt;&gt;END-IF</c>), so the only thing a mutation changes is what follows the operand.</summary>
    private static readonly Dictionary<string, string[]> Witness = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TURN"] = [">>TURN EC-ALL CHECKING ON{0}"],
        ["FLAG-02"] = [">>FLAG-02 ALL ON{0}"],
        ["FLAG-14"] = [">>FLAG-14 ALL ON{0}"],
        ["COBOL-WORDS"] = [">>COBOL-WORDS UNDEFINE \"MOVE\"{0}"],
        ["DEFINE"] = [">>DEFINE FOO AS 1{0}"],
        ["IF"] = [">>IF 1 = 1{0}", ">>END-IF"],
        ["ELSE"] = [">>IF 1 = 1", ">>ELSE{0}", ">>END-IF"],
        ["END-IF"] = [">>IF 1 = 1", ">>END-IF{0}"],
        ["EVALUATE"] = [">>EVALUATE 1{0}", ">>WHEN 1", ">>END-EVALUATE"],
        ["WHEN"] = [">>EVALUATE 1", ">>WHEN 1{0}", ">>END-EVALUATE"],
        ["END-EVALUATE"] = [">>EVALUATE 1", ">>WHEN 1", ">>END-EVALUATE{0}"],
        ["DISPLAY"] = [">>DISPLAY \"A\"{0}"],
    };

    private static IEnumerable<string> StageWords() =>
        ConstructRegistry.Entries
            .Where(e => e.DirectiveOperand is { Form: DirectiveOperandForm.Stage })
            .SelectMany(e => e.DirectiveWords);

    [Fact]
    public void EveryStageDirectiveWord_HasAWitnessLine()
    {
        var words = StageWords().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = words.Where(w => !Witness.ContainsKey(w)).Order(StringComparer.Ordinal).ToList();
        var stale = Witness.Keys.Where(w => !words.Contains(w)).Order(StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            $"these directive words belong to a Stage row but have no witness line: [{string.Join(", ", missing)}] — add one, "
            + "so the stage that owns the operand is proven to consume it whole (ISO §7.3.3 SR3/SR4)");
        Assert.True(stale.Count == 0, $"these witness lines name no Stage-row directive word: [{string.Join(", ", stale)}]");
    }

    [Theory]
    [InlineData(InitialReferenceFormat.Fixed)]   // §7.3.3 SR3
    [InlineData(InitialReferenceFormat.Free)]    // §7.3.3 SR4
    public void EveryStageDirectiveWord_RejectsAWordAfterItsOperand_AndAcceptsItsOperandAndAComment(InitialReferenceFormat format)
    {
        var bad = new List<string>();
        foreach (string word in StageWords())
        {
            Assert.True(Witness.TryGetValue(word, out var lines), $"no witness line for >>{word}");
            if (Errors(lines!, "", format) is { Count: > 0 } conforming)
                bad.Add($">>{word}: its conforming operand is rejected — {conforming[0]}");
            if (Errors(lines!, "   *> why", format) is { Count: > 0 } commented)
                bad.Add($">>{word}: an inline comment after the operand is rejected — {commented[0]} (§7.3.3 SR3/SR4)");
            if (Errors(lines!, " ZZJUNK", format).Count == 0)
                bad.Add($">>{word}: a word after the operand is accepted in silence (§7.3.3 SR3/SR4)");
        }

        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    /// <summary>Compile <paramref name="lines"/> with <paramref name="suffix"/> substituted into the witness line and return
    /// the error diagnostics the REAL front end reports (every stage, in order — never one stage called on its own).</summary>
    private List<string> Errors(string[] lines, string suffix, InitialReferenceFormat format)
    {
        string indent = format == InitialReferenceFormat.Free ? "" : "       ";
        string text = string.Concat(lines.Select(l => indent + string.Format(l, suffix) + "\n"))
            + $"{indent}IDENTIFICATION DIVISION.\n{indent}PROGRAM-ID. SODO.\n{indent}PROCEDURE DIVISION.\n"
            + $"{indent}    STOP RUN.\n";
        string path = Path.Combine(TempDir, "sodo.cob");
        File.WriteAllText(path, text);
        var bag = new DiagnosticBag();
        new CobolNet.Frontend.Frontend { InitialFormat = format, DialectLevel = 2023 }.Parse(path, bag);
        return bag.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => $"{d.Code}: {d.Message}").ToList();
    }
}
