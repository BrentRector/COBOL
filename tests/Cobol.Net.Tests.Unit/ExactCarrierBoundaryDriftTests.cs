// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB252 — the invariants of the exact Int128 carrier, held STRUCTURALLY rather than by memory.
/// </summary>
/// <remarks>
/// <para><b>1. Never wrap.</b> D1 evaluates the exact intrinsic family as unscaled <see cref="Int128"/> values, and
/// §14.7.5 rule 5 makes an arithmetic operation that takes the intermediate outside the implementor's checked range
/// the SIZE ERROR condition — not a modular wrap. PB32 wrote that policy but implemented it only for MEDIAN and
/// MIDRANGE, naming the helpers after MEDIAN's halving; SUM's <c>s += x</c> and RANGE's <c>max - min</c> then sat
/// unguarded for eight months and returned a SIGN-FLIPPED value through an <c>ON SIZE ERROR</c> phrase that was not
/// taken. A boundary pin per function would have the same blind spot, so the completeness half is driven by
/// REFLECTION over the whole exact-carrier entry-point set: every public entry that takes and returns only carrier
/// values is either pinned below AND at the boundary, or named in <see cref="CannotLeaveTheCarrier"/> with its
/// reason. A new one is neither, and the build goes red.</para>
/// <para><b>2. A standard mode never touches the carrier's boundary at all.</b> §15.4.1 r1 admits no approximation
/// under STANDARD-DECIMAL / STANDARD-BINARY: the returned value shall EQUAL the equivalent arithmetic expression,
/// and §8.8.1.5.2 r1 converts each argument to the SDIDI individually — so no common scale is ever formed. The
/// NATIVE arms align first, and alignment multiplies (a 31-digit integer beside a scale-18 operand needs 49
/// digits), so exactly the arms that CROSS-ALIGN must route to their <c>…Dec</c> bodies under a standard mode.
/// That routing was also built one arm at a time — PB62 moved the summing family, PB252 found MOD and REM still
/// on the Int128 lane — so the set is now checked against the switch that defines it.</para>
/// <para><b>3. An arm that can be HANDED an SDIDI operand has a Dec body at all.</b> Invariant 2 asks which arms
/// MUST leave the carrier under a standard mode; kb/Work PB620 is the prior question, and the one nothing asked:
/// an arm with no <c>RenderDec</c> label falls through to the exact switch whatever the argument's carrier, where
/// <c>Arg</c> lands an SDIDI operand at max(receiver scale, 6). COMBINED-DATETIME was that arm — the one §15.4.1
/// r1 function PB56's sweep left on the landing intake — and its own clause could not see the defect, because the
/// rule it breaks (§15.4.1 r1's "the returned value shall equal the value of the equivalent arithmetic
/// expression") is broken in the SHARED intake and not in §15.17.</para>
/// </remarks>
public sealed class ExactCarrierBoundaryDriftTests
{
    private static readonly Int128 Max = Int128.MaxValue;

    // ── 1. The carrier boundary ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The exact-carrier entries that CANNOT leave the carrier, each with the reason it needs no guard. An entry
    /// is exempt only when its result is bounded by its OPERANDS, which the emitter's per-argument
    /// <c>CobolNum.RescaleEscape</c> already holds at or below <see cref="Int128.MaxValue"/> — so
    /// <see cref="Int128.MinValue"/> is unreachable and the asymmetry of two's complement never bites.
    /// </summary>
    private static readonly Dictionary<string, string> CannotLeaveTheCarrier = new()
    {
        ["MaxScaled"] = "§15.59.4 r1 returns the CONTENT of an argument — pure selection, no arithmetic at all.",
        ["MinScaled"] = "§15.63.4 r1 returns the CONTENT of an argument — pure selection, no arithmetic at all.",
        ["RemScaled"] = "§15.77.4 r1's EAE is C#'s `%` exactly (truncated remainder), whose result satisfies "
                      + "|r| < |b| ≤ Int128.MaxValue for every operand pair the aligner can produce.",
        // ABS is not listed: |Int128.MinValue| = 2^127 is past the carrier, so it is boundary-pinned below (kb/Work
        // PB2698). The premise this table rests on — RescaleEscape holds every ARGUMENT at or below MaxValue — is
        // true of a RESCALED argument only; a signed 16-byte field read reaches an arm at MinValue unrescaled.
    };

