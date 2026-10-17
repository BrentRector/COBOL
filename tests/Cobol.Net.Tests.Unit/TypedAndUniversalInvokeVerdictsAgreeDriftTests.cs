// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;
using CobolNet.Compiler.Oo;
using CobolNet.Editions;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A BY REFERENCE INVOKE ARGUMENT GETS ONE VERDICT ON THE TYPED AND THE UNIVERSAL LANE (kb/Work PB2077, decision
/// PB3098; COBOLNET_OO_DESIGN D6 item 6). ISO §14.9.23.4 GR7 b) locates the method for EVERY INVOKE by §9.3.6's match,
/// and §14.9.23.3 SR5 c) with §14.8.1 asks a typed receiver's §14.8.2 conformance at compile time: one relation, read
/// at two times. The universal switch asks <see cref="ActivationRelations.Matches"/> and
/// <see cref="ActivationRelations.ParameterViolation"/> at run time; the typed binder
/// (<c>OoBinder.ByReferenceArgumentReason</c>) asks <see cref="ActivationRelations.MatchViolation"/> and the same
/// <c>ParameterViolation</c> over the SAME descriptions (<see cref="ActivationDescriptions"/>). Before PB2077 the typed
/// lane carried its own comparator — a hand-written reference-modification arm and <c>OoConformance.DescriptionMismatch</c>
/// — and the two lanes gave opposite verdicts: a slice into an equal-length <c>PIC X(5)</c> ran on one and raised
/// EC-OO-METHOD on the other, a slice into a smaller group formal the reverse.
/// <list type="bullet">
/// <item>STRUCTURAL: <c>OoBinder</c> calls no <c>SignatureDifference</c>, the pair-mode relation that replaced
/// <c>DescriptionMismatch</c> (deleted everywhere by kb/Work PB3101;
/// <c>ActivationConformanceDriftTests.PairMode_AsksTheDescriptionRelation_ThroughOneEntry</c>), its BY REFERENCE
/// verdict asks the two relations and names no <c>RefModPlace</c>, and no
/// <c>if</c> of <c>OoBindInvocationArg</c> tests for one.</item>
/// <item>BEHAVIORAL: over every elementary pair the comparator's own drift test enumerates, plus reference-modified and
/// ANY LENGTH sides, the binder's verdict is the relation's; and PB3098's rows compile to the verdicts the table
/// derives from the standard (the universal lane's twin is the golden
/// <c>conformance:2002/pb2077_refmod_argument_both_lanes</c>).</item>
/// </list>
/// </summary>
public sealed class TypedAndUniversalInvokeVerdictsAgreeDriftTests
{
    private static readonly string OoBinderFile =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs");

    private static MethodDeclarationSyntax[] Methods(string name) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(OoBinderFile)).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == name).ToArray();

    [Fact]
    public void OoBinder_HoldsNoArgumentComparatorOfItsOwn()
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(OoBinderFile)).GetRoot();
        var comparatorCalls = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "SignatureDifference" })
            .Select(i => i.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
            .ToList();
        Assert.True(comparatorCalls.Count == 0,
            "OoBinder calls the pair-mode relation SignatureDifference (line " + string.Join(", ", comparatorCalls)
            + "): an INVOKE argument would be judged by an identity the universal lane does not ask (kb/Work PB2077).");

        var verdict = Assert.Single(Methods("ByReferenceArgumentReason"));
        string body = verdict.Body!.ToString();
        Assert.Contains("ActivationRelations.MatchViolation(", body);
        Assert.Contains("ActivationRelations.ParameterViolation(", body);
        Assert.DoesNotContain("RefModPlace", body);

        var bind = Assert.Single(Methods("OoBindInvocationArg"));
        Assert.Contains("ByReferenceArgumentReason(", bind.Body!.ToString());
        var refModArms = bind.DescendantNodes().OfType<IfStatementSyntax>()
            .Where(s => s.Condition.ToString().Contains("RefModPlace", StringComparison.Ordinal))
            .Select(s => s.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
            .ToList();
        Assert.True(refModArms.Count == 0,
            "OoBindInvocationArg branches on a reference-modified argument (line " + string.Join(", ", refModArms) + "): "
            + "a slice is the unique data item §8.4.3.3.4 GR5 / GR6 create and is described by ActivationDescriptions, "
            + "never judged by a conformance arm of the binder's own (kb/Work PB2077).");
    }

    private static DataItem Item(string picture, Usage usage = Usage.Display, bool anyLength = false)
    {
        var pic = PictureAnalyzer.Analyze(picture, usage, new EditionContext(2023), "data item 'P'");
        Assert.False(pic.IsRecovery, $"PIC {picture} did not analyze");
        return new DataItem
        {
            Level = 1, CobolName = "P", CsName = "P", Pic = pic, PictureText = picture, IsAnyLength = anyLength,
        };
    }

    private static Place Whole(DataItem item) => new MemberPlace(new AccessPath([]), item);

    private static Place Slice(DataItem item, int start, int? length) =>
        new RefModPlace(Whole(item), new PositionConstant(start), length is { } n ? new PositionConstant(n) : null);

    [Fact]
    public void TheBindersVerdict_IsTheRelation_OverEveryElementaryPair()
    {
        var items = new List<DataItem>
        {
            Item("X(5)"), Item("XXXXX"), Item("A(5)"), Item("X(4)"), Item("XBX"), Item("X9X"),
            Item("9(4)"), Item("S9(4)"), Item("9(2)V99"), Item("9(4)", Usage.Binary),
            Item("N(3)", Usage.National), Item("N(4)", Usage.National), Item("1(4)"), Item("1(4)", Usage.Bit),
            Item("ZZ9.99"), Item("X", anyLength: true), Item("N", Usage.National, anyLength: true),
        };
        var arguments = items.Where(i => !i.IsAnyLength).Select(Whole)
            .Concat(items.Where(i => !i.IsAnyLength).SelectMany(i => new[] { Slice(i, 1, 4), Slice(i, 2, null) }))
            .ToList();
        int pairs = 0, matched = 0;
        foreach (var formal in items)
            foreach (var argument in arguments)
            {
                var oof = new OoFormal(formal, 0, "p");
                var da = ActivationDescriptions.OfPlace(argument, classes: null)!;
                var df = ActivationDescriptions.OfFormal(oof, classes: null)!;
                bool relation = ActivationRelations.Matches(da, null, df, null!)
                    && ActivationRelations.ParameterViolation(da, df) is null;
                bool binder = OoBinder.ByReferenceArgumentReason(argument, oof, null, ObjectRefDescriptor.Universal) is null;
                pairs++;
                if (binder) matched++;
                Assert.True(binder == relation,
                    $"formal PIC {formal.PictureText}{(formal.IsAnyLength ? " ANY LENGTH" : "")} vs argument "
                    + $"{(argument is RefModPlace ? "slice of " : "")}PIC {argument.Item.PictureText}: the typed binder says "
                    + $"{(binder ? "conforms" : "refused")}, the universal relation {(relation ? "conforms" : "refused")}");
                if (argument is RefModPlace)
                    Assert.False(binder, $"a slice of PIC {argument.Item.PictureText} matched the elementary formal PIC "
                        + $"{formal.PictureText}: the unique data item has no PICTURE clause (ISO §8.4.3.3.4 GR6; §9.3.6 3 e))");
            }
        Assert.True(pairs > 600, $"only {pairs} pairs — the population no longer exercises the invariant");
        Assert.True(matched > 15, $"only {matched} matching pairs — the population no longer reaches a match");
    }

    private static bool TypedLaneAccepts(string formal, string argument) => TypedLaneErrors(formal, argument).Count == 0;

    private static IReadOnlyList<string> TypedLaneErrors(string formal, string argument)
    {
        string source = string.Join("\n",
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. PB2077DT.",
            "       ENVIRONMENT DIVISION.",
            "       CONFIGURATION SECTION.",
            "       REPOSITORY.",
            "           CLASS C2077DT.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       01 T  USAGE OBJECT REFERENCE C2077DT.",
            "       01 X  PIC X(10).",
            "       01 Y  PIC X(4).",
            "       01 N  PIC 9(2).",
            "       01 G.",
            "          05 G1 PIC X(5).",
            "          05 G2 PIC X(5).",
            "       01 J  PIC X(4) JUSTIFIED RIGHT.",
            "       PROCEDURE DIVISION.",
            "           INVOKE C2077DT \"NEW\" RETURNING T",
            $"           INVOKE T \"M\" USING {argument}",
            "           STOP RUN.",
            "       END PROGRAM PB2077DT.",
            "       IDENTIFICATION DIVISION.",
            "       CLASS-ID. C2077DT INHERITS FROM BASE.",
            "       ENVIRONMENT DIVISION.",
            "       CONFIGURATION SECTION.",
            "       REPOSITORY.",
            "           CLASS BASE",
            "           CLASS C2077DT.",
            "       IDENTIFICATION DIVISION.",
            "       OBJECT.",
            "       PROCEDURE DIVISION.",
            "       METHOD-ID. M.",
            "       DATA DIVISION.",
            "       LINKAGE SECTION.",
            formal,
            "       PROCEDURE DIVISION USING LF.",
            "           CONTINUE.",
            "       END METHOD M.",
            "       END OBJECT.",
            "       END CLASS C2077DT.",
            "");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB2077_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb2077.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "pb2077.dll"),
                DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Errors.All(e => e.Contains("COBOLNET0828", StringComparison.Ordinal)),
                "the probe program drew an error other than the verdict under test:\n" + string.Join("\n", r.Errors));
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private const string Group4 = "       01 LF.\n          05 LF1 PIC X(4).";

    /// <summary>PB3098's verdict table, typed column (ISO §9.3.6 3 c) / 3 e), §14.8.2.2 rule 1, §9.3.6 4 d),
    /// §14.8.2.3.2 "Additionally" d); docs/CONFORMANCE.md D-RM1).</summary>
    [Theory]
    [InlineData("       01 LF PIC X(5).", "X(1:5)", false)]           // 3 e): the slice has no PICTURE clause
    [InlineData("       01 LF PIC X(5).", "X(1:N)", false)]
    [InlineData("       01 LF PIC X ANY LENGTH.", "X(1:5)", false)]   // d) relaxes the length, not the PICTURE
    [InlineData(Group4, "X(1:6)", true)]                               // a group of 4 <= 6 bytes
    [InlineData(Group4, "X(1:3)", false)]                              // rule 1: 4 > 3
    [InlineData(Group4, "X(1:N)", true)]                               // rule 1 on the maximum extent, 10
    [InlineData(Group4, "X(7:N)", true)]                               // maximum extent 4 = 4
    [InlineData(Group4, "X(8:N)", false)]                              // maximum extent 3 < 4
    [InlineData(Group4, "X", false)]                                   // 3 e): a PICTURE on one side only
    [InlineData("       01 LF PIC X(5).", "BY CONTENT X(1:5)", true)] // 4 d): a valid MOVE
    [InlineData(Group4, "BY CONTENT X(2:6)", true)]                    // 4 d): a MOVE into a group
    [InlineData("       01 LF PIC X ANY LENGTH.", "Y", true)]          // d): the length is considered to match
    // A slice of a GROUP: identifier-1's length is static (ten characters) though the group has no PICTURE, so rule 1
    // reads the same extents a slice of PIC X(10) gives (kb/Work PB2077 review R1-1: these were measured as zero).
    [InlineData(Group4, "G(1:N)", true)]                               // maximum extent 10 >= 4
    [InlineData(Group4, "G(3:)", true)]                                // to the end: 8 >= 4
    [InlineData(Group4, "G(7:N)", true)]                               // maximum extent 4 = 4
    [InlineData(Group4, "G(8:)", false)]                               // to the end: 3 < 4
    [InlineData(Group4, "G(1:3)", false)]                              // rule 1: 4 > 3
    public void PB3098sRows_CompileToTheirVerdicts(string formal, string argument, bool accepted) =>
        Assert.Equal(accepted, TypedLaneAccepts(formal, argument));

    /// <summary>The refusal names the clause that differs, never the length §14.8.2.3.2 "Additionally" d) relaxes
    /// (kb/Work PB2077 review R1-2): opposite an ANY LENGTH formal, a JUSTIFIED argument is refused for JUSTIFIED.</summary>
    [Fact]
    public void AnAnyLengthFormalsRefusal_NamesTheClauseThatDiffers_NotTheLength()
    {
        string error = Assert.Single(TypedLaneErrors("       01 LF PIC X ANY LENGTH.", "J"));
        string difference = error[error.IndexOf("match rule 3 e)):", StringComparison.Ordinal)..];
        Assert.Contains("the argument is described with JUSTIFIED and the formal parameter is not", difference);
        Assert.DoesNotContain("PICTURE", difference);
    }

    /// <summary>⛔ A REFUSAL SPELLS ITS DESCRIPTIONS AS THEIR ENTRIES DO, NEVER AS AN IDENTITY KEY (kb/Work PB2077 review
    /// round 2). The typed lane quotes the relation's text, which renders each description
    /// (<see cref="ActivationDescription.ToString"/>) and the clause the pair differs in from
    /// <see cref="ActivationDescription.ClausePhrases"/>; the <see cref="ActivationDescription.Clauses"/> key — a
    /// restricted pointer's type layout with its leaves' analyzed profiles, a PICTURE identity's packed fields — is an
    /// identity, and printing it put <c>PicInfo { … }</c> dumps into COBOLNET0828. Each row is a negative program whose
    /// refusal names the difference §9.3.6 match rule 3 e) / §14.8.2.3.2 rule 4 found, and contains no key separator.</summary>
    [Theory]
    [InlineData("pb480-typed-restricted-pointer-type.cob",
        "(an elementary data-pointer item described with USAGE POINTER TO T-REC)",
        "the two are restricted to type declarations that are not equivalent")]
    [InlineData("w61b-pb1166-invoke-currency-dpc.cob",
        "(an elementary numeric-edited item described with PICTURE Z9.99 USAGE DISPLAY)",
        "the formal parameter is described with DECIMAL-POINT IS COMMA and the argument is not")]
    [InlineData("oo-interface-arg-mismatch.cob",
        "(an elementary numeric item described with PICTURE 9(6) USAGE DISPLAY)",
        "the argument is described with PICTURE 9(6) where the formal parameter is described with PICTURE 9(4)")]
    public void ARefusal_SpellsItsClauses_NeverAnIdentityKey(string negative, string argument, string difference)
    {
        var r = CompilerDriver.Compile(new CompilerDriver.Options(TestRepo.SuiteCase(Path.GetFileNameWithoutExtension(negative)),
            Path.Combine(Path.GetTempPath(), "CobolNet_PB2077_" + Guid.NewGuid().ToString("N")[..8] + ".dll"),
            DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
        var refusals = r.Errors.Where(e => e.Contains("COBOLNET0828", StringComparison.Ordinal)
            && e.Contains("BY REFERENCE", StringComparison.Ordinal)).ToArray();
        Assert.Contains(refusals, e => e.Contains(argument, StringComparison.Ordinal)
            && e.Contains(difference, StringComparison.Ordinal));
        Assert.All(refusals, e =>
        {
            Assert.DoesNotContain("|", e);
            Assert.DoesNotContain("PicInfo", e);
        });
    }
}
