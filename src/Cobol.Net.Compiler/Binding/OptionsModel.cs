// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The fully-parsed content of a source unit's OPTIONS paragraph (ISO/IEC 1989:2023 §11.9), captured once at bind
/// time and exposed program-wide so every later pass can read it. CONSUMED today: ARITHMETIC +
/// INTERMEDIATE ROUNDING drive the numeric engine's standard-decimal SDIDI path (NumericRenderer.StandardDecimal
/// → CobolDec, §8.8.1.5), DEFAULT ROUNDED sets the bare-ROUNDED mode (§11.9.6). Still capture-only:
/// FLOAT-BINARY/DECIMAL (standard-float USAGE encodings) and INITIALIZE (the initial-state fill).
/// </summary>
/// <remarks>An absent OPTIONS paragraph (or an absent clause) yields the ISO-implied default — see
/// <see cref="Default"/>. The model carries the program's <i>intent</i>; whether a clause is yet <i>applied</i> is the
/// consuming feature's concern.</remarks>
public sealed record OptionsModel
{
    /// <summary>ARITHMETIC clause (§11.9.5). Default <see cref="ArithmeticMode.Native"/> when absent (§11.9.5.2 r4).</summary>
    public ArithmeticMode Arithmetic { get; init; } = ArithmeticMode.Native;

    /// <summary>DEFAULT ROUNDED clause (§11.9.6) — the rounding mode applied to a bare <c>ROUNDED</c> phrase that omits
    /// its own MODE. Default <see cref="CobolRounding.NearestAwayFromZero"/> when absent (§11.9.6.3 r2).</summary>
    public CobolRounding DefaultRounding { get; init; } = CobolRounding.NearestAwayFromZero;

    /// <summary>ENTRY-CONVENTION clause (§11.9.7): <c>COBOL</c> or an entry-convention-name, as written; null when
    /// absent (the entry convention is then COBOL, §11.9.7.4 GR4 c). Only COBOL is provided — any other name is
    /// refused by <c>OptionsBinder.EntryConventionOf</c> (COBOLNET2385, kb/Work PB232).</summary>
    public string? EntryConvention { get; init; }

    /// <summary>FLOAT-BINARY clause endianness (§11.9.8). <see cref="FloatEndianness.Unspecified"/> when absent (the
    /// implementor default applies).</summary>
    public FloatEndianness FloatBinaryEndianness { get; init; } = FloatEndianness.Unspecified;

    // No FLOAT-DECIMAL clause fields (§11.9.9): the clause is declined and accepted inert (COBOLNET2424,
    // OptionsBinder.DeclineFloatDecimal) because the standard decimal floating-point usages it configures are not
    // provided (Annex A.3 items 13 and 19).

    /// <summary>INTERMEDIATE ROUNDING clause (§11.9.11) — rounding for intermediate results under standard
    /// arithmetic. Default <see cref="CobolRounding.NearestAwayFromZero"/> when absent (§11.9.11.2 r3a).</summary>
    public CobolRounding IntermediateRounding { get; init; } = CobolRounding.NearestAwayFromZero;

    /// <summary>INITIALIZE clause (§11.9.10) — the section-background fill applied at allocation; null when absent.</summary>
    public OptionsInitialize? Initialize { get; init; }

    /// <summary>The all-defaults model used when a source unit has no OPTIONS paragraph (every ISO-implied default).</summary>
    public static readonly OptionsModel Default = new();
}

/// <summary>The ARITHMETIC clause mode (§11.9.5.1). <see cref="Standard"/> is the ISO/IEC 1989:2002 standardized
/// mode (NATIVE|STANDARD was the 2002 clause), designated obsolete by 2014 and removed by 2023 (Annex E.2
/// item 21) in favor of <see cref="StandardBinary"/>/<see cref="StandardDecimal"/>; the CCVS still writes it.</summary>
public enum ArithmeticMode { Native, Standard, StandardBinary, StandardDecimal }