    /// <summary>The closed set of exact-carrier value entries: they take and return CARRIER values only (an
    /// <c>Int128</c> or an <c>Int128[]</c>, plus the optional COBOL function name a shared body is told), so every
    /// one of them evaluates a §15.4.1 equivalent arithmetic expression on the D1 intermediate. Entries taking a
    /// scale, a count or a <c>double</c> are a different class (a rescale or a landing) and guard elsewhere.</summary>
    private static List<MethodInfo> ExactCarrierEntries() =>
        [.. typeof(CobolIntrinsics).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(Int128))
            .Where(m => m.GetParameters().Length > 0)
            .Where(m => m.GetParameters().All(p => p.ParameterType == typeof(Int128)
                                                || p.ParameterType == typeof(Int128[])
                                                || p.ParameterType == typeof(string)))
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(Int128)
                                                || p.ParameterType == typeof(Int128[])))];

    /// <summary>Every exact-carrier entry is either boundary-pinned by name below or exempted with a reason —
    /// the completeness half, so a NEW exact function cannot ship unguarded and unnoticed.</summary>
    [Fact]
    public void EveryExactCarrierEntry_IsEitherGuardedOrExplicitlyExempt()
    {
        string[] guarded = ["SumScaled", "RangeScaled", "MedianScaled", "MidrangeScaled", "ModScaled", "AbsScaled"];
        var entries = ExactCarrierEntries().Select(m => m.Name).ToHashSet(StringComparer.Ordinal);

        Assert.True(entries.Count >= 9,
            $"the exact-carrier entry filter found only {entries.Count} methods — it has stopped matching the "
            + "family it is meant to close over, and a silent wrap could return through the gap");

        foreach (string name in entries)
            Assert.True(guarded.Contains(name) || CannotLeaveTheCarrier.ContainsKey(name),
                $"CobolIntrinsics.{name} evaluates a §15.4.1 EAE on the D1 Int128 intermediate but is neither "
                + "boundary-pinned in this file nor listed in CannotLeaveTheCarrier with the reason it cannot "
                + "overflow. §14.7.5 rule 5 makes an operation past the checked range a SIZE ERROR — never a wrap "
                + "(kb/Work PB252: SUM returned a sign-flipped value through an untaken ON SIZE ERROR phrase).");

        foreach (string stale in guarded.Concat(CannotLeaveTheCarrier.Keys))
            Assert.True(entries.Contains(stale),
                $"{stale} is claimed here but is no longer an exact-carrier entry — this guard is stale");
    }

    /// <summary>At the boundary every guarded entry raises the §14.7.5 r5 size error, carrying the Table 13
    /// level-3 name that <c>EcEmitter</c>'s ON SIZE ERROR arm filters on.</summary>
    [Theory]
    [InlineData("SUM")]
    [InlineData("RANGE")]
    [InlineData("MEDIAN-ODD")]
    [InlineData("MEDIAN-EVEN")]
    [InlineData("MIDRANGE")]
    [InlineData("MOD")]
    [InlineData("ABS")]
    public void AtTheBoundary_TheExactCarrierRaisesEcSizeOverflow(string which)
    {
        var e = Assert.Throws<CobolSizeError>(() => AtBoundary(which));
        Assert.Equal("EC-SIZE-OVERFLOW", e.EcName);
        Assert.Contains("outside the Int128 carrier's range", e.Message);
        Assert.Contains("§14.7.5 rule 5", e.Message);
    }

    private static Int128 AtBoundary(string which) => which switch
    {
        // Three arguments each individually inside the carrier whose SUM is not: the shape RescaleEscape's
        // per-argument bound cannot see, and the one PB252 measured from COBOL source.
        "SUM" => CobolIntrinsics.SumScaled("SUM", Max / 2, Max / 2, Max / 2),
        // A positive maximum less a negative minimum — the difference is up to twice the larger operand.
        "RANGE" => CobolIntrinsics.RangeScaled(Max, Int128.MinValue + 1),
        // The ×10 / ×5 that keeps the halving exact spends a decimal digit of headroom (PB32).
        "MEDIAN-ODD" => CobolIntrinsics.MedianScaled(Max / 2),
        "MEDIAN-EVEN" => CobolIntrinsics.MedianScaled(Max / 2, Max / 2),
        "MIDRANGE" => CobolIntrinsics.MidrangeScaled(Max / 2, Max / 2),
        // b × FUNCTION INTEGER(a/b) reaches |a| + |b| once the floor adjustment fires on opposite signs.
        "MOD" => CobolIntrinsics.ModScaled(Max, -(Max - 1)),
        // -Int128.MinValue wraps to itself: the magnitude 2^127 is the one value past the carrier (kb/Work PB2698).
        "ABS" => CobolIntrinsics.AbsScaled(Int128.MinValue),
        _ => throw new ArgumentOutOfRangeException(nameof(which)),
    };

    /// <summary>⛔ The failure branch of the guard above must be reachable ONLY at the boundary: one step inside
    /// it, every one of these returns its exact §15.4.1 value. A guard that raised early would be just as wrong
    /// as one that wrapped, and a green boundary theory alone cannot tell the two apart.</summary>
    [Fact]
    public void JustInsideTheBoundary_TheExactCarrierStillAnswersExactly()
    {
        Assert.Equal(Max, CobolIntrinsics.SumScaled("SUM", Max - 1, 1));
        Assert.Equal(Max, CobolIntrinsics.SumScaled("MEAN", Max - 3, 1, 1, 1));
        Assert.Equal(Max, CobolIntrinsics.RangeScaled(Max, 0));
        Int128 tenth = Max / 10;
        Assert.Equal(tenth * 10, CobolIntrinsics.MedianScaled(tenth));               // odd ⇒ middle × 10
        Assert.Equal(tenth * 5, CobolIntrinsics.MidrangeScaled(tenth, 0));           // (max + min) × 5
        Assert.Equal(4, CobolIntrinsics.ModScaled(-11, 5));                          // §15.64.4 NOTE's sign table
        Assert.Equal(-1, CobolIntrinsics.RemScaled(-11, 5));                         // §15.77.4 truncates
        Assert.Equal(Max, CobolIntrinsics.AbsScaled(Int128.MinValue + 1));           // -(2^127 - 1): the largest magnitude that fits
        Assert.Equal(Max, CobolIntrinsics.AbsScaled(Max));
    }

    // ── 2. Cross-aligning arms route to the SDIDI under a standard mode ────────────────────────────────────

    private static string RendererSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));

    /// <summary>The native switch's BODY — comments stripped, bounded at its own <c>default:</c> label.</summary>
    /// <remarks>Strip EVERY <c>//</c> comment, trailing ones included: the switch's arms carry long trailing
    /// citations, and a whole-line-only strip left <c>case "ModScaled":   // §15.64 …</c> looking like an arm
    /// with a body, which dropped MOD from the scan. (No string literal in this switch contains <c>//</c>.)
    /// <para>⛔ AND BOUND THE SCAN TO THE SWITCH BLOCK. Without the <c>default:</c> anchor the LAST arm's "body"
    /// runs to end-of-file and picks up every helper call in the methods below it — the scan then reports an arm
    /// that does not align and the guard measures the file, not the switch. It failed exactly that way on first
    /// run.</para></remarks>
    private static string ExactSwitchBody()
    {
        string src = RendererSource();
        int from = src.IndexOf("switch (sig.RuntimeMethod)", StringComparison.Ordinal);
        Assert.True(from > 0, "IntrinsicRenderer no longer switches on sig.RuntimeMethod — this guard is blind");
        string body = Regex.Replace(src[from..], @"//[^\n]*", "");
        int end = body.IndexOf("\n            default:", StringComparison.Ordinal);
        Assert.True(end > 0, "the native switch has no `default:` label — the arm scan has no end anchor");
        return body[..end];
    }

    /// <summary>The switch's arms, each as its LABELS and the code that follows them — the ONE reader of this
    /// switch's shape, so the two guards below cannot disagree about what an arm is (kb/Work PB620).</summary>
    /// <remarks>⛔ <c>case "ModScaled":</c> sits ALONE above <c>case "RemScaled":</c> and their shared body — a
    /// chunk with no body of its own. Reading each chunk independently silently dropped MOD, which is the very
    /// arm invariant 2 exists to catch; the ≥ floors below are what surfaced it. Stacked labels are carried
    /// forward onto the arm that does have a body.</remarks>
    private static List<(List<string> Labels, string Body)> ArmsOf(string switchBody)
    {
        // Split on the switch's own case indent so each chunk is exactly one label list plus whatever follows it.
        string[] chunks = Regex.Split(switchBody, @"(?m)^            (?=case\s+"")");
        var arms = new List<(List<string>, string)>();
        var pending = new List<string>();                       // labels STACKED above a shared body (MOD/REM)
        foreach (string arm in chunks.Where(a => a.StartsWith("case ", StringComparison.Ordinal)))
        {
            var labels = Regex.Match(arm, @"^case\s+(?<lbl>""[A-Za-z0-9]+""(?:\s+or\s+""[A-Za-z0-9]+"")*)\s*(?::|when\b)");
            Assert.True(labels.Success, $"could not read the case labels of an arm: {arm[..Math.Min(90, arm.Length)]}");
            var here = Regex.Matches(labels.Groups["lbl"].Value, @"""(?<n>[A-Za-z0-9]+)""")
                            .Select(x => x.Groups["n"].Value).ToList();
            string rest = arm[labels.Length..];
            if (rest.Trim().Length == 0) { pending.AddRange(here); continue; }
            here.AddRange(pending);
            pending.Clear();
            arms.Add((here, rest));
        }
        Assert.Empty(pending);
        return arms;
    }

    /// <summary>The case labels of every native-switch arm that cross-aligns its arguments to ONE common scale,
    /// read from the switch itself.</summary>
    /// <remarks>AlignedArgs/AlignedArgsEx take the LIST to one common scale; <c>NumericRenderer.Align(x, s)</c>
    /// is the pairwise form MOD/REM use. <c>RawArgPairs</c> is deliberately NOT here — it rescales each argument
    /// to its OWN scale (an identity), which is why MAX/MIN/ORD-MAX/ORD-MIN are pure selection (PB65).</remarks>
    private static HashSet<string> CrossAligningArmsInTheSwitch() =>
        new(ArmsOf(ExactSwitchBody())
                .Where(a => Regex.IsMatch(a.Body, @"\bAlignedArgs(Ex)?\(")
                         || a.Body.Contains("NumericRenderer.Align(", StringComparison.Ordinal))
                .SelectMany(a => a.Labels),
            StringComparer.Ordinal);

    /// <summary>⛔ THE PB62/PB252 GUARD. Every arm that cross-aligns is in <c>CrossAlignedNativeArms</c>, and every
    /// name in that set is still such an arm. Adding an aligning arm without routing it fails here rather than
    /// eight months later on a user's program.</summary>
    [Fact]
    public void EveryCrossAligningNativeArm_IsRoutedToTheSdidiUnderAStandardMode()
    {
        var inSwitch = CrossAligningArmsInTheSwitch();
        Assert.True(inSwitch.Count >= 7,
            $"only {inSwitch.Count} cross-aligning arms found ({string.Join(", ", inSwitch.Order())}) — the scan "
            + "has stopped seeing the switch and would pass with the routing set empty");

        foreach (string arm in inSwitch)
            Assert.True(IntrinsicRenderer.CrossAlignedNativeArms.Contains(arm),
                $"the native arm {arm} aligns its arguments to a COMMON scale on the Int128 carrier but is not in "
                + "IntrinsicRenderer.CrossAlignedNativeArms, so under ARITHMETIC IS STANDARD-DECIMAL it still "
                + "evaluates there. Alignment multiplies — a 31-digit integer beside a scale-18 operand needs 49 "
                + "digits — so it raises EC-SIZE-OVERFLOW where §15.4.1 rule 1 requires the equivalent arithmetic "
                + "expression's exact SDIDI value (kb/Work PB62, then PB252 for MOD/REM).");

        foreach (string arm in IntrinsicRenderer.CrossAlignedNativeArms)
            Assert.True(inSwitch.Contains(arm),
                $"{arm} is routed as a cross-aligning arm but no longer aligns in the native switch — the set is "
                + "stale, and a stale entry silently changes that function's standard-mode carrier");
    }

    /// <summary>The pure-SELECTION arms are the complement, and they must STAY out: §15.59.4 / §15.63.4 / §15.71.4
    /// / §15.72.4 return the content or the ordinal of an argument, so re-routing them onto the SDIDI would undo
    /// PB65's own-scale comparison. Measuring the complement is the half a membership test always omits.</summary>
    [Fact]
    public void ThePureSelectionArms_AreNotRoutedAsCrossAligning()
    {
        foreach (string sel in new[] { "MaxScaled", "MinScaled", "OrdMax", "OrdMin" })
            Assert.False(IntrinsicRenderer.CrossAlignedNativeArms.Contains(sel),
                $"{sel} is pure selection (RawArgPairs rescales each argument to its OWN scale) — it forms no "
                + "common scale and has no boundary to escape (kb/Work PB65)");
    }

    // ── 3. An arm that can be handed a Dec argument has a Dec body ─────────────────────────────────────────
    //
    // ⛔ THE THIRD INVARIANT, AND THE ONE PB620 COST. Invariant 2 asks WHICH arms must leave the Int128 carrier
    // under a standard mode; this asks the prior question — which arms can be handed an SDIDI operand at all.
    // The answer is structural: every arm that consumes a FRACTION-BEARING numeric intake can, because an
    // argument reaches the renderer on three carriers (§8.8.1.5.2's SDIDI under a standard mode, the native
    // integer power / float-literal Dec producers, and the exact Int128 lane), and only `RenderDec` has a body
    // for the first two. An arm with no RenderDec label falls through to the exact switch, where `Arg` LANDS the
    // operand at max(receiver scale, 6) and truncates past it. COMBINED-DATETIME was that arm and nothing could
    // see it: it is the ONE §15.4.1 r1 function PB56's sweep left behind, and the §15 clause sweep could not see
    // it from inside §15.17 because the defect is in the shared intake, not in the function's own rules. An arm
    // whose only numeric intake is IntArg/ArgInt is NOT in scope — §15.3's integer positions are scale-0 by
    // definition, so there is no fraction for a landing to eat.

    /// <summary>A FRACTION-BEARING numeric intake: the operand arrives with a scale, so an SDIDI operand can
    /// reach this arm. <c>IntArg</c>/<c>ArgInt</c> deliberately do not match (<c>\b</c> stops <c>Arg(</c> from
    /// matching inside <c>IntArg(</c>), and neither does the string channel's <c>Str(</c>.</summary>
    private static readonly Regex FractionBearingIntake =
        new(@"\b(?:Arg|RawArg)\(ic,\s*\d+\)|\bAlignedArgs(?:Ex)?\(|\bRawArgPairs\(", RegexOptions.Compiled);

    /// <summary>The arm labels of <c>RenderDec</c>'s expression switch — <c>"Name" =&gt;</c> and
    /// <c>"Name" when … =&gt;</c> — read from the switch itself, bounded by its own <c>_ =&gt; null,</c>.</summary>
    private static HashSet<string> DecArmLabels()
    {
        string src = RendererSource();
        int from = src.IndexOf("return m switch", StringComparison.Ordinal);
        Assert.True(from > 0, "IntrinsicRenderer.RenderDec no longer switches on m — this guard is blind");
        int end = src.IndexOf("_ => null,", from, StringComparison.Ordinal);
        Assert.True(end > from, "RenderDec's switch has no `_ => null,` arm — the scan has no end anchor");
        string body = Regex.Replace(src[from..end], @"//[^\n]*", "");
        return new HashSet<string>(
            Regex.Matches(body, @"""(?<n>[A-Za-z0-9]+)""\s*(?:when\b[^\n]*?)?=>").Select(m => m.Groups["n"].Value),
            StringComparer.Ordinal);
    }

    /// <summary>The detector, shared by the guard and its self-test — a self-test against a re-typed copy of the
    /// pattern would prove nothing about the pattern actually shipped.</summary>
    private static List<string> ArmsWithNoDecBody(string exactSwitchBody, ISet<string> decArms) =>
        [.. ArmsOf(exactSwitchBody)
             .Where(a => FractionBearingIntake.IsMatch(a.Body))
             .SelectMany(a => a.Labels)
             .Where(n => !decArms.Contains(n))
             .Distinct(StringComparer.Ordinal)];

    /// <summary>⛔ THE GUARD MUST FAIL ON THE DEFECT IT EXISTS FOR (feedback_green_gates_arent_evidence). Input
    /// one is COMBINED-DATETIME's arm exactly as PB620 found it; input two is the landed pairing; input three is
    /// FACTORIAL, whose only intake is the scale-0 <c>IntArg</c> and which therefore must NOT be demanded of
    /// RenderDec — the false positive that would have forced a Dec body for an integer argument; input four is
    /// the string channel, and input five is a stacked label pair, where a chunk with no body of its own must
    /// still be judged by the body it shares.</summary>
    [Fact]
    public void TheDecBodyDetector_CatchesTheDefectAndNothingElse()
    {
        var none = new HashSet<string>(StringComparer.Ordinal);
        Assert.Equal(["CombinedDatetime"], ArmsWithNoDecBody(
            "            case \"CombinedDatetime\":\n{ NumX t = Arg(ic, 1); return X(t.Expr, t.Scale); }", none));
        Assert.Empty(ArmsWithNoDecBody(
            "            case \"CombinedDatetime\":\n{ NumX t = RawArg(ic, 1); return X(t.Expr, t.Scale); }",
            new HashSet<string>(new[] { "CombinedDatetime" }, StringComparer.Ordinal)));
        Assert.Empty(ArmsWithNoDecBody(
            "            case \"Factorial\":\nreturn new NumX(Intrinsic(m, IntArg(ic, 0)), 0);", none));
        Assert.Empty(ArmsWithNoDecBody(
            "            case \"Ord\":\nreturn new NumX(Intrinsic(m, Str(ic.Args[0])), 0);", none));
        Assert.Equal(["ModScaled", "RemScaled"], ArmsWithNoDecBody(
            "            case \"RemScaled\":\n            case \"ModScaled\":\n{ var a = Arg(ic, 0); }", none)
            .Order().ToList());
    }

    /// <summary>The guard proper, over the shipped renderer: every exact-switch arm that can be handed an SDIDI
    /// operand has a <c>RenderDec</c> arm to claim it first.</summary>
    [Fact]
    public void EveryArmThatCanTakeADecArgument_HasADecBody()
    {
        string exact = ExactSwitchBody();
        var decArms = DecArmLabels();
        // Both populations are asserted: a scan that has stopped seeing either switch would pass with nothing
        // in it, and this guard's whole value is the arm nobody listed (feedback_green_gates_arent_evidence).
        var scanned = ArmsOf(exact).Where(a => FractionBearingIntake.IsMatch(a.Body)).SelectMany(a => a.Labels)
                                   .Distinct(StringComparer.Ordinal).ToList();
        Assert.True(scanned.Count >= 15,
            $"only {scanned.Count} fraction-bearing arms found ({string.Join(", ", scanned.Order())}) — the scan "
            + "has stopped seeing the exact switch and would pass with the Dec set empty");
        Assert.True(decArms.Count >= 20,
            $"only {decArms.Count} RenderDec arms found — the Dec-switch scan has stopped seeing the switch");
        Assert.Contains("CombinedDatetime", scanned);            // the arm this invariant was bought with

        var offenders = ArmsWithNoDecBody(exact, decArms);
        Assert.True(offenders.Count == 0,
            "these exact-switch arms consume a FRACTION-BEARING argument intake but have no arm in RenderDec, so "
            + "an SDIDI-carried argument falls through to the exact Int128 lane and is LANDED at "
            + "max(receiver scale, 6) — the returned value then depends on the shape of whatever consumes it, "
            + "and under a standard mode §15.4.1 r1 requires it to EQUAL the function's equivalent arithmetic "
            + "expression (kb/Work PB620: COMBINED-DATETIME(1, WS-S + 0) lost two digits where "
            + "COMBINED-DATETIME(1, WS-S) did not). Give the function a RenderDec arm over a `…Dec` body. "
            + "Offenders: " + string.Join(", ", offenders));
    }
}
