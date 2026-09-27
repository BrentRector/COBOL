// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>The ISO §15.2 function-type classification — THE return-type column of the catalog (deep-dive D1):
/// Integer → scale-0 native integer; Numeric → exact NumX (unscaled long + scale) or double for the §15.4.1
/// floating-math family; Alphanumeric/National → string; Boolean → bool; Index → occurrence number.</summary>
public enum IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }

/// <summary>The §15.3 arity model: a fixed argument count, optional trailing arguments (e.g. the RANDOM seed,
/// NUMVAL-C's currency), or a variable-length list (the statistical functions).</summary>
public enum IntrinsicArity { Fixed, OptionalTrailing, Variadic }

/// <summary>The MATHEMATICALLY BOUNDED codomains a §15.x.4 returned-value rule states, for the §15.4.1 float
/// family (fix-queue PB65 / RV-15.75.4-1). The family enumerates MECHANICALLY from the rules: a bound belongs
/// here exactly when the away-from-zero quantization at the working scale can exit it — an OPEN bound
/// (RANDOM's "less than one", ATAN's ±π/2) or a CLOSED-but-IRRATIONAL one (ASIN's ±π/2, ACOS's π — rounding
/// <c>asin(1)</c> at scale 9 gives 1.570796327 &gt; π/2, outside even the closed bound). SIN/COS's closed
/// RATIONAL ±1 is deliberately absent: rounding cannot exceed an exactly-representable bound the double
/// already respects. FRACTION-PART's (−1, 1) is EAE-derived (§15.42.4 r1 — a fraction part of magnitude 1 is
/// impossible) rather than stated. The quantizer's ROUNDING itself must stay — it is what recovers
/// <c>SQRT(10) ** 2</c> = 10 from the binary64 artifact (NIST IF136A) — so the fix is a clamp on the
/// QUANTIZED value, against Int128 scale-37 constants (<c>CobolIntrinsics.FromDoubleBounded</c>).</summary>
public enum IntrinsicCodomain { None, UnitOpen, HalfPi, Pi }

/// <summary>The rational VALUE domain a §15.x.3 "The value of argument-1 shall be …" rule states for a §15.4.1
/// binary64-family row (kb/Work PB952) — the argument-side twin of <see cref="IntrinsicCodomain"/>. §15.3 rule 14
/// makes a value outside it EC-ARGUMENT-FUNCTION, and the renderer screens it on the EXACT operand before the
/// binary64 conversion (<c>CobolIntrinsics.DomainScaled</c> / <c>DomainDec</c> / <c>DomainReal</c>), because a
/// correctly-rounded conversion moves a value across the bound. <c>IntrinsicArgumentDomainDriftTests</c> derives
/// the column from the standard's own rule text, so a new row with such a rule cannot omit it.</summary>
public enum IntrinsicDomain { None, ClosedUnit, NonNegative, Positive, AboveMinusOne }

/// <summary>How a catalog row binds: <see cref="Runtime"/> — a runtime body; <see cref="Fold"/> — a
/// compile-time fold; <see cref="Deferred"/> — the renderer's never-hit backstop (P11 drove the backlog to
/// zero and it stays only so an unregistered row fails LOUD).
/// <para>⚠ The former <c>Unsupported</c> member — the A.4.9 locale module's documented-non-support bind,
/// COBOLNET1518 — is DELETED (kb/Work PB64 T6): every A.4.9 element is implemented (owner decision Q1,
/// 2026-08-18; DESIGN-locale-facility §12 T1–T7), the last rows moved to <see cref="Runtime"/> at T4, and an
/// enum case with zero rows is a lookup nobody reads (feedback_a_dead_lookup_is_also_unverified).</para></summary>
public enum IntrinsicBind { Runtime, Fold, Deferred }