/// <summary>Facts the standard states PER ARITHMETIC MODE. One table per fact, exhaustive over
/// <see cref="ArithmeticMode"/>, so adding a mode is a compile-or-drift-test failure rather than a silent
/// fall-through to whatever the old ternary's else-branch happened to be.</summary>
public static class ArithmeticModes
{
    /// <summary>The NUMVAL-family digit cap — the number of digits beyond which §15.93.4 r1b / §15.94.4 r1b
    /// report the next digit's position as the first character in error. The standard states it per mode, in
    /// three sub-notes of the same rule: <b>2)</b> "If native arithmetic is in effect, because the character in
    /// error for an argument that is greater than 31 digits is the 32nd digit…"; <b>3)</b> "If standard-binary
    /// arithmetic is in effect, and the argument has more than 35 digits…"; <b>4)</b> "If standard-decimal
    /// arithmetic is in effect, and the argument has more than 34 digits…".
    ///
    /// <para><see cref="ArithmeticMode.Standard"/> (the 2002 mode) is not named by those sub-notes; it takes 34
    /// because it routes to the SAME SDIDI decimal engine as STANDARD-DECIMAL for its reachable operands
    /// (<c>NumericRenderer.StandardDecimal</c>, <c>DataBinder.BindDeclarations</c>).</para>
    ///
    /// <para>⛔ <see cref="ArithmeticMode.StandardBinary"/>'s 35 is UNREACHABLE and is written down anyway. The
    /// mode is declined at bind (<c>OptionsBinder.ArithmeticOf</c> → COBOLNET0806), so no compilation can carry
    /// it this far — but a table with a hole is a table that answers a question it has never been asked, and the
    /// previous shape here was a two-state ternary whose else-branch silently gave standard-binary the NATIVE
    /// cap. Recording the spec's real value keeps the double defence honest: the mode is rejected, AND if it
    /// ever were not, this lane would not quietly emit the wrong number.
    /// <c>ArithmeticModeTableTests</c> asserts the table is exhaustive over the enum.</para></summary>
    public static int NumvalDigitCap(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native => 31,            // §15.93.4 r1b sub-note 2
        ArithmeticMode.Standard => 34,          // routes to the SDIDI decimal engine (see the remark above)
        ArithmeticMode.StandardBinary => 35,    // §15.93.4 r1b sub-note 3 — unreachable; declined at bind
        ArithmeticMode.StandardDecimal => 34,   // §15.93.4 r1b sub-note 4
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no §15.93.4 r1b digit cap for this mode"),
    };

    /// <summary>The cap the runtime assumes when the emitted call omits the argument — the native one. Kept
    /// beside the table so "which value means omit" cannot drift away from "what the table says native is".</summary>
    public const int DefaultDigitCap = 31;

    /// <summary>⛔ THE ONE SPELLING OF "this mode's arithmetic runs on the SDIDI decimal engine" (kb/Work PB194).
    /// <see cref="ArithmeticMode.Standard"/> — the 2002 mode, obsolete 2014, removed 2023 — is NOT a third engine:
    /// its standard intermediate data item for every operand WiseOwl COBOL can carry IS the standard DECIMAL one, so it
    /// routes to the same <c>CobolDec</c> path as STANDARD-DECIMAL (<c>NumericRenderer.StandardDecimal</c>,
    /// <c>DataBinder.BindDeclarations</c>, and <see cref="NumvalDigitCap"/>'s 34 above all say so).
    /// <para>The set used to be written down in four places and TWO of the copies named STANDARD-DECIMAL alone.
    /// Measured 2026-08-31 at <c>--std 2014</c>: <c>01 EA PIC 9(3)E+999.</c> with
    /// <c>OPTIONS. ARITHMETIC IS STANDARD.</c> was REJECTED — "COBOLNET1660 … outside the native (binary64)
    /// intermediate's range" — in a program that had selected a standard mode, while the identical entry under
    /// STANDARD-DECIMAL compiled and printed <c>999E+999</c>. One arithmetic mode, two contradictory bounds,
    /// decided by which file the code path happened to reach. <c>ArithmeticModeTableTests</c> now holds the set to
    /// one home (and to this accessor by a drift test).</para></summary>
    public static bool IsDecimalEngine(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native => false,
        ArithmeticMode.Standard => true,         // the 2002 mode — the SAME SDIDI engine (see the remark)
        ArithmeticMode.StandardBinary => false,  // SBIDI, §8.8.1.4.2 — unreachable; declined at bind (COBOLNET0806)
        ArithmeticMode.StandardDecimal => true,  // SDIDI, §8.8.1.5.2
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no engine classification for this mode"),
    };

