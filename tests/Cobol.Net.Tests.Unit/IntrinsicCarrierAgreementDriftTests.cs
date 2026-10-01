// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO CARRIERS IMPLEMENT ONE RULE, SO THE RULE MUST BE ASSERTED ON BOTH (fix-queue PB32).
/// </summary>
/// <remarks>
/// <para>
/// <c>IntrinsicRenderer.RenderNum</c> routes on <c>AnyRealArgument</c>, so every exact-family function has an
/// exact <see cref="Int128"/> body AND a binary64 one. Its sibling <c>IntrinsicRealArgDriftTests</c> asserts the
/// second body EXISTS; nothing asserted that the two AGREE, and they did not. MOD's §15.64.3 r2 zero-divisor rule
/// was written into both bodies and then corrected in only one: <c>ModReal</c> read <c>b == 0 ? 0 : …</c>, a
/// second independent guard that returned the §15.3 default without ever SETTING EC-ARGUMENT-FUNCTION. Under
/// <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING ON</c> the run unit sailed past a condition Table 13
/// (§14.6.13.1.1) makes FATAL.
/// </para>
/// <para>
/// ⚠ A GUARD THAT ONLY KNOWS ABOUT MOD AND REM WOULD BE THE SIXTH INDIVIDUAL FIX. The second test below is the
/// general one: it reads <c>CobolIntrinsics.RealArgs.cs</c> and fails on ANY real body that answers a domain
/// guard with a literal instead of routing to a shared raise site. That is the shape — an exact body raising
/// where its float twin silently returns — rather than the two instances of it that happen to exist today.
/// </para>
/// </remarks>
public sealed class IntrinsicCarrierAgreementDriftTests
{
    /// <summary>Run <paramref name="body"/> with EC-ARGUMENT-FUNCTION checking forced on, restoring it after —
    /// the flag is process-global ambient state, so a leak would silently arm every later test in the class.</summary>
    private static void UnderChecking(Action body)
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = true;
        try { body(); }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>
    /// §15.64.3 r2 / §15.77.3 r2: a zero divisor sets EC-ARGUMENT-FUNCTION — from EVERY carrier: the exact
    /// <see cref="Int128"/> lane, binary64, and the SDIDI decimal carrier (§15.3 argument type 6 — an
    /// arithmetic-expression argument under ARITHMETIC IS STANDARD-DECIMAL). This is the assertion that was
    /// missing; <c>ModReal(1, 0)</c> returned 0.0 and raised nothing. The test was named "BothCarriers" while three
    /// bodies existed, and the third — <c>RemDec</c> — raised under MOD's name and MOD's clause (kb/Work PB304).
    /// </summary>
    [Theory]
    [InlineData("MOD")]
    [InlineData("REM")]
    public void ZeroDivisor_RaisesFromEveryCarrier(string fn)
    {
        UnderChecking(() =>
        {
            // The EXACT (Int128) carrier — this arm was already correct.
            var exact = Assert.Throws<CobolFatalException>(() =>
                fn == "MOD" ? CobolIntrinsics.ModScaled(11, 0) : CobolIntrinsics.RemScaled(11, 0));
            // The BINARY64 carrier, reached whenever any argument renders as a double — this arm was silent.
            var real = Assert.Throws<CobolFatalException>(() =>
                fn == "MOD" ? CobolIntrinsics.ModReal(11d, 0d) : CobolIntrinsics.RemReal(11d, 0d));

            // The SDIDI (CobolDec) carrier — this arm named the WRONG function's rule (kb/Work PB304).
            var dec = Assert.Throws<CobolFatalException>(() => fn == "MOD"
                ? CobolIntrinsics.ModDec(CobolRounding.NearestAwayFromZero, CobolDec.From(11, 0), CobolDec.From(0, 0))
                : CobolIntrinsics.RemDec(CobolRounding.NearestAwayFromZero, CobolDec.From(11, 0), CobolDec.From(0, 0)));

            foreach (var e in new[] { exact, real, dec })
            {
                Assert.Equal("EC-ARGUMENT-FUNCTION", e.EcName);
                // One raise site per RULE means one message per rule: the citation cannot drift between carriers.
                Assert.Equal(exact.Message, e.Message);
                Assert.Contains(fn + " with a zero divisor", e.Message);   // the raise site names its OWN function
            }
            Assert.Contains(fn == "MOD" ? "15.64.3" : "15.77.3", exact.Message);
        });
    }

