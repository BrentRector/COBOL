// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT TEST for the SET-family AMOUNT landing and the guarded index store (kb/Work PB459).
/// <para>§14.9.39.4 states the SAME integrality guard once per amount-taking format — GR2 a) 1. a (Format 1),
/// GR3 (Format 2), GR19 (Format 10, the data pointer) and GR29 (Format 14, the dynamic capacity) — each time
/// with the same three consequents (the condition is set to exist · the SET is unsuccessful · the receiving
/// operand is unchanged) and only the exception-condition NAME differing. Exactly ONE of the four was
/// implemented: the pointer arm kept its amount's fraction alive into <c>CobolPtr</c> (kb/Work PB151), while the
/// other three each rendered a bare <c>long __x = (long)(NumericRenderer.Align(…, 0))</c> at the emitter — a cast
/// that TRUNCATES the fraction the guard tests for and WRAPS a magnitude the carrier cannot hold, so neither
/// consequent could ever be reached. Measured: <c>SET IX UP BY 1.5</c> moved IX by 1; <c>SET CAP TO -3</c>
/// destroyed five live occurrences; two <c>UP BY</c>s past 2^63 left an index holding a NEGATIVE occurrence
/// number and the program carried on.</para>
/// <para>kb/Work PB465 then closed the FOURTH: the pointer arm's own copy of GR19 had a MAGNITUDE guard folded
/// into it (an integral 1.0E19 set EC-SIZE-ADDRESS, whose antecedent is false, and aborted the run unit), and its
/// emitter still rendered <c>long __ptrBy = (long)(…)</c> — the same narrowing as the other three, by a
/// different spelling, which is why the regex below it did not see it. The integrality DECISION now lives in
/// <c>SetAmount</c>, shared by all four, and each format maps the outcome to its own conditions.</para>
/// <para>The repair was structural, so this test is structural: the guard is written ONCE in
/// <c>SetAmount</c>, every SET-family amount reaches it through <c>SetEmitter.LandAmount</c> or
/// <c>CobolPtr.UpByAmount</c>, and every modification of an index goes through <c>CobolIndex.Augment</c>. A SOURCE-TEXT check is the right instrument
/// for exactly the reason the sibling <see cref="NumericRoundUpSiteDriftTests"/> gives: the defect was never that
/// the helper computed the wrong thing, it was that a call site did not use it, and only the call sites can
/// witness that. The behavioural half lives in the conformance goldens (set-index-amount-*).</para>
/// <para>Proven to FAIL before it was trusted: restoring <c>EmitSetUpDown</c>'s
/// <c>long tmp = (long)(NumericRenderer.Align(...))</c> makes
/// <see cref="EverySetFamilyAmountSite_LandsThroughCobolIndex"/> red by name, and restoring
/// <c>AugmentSetTarget</c>'s <c>{ix.IndexField} {op}= …</c> makes
/// <see cref="TheIndexAugmentIsGuarded_NotFormedInTheCarrier"/> red by name.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SetIndexAmountLandingDriftTests
{
    private static readonly string SetEmitter =
        Path.Combine("Cobol.Net.Compiler", "CodeGen", "Verbs", "SetEmitter.cs");
    private static readonly string ControlFlowEmitter =
        Path.Combine("Cobol.Net.Compiler", "CodeGen", "Verbs", "ControlFlowEmitter.cs");
    private static readonly string PtrEmitter =
        Path.Combine("Cobol.Net.Compiler", "CodeGen", "Verbs", "PtrEmitter.cs");

    /// <summary>Every emitter method that evaluates a value destined for an INDEX, with the rule that governs it.
    /// The pointer arm is listed with its own landing because GR19 is the same guard over a different carrier —
    /// naming it here is what keeps the population closed rather than sampled.</summary>
    private static readonly (string File, string Method, string Landing, string Rule)[] AmountSites =
    [
        (SetEmitter, "EmitSetTo", "LandAmount", "ISO 14.9.39.4 GR2 a) 1. a — SET … TO arithmetic-expression-1"),
        (SetEmitter, "EmitSetUpDown", "LandAmount", "ISO 14.9.39.4 GR3 — SET … UP/DOWN BY arithmetic-expression-2"),
        (SetEmitter, "EmitSetCapacity", "LandAmount", "ISO 14.9.39.4 GR29 — SET capacity arithmetic-expression-4"),
        (ControlFlowEmitter, "InitVaryingTarget", "LandAmount",
            "ISO 13.18.38.4 GR2 — PERFORM VARYING creates a value for an index"),
        (PtrEmitter, "EmitSetPointerUpDown", "PtrUpByAmount",
            "ISO 14.9.39.4 GR19/GR20 — SET pointer UP/DOWN BY arithmetic-expression-3"),
    ];

    /// <summary>Each site renders its amount through a landing that still has the fraction and the full
    /// magnitude, and none of them narrows the amount with a bare <c>(long)</c> cast first.</summary>
    [Fact]
    public void EverySetFamilyAmountSite_LandsThroughCobolIndex()
    {
        foreach (var (file, method, landing, rule) in AmountSites)
        {
            string body = MethodBody(TestRepo.Src(file), method);
            Assert.True(body.Contains(landing, StringComparison.Ordinal),
                $"{file}#{method} implements {rule} but does not render through {landing} — "
                + "the SET-family amount guard has grown a second home.");
            Assert.False(Regex.IsMatch(body, @"\(long\)\(\s*NumericRenderer\.Align\("),
                $"{file}#{method} implements {rule} but narrows its amount with `(long)(NumericRenderer.Align(…))` "
                + "before any guard can see it. That cast IS the PB459 defect: it truncates the fraction "
                + "the integrality rule tests for and wraps the magnitude the range rule rejects.");
            // ⛔ kb/Work PB465 — THE COMPLEMENT OF THE ASSERTION ABOVE. It was written against the exact
            // SPELLING the three index sites used, `(long)(NumericRenderer.Align(…))`, so the pointer site's
            // `long __ptrBy = (long)({x.Expr})` — the same narrowing by a different route — sat inside this
            // very loop and passed. Measured on the arm that passed: `SET P UP BY 18446744073709551618` moved the
            // pointer by 2. The invariant is about the CARRIER the amount is rendered into, not about one cast's
            // text: an amount temp is `Int128` (exact, full magnitude) or `double` (the native-float lane), never
            // the 64-bit carrier a guard is supposed to be protecting.
            Assert.False(Regex.IsMatch(body, @"""long \{"),
                $"{file}#{method} implements {rule} but renders its amount into a `long` temp. The guard cannot "
                + "see a fraction or a magnitude that the declaration itself has already destroyed — the amount "
                + "stays Int128 (or the double of the native-float lane) all the way into the runtime landing.");
        }
    }

    /// <summary>The shared augment forms the new occurrence number WIDER than the carrier, through the one
    /// guarded entry — once per index arm (an index-name field and an index DATA item), never with a
    /// <c>+=</c>/<c>-=</c> in the <c>long</c> itself, where the 64-bit boundary is a silent wrap.</summary>
    [Fact]
    public void TheIndexAugmentIsGuarded_NotFormedInTheCarrier()
    {
        string body = MethodBody(TestRepo.Src(SetEmitter), "AugmentSetTarget");
        Assert.Equal(2, Regex.Matches(body, @"RuntimeApi\.IndexAugment\(").Count);
        Assert.False(body.Contains("{op}=", StringComparison.Ordinal),
            "SetEmitter.AugmentSetTarget renders an in-carrier `+=`/`-=` into an index. ISO 14.9.39.4 GR4 a) and "
            + "13.18.38.4 GR2 make a result outside the implementor index range EC-RANGE-INDEX with the index "
            + "UNCHANGED, which a wrap cannot express — the sum has to be formed in Int128 (CobolIndex.Augment).");
        Assert.True(body.Contains("(Int128)(", StringComparison.Ordinal),
            "SetEmitter.AugmentSetTarget narrows its amount before the guard. A PERFORM VARYING BY operand is an "
            + "integer data item of up to 31 digits (ISO 14.9.28.3 SR4 a), so the amount stays Int128 into "
            + "CobolIndex.Augment.");
    }

    /// <summary>Every caller of the shared index STORE lands first. <c>StoreSetTarget</c> deliberately carries no
    /// guard of its own — by the time it renders, the value is already a <c>long</c> and both the fraction and
    /// the overflow are gone — so its precondition is a property of its callers, and this is what holds it.</summary>
    [Fact]
    public void EveryCallerOfTheIndexStore_LandsTheValueFirst()
    {
        (string File, string Method)[] callers =
        [
            (SetEmitter, "EmitSetTo"),
            (ControlFlowEmitter, "InitVaryingTarget"),
        ];
        foreach (var (file, method) in callers)
        {
            string body = MethodBody(TestRepo.Src(file), method);
            Assert.True(body.Contains("StoreSetTarget(", StringComparison.Ordinal),
                $"{file}#{method} no longer calls StoreSetTarget — this drift list names a site that moved.");
            Assert.True(body.Contains("LandAmount(", StringComparison.Ordinal),
                $"{file}#{method} stores into a SET target without landing the value first (kb/Work PB459). "
                + "StoreSetTarget cannot apply ISO 14.9.39.4 GR2 a) 1's guards — it receives a long.");
        }
        // And the population is closed: no OTHER file calls the shared store/augment pair.
        foreach (string call in new[] { "StoreSetTarget(", "AugmentSetTarget(" })
            foreach (string path in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                         SearchOption.AllDirectories))
            {
                if (path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    continue;
                string name = Path.GetFileName(path);
                if (name is "SetEmitter.cs" or "ControlFlowEmitter.cs") continue;
                Assert.False(File.ReadAllText(path).Contains(call, StringComparison.Ordinal),
                    $"{name} calls {call} — a THIRD verb now rides the shared SET store/augment pair. "
                    + "Add it to this drift list and make sure it lands its value (kb/Work PB459).");
            }
    }

    /// <summary>The population is CLOSED, not sampled. §14.9.39.4 states the integrality guard in exactly FOUR
    /// general rules; if a future revision of the transcription grows a fifth, this fails and the new rule has to
    /// be given the one landing rather than a fifth hand-written test.</summary>
    [Fact]
    public void TheSpecStatesTheSetAmountIntegralityGuard_InExactlyTheRulesThisTestCovers()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        int start = spec.IndexOf("##### 14.9.39.4 General rules", StringComparison.Ordinal);
        Assert.True(start > 0, "the SET general rules moved in the transcription.");
        int end = spec.IndexOf("\n#### ", start, StringComparison.Ordinal);
        string rules = end > start ? spec[start..end] : spec[start..];

        var found = new List<string>();
        foreach (string line in rules.Split('\n'))
        {
            if (!Regex.IsMatch(line, @"does not (result in|evaluate to) an? (nonnegative )?integer")) continue;
            if (line.Contains("arithmetic-expression-1", StringComparison.Ordinal)) found.Add("GR2 a) 1. a");
            else if (line.Contains("arithmetic-expression-2", StringComparison.Ordinal)) found.Add("GR3");
            else if (line.Contains("arithmetic-expression-3", StringComparison.Ordinal)) found.Add("GR19");
            else if (line.Contains("arithmetic-expression-4", StringComparison.Ordinal)) found.Add("GR29");
            else found.Add("UNCLASSIFIED: " + line.Trim());
        }

        Assert.DoesNotContain(found, c => c.StartsWith("UNCLASSIFIED", StringComparison.Ordinal));
        Assert.Equal(new[] { "GR2 a) 1. a", "GR3", "GR19", "GR29" }, found);
    }

    /// <summary>Brace-matched body of <paramref name="method"/> in <paramref name="path"/> (the
    /// <see cref="NumericRoundUpSiteDriftTests"/> reader — the DECLARATION, never an earlier call site).</summary>
    private static string MethodBody(string path, string method)
    {
        string src = File.ReadAllText(path);
        var m = Regex.Match(src,
            $@"^[ \t]*(?:private|public|internal|protected)[^\n(]*\b{Regex.Escape(method)}\s*\(",
            RegexOptions.Multiline);
        Assert.True(m.Success, $"{path} has no method named {method} — the drift list names a site that moved.");
        int i = src.IndexOfAny(['{', '=', ';'], m.Index);
        Assert.True(i >= 0 && src[i] != ';', $"{method} in {path} has no body.");
        if (src[i] == '=')
            return src[i..src.IndexOf(';', i)];
        int depth = 0;
        for (int j = i; j < src.Length; j++)
        {
            if (src[j] == '{') depth++;
            else if (src[j] == '}' && --depth == 0) return src[i..(j + 1)];
        }
        throw new Xunit.Sdk.XunitException($"unbalanced braces scanning {method} in {path}");
    }
}