    /// <summary>The DECIMAL EXPONENT bounds of the mode's intermediate data item, as the §8.8.4.4.4 GR3 l
    /// IN-ARITHMETIC-RANGE test (§15.43.4 r1 / §15.58.4 r1 / §15.83.4 r1) measures a data description's extremes
    /// against them.
    /// <para>⛔ <c>Farthest</c> IS A STRICT UPPER DECADE BOUND, NOT THE LARGEST MAGNITUDE'S OWN EXPONENT — it is
    /// the largest f for which every magnitude the caller can present below 10^f is in range, which is what the
    /// caller's own over-approximation (<c>intDigits + fm.MaxExp</c>, an "exponent the magnitude is strictly
    /// below") needs to be compared against. That is why the SDIDI row reads 6145 and not 6144: decimal128's
    /// maximum is (1 − 10⁻³⁴)·10^6145, i.e. 9.999…E+6144 with 34 nines, so the all-nines extremes this test
    /// receives sit just under 10^6145. Binary64 reads 308 rather than 309 for the mirror reason — its maximum
    /// leads with 1.797, so 9.99E+308 is NOT representable and a bound of 309 would admit it. This summary used
    /// to say "the decimal exponent of the largest representable magnitude", which is a floor(log₁₀) rule the
    /// SDIDI rows do not obey, and no single floor/ceil rule describes all four (kb/Work PB275).</para>
    /// <para>⚠ AND THE COLUMN IS READ UNDER TWO CONVENTIONS. <c>IntrinsicBinder</c>'s float-EDITED arm supplies
    /// <c>intDigits + fm.MaxExp</c> (the strict upper bound above); its float-ITEM arm supplies the carrier's
    /// EXACT magnitude exponent ({28, 38, 308}). Nothing separates them today — §13.18.40.4 GR13 b caps the
    /// exponent part at four digit positions and §13.18.40.3 SR15 the significand at 36, so the edited arm's
    /// reachable values are confined to [9,45] ∪ [99,135] ∪ [999,1035] ∪ [9999,10035] and 6144/6145 (and
    /// 308/309) are behaviourally interchangeable. If the ITEM arm ever gains a TRUE decimal128 carrier row it
    /// must be written 6144, its exact magnitude exponent, NOT 6145.</para>
    /// <para><c>Closest</c> is the EXACT decimal exponent of the smallest nonzero magnitude — exact for every
    /// row because that value is a power of ten in each format.</para>
    /// <list type="bullet">
    /// <item>NATIVE — the implementor-defined intermediate is IEEE binary64 (numeric design D16): 1.797E+308 down
    /// to 4.94E−324.</item>
    /// <item>STANDARD / STANDARD-DECIMAL — the SDIDI. §8.8.1.5.2 NOTE 2: the range is ±9.999…E+6144 "with a maximum
    /// precision of 34 decimal digits; the smallest positive nonzero value is 1.0E-6176".</item>
    /// <item>STANDARD-BINARY — the SBIDI. §8.8.1.4.2 NOTE 3: ±(2**16384 − 2**16271) ≈ 1.19E+4932, and "the smallest
    /// positive nonzero value is 2**-16494" ≈ 6.5E−4966. UNREACHABLE (declined at bind), recorded anyway for the
    /// same reason <see cref="NumvalDigitCap"/> records its 35: a table with a hole answers a question it has never
    /// been asked, and the shape this replaced was a two-state ternary whose else-branch silently gave every
    /// non-STANDARD-DECIMAL mode the NATIVE bounds.</item>
    /// </list></summary>
    public static (int Farthest, int Closest) IntermediateExponentRange(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native => (308, -324),           // binary64 (numeric design D16)
        ArithmeticMode.Standard => (6145, -6176),       // the SDIDI — same engine as STANDARD-DECIMAL
        ArithmeticMode.StandardBinary => (4932, -4966), // SBIDI, §8.8.1.4.2 NOTE 3 — unreachable; declined at bind
        ArithmeticMode.StandardDecimal => (6145, -6176),// SDIDI, §8.8.1.5.2 NOTE 2
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no intermediate exponent range for this mode"),
    };

