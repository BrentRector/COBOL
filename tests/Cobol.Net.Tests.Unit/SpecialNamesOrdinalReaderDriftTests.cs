// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY SPECIAL-NAMES ORDINAL IS READ BY THE ONE INTEGER READER (kb/Work PB1091, PB1557's sibling).
/// <para>An ordinal — the unsigned integer that names a 1-based position in a character set (ISO §12.3.7.3 SR14 b1/c1
/// for an ALPHABET literal phrase, SR17 b2/c2 for CLASS, SR16 e/f for SYMBOLIC CHARACTERS) — can only be out of range:
/// it is an INTEGER whatever its length. <c>int.TryParse</c> answers "is this an integer?" and "what is its value?" as
/// one question and gets the first wrong for a literal too long for an <c>int</c>, which is how SYMBOLIC CHARACTERS
/// skipped a name without a diagnostic and ALPHABET / CLASS reported the noninteger-literal class rule for an integer.
/// The three sites now read through <c>IntegerOperandRules.HostValue</c>, which separates the two questions and
/// saturates; this test keeps the NEXT ordinal site (a fourth SPECIAL-NAMES clause, a private helper that re-parses
/// the digits) automatic by requiring <c>DataBinder.Switches.cs</c> to carry no host-integer parse of its own.</para>
/// </summary>
public sealed class SpecialNamesOrdinalReaderDriftTests
{
    private static readonly string[] OwnParses =
        ["int.TryParse(", "int.Parse(", "long.TryParse(", "long.Parse(", "uint.TryParse(", "OrdinalValue("];

    /// <summary>The code of a source line — its text before any <c>//</c> comment (this file's prose names the forbidden
    /// calls in its own comments and strings' neighbours, never inside an executable statement).</summary>
    private static string Code(string line)
    {
        int at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    /// <summary>⛔ THE SCANNER IS PROVEN ABLE TO FAIL before its silence is trusted: a parse in code is seen, the same
    /// words in a comment are not.</summary>
    [Fact]
    public void TheScanner_SeesAParseInCode_AndIgnoresAComment()
    {
        Assert.Contains(OwnParses, p => Code("if (!int.TryParse(text, out int n)) continue;").Contains(p));
        Assert.DoesNotContain(OwnParses, p => Code("/// it used to call int.TryParse(text) and continue").Contains(p));
    }

    [Fact]
    public void TheSpecialNamesBinder_ReadsEveryOrdinalThroughTheOneReader()
    {
        string path = TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Switches.cs");
        var lines = File.ReadAllLines(path);
        var offenders = new List<string>();
        int reads = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string code = Code(lines[i]);
            foreach (string parse in OwnParses)
                if (code.Contains(parse, StringComparison.Ordinal))
                    offenders.Add($"DataBinder.Switches.cs:{i + 1}: {lines[i].Trim()}");
            if (code.Contains("IntegerOperandRules.HostValue(", StringComparison.Ordinal)) reads++;
        }
        Assert.True(offenders.Count == 0,
            "the SPECIAL-NAMES binder parses an integer itself — an ordinal is read by IntegerOperandRules.HostValue, "
            + "the ONE reader that separates 'is it an integer' from 'what is its value' and saturates (kb/Work PB1091):\n"
            + string.Join("\n", offenders));
        // The literal phrases (ALPHABET, CLASS) and SYMBOLIC CHARACTERS each read one: a count of zero would make the
        // silence above vacuous.
        Assert.True(reads >= 2, $"expected the ordinal reads of the literal phrases and SYMBOLIC CHARACTERS, found {reads}");
    }
}