/// <summary>
/// One catalog row (deep-dive D2 — the SINGLE source of result-category truth). <paramref name="ArgKinds"/> is the
/// per-position argument-TYPE code per §15.3 — <c>'n'</c> numeric (type 10), <c>'i'</c> integer (type 6),
/// <c>'s'</c> alphanumeric/string (types 1/2/9), <c>'c'</c> the CONCAT set, <c>'b'</c> boolean (type 3),
/// <c>'p'</c> category-polymorphic (MAX/MIN families resolve by the bound arguments), <c>'o'</c> ordering-name
/// (type 12 — a NAME the function's own binder resolves, never an operand) — with the LAST character repeating
/// for arguments past the string's length (the variadic tail). A code that is not an OPERAND kind carries its
/// disposition in <c>IntrinsicArgumentRules.NonOperandArgumentKinds</c>, and
/// <c>IntrinsicArgumentClassDriftTests</c> fails on a code with neither an <c>Admissible</c> arm nor an entry
/// there — the PB1 dead-column trap, closed for every future name/keyword kind.
/// <paramref name="Float"/> marks the §15.4.1 floating-math family (computes in double; the emitter quantizes via
/// the one <c>FromDouble</c>). <paramref name="IntroducedIn"/>/<paramref name="RemovedIn"/> are the D8 edition
/// window (85 = the 1989 Intrinsic Function Module amendment, part of the CCVS-85 corpus), enforced by the binder
/// against <c>--std</c> with a per-edition diagnostic.
/// </summary>
public readonly record struct IntrinsicSig(
    string Name, IntrinsicType Type, IntrinsicArity Arity, int MinArgs, int MaxArgs,
    string ArgKinds, string RuntimeMethod, IntrinsicBind Bind, bool Float,
    int IntroducedIn, int? RemovedIn = null,
    IntrinsicResultRule Result = IntrinsicResultRule.Fixed,
    IntrinsicCodomain Codomain = IntrinsicCodomain.None,
    IntrinsicDomain Domain = IntrinsicDomain.None, string? DomainRule = null)
{
    /// <summary>The §15.3 kind code of argument position <paramref name="i"/> (0-based; the last code repeats).</summary>
    public char ArgKind(int i) =>
        ArgKinds.Length == 0 ? 'n' : ArgKinds[Math.Min(i, ArgKinds.Length - 1)];

    /// <summary>Does the function's DEFINITION "permit an argument to be repeated a variable number of times"
    /// (ISO §15.3 — the precondition for the ALL subscript in an argument; kb/Work PB62)? True exactly when the
    /// §15.x.2 general format carries an ellipsis (<c>{ argument-1 } …</c>, <c>argument-2 …</c>, <c>[ argument-2 ] …</c>),
    /// which is the same fact the row's UNBOUNDED <see cref="MaxArgs"/> records — the equivalence is pinned against
    /// the spec's formats by <c>IntrinsicRepeatedArgumentDriftTests</c>, so this is a derivation, not a second column.
    /// (CONVERT and FIND-STRING are Variadic through their PHRASE words and repeat nothing: MaxArgs 4 and 3.)</summary>
    public bool RepeatsAnArgument => MaxArgs == int.MaxValue;

    /// <summary>The data category of the function result (§15.2 → §8.4.2) — what MOVE/comparison/DISPLAY consult.
    /// <para>
    /// ⚠ For a row whose <see cref="Result"/> rule is not <see cref="IntrinsicResultRule.Fixed"/> this is the
    /// DECLARED type's category, which is only the answer for a call whose arguments select that row of the
    /// §15.x.1 table. The binder resolves the call's actual type through
    /// <c>IntrinsicResultType.Resolve</c> and stores the resulting category on the bound node — read
    /// <c>BoundIntrinsicCall.ResultCategory</c>, not this, when a call is in hand.
    /// </para></summary>
    public PicCategory ResultCategory => CategoryOf(Type);

    /// <summary>THE §15.2-type → §8.4.2-category mapping, in ONE place so the declared and the argument-resolved
    /// paths cannot disagree. A NATIONAL-type function's result IS category national (§15.2 type 4 — NATIONAL-OF
    /// §15.66.1: "the type of the function is national"), and a BOOLEAN-type function's result IS class/category
    /// boolean with implicit usage bit (§15.2 item 2; §8.5.2.5 item 4 lists "a boolean function"), so the
    /// §14.9.25.3 Table-16 legality and the string channels see the correct class — never an alphanumeric or
    /// numeric fold.
    /// <para>
    /// ⚠ INTEGER, NUMERIC and INDEX all fold to <see cref="PicCategory.Numeric"/> here. That is correct for every
    /// consumer that exists today — the distinctions are §15.2 TYPE distinctions with no category of their own in
    /// §8.4.2 — and it is exactly why <c>IntrinsicResultType.Resolve</c> returns an
    /// <see cref="IntrinsicType"/> rather than a category: the standard's classification survives the fold.
    /// </para></summary>
    public static PicCategory CategoryOf(IntrinsicType type) => type switch
    {
        IntrinsicType.National => PicCategory.National,
        IntrinsicType.Alphanumeric => PicCategory.Alphanumeric,
        IntrinsicType.Boolean => PicCategory.Boolean,
        _ => PicCategory.Numeric,
    };
}