    /// <summary>The mode's intermediate data item's own extreme MAGNITUDES, as exact decimal literal text — "the
    /// value farthest away from zero permitted by the specifications appropriate to the mode of arithmetic" and
    /// its nearest-to-zero twin, which ISO §14.9.39.4 GR32 b) and GR36 b) compare an item's extremes against for
    /// the SET Format 15 IN-ARITHMETIC-RANGE phrase.
    /// <para>⛔ DISTINCT FROM <see cref="IntermediateExponentRange"/>, and the pair must not be conflated: that
    /// one is a strict DECADE BOUND for a well-formedness SCREEN (its SDIDI row reads 6145 precisely because no
    /// representable value has that exponent), so using it as a VALUE would store 10^6145 — a magnitude the
    /// intermediate cannot hold. These are the representable extremes themselves.</para>
    /// <para>⚠ EVERY ROW IS CURRENTLY UNREACHABLE AS A CLAMP and is written anyway, exactly as
    /// <see cref="NumvalDigitCap"/>'s standard-binary row is. GR32 b/GR36 b take whichever bound is closer to /
    /// farther from zero, and for every data description WiseOwl COBOL can declare today the ITEM's bound wins or
    /// ties: the widest declarable carrier is binary64, whose extremes ARE the native row below, and the
    /// standard modes' SDIDI is wider still. The clamp bites the moment a wider carrier lands — a true IEEE
    /// binary128 <c>FLOAT-BINARY-128</c> reaches 1.19E+4932, past the native intermediate — and then it bites
    /// automatically rather than needing to be remembered.</para>
    /// <list type="bullet">
    /// <item>NATIVE — IEEE binary64 (numeric design D16): ±1.7976931348623157E+308, smallest nonzero (subnormal)
    /// 4.94…E−324.</item>
    /// <item>STANDARD / STANDARD-DECIMAL — the SDIDI, §8.8.1.5.2 NOTE 2: ±9.999…E+6144 at 34 digits of precision,
    /// smallest positive nonzero 1.0E−6176.</item>
    /// <item>STANDARD-BINARY — the SBIDI, §8.8.1.4.2 NOTE 3: ±(2**16384 − 2**16271), smallest positive nonzero
    /// 2**−16494. Declined at bind (COBOLNET0806), recorded for the same reason as above.</item>
    /// </list></summary>
    public static (string Farthest, string Nearest) IntermediateExtremes(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native =>
            ("1.7976931348623157E+308", "4.940656458412465441765687928682214E-324"),
        ArithmeticMode.Standard or ArithmeticMode.StandardDecimal =>
            ("9.999999999999999999999999999999999E+6144", "1E-6176"),
        ArithmeticMode.StandardBinary =>
            ("1.189731495357231765085759326628007E+4932", "6.475175119438025110924438958227646E-4966"),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no intermediate extremes for this mode"),
    };

    /// <summary>How a diagnostic NAMES the mode's intermediate data item. Beside the bounds so a message can never
    /// describe a compilation as "native (binary64)" while screening it against the SDIDI's numbers, which is
    /// exactly what the two drifted copies did (kb/Work PB194).</summary>
    public static string IntermediateName(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native => "native (binary64)",
        ArithmeticMode.Standard => "standard-decimal",          // ARITHMETIC IS STANDARD routes to the SDIDI
        ArithmeticMode.StandardBinary => "standard-binary",
        ArithmeticMode.StandardDecimal => "standard-decimal",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no intermediate name for this mode"),
    };
}

/// <summary>Endianness of a standard floating-point usage (FLOAT-BINARY / FLOAT-DECIMAL clauses, §11.9.8/§11.9.9,
/// and the USAGE clause's own endianness-phrase, §13.18.60.2).</summary>
public enum FloatEndianness { Unspecified, HighOrderLeft, HighOrderRight }