    /// <summary>With checking DISABLED, §15.3's closing paragraph makes the returned value implementor-defined —
    /// but it must be the SAME implementor-defined value from both carriers, for the same reason §15.4 forbids a
    /// result that depends on its receiver's shape.</summary>
    [Fact]
    public void ZeroDivisor_UncheckedDefault_AgreesAcrossCarriers()
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = false;
        try
        {
            Assert.Equal(0, (double)CobolIntrinsics.ModScaled(11, 0));
            Assert.Equal(0, CobolIntrinsics.ModReal(11d, 0d));
            Assert.Equal(0, (double)CobolIntrinsics.RemScaled(11, 0));
            Assert.Equal(0, CobolIntrinsics.RemReal(11d, 0d));
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>
    /// §15.17.3's two VALUE rules hold from ALL THREE carriers COMBINED-DATETIME has (kb/Work PB620): the exact
    /// <see cref="Int128"/> lane, the SDIDI, and binary64. The third had NO argument rule at all — measured,
    /// <c>FUNCTION COMBINED-DATETIME(1, F)</c> with a COMP-2 F holding 86400 computed 1.8639999999999 and set no
    /// exception condition, while the identical value in a <c>PIC 9(5)V9(8)</c> item terminated the run unit in
    /// the same program. §15.17.3 r2 constrains the argument's VALUE ("in standard numeric time form", which
    /// §15.5.5 defines by magnitude), so the USAGE of the item holding it cannot decide whether §15.3's
    /// "incorrect value for that argument" applies. One rule ⇒ one raise site ⇒ one message, asserted.
    /// </summary>
    [Fact]
    public void CombinedDatetime_ScreensItsArguments_FromEveryCarrier()
    {
        UnderChecking(() =>
        {
            // §15.17.3 r2 — 86 400 seconds is outside standard numeric time form (§7.3.17.4 GR5, the implied OFF).
            var exact = Assert.Throws<CobolFatalException>(() => CobolDate.CombinedDatetime(1, 86400, 0));
            var dec = Assert.Throws<CobolFatalException>(() => CobolDate.CombinedDatetimeDec(
                CobolRounding.NearestAwayFromZero, 1, CobolDec.From(86400, 0)));
            var real = Assert.Throws<CobolFatalException>(() => CobolIntrinsics.CombinedDatetimeReal(1, 86400));
            foreach (var e in new[] { exact, dec, real })
            {
                Assert.Equal("EC-ARGUMENT-FUNCTION", e.EcName);
                Assert.Equal(exact.Message, e.Message);
            }
            // §15.17.3 r1 — and the §15.5.2 integer date form's ceiling, from each carrier, with its own message.
            var d1 = Assert.Throws<CobolFatalException>(() => CobolDate.CombinedDatetime(3067672, 0, 0));
            var d2 = Assert.Throws<CobolFatalException>(() => CobolDate.CombinedDatetimeDec(
                CobolRounding.NearestAwayFromZero, 3067672, CobolDec.From(0, 0)));
            var d3 = Assert.Throws<CobolFatalException>(() => CobolIntrinsics.CombinedDatetimeReal(3067672, 0));
            Assert.Equal(d1.Message, d2.Message);
            Assert.Equal(d1.Message, d3.Message);
            Assert.Contains("15.5.2", d1.Message);
        });
        // ⛔ AND THE FAILURE BRANCH MUST BE REACHABLE ONLY AT THE BOUND. One step inside it every carrier answers
        // §15.17.4 r1's expression — a guard that raised early would be as wrong as one that never raised.
        // Each expected value is §15.17.4 r1's expression evaluated by hand at the argument's OWN scale,
        // never a measurement: argument-1 + (argument-2 / 100000).
        //   exact   86 399.999 99       / 100000 = 0.863 999 999 9       + 1 = 1.863 999 999 9
        //                                             read at scale 5+5 = 10 -> the unscaled 18 639 999 999
        //   SDIDI   86 399.999 999 99   / 100000 = 0.863 999 999 999 9   + 1 = 1.863 999 999 999 9
        //   binary64 86 399.999 9       / 100000 = 0.863 999 999         + 1 = 1.863 999 999
        Assert.Equal((Int128)18639999999, CobolDate.CombinedDatetime(1, 8639999999, 5));
        // ↳ compared as a VALUE, not as a (Sig, Exp) pair: §15.4.1 r1 says the returned value shall EQUAL the
        //   expression's, and a carrier is free to keep trailing zeros that record equality would reject.
        Assert.Equal(0, CobolDec.Compare(
            CobolDec.From(18639999999999, 13),
            CobolDate.CombinedDatetimeDec(CobolRounding.NearestAwayFromZero, 1, new CobolDec(8639999999999, -8))));
        Assert.Equal(1.863999999, CobolIntrinsics.CombinedDatetimeReal(1, 86399.9999), 9);
    }

    /// <summary>
    /// ⛔ THE GENERAL GUARD: no <c>…Real</c> body may answer a domain guard with a bare literal.
    /// </summary>
    /// <remarks>
    /// Every legitimately-guarded real body routes to a shared raise site — <c>TryIntegerArg</c> for the §15.3
    /// type-6 integer family, <c>ModZeroDivisor</c> / <c>RemZeroDivisor</c> for the two division rules. A body
    /// that instead writes <c>x == 0 ? 0 : …</c> has silently re-implemented an exception condition as a default
    /// value, which is precisely how MOD and REM drifted from their exact twins. The empty-argument
    /// <c>xs.Length == 0 ? 0</c> forms are NOT this defect and are excluded by name: a zero-argument variadic
    /// call cannot be written in COBOL (the general formats require at least one argument), so those are
    /// unreachable defensive returns rather than a rule being defaulted.
    /// <para>⚠ IT SCANS CODE ONLY, AND IT PROVED IT NEEDED TO BY FAILING ON ITS OWN FIRST RUN. The prose above
    /// and the remarks on <c>ModReal</c> both QUOTE the defective form <c>b == 0 ? 0 : …</c> so a later reader
    /// knows what was wrong; scanning raw file text therefore reported the fixed code as an offender. Comments
    /// are stripped before matching. A guard that cannot tell a defect from a description of a defect would have
    /// had to be silenced, and a silenced guard is the dead lookup this project keeps finding.</para>
    /// </remarks>
    /// <summary>The detector, so that both the guard below and its self-test use the SAME one — a self-test
    /// against a re-typed copy of the pattern proves nothing about the pattern actually shipped.</summary>
    private static List<string> DomainGuardOffenders(string source)
    {
        // Strip block and line comments (including `///` doc comments) — see the remark on the guard below.
        string src = Regex.Replace(Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
        // A ternary whose CONDITION tests an argument against zero and whose TRUE arm is a bare numeric literal.
        return [.. Regex.Matches(src, @"(?<cond>\b[A-Za-z_][A-Za-z0-9_]*\s*==\s*0)\s*\?\s*(?<then>-?\d+(?:\.\d+)?)\s*:")
            .Select(m => new { Cond = m.Groups["cond"].Value, Line = src[..m.Index].Count(c => c == '\n') + 1 })
            // `xs.Length == 0` is the unreachable-variadic guard, not a domain rule.
            .Where(o => !o.Cond.Contains("Length", StringComparison.Ordinal))
            .Select(o => $"line {o.Line}: `{o.Cond} ? …`")];
    }

    /// <summary>⛔ THE GUARD MUST FAIL ON THE DEFECT IT EXISTS FOR — asserted, not assumed
    /// (feedback_green_gates_arent_evidence). The first input is <c>ModReal</c> exactly as it was written before
    /// PB32; the second is the fixed form; the third is the excluded variadic guard; the fourth is a COMMENT
    /// describing the defect, which is what made the guard fail on its own first run.</summary>
    [Fact]
    public void TheDomainGuardDetector_CatchesTheDefectAndNothingElse()
    {
        Assert.Single(DomainGuardOffenders(
            "public static double ModReal(double a, double b) => b == 0 ? 0 : a - (b * Math.Floor(a / b));"));
        Assert.Empty(DomainGuardOffenders(
            "public static double ModReal(double a, double b) => b == 0 ? ModZeroDivisor() : a - b;"));
        Assert.Empty(DomainGuardOffenders(
            "public static double MaxReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max();"));
        Assert.Empty(DomainGuardOffenders("/// this body used to answer <c>b == 0 ? 0 : …</c>"));
    }

    /// <summary>
    /// ⛔ EACH ZERO-DIVISOR RAISE SITE NAMES ITS OWN FUNCTION'S RULE, IN EVERY CARRIER FILE (kb/Work PB304).
    /// §15.64.3 r2 and §15.77.3 r2 are word-for-word identical ("The value of argument-2 shall not be zero.") and
    /// belong to different functions, so a copy-pasted body that calls the other function's raise site reads as
    /// correct. This scans every <c>CobolIntrinsics*.cs</c> for a <c>Mod…</c>/<c>Rem…</c> body and fails when one
    /// calls the other's <c>…ZeroDivisor()</c> — a rule over ALL carriers, so a fourth carrier cannot repeat it
    /// without this test seeing it (a hand-added parametrisation above would not).
    /// </summary>
    [Fact]
    public void EveryModRemBody_RaisesItsOwnFunctionsZeroDivisorRule()
    {
        string dir = TestRepo.Src("Cobol.Net.Runtime", "Intrinsics");
        var header = new Regex(@"^\s*(?:public|internal|private)\s+static\s+[\w<>\[\]?]+\s+(Mod|Rem)\w*\s*\(",
            RegexOptions.Multiline);
        var nextMember = new Regex(@"^\s*(?:public|internal|private)\s+(?:static\s+)?[\w<>\[\]?]+\s+\w+\s*[\(=]",
            RegexOptions.Multiline);
        var raise = new Regex(@"\b(Mod|Rem)ZeroDivisor\(\)");
        var offenders = new List<string>();
        int bodies = 0;
        foreach (string file in Directory.GetFiles(dir, "CobolIntrinsics*.cs"))
        {
            string text = File.ReadAllText(file);
            foreach (Match h in header.Matches(text))
            {
                int start = h.Index + h.Length;
                Match next = nextMember.Match(text, start);
                string body = text[start..(next.Success ? next.Index : text.Length)];
                string fn = h.Groups[1].Value;
                foreach (Match r in raise.Matches(body))
                {
                    bodies++;
                    if (r.Groups[1].Value != fn)
                        offenders.Add($"{Path.GetFileName(file)}: a {fn} body calls {r.Value}");
                }
            }
        }
        Assert.True(bodies >= 6, $"the scan found only {bodies} zero-divisor raise calls — it no longer sees the carriers");
        Assert.True(offenders.Count == 0, "a MOD/REM body raises the OTHER function's §15 zero-divisor rule: "
            + string.Join("; ", offenders));
    }

    [Fact]
    public void NoRealBody_AnswersADomainGuardWithALiteral()
    {
        var offenders = DomainGuardOffenders(
            File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics", "CobolIntrinsics.RealArgs.cs")));

        Assert.True(offenders.Count == 0,
            "A …Real body answers a domain guard with a literal instead of a shared raise site — its exact twin "
            + "raises an exception condition for the same input, so the two carriers disagree (fix-queue PB32). "
            + "Route it through the rule's ONE raise site, as ModZeroDivisor/RemZeroDivisor do. Offenders: "
            + string.Join(", ", offenders));
    }

    // ── THE INTAKE WIDTH IS THE RUNTIME BODY'S DECLARED CARRIER (kb/Work PB254) ────────────────────────────────
    //
    // PB22 landed ONE narrowing — CobolIntrinsics.IntegerArg — in front of every §15 integer argument, and it is
    // right for a PARTIAL function: §15.5.2's date forms and FACTORIAL's unrepresentable results genuinely make
    // an out-of-range argument "an incorrect value … according to the rules specified in the function
    // definition" (§15.3), so raising EC-ARGUMENT-FUNCTION there is the standard's own answer. It swept up the
    // TOTAL functions too. §15.90.3 r1 / §15.91.3 r1 say only "Argument-1 shall be an integer" and §15.90.4 r1a
    // / §15.91.4 r1a are CATCH-ALLS; §15.37.3 r3 says only "argument-3 shall be an integer data item or integer
    // literal" and §15.37.4 r2/r3 answer for every integer. For those three the landing MANUFACTURED an
    // exception condition — fatal under checking, and with checking off it substituted 0, which reported
    // TEST-DATE-YYYYMMDD(1.0E19) as a VALID DATE and FIND-STRING(… START AFTER 9999999999999999999) as a match
    // at position 1 where rule 3 requires zero.
    //
    // ⚠ THE PAIRING IS NOT A LIST KEPT HERE. Totality is declared ONCE, by the runtime body's parameter type —
    // Int128 for a total argument, long for one the argument rules bound — and this guard re-derives the pairing
    // from the shipped signatures and fails when an arm disagrees with its body. Widening a body is therefore
    // enough to force its arm wide; forgetting the arm is red, not silent.

    /// <summary>Every runtime intrinsic body whose signature makes the argument↔parameter correspondence
    /// UNAMBIGUOUS — exactly one <c>long</c>/<c>Int128</c> parameter, agreed across every overload — mapped to
    /// whether that carrier is the WIDE one. A body with two or more integer carriers (BOOLEAN-OF-INTEGER's
    /// value+length, the windowing trio's date+offset+base-year, MOD/REM's numeric pair) is deliberately not
    /// checked: the renderer's emitted argument list is not this test's to parse position by position, and a
    /// guard that guessed the correspondence would be the dead lookup this project keeps finding.</summary>
    private static Dictionary<string, bool> SingleCarrierIntrinsicBodies()
    {
        var byName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        foreach (Type t in typeof(CobolIntrinsics).Assembly.GetExportedTypes())
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var carriers = m.GetParameters().Select(p => p.ParameterType)
                    .Where(p => p == typeof(long) || p == typeof(Int128)).ToList();
                if (carriers.Count == 0) continue;                          // no integer carrier — nothing to pair
                if (carriers.Count > 1) { ambiguous.Add(m.Name); continue; } // correspondence not derivable here
                if (!byName.TryGetValue(m.Name, out var seen)) byName[m.Name] = seen = [];
                seen.Add(carriers[0]);
            }
        return byName
            .Where(kv => !ambiguous.Contains(kv.Key) && kv.Value.Distinct().Count() == 1)
            .ToDictionary(kv => kv.Key, kv => kv.Value[0] == typeof(Int128), StringComparer.Ordinal);
    }

    /// <summary>An arm head in either switch shape the renderer uses — the statement form
    /// <c>case "A" or "B":</c> and the expression form <c>"A" or "B" =&gt;</c>, with or without a
    /// <c>when</c> clause. Anchoring on the head (rather than on the intake call) is what lets the guard say
    /// WHICH function is mis-rendered.</summary>
    private static readonly Regex ArmHead = new(
        """(?:case\s+)?"(?<n>[A-Za-z][A-Za-z0-9]*)"(?<more>(?:\s+or\s+"[A-Za-z][A-Za-z0-9]*")*)\s*(?:when\b[^\n]*?)?(?::|=>)""",
        RegexOptions.Compiled);

    private static readonly Regex NarrowIntake = new(@"\b(?:IntArg|ArgInt)\(", RegexOptions.Compiled);
    private static readonly Regex WideIntake = new(@"\b(?:IntArgWide|ArgIntWide)\(", RegexOptions.Compiled);

    /// <summary>The detector, shared by the guard and its self-test. Each arm chunk runs from its head to the
    /// next head; an arm that renders no integer intake at all is not this rule's business.</summary>
    private static List<string> IntakeWidthOffenders(string rendererSource, IReadOnlyDictionary<string, bool> wide)
    {
        // Comments are stripped for the same reason the PB32 guard strips them: the remarks on IntArg and
        // AsIntWide NAME both helpers, so raw text would report the documentation as the defect.
        string src = Regex.Replace(Regex.Replace(rendererSource, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
        var heads = ArmHead.Matches(src).ToList();
        var offenders = new List<string>();
        for (int k = 0; k < heads.Count; k++)
        {
            int end = k + 1 < heads.Count ? heads[k + 1].Index : src.Length;
            string arm = src[heads[k].Index..end];
            bool narrow = NarrowIntake.IsMatch(arm), broad = WideIntake.IsMatch(arm);
            if (!narrow && !broad) continue;                                // renders no integer argument
            foreach (string name in Names(heads[k]))
            {
                if (!wide.TryGetValue(name, out bool wantWide)) continue;    // no unambiguous body to pair with
                if (wantWide && (!broad || narrow))
                    offenders.Add($"{name}: body declares Int128 (a TOTAL §15 integer argument) but the arm narrows");
                else if (!wantWide && broad)
                    offenders.Add($"{name}: body declares long (a BOUNDED argument) but the arm takes the wide intake");
            }
        }
        return offenders;

        static IEnumerable<string> Names(Match m) =>
            [m.Groups["n"].Value, .. Regex.Matches(m.Groups["more"].Value, "\"([A-Za-z][A-Za-z0-9]*)\"")
                                          .Select(x => x.Groups[1].Value)];
    }

    /// <summary>⛔ THE GUARD MUST FAIL ON THE DEFECT IT EXISTS FOR (feedback_green_gates_arent_evidence). Input
    /// one is the PB254 arm exactly as it was written — both TEST- validators narrowed — and the guard has to
    /// name BOTH functions, because a multi-name arm head is how the pair hid behind one line. Input two is the
    /// landed form. Input three is the opposite drift: a wide intake in front of a bounded body. Input four is a
    /// COMMENT naming the narrow helper, which is what made the sibling PB32 guard fail on its own first run.</summary>
    [Fact]
    public void TheIntakeWidthDetector_CatchesTheDefectAndNothingElse()
    {
        var wide = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["TestDateYyyymmdd"] = true, ["TestDayYyyyddd"] = true, ["Factorial"] = false,
        };
        Assert.Equal(2, IntakeWidthOffenders(
            """case "TestDateYyyymmdd" or "TestDayYyyyddd": return DateFn(m, IntArg(ic, 0));""", wide).Count);
        Assert.Empty(IntakeWidthOffenders(
            """case "TestDateYyyymmdd" or "TestDayYyyyddd": return DateFn(m, IntArgWide(ic, 0));""", wide));
        Assert.Single(IntakeWidthOffenders("""case "Factorial": return Intrinsic(m, IntArgWide(ic, 0));""", wide));
        Assert.Empty(IntakeWidthOffenders(
            """/// this arm used to read case "TestDateYyyymmdd": … IntArg(ic, 0)""", wide));
    }

    /// <summary>The guard proper, over the shipped renderer and the shipped runtime signatures.</summary>
    [Fact]
    public void EveryTotalIntegerArgument_TakesTheWideIntake()
    {
        var wide = SingleCarrierIntrinsicBodies();
        // The guard is worthless if it pairs nothing: assert the population it actually examined (PB254's own
        // three functions are the ones that must be in it).
        Assert.True(wide.GetValueOrDefault("TestDateYyyymmdd"), "TestDateYyyymmdd is not paired as a wide body");
        Assert.True(wide.GetValueOrDefault("TestDayYyyyddd"), "TestDayYyyyddd is not paired as a wide body");
        Assert.True(wide.GetValueOrDefault("FindString"), "FindString is not paired as a wide body");
        // kb/Work PB636 — RANDOM's seed joined the set: §15.75.3 r2 constrains the sign alone and §15.75.4 r3
        // makes the distinct-sequence subset a floor, so the body declares Int128 and the arm must go wide.
        Assert.True(wide.GetValueOrDefault("Random"), "Random is not paired as a wide body");
        Assert.False(wide.GetValueOrDefault("Factorial", true), "Factorial is not paired as a narrow body");

        var offenders = IntakeWidthOffenders(
            File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs")), wide);

        Assert.True(offenders.Count == 0,
            "A renderer arm's integer intake disagrees with the runtime body's declared carrier (kb/Work PB254). "
            + "The carrier IS the totality claim: Int128 means the function's argument rules place no constraint "
            + "on the VALUE, so §15.3 has no incorrect argument to raise on and the arm must use IntArgWide / "
            + "ArgIntWide; long means the argument rules bound it, so the arm must use IntArg / ArgInt and keep "
            + "the §15.3 landing. Offenders: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// ⛔ THE INTEGER INTAKE IS TOTAL OVER EVERY CARRIER A NumX CAN BE (kb/Work PB638 + PB1526). The bounded and the
    /// total landing were two hand-copied dispatches; the narrow one had no unsigned-wide arm, so a COMP-5 item over
    /// a 19+-digit unsigned PICTURE at INTEGER-OF-DATE / CHAR / BASE-CONVERT reached Roslyn as a CS1503. The carrier
    /// list is NOT kept here: <see cref="CobolNet.CodeGen.Emit.NumXCarrier"/> is the compiler's own enumeration, so a
    /// fifth carrier fails THIS test (the intake throws on a carrier it does not map) before it can reach a user.
    /// Every (carrier × width) pair must render a call onto the runtime's integrality-checked intake family.
    /// </summary>
    [Fact]
    public void EveryIntegerIntake_IsTotalOverTheNumXCarriers()
    {
        foreach (var carrier in Enum.GetValues<CobolNet.CodeGen.Emit.NumXCarrier>())
        foreach (bool wide in new[] { false, true })
        {
            var x = carrier switch
            {
                CobolNet.CodeGen.Emit.NumXCarrier.Scaled => new CobolNet.CodeGen.Emit.NumX("e", 2),
                CobolNet.CodeGen.Emit.NumXCarrier.UnsignedWide => new CobolNet.CodeGen.Emit.NumX("u", 2, U: true),
                CobolNet.CodeGen.Emit.NumXCarrier.Sdidi => new CobolNet.CodeGen.Emit.NumX("d", 0, Dec: true),
                CobolNet.CodeGen.Emit.NumXCarrier.Binary64 => new CobolNet.CodeGen.Emit.NumX("r", 0, Real: true),
                _ => throw new InvalidOperationException($"the test builds no operand for the new carrier {carrier}"),
            };
            Assert.Equal(carrier, x.Carrier);
            string call = CobolNet.CodeGen.Emit.IntrinsicRenderer.IntegerIntake(x, wide);
            Assert.StartsWith("CobolIntrinsics.IntegerArg", call);
            // A scaled operand (scale != 0) and every non-scaled carrier go through the integrality-checked family; a
            // scale-0 operand is integral by construction and is the only shape that is not routed through it.
            Assert.Contains(carrier == CobolNet.CodeGen.Emit.NumXCarrier.Scaled || carrier == CobolNet.CodeGen.Emit.NumXCarrier.UnsignedWide
                ? "Scaled" : carrier == CobolNet.CodeGen.Emit.NumXCarrier.Sdidi ? "Dec" : "Real", call);
        }
    }

    /// <summary>⛔ THE §15.3 TYPE-6 INTEGRALITY RULE, ASSERTED ON THE RUNTIME BODIES ON EVERY CARRIER (kb/Work PB1526):
    /// a value with a nonzero fraction is an incorrect argument — EC-ARGUMENT-FUNCTION is raised under enabled
    /// checking — while an integral value of the same carrier (a scale-1 <c>2.0</c>, a COMP-2 <c>3.0</c>, the SDIDI
    /// <c>10 × 10⁻¹</c>) is accepted and is the integer it names; with checking off the truncated integer continues.</summary>
    [Fact]
    public void TheIntegerIntake_RaisesOnAFractionOnEveryCarrier_AndAcceptsAnIntegralValue()
    {
        // Integral values are integers on every carrier, narrow and wide.
        Assert.Equal(2, CobolIntrinsics.IntegerArgScaled(20, 1));
        Assert.Equal(3, CobolIntrinsics.IntegerArgReal(3.0));
        Assert.Equal(1, CobolIntrinsics.IntegerArgDec(new CobolDec(10, -1)));
        Assert.Equal((Int128)2, CobolIntrinsics.IntegerArgWideScaled(20, 1));
        Assert.Equal((Int128)3, CobolIntrinsics.IntegerArgWideReal(3.0));
        Assert.Equal((Int128)1, CobolIntrinsics.IntegerArgWideDec(new CobolDec(10, -1)));
        // A trailing-P item (negative scale) is the stored digits times 10^|scale|.
        Assert.Equal(200, CobolIntrinsics.IntegerArgScaled(2, -2));

        // A fraction raises EC-ARGUMENT-FUNCTION when its checking is enabled (fatal, Table 13) ...
        UnderChecking(() =>
        {
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgScaled(35, 1));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgReal(2.5));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgDec(new CobolDec(35, -1)));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgWideScaled(35, 1));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgWideReal(2.5));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgWideDec(new CobolDec(35, -1)));
            // ... a digit PAST the ninth fraction digit is still a fraction (the exact value, never a truncated copy).
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgScaled(Int128.Parse("20000000000000001"), 16));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.IntegerArgDec(new CobolDec(1, -50)));
        });
        // ... and with checking off the truncated integer is the result the implementor defines.
        Assert.Equal(3, CobolIntrinsics.IntegerArgScaled(35, 1));
        Assert.Equal(2, CobolIntrinsics.IntegerArgReal(2.5));
        Assert.Equal(-3, CobolIntrinsics.IntegerArgScaled(-35, 1));
    }

    /// <summary>The behaviour the carrier exists for, asserted end to end on the runtime bodies: the three total
    /// arguments answer their spec verdict past <c>long</c>, from BOTH carriers, and raise nothing even with
    /// EC-ARGUMENT-FUNCTION checking ENABLED — the leg that used to abort the run unit on conforming source.</summary>
    [Fact]
    public void TheTotalArguments_AnswerPastLong_AndRaiseNothingUnderChecking()
    {
        Int128 past = Int128.Parse("12345678901234567890");                 // 20 digits — beyond long.MaxValue
        UnderChecking(() =>
        {
            Assert.Equal(1, CobolDate.TestDateYyyymmdd(past));               // §15.90.4 r1a — > 99 999 999
            Assert.Equal(1, CobolDate.TestDayYyyyddd(past));                 // §15.91.4 r1a — > 9 999 999
            Assert.Equal(1, CobolDate.TestDateYyyymmdd(-past));              // r1a's LOWER half
            Assert.Equal(1, CobolDate.TestDayYyyyddd(-past));
            Assert.Equal(1, CobolIntrinsics.TestDateYyyymmddReal(1e19));     // the binary64 carrier — was 0, "valid"
            Assert.Equal(1, CobolIntrinsics.TestDayYyyydddReal(1e19));
            Assert.Equal(1, CobolIntrinsics.TestDateYyyymmddReal(double.PositiveInfinity));
            Assert.Equal(1, CobolIntrinsics.TestDayYyyydddReal(double.NegativeInfinity));
            // §15.37.4 r3 — argument-3 ignores every match, so none is left and the answer is 0, not a position.
            Assert.Equal(0, CobolIntrinsics.FindString("abcabc", "abc", false, past, false));
            Assert.Equal(0, CobolIntrinsics.FindString("abcabc", "abc", true, past, false));
        });
        // The in-window verdicts are unchanged by the widening (the r1a guard makes the int narrowing exact).
        Assert.Equal(0, CobolDate.TestDateYyyymmdd(20240229));
        Assert.Equal(3, CobolDate.TestDateYyyymmdd(20230229));
        Assert.Equal(0, CobolDate.TestDayYyyyddd(2024366));
        Assert.Equal(2, CobolDate.TestDayYyyyddd(2023366));
        Assert.Equal(4, CobolIntrinsics.FindString("abcabc", "abc", false, 1, false));
    }
}
