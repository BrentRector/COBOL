// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB728 / PB575 — the invariant that keeps ISO §8.8.4.2.1's group sentence TRUE as condition-name codegen
/// grows: <b>a condition-name test over a GROUP conditional variable renders the same comparison rules as the
/// identical test over the ELEMENTARY item of that group's class.</b>
///
/// <para><b>Why this test exists.</b> §8.8.4.5.3 GR2 — "The rules for comparing a conditional variable with a
/// condition-name value are the same as those specified for relation conditions" — and §8.8.4.2.1 — "A national
/// group item or a bit group item shall be treated as an elementary national data item or an elementary bit data
/// item, respectively" — together say the subject's CLASS decides the comparison, for a group exactly as for an
/// elementary item. The renderer had that rule written down twice: the operand TEXT came from the ONE category
/// reader (<c>DataItem.OperandPic</c>, which answers the §13.18.29.4 GR1b/GR2b as-if PICTURE for a bit / national
/// group), while the COLLATING SEQUENCE came from a raw <c>Pic?.Category</c>, which is null for every group. The
/// two halves disagreed about what the operand IS: a <c>GROUP-USAGE NATIONAL</c> 88 rendered a national image
/// (<c>.AsNat()</c>) weighed on the ALPHANUMERIC table, and a <c>GROUP-USAGE BIT</c> 88 lost §8.8.4.2.8's
/// right-extension with boolean zeros. Both were silent: under the default native sequences the two collations
/// coincide, so every existing green test passed.</para>
///
/// <para>The pairing is asserted on the EMITTED C#, per class, by comparing the group leg's rendered comparison
/// against its elementary twin's — everything but the first argument (the operand read, which legitimately
/// differs: a group reads through its as-if channel). A future site that re-derives the category from a raw
/// picture fails here rather than in a user's internationalized program.</para>
/// </summary>
public sealed class ConditionNameSubjectClassDriftTests
{
    /// <summary>Two DIFFERENT collating sequences, deliberately, so neither leg can pass for the wrong reason: an
    /// alphanumeric alphabet in which 'A' and 'B' share one position (ALSO) and a national alphabet in which they
    /// do not. A test that renders the wrong sequence therefore renders a DIFFERENT argument, not merely a
    /// different name for the same order.</summary>
    private const string Alphabets = """
                   ALPHABET AN-EQ IS "A" ALSO "B"
                   ALPHABET NAT-D FOR NATIONAL IS N"A" N"B".
               OBJECT-COMPUTER. DRIFT-COMPUTER
                   PROGRAM COLLATING SEQUENCE IS AN-EQ NAT-D.
        """;

