// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE TO-LESS POINTER OPERAND (kb/Work PB848).
///
/// <para>ISO/IEC 1989:2023 §13.18.60.2 (rendered, folio 503) underlines POINTER / FUNCTION-POINTER /
/// PROGRAM-POINTER and never TO, so by §5.2.3 TO is an optional word and the operand may follow the keyword
/// directly. The TO-less operand is a bare <c>cobolWord</c> at the END of a clause, so the grammar guards it with
/// <c>pointerOperandHere()</c>, whose exclusion set is FOLLOW(<c>usageKeyword</c>) computed off the generated ATN.
/// <c>cobolWord</c>'s reservation gate is NOT enough on its own — it exempts the §15 function names, and BIT and
/// NATIONAL are also bare USAGE keywords (this test caught <c>USAGE POINTER BIT</c> binding BIT as a type-name
/// when the guard was briefly removed), and a first cut that excluded only the clause-leading words let
/// <c>USAGE POINTER HIGH-ORDER-RIGHT</c> bind the endianness phrase as a type-name (caught by the Unit assembly's
/// UsageFloatFormatPhraseDriftTests). So the per-word fact checks EVERY word that may follow a usage keyword and
/// that <c>cobolWord</c> can match, at every edition, reserved there or not.</para>
/// <para>The overlap fact flips the axis the subject holds fixed (feedback_probe_the_shape_the_subject_hides): it
/// requires the overlap to be NON-EMPTY, so the per-word fact can never pass vacuously.</para>
/// </summary>
public sealed class PointerUsageOperandDriftTests
{
    private static (CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) Parse(string src, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_ptr_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = edition }.Parse(path, diags);
            return (tree, diags);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static string Prog(string entries) =>
        "IDENTIFICATION DIVISION.\n" +
        "PROGRAM-ID. PTROPND.\n" +
        "DATA DIVISION.\n" +
        "WORKING-STORAGE SECTION.\n" + entries +
        "PROCEDURE DIVISION.\n" +
        "MAIN.\n    STOP RUN.\n";

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    /// <summary>The COBOL words that can both FOLLOW a usage keyword and be matched by <c>cobolWord</c>, read off
    /// the generated ATN (FOLLOW(usageKeyword) ∩ FIRST(cobolWord), less IDENTIFIER — a plain user word IS the
    /// operand).</summary>
    private static List<string> ClauseLeadingWordsCobolWordAdmits()
    {
        var atn = CobolParserCore._ATN;
        var follow = CobolParserCoreBase.UsageFollowSet(atn);
        var wordFirst = atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_cobolWord]);
        return wordFirst.And(follow).ToList()
            .Where(t => t != CobolParserCore.IDENTIFIER)
            .Select(t => CobolParserCore.DefaultVocabulary.GetLiteralName(t)?.Trim('\'')
                         ?? CobolParserCore.DefaultVocabulary.GetSymbolicName(t).Replace('_', '-'))
            .ToList();
    }

    [Fact]
    public void TheOverlap_IsReadFromTheAtn_AndIsNotEmpty()
    {
        var words = ClauseLeadingWordsCobolWordAdmits();
        // SECURE begins the §13.18.50 SECURE clause and HIGH-ORDER-RIGHT is a USAGE-clause tail phrase; both are
        // cobolWord alternatives, so if either drops out the derivation (not the grammar) is what broke.
        // ⚠ It named PROPERTY (§13.18.42) until the PB655 merge: PROPERTY is §8.9-reserved from 2002, so since the
        // token-level reservation gate it is a `reservedGatedWord` alternative only — never FIRST(cobolWord) — and
        // at an edition that leaves it free the rewriter retypes a DECLARED one to IDENTIFIER (the plain name arm).
        Assert.Contains("SECURE", words);
        Assert.Contains("HIGH-ORDER-RIGHT", words);
    }

    public static IEnumerable<object[]> Editions() => [[85], [2002], [2014], [2023]];

    /// <summary>A clause-leading keyword is never the TO-less operand of a pointer usage: <c>01 P USAGE POINTER W.</c>
    /// must leave the operand empty for every W in the overlap, at every edition (the grammar parses the pointer
    /// usages as a superset at every edition; the introduction gate is the binder's).</summary>
    [Theory]
    [MemberData(nameof(Editions))]
    public void AClauseLeadingKeyword_IsNeverTheToLessOperand(int edition)
    {
        var checkedWords = new List<string>();
        foreach (string w in ClauseLeadingWordsCobolWordAdmits())
        {
            foreach (string usage in new[] { "POINTER", "PROGRAM-POINTER", "FUNCTION-POINTER" })
            {
                var (tree, _) = Parse(Prog($"01 P USAGE {usage} {w}.\n"), edition);
                // A SYNTAX ERROR is also a pass: `01 P USAGE POINTER W.` is well-formed whenever W is taken as the
                // operand, so an entry that fails to parse (W began a clause whose remainder is missing — a bare
                // `CONSTANT.`) proves W was NOT the operand.
                if (tree is null) continue;
                var kw = Descendants<CobolParserCore.UsageKeywordContext>(tree).FirstOrDefault();
                Assert.True(kw is not null, $"'{usage} {w}' at --std {edition}: no usage keyword parsed");
                var operand = kw!.dataPointerUsage()?.cobolWord() ?? kw.programPointerUsage()?.cobolWord()
                              ?? kw.functionPointerUsage()?.cobolWord();
                Assert.True(operand is null, $"USAGE {usage} {w} at --std {edition}: the clause-leading keyword "
                    + $"{w} was bound as the pointer's TO-less operand — it must begin its clause");
            }
            checkedWords.Add(w);
        }
        Assert.NotEmpty(checkedWords);   // a run that checked nothing proves nothing
    }

    [Theory]
    [InlineData("POINTER", "01 PT-T USAGE POINTER REC-T IS TYPEDEF.\n")]
    [InlineData("POINTER", "01 PT-T USAGE POINTER TO REC-T IS TYPEDEF.\n")]
    [InlineData("PROGRAM-POINTER", "01 PP-T USAGE PROGRAM-POINTER REC-T IS TYPEDEF.\n")]
    [InlineData("FUNCTION-POINTER", "01 FP USAGE FUNCTION-POINTER REC-T.\n")]
    [InlineData("FUNCTION-POINTER", "01 FP FUNCTION-POINTER TO REC-T.\n")]
    public void TheOperand_IsRecognized_WithOrWithoutTo(string usage, string entry)
    {
        var (tree, diags) = Parse(Prog("01 REC-T IS TYPEDEF.\n   05 A PIC X.\n" + entry), 2023);
        Assert.NotNull(tree);
        Assert.False(diags.HasErrors, string.Join("; ", diags.Diagnostics.Select(d => d.ToString())));
        var kw = Descendants<CobolParserCore.UsageKeywordContext>(tree!).Single();
        CobolParserCore.CobolWordContext? operand = usage switch
        {
            "POINTER" => kw.dataPointerUsage()?.cobolWord(),
            "PROGRAM-POINTER" => kw.programPointerUsage()?.cobolWord(),
            _ => kw.functionPointerUsage()?.cobolWord(),
        };
        Assert.Equal("REC-T", operand?.GetText());
    }
}