/// <summary>
/// The ONE declarative intrinsic-function table (ISO §15.6 summary of functions; deep-dive D2 — replaces the
/// legacy's ad-hoc AlphanumericFunctions set + scattered special cases). Adding a function is one row. Every
/// catalogued ISO function is LIVE (P11 drove the <see cref="IntrinsicBind.Deferred"/> backlog to zero): each row
/// binds <see cref="IntrinsicBind.Runtime"/> (a runtime body) or <see cref="IntrinsicBind.Fold"/> (a compile-time
/// fold — LENGTH/BYTE-LENGTH/the ALGEBRAIC family/WHEN-COMPILED); the A.4.9 locale rows bind Runtime since
/// PB64 T4–T6 (the former Unsupported member is deleted — the module is claimed). The
/// <see cref="IntrinsicBind.Deferred"/> member remains only as the renderer's never-hit backstop. Edition windows for post-85 rows beyond the 2023 seven-function
/// delta (docs/VERSION_CHANGE_REFERENCE.md rows 65–73) are firmed against the 2002/2014/2023 standards per the
/// docs/rearchitecture/PHASE-11 §7 window-authority table.
/// </summary>
public static class IntrinsicCatalog
{
    public static bool TryGet(string name, out IntrinsicSig sig) => Table.TryGetValue(name, out sig);

    private static readonly Dictionary<string, IntrinsicSig> Table = Build();