    private static string Program(string decls, string tests) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. CNDRIFT.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
        {Alphabets}
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {decls}
               PROCEDURE DIVISION.
               MAIN.
        {tests}
                   STOP RUN.
        """;

    /// <summary>The GROUP leg and the ELEMENTARY leg of one class, as a source pair. The 88s carry the SAME
    /// literal so the only admissible difference in the emitted comparison is the operand read.</summary>
    public static TheoryData<string, string, string> ClassPairs() => new()
    {
        // NATIONAL — §8.8.4.2.9: ordered by the national program collating sequence (NAT-D), never the
        // alphanumeric one (§13.18.29.4 GR2b gives the group the as-if PICTURE N(3)).
        {
            "national",
            """
                   01 GN GROUP-USAGE NATIONAL.
                      88 GN-IS-A VALUE N"AAA".
                      05 GN-A PIC N(3).
                   01 EN PIC N(3).
                      88 EN-IS-A VALUE N"AAA".
            """,
            """
                       IF GN-IS-A DISPLAY "G" END-IF.
                       IF EN-IS-A DISPLAY "E" END-IF.
            """
        },
        // BOOLEAN — §8.8.4.2.8: a VALUE comparison "regardless of their usage", the shorter operand extended on
        // the right with boolean zeros, and never the alphanumeric weight table (§13.18.29.4 GR1b gives the group
        // the as-if PICTURE 1(3)).
        {
            "boolean",
            """
                   01 GB GROUP-USAGE BIT.
                      88 GB-ON VALUE B"10".
                      05 GB-A PIC 1(3) USAGE BIT.
                   01 EB PIC 1(3) USAGE BIT.
                      88 EB-ON VALUE B"10".
            """,
            """
                       IF GB-ON DISPLAY "G" END-IF.
                       IF EB-ON DISPLAY "E" END-IF.
            """
        },
        // ALPHANUMERIC — the OVER-REJECTION CONTROL: an ordinary group has no as-if PICTURE, §8.8.4.2.1 treats it
        // as an elementary alphanumeric data item, and §8.8.4.2.7 collates it under the alphanumeric sequence.
        // The fix must not move this leg.
        {
            "alphanumeric",
            """
                   01 GA.
                      88 GA-IS-A VALUE "AAA".
                      05 GA-A PIC X(3).
                   01 EA PIC X(3).
                      88 EA-IS-A VALUE "AAA".
            """,
            """
                       IF GA-IS-A DISPLAY "G" END-IF.
                       IF EA-IS-A DISPLAY "E" END-IF.
            """
        },
    };

    [Theory]
    [MemberData(nameof(ClassPairs))]
    public void GroupAndElementaryConditionName_RenderTheSameComparisonRules(string klass, string decls, string tests)
    {
        string dir = Directory.CreateTempSubdirectory("cndrift").FullName;
        try
        {
            string src = Path.Combine(dir, "CNDRIFT.cob");
            File.WriteAllText(src, Program(decls, tests));
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, $"{klass}: " + string.Join("\n", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);

            var compares = CompareCalls(File.ReadAllText(r.GeneratedCsPath!));
            // Exactly two comparisons reach the emitted body: the group leg then the elementary leg. A different
            // count means the source stopped exercising the pairing — fail loudly rather than compare nothing.
            Assert.Equal(2, compares.Count);
            string group = TailAfterFirstArgument(compares[0]), elementary = TailAfterFirstArgument(compares[1]);
            Assert.True(group == elementary,
                $"{klass}: the GROUP condition-name renders '{compares[0]}' where its ELEMENTARY twin renders "
                + $"'{compares[1]}' — the comparison rules differ after the operand read ('{group}' vs "
                + $"'{elementary}'). ISO §8.8.4.5.3 GR2 + §8.8.4.2.1: the subject's CLASS decides the comparison, "
                + "for a group as for an elementary item.");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>Every <c>CobolString.Compare(…)</c> argument list in the emitted PROCEDURE body, in source order,
    /// balanced on parentheses (the operand read is itself a call, so a naive split would truncate it).</summary>
    private static List<string> CompareCalls(string cs)
    {
        const string Marker = "CobolString.Compare(";
        var found = new List<string>();
        for (int i = cs.IndexOf(Marker, StringComparison.Ordinal); i >= 0;
             i = cs.IndexOf(Marker, i + 1, StringComparison.Ordinal))
        {
            int open = i + Marker.Length, depth = 1, j = open;
            for (; j < cs.Length && depth > 0; j++)
            {
                if (cs[j] == '(') depth++;
                else if (cs[j] == ')') depth--;
            }
            found.Add(cs[open..(j - 1)]);
        }
        return found;
    }

    /// <summary>The SCREENS must agree too, not only the rendered comparison. §13.18.63.3's Format-3 syntax rules
    /// are stated over "the category of the subject of the entry" (SR29) and over the subject's class
    /// (SR4/SR5/SR10, carried into Format 3 by SR24), and §13.18.29.4 GR1b/GR2b give a bit / national GROUP that
    /// category — so a THROUGH range must draw the SAME verdict on the group as on its elementary twin. Every
    /// guard in <c>BindCondition</c> used to read the raw picture, which a group does not have, so the group leg
    /// slipped every screen: a boolean THROUGH that SR29 forbids compiled, and the range then reached codegen and
    /// was answered on the wrong collating sequence (kb/Work PB575 + PB728, landed together).
    ///
    /// <para>⛔ This asserts AGREEMENT, not a particular diagnostic. Pinning the identity of a decline would make a
    /// green test hold the stage open; if the national THROUGH range's staged decline is later discharged, both
    /// legs simply compile and this still passes — what it can never allow again is the two legs DISAGREEING.</para></summary>
    [Theory]
    // §13.18.63.3 SR29 — a boolean subject may not carry THROUGH at all, for either subject shape.
    [InlineData("bit", """
           01 GB GROUP-USAGE BIT.
              88 GB-R VALUE B"000" THRU B"111".
              05 GB-A PIC 1(3) USAGE BIT.
    """, """
           01 EB PIC 1(3) USAGE BIT.
              88 EB-R VALUE B"000" THRU B"111".
    """)]
    // A national THROUGH range is spec-legal (§14.7.8 rule 2 orders it under the national sequence); whatever this
    // compiler does with it, it must do the same to both subject shapes.
    [InlineData("national", """
           01 GN GROUP-USAGE NATIONAL.
              88 GN-R VALUE N"AAA" THRU N"CCC".
              05 GN-A PIC N(3).
    """, """
           01 EN PIC N(3).
              88 EN-R VALUE N"AAA" THRU N"CCC".
    """)]
    // The class funnel (§13.18.63.3 SR5 over a national subject): an alphanumeric literal is not a national one.
    [InlineData("national-class", """
           01 GN GROUP-USAGE NATIONAL.
              88 GN-C VALUE "AB".
              05 GN-A PIC N(3).
    """, """
           01 EN PIC N(3).
              88 EN-C VALUE "AB".
    """)]
    public void GroupAndElementaryConditionName_DrawTheSameScreens(string klass, string groupDecl, string elemDecl)
    {
        Assert.Equal(CompileVerdict(klass + "-g", groupDecl), CompileVerdict(klass + "-e", elemDecl));
    }

    /// <summary>Whether a one-item program carrying <paramref name="decls"/> compiles, and the diagnostic CODES it
    /// draws (never their text or their line numbers, which legitimately differ between the two shapes).</summary>
    private static string CompileVerdict(string tag, string decls)
    {
        string dir = Directory.CreateTempSubdirectory("cnscreen").FullName;
        try
        {
            string src = Path.Combine(dir, "CNDRIFT.cob");
            File.WriteAllText(src, Program(decls, "            CONTINUE."));
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            var codes = r.Errors
                .SelectMany(e => e.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(w => w.StartsWith("COBOLNET", StringComparison.Ordinal))
                .Select(w => w.TrimEnd(':'))
                .Distinct().Order(StringComparer.Ordinal);
            return $"{tag[..^2]} success={r.Success} codes=[{string.Join(",", codes)}]";
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    /// <summary>An argument list minus its FIRST argument — the collating / padding rules the two legs must
    /// share. The first argument is the operand read, which legitimately differs (a bit / national group reads
    /// through its as-if channel, <c>AsBits()</c> / <c>AsNat()</c>, where an elementary item reads its field).</summary>
    private static string TailAfterFirstArgument(string args)
    {
        int depth = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == '(') depth++;
            else if (args[i] == ')') depth--;
            else if (args[i] == ',' && depth == 0) return args[(i + 1)..].Trim();
        }
        return "";
    }
}
