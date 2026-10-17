// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NEGATIVE GOLDEN'S HEADER THAT STATES ITS EXPECTED DIAGNOSTIC NAMES A CODE ITS <c>.err</c> PINS (kb/Work PB1784,
/// branch review R1-1).
/// <para><b>What happened.</b> kb/Work PB1784 gave each OO definition rule its own descriptor: the OVERRIDE / FINAL
/// prototype rules left COBOLNET0840 for COBOLNET4212, the interface INHERITS cycle left it for COBOLNET2791, and the
/// GOBACK / EXIT RAISING operand rule left COBOLNET0849 for COBOLNET4211. The <c>.err</c> files were re-pinned, and the
/// gate stayed green, but five golden headers still said <c>Expected: COBOLNET0840</c>, <c>Diagnostic COBOLNET0840</c>
/// or <c>-&gt; COBOLNET0849</c>. Nothing reads a header, so a reader of the golden was told the wrong diagnostic and
/// nothing could notice.</para>
/// <para><b>The rule.</b> In the leading comment block of every negative golden whose <c>.err</c> pins at least one
/// <c>COBOLNETnnnn</c> code, every code the header STATES as the expectation — written after <c>Expected</c>,
/// <c>Diagnostic</c> or <c>-&gt;</c> — is one the <c>.err</c> pins. A code named any other way (a history sentence, a
/// sibling rule's code, the code a fix replaced) is prose and is not read. A <c>.err</c> that pins only message text
/// carries no code to compare, so its golden is not asked.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NegativeGoldenHeaderCodeDriftTests
{
    /// <summary>A code the header states as the expected diagnostic: <c>Expected: COBOLNETnnnn</c>,
    /// <c>Diagnostic COBOLNETnnnn</c> (optionally <c>is</c>) or <c>-&gt; COBOLNETnnnn</c>.</summary>
    private static readonly Regex StatedCode = new(
        @"(?:\bExpected\b|\bDiagnostic\b|->)\s*:?\s*(?:is\s+)?(COBOLNET\d{4})", RegexOptions.CultureInvariant);

    private static readonly Regex AnyCode = new(@"COBOLNET\d{4}", RegexOptions.CultureInvariant);

    /// <summary>The golden's leading comment block, one line per comment line, the <c>*&gt;</c> marker and the
    /// sequence area removed, joined so a statement that wraps a line still reads as one.</summary>
    private static string HeaderOf(IEnumerable<string> lines)
    {
        var text = new List<string>();
        foreach (string line in lines)
        {
            int marker = line.IndexOf("*>", System.StringComparison.Ordinal);
            if (marker < 0 || line[..marker].Trim().Length != 0)
                break;
            text.Add(line[(marker + 2)..].Trim());
        }

        return string.Join(' ', text);
    }

    /// <summary>The codes the header states that the <c>.err</c> does not pin.</summary>
    internal static IReadOnlyList<string> StaleStatedCodes(IEnumerable<string> cobLines, IReadOnlySet<string> pinned)
        => StatedCode.Matches(HeaderOf(cobLines)).Select(m => m.Groups[1].Value)
            .Where(code => !pinned.Contains(code)).Distinct().ToArray();

    /// <summary>The detector is driven first by the shape it exists for: l1m7-method-prototype-override's header as
    /// it read before this guard, against its re-pinned <c>.err</c>, and the wrapped <c>Diagnostic</c> form of
    /// l1c18-method-prototype-final. A detector that stopped looking turns this red rather than the sweep green.</summary>
    [Fact]
    public void TheDetector_TripsOnAHeaderStatingASupersededCode()
    {
        var pinned = new HashSet<string> { "COBOLNET4212" };
        Assert.Equal(["COBOLNET0840"], StaleStatedCodes(
            ["      *> reject-at: 2002 2014 2023", "      *> Expected: COBOLNET0840 with the prototype message head.",
             "       IDENTIFICATION DIVISION."], pinned));
        Assert.Equal(["COBOLNET0840"], StaleStatedCodes(
            ["      *> no OVERRIDE is written, so only SR8 is violated.  Diagnostic",
             "      *> COBOLNET0840 with the prototype message head."], pinned));
        Assert.Empty(StaleStatedCodes(
            ["      *> Expected: COBOLNET4212; before kb/Work PB1784 it was COBOLNET0840."], pinned));
    }

    /// <summary>Every negative golden whose <c>.err</c> pins a code states only pinned codes in its header.</summary>
    [Fact]
    public void EveryStatedExpectation_IsAPinnedCode()
    {
        string dir = TestRepo.Tests("conformance", "negative");
        var stale = new List<string>();
        int asked = 0;
        foreach (string cob in Directory.EnumerateFiles(dir, "*.cob").Order(System.StringComparer.Ordinal))
        {
            string err = Path.ChangeExtension(cob, ".err");
            if (!File.Exists(err))
                continue;
            var pinned = AnyCode.Matches(File.ReadAllText(err)).Select(m => m.Value).ToHashSet();
            if (pinned.Count == 0)
                continue;
            asked++;
            foreach (string code in StaleStatedCodes(File.ReadLines(cob), pinned))
                stale.Add($"{Path.GetFileName(cob)}: the header states {code}; the .err pins "
                    + string.Join(", ", pinned.Order(System.StringComparer.Ordinal)));
        }

        Assert.True(asked > 0, "no negative golden pins a code: the sweep read nothing");
        Assert.True(stale.Count == 0, "a negative golden's header states a code its .err does not pin — correct the "
            + "header to the rule's current code:\n" + string.Join("\n", stale));
    }
}