    private static Dictionary<string, IntrinsicSig> Build()
    {
        var t = new Dictionary<string, IntrinsicSig>(StringComparer.OrdinalIgnoreCase);
        void Add(IntrinsicSig s) => t.Add(s.Name, s);
        const int inf = int.MaxValue;

        // ── The 1989 Intrinsic Function Module (IntroducedIn 85 — the NIST IF101A..IF142A surface) ──────────
        // §15.4.1 floating-math family (double + FromDouble quantization; Float: true).
        Add(new("ACOS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Acos", IntrinsicBind.Runtime, true, 85, Codomain: IntrinsicCodomain.Pi, Domain: IntrinsicDomain.ClosedUnit, DomainRule: "§15.8.3 rule 2"));         // §15.8.4 r1: [0, π], π irrational
        Add(new("ASIN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Asin", IntrinsicBind.Runtime, true, 85, Codomain: IntrinsicCodomain.HalfPi, Domain: IntrinsicDomain.ClosedUnit, DomainRule: "§15.10.3 rule 2"));     // §15.10.4 r1: [−π/2, π/2], irrational
        Add(new("ATAN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Atan", IntrinsicBind.Runtime, true, 85, Codomain: IntrinsicCodomain.HalfPi));     // §15.11.4 r1: (−π/2, π/2), OPEN
        Add(new("COS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Cos", IntrinsicBind.Runtime, true, 85));             // §15.20
        Add(new("SIN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Sin", IntrinsicBind.Runtime, true, 85));             // §15.82
        Add(new("TAN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Tan", IntrinsicBind.Runtime, true, 85));             // §15.89
        Add(new("SQRT", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Sqrt", IntrinsicBind.Runtime, true, 85, Domain: IntrinsicDomain.NonNegative, DomainRule: "§15.84.3 rule 2"));           // §15.84
        Add(new("LOG", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Log", IntrinsicBind.Runtime, true, 85, Domain: IntrinsicDomain.Positive, DomainRule: "§15.55.3 rule 2"));             // §15.55
        Add(new("LOG10", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Log10", IntrinsicBind.Runtime, true, 85, Domain: IntrinsicDomain.Positive, DomainRule: "§15.56.3 rule 2"));         // §15.56
        Add(new("ANNUITY", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "ni", "Annuity", IntrinsicBind.Runtime, true, 85, Domain: IntrinsicDomain.NonNegative, DomainRule: "§15.9.3 rule 2"));    // §15.9
        Add(new("PRESENT-VALUE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 2, inf, "n", "PresentValue", IntrinsicBind.Runtime, true, 85, Domain: IntrinsicDomain.AboveMinusOne, DomainRule: "§15.74.3 rule 2")); // §15.74
        Add(new("RANDOM", IntrinsicType.Numeric, IntrinsicArity.OptionalTrailing, 0, 1, "i", "Random", IntrinsicBind.Runtime, true, 85, Codomain: IntrinsicCodomain.UnitOpen));        // §15.75.4 r1: [0, 1), OPEN
        Add(new("STANDARD-DEVIATION", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "StandardDeviation", IntrinsicBind.Runtime, true, 85)); // §15.86
        Add(new("VARIANCE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "Variance", IntrinsicBind.Runtime, true, 85));          // §15.98

        // Exact numeric / integer family (unscaled Int128 at a known scale — deep-dive D1).
        Add(new("FACTORIAL", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "Factorial", IntrinsicBind.Runtime, false, 85)); // §15.36
        Add(new("INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "Floor", IntrinsicBind.Runtime, false, 85));       // §15.44
        Add(new("INTEGER-PART", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "Truncate", IntrinsicBind.Runtime, false, 85)); // §15.49
        Add(new("MOD", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ii", "ModScaled", IntrinsicBind.Runtime, false, 85));      // §15.64
        Add(new("REM", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "nn", "RemScaled", IntrinsicBind.Runtime, false, 85));      // §15.77
        Add(new("MAX", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "p", "MaxScaled", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.FollowsUniformArguments));  // §15.59.1
        Add(new("MIN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "p", "MinScaled", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.FollowsUniformArguments));  // §15.63.1
        Add(new("ORD-MAX", IntrinsicType.Integer, IntrinsicArity.Variadic, 1, inf, "p", "OrdMax", IntrinsicBind.Runtime, false, 85)); // §15.71
        Add(new("ORD-MIN", IntrinsicType.Integer, IntrinsicArity.Variadic, 1, inf, "p", "OrdMin", IntrinsicBind.Runtime, false, 85)); // §15.72
        Add(new("SUM", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "SumScaled", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.IntegerFollowsAllArguments));  // §15.88.1
        Add(new("MEAN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MeanScaled", IntrinsicBind.Runtime, false, 85)); // §15.60
        Add(new("MEDIAN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MedianScaled", IntrinsicBind.Runtime, false, 85)); // §15.61
        Add(new("MIDRANGE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MidrangeScaled", IntrinsicBind.Runtime, false, 85)); // §15.62
        Add(new("RANGE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "RangeScaled", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.IntegerFollowsAllArguments)); // §15.76.1
        Add(new("NUMVAL", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "Numval", IntrinsicBind.Runtime, false, 85));       // §15.67
        Add(new("NUMVAL-C", IntrinsicType.Numeric, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "NumvalC", IntrinsicBind.Runtime, false, 85)); // §15.68

        // Character family + LENGTH/ORD.
        Add(new("CHAR", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "i", "Char", IntrinsicBind.Runtime, false, 85));      // §15.15
        Add(new("ORD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "Ord", IntrinsicBind.Runtime, false, 85));             // §15.70
        Add(new("LENGTH", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "Length", IntrinsicBind.Fold, false, 85));          // §15.50 (D7 compile-time fold)
        Add(new("LOWER-CASE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "LowerCase", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.FollowsArgument1)); // §15.57.1
        Add(new("UPPER-CASE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "UpperCase", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.FollowsArgument1)); // §15.97.1
        Add(new("REVERSE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "Reverse", IntrinsicBind.Runtime, false, 85, Result: IntrinsicResultRule.FollowsArgument1)); // §15.78.1

        // Date/time family (CobolDate; integer date form §15.5.2).
        Add(new("CURRENT-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "CurrentDate", IntrinsicBind.Runtime, false, 85)); // §15.21
        Add(new("WHEN-COMPILED", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "WhenCompiled", IntrinsicBind.Fold, false, 85));  // §15.99 (compile-time constant, r2)
        Add(new("DATE-OF-INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "DateOfInteger", IntrinsicBind.Runtime, false, 85)); // §15.22
        Add(new("DAY-OF-INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "DayOfInteger", IntrinsicBind.Runtime, false, 85));   // §15.24
        Add(new("INTEGER-OF-DATE", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "IntegerOfDate", IntrinsicBind.Runtime, false, 85)); // §15.46
        Add(new("INTEGER-OF-DAY", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "IntegerOfDay", IntrinsicBind.Runtime, false, 85));   // §15.47

        // ── COBOL-2002 additions (implemented where the runtime body is trivial; Deferred otherwise) ─────────
        Add(new("E", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "E", IntrinsicBind.Runtime, true, 2002));                 // §15.27
        Add(new("PI", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "Pi", IntrinsicBind.Runtime, true, 2002));               // §15.73
        Add(new("EXP", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Exp", IntrinsicBind.Runtime, true, 2002));            // §15.34
        Add(new("EXP10", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Exp10", IntrinsicBind.Runtime, true, 2002));        // §15.35
        Add(new("SIGN", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "SignOf", IntrinsicBind.Runtime, false, 2002));       // §15.81
        Add(new("FRACTION-PART", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "FractionPart", IntrinsicBind.Runtime, false, 2002, Codomain: IntrinsicCodomain.UnitOpen)); // §15.42.4 r1: |v| < 1 by the EAE (float twin only)
        // BOOLEAN-OF-INTEGER (§15.13) — argument-1's binary value as a boolean item of argument-2 positions
        // (rightmost = low-order digit; zero-filled or TRUNCATED ON THE LEFT — the result is arg-1 mod
        // 2^arg-2, Annex D.10). A boolean function result IS class/category boolean (§15.2 item 2).
        Add(new("BOOLEAN-OF-INTEGER", IntrinsicType.Boolean, IntrinsicArity.Fixed, 2, 2, "ii", "BooleanOfInteger", IntrinsicBind.Runtime, false, 2002)); // §15.13
        // BYTE-LENGTH (§15.14) — a COMPILE-TIME FOLD like LENGTH (§15.50), but counting BYTES, not character
        // positions (the D7 distinction). The per-usage byte widths are IMPLEMENTOR-DEFINED (§13.18.60 GR4/6/7/
        // 8/11/12; §8.1.2) — WiseOwl COBOL pins them in DataItem.ByteWidth (documented in COBOLNET_INTRINSICS_DESIGN):
        // 1 byte/character-position for DISPLAY (and boolean/BIT, the §13.18.40.4 R14 one-character
        // representation), 2 bytes/position for NATIONAL (UTF-16, D-N1), the binary/packed StorageWidth,
        // 4/8 for the float trio, 8 for index/pointer/object-reference carriers. Runtime-length shapes
        // (ref-mod / ODO / ANY LENGTH) stay loud by name (§15.14.4 r2/r5, the LENGTH discipline).
        Add(new("BYTE-LENGTH", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "ByteLength", IntrinsicBind.Fold, false, 2002));   // §15.14 (byte size ≠ FUNCTION LENGTH, D7); the runtime body serves the runtime-length shapes (PB61)
        // CHAR-NATIONAL (§15.16) — the national twin of CHAR: the character at the 1-based ordinal position of
        // the NATIONAL program collating sequence (native UTF-16 order; a non-native ALPHABET … FOR NATIONAL
        // sequence rides the CollateNat/__COLLATE_NAT channel, P10 Step 4). Result class national (§15.16.1).
        Add(new("CHAR-NATIONAL", IntrinsicType.National, IntrinsicArity.Fixed, 1, 1, "i", "CharNational", IntrinsicBind.Runtime, false, 2002));  // §15.16
        // The Y2K windowing trio (§15.23/§15.25/§15.100) — ONE windowing core (CobolDate.YearToYyyy); the
        // composite pair is defined BY REFERENCE to it (§15.23.4 r1 / §15.25.4 r1). The window is ALWAYS 100
        // years ending at maximum-year = argument-2 + argument-3 (argument-2 = a SIGNED offset, default 50 —
        // NOT a window size; argument-3 defaults to the year at EXECUTION time, §15.100.3 r5).
        Add(new("DATE-TO-YYYYMMDD", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "DateToYyyymmdd", IntrinsicBind.Runtime, false, 2002)); // §15.23
        Add(new("DAY-TO-YYYYDDD", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "DayToYyyyddd", IntrinsicBind.Runtime, false, 2002));   // §15.25
        Add(new("YEAR-TO-YYYY", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "YearToYyyy", IntrinsicBind.Runtime, false, 2002));     // §15.100
        // DISPLAY-OF (§15.26) / NATIONAL-OF (§15.66) — the sanctioned national↔alphanumeric repertoire pair
        // (P10 national wave). Argument-2 is a one-character SUBSTITUTION CHARACTER (§15.26.3 r2 / §15.66.3 r2 —
        // the 2023 text names no codeset facility), so both argument forms are fully implemented.
        Add(new("DISPLAY-OF", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "DisplayOf", IntrinsicBind.Runtime, false, 2002));   // §15.26
        Add(new("NATIONAL-OF", IntrinsicType.National, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "NationalOf", IntrinsicBind.Runtime, false, 2002));     // §15.66
        Add(new("EXCEPTION-FILE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 0, 1, "s", "EcFile", IntrinsicBind.Runtime, false, 2002)); // §15.28 (no-arg form r1; the 2023 file-connector-arg form renders loud — VCR row 68)
        // The EXCEPTION-* family renders the runtime last-exception register (EcFunctions, the §11 EC model).
        // The -N national twins — EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31, the ONLY two ISO
        // defines (no -N exists for EXCEPTION-STATEMENT/-STATUS) — are the same renderings projected national
        // through the ONE NationalOf repertoire translator (P10 Step-11 EC-N wave); their National type rows
        // carry the category-national result. EXCEPTION-FILE-N's 2023 file-connector-argument form (§15.29.4
        // r2, E.3.3 item 26) renders loud like the base's — VCR row 69, PHASE-13 Step 9.
        Add(new("EXCEPTION-FILE-N", IntrinsicType.National, IntrinsicArity.OptionalTrailing, 0, 1, "s", "EcFileN", IntrinsicBind.Runtime, false, 2002));   // §15.29
        Add(new("EXCEPTION-LOCATION", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcLocation", IntrinsicBind.Runtime, false, 2002));  // §15.30
        Add(new("EXCEPTION-LOCATION-N", IntrinsicType.National, IntrinsicArity.Fixed, 0, 0, "", "EcLocationN", IntrinsicBind.Runtime, false, 2002));   // §15.31
        Add(new("EXCEPTION-STATEMENT", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcStatement", IntrinsicBind.Runtime, false, 2002)); // §15.32
        Add(new("EXCEPTION-STATUS", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcStatus", IntrinsicBind.Runtime, false, 2002));    // §15.33
        Add(new("HIGHEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "", IntrinsicBind.Fold, false, 2002, Result: IntrinsicResultRule.IntegerFollowsArgument1)); // §15.43.1 (compile-time PICTURE fold)
        Add(new("LOWEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "", IntrinsicBind.Fold, false, 2002, Result: IntrinsicResultRule.IntegerFollowsArgument1));  // §15.58.1 (compile-time PICTURE fold)
        Add(new("INTEGER-OF-BOOLEAN", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "IntegerOfBoolean", IntrinsicBind.Runtime, false, 2002)); // §15.45 — the unsigned MSB-first value of the bit configuration (r1)
        // The A.4.9 locale module (optional): the FOUR locale functions are documented non-support until their
        // increment lands (owner decision Q1, 2026-08-18 — the module IS being implemented, §12 of
        // DESIGN-locale-facility sequences it), and they take a bare POSITIONAL [locale-name-1] (no LOCALE
        // keyword, §15.51.2/.52.2/.53.2/.54.2). STANDARD-COMPARE (§15.85) is A.4.9 item 11 but NOT
        // locale-dependent — it consumes an ISO/IEC 14651:2020 cultural ordering table (SPECIAL-NAMES ORDER
        // TABLE, §12.3.7.4 GR17) — and it is IMPLEMENTED (kb/Work PB101 T7, owner decision Q4): support IS
        // claimed under A.3 item 25 over the derived CLDR/UCA collation engine, so its row binds Runtime.
        // ⛔ LOCALE-DATE AND LOCALE-TIME BOTH READ `"is"` — ARGUMENT-1 AS AN INTEGER — AND BOTH WERE WRONG
        // (fix-queue PB27). §15.52.3 r1 and §15.53.3 r1 each say "Argument-1 shall be of class ALPHANUMERIC OR
        // NATIONAL and shall be 8 [resp. 6] CHARACTER POSITIONS in length", and the normative Table 21 agrees
        // for both: `Anum1 or Nat1, Loc2`. The queue entry named LOCALE-TIME alone; LOCALE-DATE is the line
        // ABOVE it with the identical wrong value (feedback_scan_all_similar).
        // ⚠ ANNEX D DISAGREES AND DOES NOT GOVERN: D.31.4.2 describes argument-1 as "a date in standard date
        // form (YYYYMMDD)", which reads as an integer — but Annex D is marked `(informative)` and §1167 calls it
        // an explanation of features, so clause 15 and Table 21 decide. A concepts annex is not a rule.
        // ⚙ Until PB64 T4 the column was never read (the rows bound as documented non-support, so
        // COBOLNET1518 fired first) — a dead lookup is also an unverified one
        // (feedback_a_dead_lookup_is_also_unverified); the Verified schemas re-derived it before the rows
        // went live. PB1 is the standing proof of the cost of skipping that re-derivation.
        // LIVE since kb/Work PB64 T4 (DESIGN-locale-facility §4.7/§4.8): the trailing 'l' is §15.3 argument type 8, a
        // LOCALE-NAME — a word resolved in the SPECIAL-NAMES locale table by IntrinsicBinder.BindLocaleFunction, never an
        // operand (IntrinsicArgumentRules.NonOperandArgumentKinds['l']). The runtime methods are CobolLocale.*.
        Add(new("LOCALE-COMPARE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 3, "ssl", "Compare", IntrinsicBind.Runtime, false, 2002)); // §15.51 (A.4.9 item 2) — Table 21 `Alph1/Anum1/Nat1, Alph2/Anum2/Nat2, Loc3`
        Add(new("LOCALE-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "sl", "Date", IntrinsicBind.Runtime, false, 2002));     // §15.52 (A.4.9 item 3) — §15.52.3 r1: alphanumeric/national, 8 character positions
        Add(new("LOCALE-TIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "sl", "Time", IntrinsicBind.Runtime, false, 2002));     // §15.53 (A.4.9 item 4) — §15.53.3 r1: alphanumeric/national, 6 character positions
        // ⛔ 2014, not 2002 (kb/Work R28, decided 2026-08-08): the WG4 CD 1.2 (2009) Annex D.2 item 4 lists
        // BOTH among "the intrinsic functions ... introduced in this committee draft International Standard"
        // (the draft of ISO 1989:2014). They were previously windowed 2002, over-accepting at --std 2002.
        Add(new("LOCALE-TIME-FROM-SECONDS", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "nl", "TimeFromSeconds", IntrinsicBind.Runtime, false, 2014)); // §15.54 (A.4.9 item 5) — LIVE (PB64 T4)
        Add(new("SECONDS-PAST-MIDNIGHT", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "SecondsPastMidnight", IntrinsicBind.Runtime, false, 2014)); // §15.80 — NUMERIC (fractional seconds); the RunUnit.Clock seam, scale 7
        // STANDARD-COMPARE (§15.85.2: `FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ]
        // [ argument-4 ] )`) — LIVE since kb/Work PB101 T7 (BindStandardCompare + CobolIntrinsics.StandardCompare).
        // ⛔ THE ARGKINDS COLUMN WAS `"ssis"`, WHICH IS THE FORMAT READ BACKWARDS: position 3 is the
        // ORDERING-NAME (§15.3 argument type 12 — a NAME, never an operand) and position 4 is the integer
        // ordering level (§15.85.3 r6). It never contradicted anything because Bind = Unsupported made the column
        // a dead lookup (feedback_a_dead_lookup_is_also_unverified) — the exact PB1 shape, and the reason the row
        // was re-derived from §15.85.2 rather than trusted. `'o'` is the ordering-name kind; it never reaches an
        // operand list (the bespoke binder consumes the word), so IntrinsicArgumentRules.NonOperandArgumentKinds
        // carries its disposition and the drift test holds catalog vocabulary and screen vocabulary in agreement.
        // The IntroducedIn 2002 is now ENFORCED rather than suppressed (Bind is no longer Unsupported): ORDER and
        // its clause are 2002 reservations (reserved-words.json ORDER r85 false / r2002 true), and Annex E lists
        // no STANDARD-COMPARE delta — VERSION_CHANGE_REFERENCE records the derivation and its limits.
        Add(new("STANDARD-COMPARE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 4, "ssoi", "StandardCompare", IntrinsicBind.Runtime, false, 2002)); // §15.85 (A.4.9 item 11 + A.3 item 25 — support claimed)
        // The TEST validators (§15.90/§15.91/§15.93/§15.94) — verdict chains beside their value parsers:
        // the date pair is year-before-month-before-day (D.31.3.8/9 confirm codes 0/1/2[/3]); the NUMVAL
        // pair is 0 / first-error position (the "0 1"→3 embedded-space sub-note; arithmetic-mode digit caps)
        // / LENGTH+1 (the r1c leg — zero-length, all-spaces, incomplete like " +."). TEST-NUMVAL-C rides the
        // ONE BindNumvalCFamily bespoke bind (currency injection + ANYCASE, §15.94.3 r1 → §15.68.3).
        Add(new("TEST-DATE-YYYYMMDD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "TestDateYyyymmdd", IntrinsicBind.Runtime, false, 2002)); // §15.90
        Add(new("TEST-DAY-YYYYDDD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "TestDayYyyyddd", IntrinsicBind.Runtime, false, 2002));   // §15.91
        Add(new("TEST-NUMVAL", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "TestNumval", IntrinsicBind.Runtime, false, 2002));        // §15.93
        Add(new("TEST-NUMVAL-C", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "TestNumvalC", IntrinsicBind.Runtime, false, 2002)); // §15.94
        // NOTE — CONCATENATE is NOT an ISO/IEC 1989 function and is intentionally ABSENT from this catalog.
        // The P11 anchor re-scout established (docs/rearchitecture/PHASE-11-scout-notes.md, spec:concat-smallest)
        // that the string "CONCATENATE" has ZERO occurrences anywhere in ISO/IEC 1989:2023 — not in §15, not in
        // the §8.9 intrinsic-function-name list, not in the Annex E incompatibility/substitution lists, not in
        // the archaic/obsolete lists. §15.18 CONCAT is a NEW-IN-2023 function (Annex E.3 item 23 "has been
        // added"); there is no evidence CONCATENATE was ever an ISO name at any edition. The earlier catalog row
        // (window [2002,2023), premised on "CONCATENATE = the 2002/2014 name removed in 2023") was audit drift —
        // a 2023 removal of a real 2002 function would appear in the E.2 incompatibility list, and it does not.
        // CONCATENATE is a vendor extension (Micro Focus / GnuCOBOL / ACUCOBOL); a reference to it correctly
        // draws COBOLNET1501 ("not an intrinsic function of ISO/IEC 1989"). Adding it as a dialect-gated vendor
        // extension is a SEPARATE decision for a future vendor-extension wave, not an ISO edition window.

        // ── COBOL-2014 additions — windows CONFIRMED 2026-08-08 (kb/Work R28): the WG4 CD 1.2 (2009) Annex
        // D.2 item 4 new-function list, adversarially verified against the published-2014 inventory. The
        // former "provisional pending the matrix wave" caveat is retired; NUMVAL-F/TEST-NUMVAL-F moved OUT
        // of this block to 2002 (the COBOL Consortium's COBOL2002 introduction names them as 2002 additions),
        // and LOCALE-TIME-FROM-SECONDS/SECONDS-PAST-MIDNIGHT moved IN from 2002. ──────────────────────────
        Add(new("ABS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "AbsScaled", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.IntegerFollowsArgument1));     // §15.7.1
        Add(new("COMBINED-DATETIME", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "in", "CombinedDatetime", IntrinsicBind.Runtime, false, 2014)); // §15.17
        // ⚠ The FORMATTED-* family's result type follows argument-1 — the FORMAT literal (§15.38.1/§15.39.1/
        // §15.40.1/§15.41.1) — so FUNCTION FORMATTED-DATE(N"YYYYMMDD" D) is a NATIONAL function even though the
        // date it renders is an integer. §15.38.3 r1 and siblings admit "a national or alphanumeric literal".
        Add(new("FORMATTED-CURRENT-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "FormattedCurrentDate", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.FollowsArgument1)); // §15.38.1
        Add(new("FORMATTED-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 2, 2, "si", "FormattedDate", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.FollowsArgument1));        // §15.39.1
        Add(new("FORMATTED-DATETIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 3, 4, "sinn", "FormattedDatetime", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.FollowsArgument1)); // §15.40.1 (a4 = optional offset minutes)
        Add(new("FORMATTED-TIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 3, "snn", "FormattedTime", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.FollowsArgument1));      // §15.41.1 (a3 = optional offset minutes)
        Add(new("INTEGER-OF-FORMATTED-DATE", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ss", "IntegerOfFormattedDate", IntrinsicBind.Runtime, false, 2014)); // §15.48
        // ⛔ 2002, not 2014 (kb/Work R28, decided 2026-08-08): the COBOL Consortium's COBOL2002 introduction
        // names NUMVAL-F/TEST-NUMVAL-F among the functions ADDED by COBOL2002, and both are ABSENT from the
        // WG4 2014-cycle drafts' new-function lists. The provisional 2014 window rejected legal 2002 COBOL.
        Add(new("NUMVAL-F", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "NumvalF", IntrinsicBind.Runtime, false, 2002));        // §15.69
        Add(new("SECONDS-FROM-FORMATTED-TIME", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "ss", "SecondsFromFormattedTime", IntrinsicBind.Runtime, false, 2014)); // §15.79
        Add(new("TEST-FORMATTED-DATETIME", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ss", "TestFormattedDatetime", IntrinsicBind.Runtime, false, 2014)); // §15.92
        Add(new("TEST-NUMVAL-F", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "TestNumvalF", IntrinsicBind.Runtime, false, 2002));   // §15.95 — 2002 per the R28 research (see NUMVAL-F above)
        Add(new("TRIM", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 1, inf, "ss", "Trim", IntrinsicBind.Runtime, false, 2014, Result: IntrinsicResultRule.FollowsArgument1)); // §15.96.1 — arg-1 + the LEADING/TRAILING phrase + one-or-more argument-2 trim chars (special bind path)

        // ── COBOL-2023 additions (docs/VERSION_CHANGE_REFERENCE.md rows 65–73) ────────────────────────────────
        Add(new("BASECONVERT", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 3, 3, "sii", "BaseConvert", IntrinsicBind.Runtime, false, 2023, Result: IntrinsicResultRule.FollowsArgument1)); // §15.12.1
        Add(new("CONCAT", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 2, inf, "s", "Concat", IntrinsicBind.Runtime, false, 2023, Result: IntrinsicResultRule.FollowsConcatArguments)); // §15.18.1
        Add(new("CONVERT", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 3, 4, "s", "Convert", IntrinsicBind.Runtime, false, 2023, Result: IntrinsicResultRule.FollowsDestinationFormat)); // §15.19.1/.2 — arg-1 source-format destination-format = 3 arguments minimum, 4 with the trailing HEX (bespoke positional bind; the destination keywords, not an argument, decide the type)
        Add(new("FIND-STRING", IntrinsicType.Integer, IntrinsicArity.Variadic, 2, 3, "sssii", "FindString", IntrinsicBind.Runtime, false, 2023)); // §15.37 — arg-1 arg-2 [LAST] [[START AFTER] arg-3] [ANYCASE] (special bind path)
        Add(new("MODULE-NAME", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "ModuleName", IntrinsicBind.Runtime, false, 2023)); // §15.65 — the ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL keyword (special bind path)
        Add(new("SMALLEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "", IntrinsicBind.Fold, false, 2023, Result: IntrinsicResultRule.IntegerFollowsArgument1)); // §15.83.1 (compile-time PICTURE fold)
        Add(new("SUBSTITUTE", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 3, inf, "s", "Substitute", IntrinsicBind.Runtime, false, 2023, Result: IntrinsicResultRule.FollowsArgument1)); // §15.87.1 — arg-1 + one-or-more [ANYCASE][FIRST|LAST] arg-2 arg-3 pairs (special bind path)

        return t;
    }
}