/// <summary>Encoding of a standard decimal floating-point usage (FLOAT-DECIMAL clause, §11.9.9, and the USAGE
/// clause's own encoding-phrase, §13.18.60.2).</summary>
public enum FloatEncoding { Unspecified, BinaryEncoding, DecimalEncoding }

/// <summary>The token → enum mapping for the two float FORMAT phrases of ISO §13.18.60.2 ("where encoding-phrase
/// is" / "where endianness-phrase is"). ONE definition for THREE citing clauses — the OPTIONS FLOAT-BINARY clause
/// (§11.9.8), the OPTIONS FLOAT-DECIMAL clause (§11.9.9) and the USAGE clause's per-item phrases (§13.18.60.2).
/// The grammar already shares one <c>encodingPhrase</c>/<c>endiannessPhrase</c> rule pair across all three; this
/// is its binder-side twin, so a fourth citing site inherits the mapping instead of copying it (kb/Work PB174 —
/// before this the mapping lived privately in <c>OptionsBinder</c> and the USAGE arm would have been copy #2).</summary>
internal static class FloatFormatPhrase
{
    /// <summary>§13.18.60.4 GR19a/b: HIGH-ORDER-LEFT is big-endian, HIGH-ORDER-RIGHT is little-endian.</summary>
    internal static FloatEndianness Endianness(Core.EndiannessPhraseContext e) =>
        e.HIGH_ORDER_LEFT() is not null ? FloatEndianness.HighOrderLeft : FloatEndianness.HighOrderRight;

    /// <summary>§13.18.60.4 GR20a/b: BINARY-ENCODING / DECIMAL-ENCODING select the ISO/IEC 60559:2020 3.5 / 3.5.2
    /// encoding of a standard decimal floating-point item.</summary>
    internal static FloatEncoding Encoding(Core.EncodingPhraseContext e) =>
        e.BINARY_ENCODING() is not null ? FloatEncoding.BinaryEncoding : FloatEncoding.DecimalEncoding;
}

/// <summary>The INITIALIZE clause (§11.9.10): which sections' background is set, and to what fill character.</summary>
/// <param name="Sections">The sections to initialize (ALL ⇒ all three, §11.9.10.4 GR1).</param>
/// <param name="Fill">The fill kind.</param>
/// <param name="FillLiteral">The literal text when <paramref name="Fill"/> is <see cref="OptionsFill.Literal"/>.</param>
/// <param name="LiteralFillChar">
/// The BYTE of literal-1 (§11.9.10.4 GR5 c), decoded and validated ONCE at bind time; null for every other
/// fill kind. Bind time is where this belongs because §11.9.10.3 SR1 — "Literal-1 shall specify a one-byte
/// hexadecimal-alphanumeric literal" — is a SYNTAX rule, and this is its diagnostic site.
/// <para>⛔ ONLY the literal arm is resolved here. GR5's four FIGURATIVE arms are NOT, because HIGH-VALUES and
/// LOW-VALUES are not compile-time constants: §8.3.3.6.4 GR6 makes the high-value format "the character … that
/// has the highest ordinal position in the program collating sequence", so the character depends on
/// OBJECT-COMPUTER's PROGRAM COLLATING SEQUENCE. The compiler already has exactly one definition of that fact —
/// <c>FigurativeConstants.FillChar</c> — and <c>InitialStateBackground</c> resolves GR5 a/b/d/e through it.
/// Writing a second map here is how the landed ALLOCATE arm came to fill with U+FFFF while every other
/// HIGH-VALUE in the compiler was U+00FF (kb/Work PB152 — measured, and the reason this parameter is the
/// literal alone).</para>
/// </param>
public sealed record OptionsInitialize(OptionsSections Sections, OptionsFill Fill, string? FillLiteral,
                                       char? LiteralFillChar);

/// <summary>The sections an INITIALIZE OPTIONS clause targets (a flags set; ALL ⇒ all three).</summary>
[Flags]
public enum OptionsSections { None = 0, LocalStorage = 1, Screen = 2, WorkingStorage = 4, All = LocalStorage | Screen | WorkingStorage }

/// <summary>The fill character of an INITIALIZE OPTIONS clause (§11.9.10.4 r5).</summary>
public enum OptionsFill { BinaryZeroes, HighValues, LowValues, Spaces, Literal }
