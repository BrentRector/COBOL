// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// The shared spine the decomposed backend emitters/renderers cooperate over (COBOLNET_DESIGN §17 §2.1): the
/// output <see cref="Writer"/> and the bound DATA DIVISION model (<see cref="Data"/>), plus the derived
/// SPECIAL-NAMES render config. IMMUTABLE since P7 Step 3 (renamed from <c>EmissionContext</c>, ending the
/// legacy-tree name collision): the four mutable receiver fields (<c>TargetScale</c>/<c>TargetReal</c>/
/// <c>TargetRounding</c>/<c>InSizeErrorContext</c>) and their manual set/reset discipline are DELETED — the
/// receiver a numeric render is computed for now travels BY PARAMETER (<see cref="ReceiverContext"/>), killing
/// the H1 staleness class by construction.
/// </summary>
internal sealed class EmitContext(CodeWriter writer, DataBinder data, NameAllocator names, Lazy<string> whenCompiledStamp)
{
    /// <summary>The C# output writer.</summary>
    public CodeWriter Writer { get; } = writer;

    /// <summary>The COMPILATION-scoped WHEN-COMPILED stamp (§15.99.3 r2) — captured ONCE per
    /// <see cref="ProgramEmitter"/> (one compilation of one run unit), so every unit of the run unit — the
    /// containing compilation unit and its contained source units alike — bakes the SAME constant (r2's second
    /// sentence), while the next compilation in the same process gets a FRESH capture (kb/Work PB120: a
    /// process-static Lazy handed every subsequent compilation the FIRST one's timestamp). Read LAZILY (kb/Work
    /// PB985): the clock is consulted only by a compilation that renders WHEN-COMPILED, so only that compilation's
    /// output — and its recorded inputs — depend on when it ran.</summary>
    public string WhenCompiledStamp => whenCompiledStamp.Value;

    /// <summary>The bound DATA DIVISION model.</summary>
    public DataBinder Data { get; } = data;

    /// <summary>⛔ THE LOCAL-STORAGE ROOTS OF THE ACTIVATION BEING INITIALIZED, when it is not the unit's own — an OO
    /// METHOD's (kb/Work PB1215). <see cref="Data"/> is the CLASS half's data model, whose <c>LocalStorageRoots</c>
    /// never list a method's LOCAL-STORAGE (it binds into the method's own <c>Binding.LocalRoots</c>), so the §11.9.10.4
    /// GR2 section selection of OPTIONS INITIALIZE could never select one. <c>OoEmitter.EmitMethod</c> sets this
    /// around the method's LOCAL-STORAGE initializers and restores it; null everywhere else.</summary>
    public IReadOnlyList<DataItem>? ActivationLocalRoots { get; set; }

    /// <summary>The RUN-UNIT-scoped unique-name allocator (P7 Step 9a) — the SAME instance rides every per-unit
    /// context of one generated module, so minted temporaries never collide across units.</summary>
    public NameAllocator Names { get; } = names;

    /// <summary>⛔ ONE EVALUATION, N STORES — the emitter half of every "… is stored in each data item referenced
    /// by identifier-N <b>in the order specified</b>" rule (kb/Work PB394). ISO §14.9.39.4 GR12 / GR14 / GR16 /
    /// GR18 each state it for a SET format: the address is "identified" once and then stored into every receiver,
    /// with only the RECEIVER's item identification repeated ("done immediately before the value of that data
    /// item is changed"). Declaring the sender's value in a local is what makes the emitted code say that;
    /// pasting the sender's expression text into N stores says the opposite, and a receiver that aliases the
    /// sender then poisons every store after it.
    /// <para>Returns the expression unchanged when there is at most one receiver — the text is emitted once
    /// anyway, so the local would be pure noise. The arithmetic family's <c>ArithmeticEmitter.Snapshot</c> is
    /// the same rule for a §14.7.7 GR4 intermediate, and <c>Binding.Procedure.SendingValueTemp</c> is the same
    /// rule for §14.9.25.4 GR1's intermediate result item, which needs a COBOL description rather than a C#
    /// local because a MOVE's receivers each CONVERT from it.</para></summary>
    /// <param name="expr">The sender's C# expression. It must be a well-typed initializer (never the bare
    /// <c>null</c> literal — a <c>TO NULL</c> sender is a constant and needs no local).</param>
    /// <param name="receivers">How many receiving operands will read it.</param>
    /// <param name="stem">The generated local's name stem, so the emitted C# reads as the statement it came from.</param>
    public string SendOnce(string expr, int receivers, string stem)
    {
        if (receivers <= 1) return expr;
        string tmp = $"__{stem}{Names.NextStoreTmp()}";
        Writer.Line($"var {tmp} = {expr};   // ONE evaluation of the sending operand (ISO §14.9.39.4 GR16)");
        return tmp;
    }

    /// <summary>⛔ THE COMPILATION'S DISPLAY OVER-PUNCH CONVENTION (kb/Work PB803, owner decision 2026-09-09;
    /// Annex A.1 items 177/178, ISO §13.18.52.4 GR4 / GR5 b) — the CLI's <c>--sign-encoding</c>, read from the ONE
    /// per-compilation context the driver set it on. EVERY emitted <c>NumProfile</c>
    /// (<c>PicInfo.ProfileInitializer</c>) and every emitted signed class condition
    /// (<c>ConditionRenderer.RenderClass</c>) states it, which is what makes the convention a property of the
    /// COMPILED PROGRAM rather than of the process: two programs of one run unit compiled under different options
    /// each read storage by their own convention, exactly as two vendors' programs do.</summary>
    public SignEncoding SignEncoding => Data.Edition.SignEncoding;

    /// <summary>The trailing collation argument for collated comparison renders — <c>", __COLLATE"</c> (the program's
    /// ONE <c>CobolCollation</c> carrier — a literal-phrase table or a LOCALE sequence, kb/Work PB101) when a
    /// PROGRAM COLLATING SEQUENCE is active (ISO §12.3.6 GR11 — relation and condition-name comparisons), else empty
    /// (the native two-argument <c>CobolString.Compare</c> overload).</summary>
    public string CollateArg => Data.Collating is null ? "" : ", __COLLATE";

    /// <summary>The NATIONAL twin of <see cref="CollateArg"/> — <c>", __COLLATE_NAT"</c> when a NON-native
    /// NATIONAL program collating sequence is active (ISO §12.3.6 GR11 / §8.8.4.2.9 — an <c>ALPHABET … FOR
    /// NATIONAL</c> literal phrase; the identity sequences NATIVE/UCS-4 stay null, D-N3), else empty. National
    /// comparisons NEVER take the alphanumeric <c>__COLLATE</c> table (its 256-entry domain would alias national
    /// characters through <c>&amp; 0xFF</c>).</summary>
    public string NatCollateArg => Data.NationalCollating is null ? "" : ", __COLLATE_NAT";

    /// <summary>The trailing collation argument a relation between operands of categories
    /// <paramref name="leftCategory"/> and <paramref name="rightCategory"/> takes — <see cref="CollateArg"/>,
    /// <see cref="NatCollateArg"/> or nothing — chosen through the ONE comparison-class rule
    /// (<c>CollatingSelection.ForComparison</c>): §8.8.4.2.7 collates an alphanumeric comparison, §8.8.4.2.9 a
    /// national one under the national sequence, and §8.8.4.2.8 / §8.8.4.2.4 collate NEITHER a boolean nor a
    /// numeric comparison — a boolean operand under the alphanumeric weight table would let an alphabet that
    /// reorders '0' and '1' invert a boolean test.
    /// <para>⛔ BOTH operands, always. A comparison's rule is not a property of one side: §8.8.4.2.5 makes a
    /// numeric-DISPLAY operand compared with an alphanumeric one an ALPHANUMERIC comparison, so asking only the
    /// numeric side returns "no sequence" and silently drops the PROGRAM COLLATING SEQUENCE (kb/Work PB741 — NIST
    /// NC215A `IF NINE-DU-9V0-1 &lt; SPACE`). Pass each item's OPERAND picture's (<c>DataItem.OperandPic</c>)
    /// category, so a bit / national GROUP is classified as the elementary operand §13.18.29.4 GR1b/GR2b makes
    /// it; a figurative constant has no category of its own and takes its context's (§8.3.3.6.4 GR1).</para></summary>
    public string CollateArgFor(PicCategory? leftCategory, PicCategory? rightCategory) =>
        CollatingSelection.ForComparison(leftCategory, rightCategory) switch
        {
            CollatingClass.National => NatCollateArg,
            CollatingClass.Alphanumeric => CollateArg,
            _ => "",
        };

    /// <summary>The editing-config suffix for a generated <c>CobolEdit</c> call over <paramref name="pic"/>'s
    /// mask (named arguments, composing after any <c>blankWhenZero:</c>): the mask's currency STRING when it is
    /// not the single character <c>$</c> (ISO §12.3.7.4 GR13 — the mask itself is canonical, its symbol already
    /// <c>$</c>; <c>PictureAnalyzer</c> recorded the string on <see cref="PicInfo.CurrencyString"/>, so a unit with
    /// SEVERAL currency signs edits each item with ITS string — kb/Work PB60 / AR-15.68.3-3) and DECIMAL-POINT IS
    /// COMMA when set (GR14). Empty under the default config, so the generated code of an ordinary program is
    /// unchanged. The ONE producer of these arguments — used by the orchestrator's MOVE/arithmetic edited stores,
    /// ACCEPT/STRING's edited receivers and the renderer's DeEdit, over BOTH numeric-edited forms (fixed-point
    /// <c>Format</c>/<c>TryFormat</c>/<c>DeEdit</c> and floating-point <c>FormatFloatStore</c>/<c>TryFormatFloat</c>/
    /// <c>DeEditFloat</c>).
    /// <para>⛔ It also carries the item's PICTURE EDITING rules (<see cref="RuntimeApi.EditsArg"/>). They were a
    /// SEPARATE suffix every caller had to remember to append, and the two floating-point arms were exactly the
    /// callers that did not — harmless only while COBOLNET1658 refused every EDITING phrase on that form; once the
    /// IS form is admitted (§13.18.40.3 SR12 bars only the FOR form's extended symbols; kb/Work PB866) a dropped
    /// suffix renders the mask LETTER. One producer, so the next edited-store site cannot drop it.</para></summary>
    public string EditCfg(PicInfo? pic) =>
        // A format-2 (LOCALE) item takes NEITHER argument: the currency string is the LOCALE's (§13.18.40.5 r9)
        // and DECIMAL-POINT IS COMMA has no effect on locale editing (§12.3.7.4 GR14) — and CobolLocaleEdit's
        // signature has no such parameters, so leaking either is a generated-code CS1739 (PB64 T6).
        pic?.LocaleEdit is not null ? ""
        : (pic?.CurrencyString is { } cur
            ? $", currencyString: {SymbolDisplay.FormatLiteral(cur, quote: true)}" : "")
        + (pic?.DecimalPointIsComma == true ? ", commaMode: true" : "")
        + RuntimeApi.EditsArg(pic?.EditingRules);

    // FigFill lives in FigurativeConstants since P7 Step 4 (the ONE figurative service).
}

/// <summary>
/// A rendered numeric expression: a C# <c>long</c>-valued expression holding the UNSCALED value, plus its fractional
/// <see cref="Scale"/> (the implied decimal position). The pair lets the backend align scales (ISO §8.8.1).
/// </summary>
/// <param name="Dec">True when <see cref="Expr"/> is a STANDARD-DECIMAL intermediate (<c>CobolDec</c>-typed,
/// §8.8.1.5) — <see cref="Scale"/> is then meaningless (the SDIDI carries its own exponent).</param>
/// <param name="Real">True when <see cref="Expr"/> is a C#-<c>double</c>-typed FLOATING-POINT intermediate (D16 —
/// any expression with a float operand evaluates in IEEE binary64); <see cref="Scale"/> is 0 and unused. <c>Real</c>
/// and <see cref="Dec"/> are mutually exclusive.</param>
/// <param name="U">True when <see cref="Expr"/> is a <c>UInt128</c>-typed UNSIGNED-WIDE intermediate (kb/Work
/// R10): a 16-byte unsigned COMP-5 item's field read, or a folded literal beyond <see cref="Int128.MaxValue"/>
/// (the HIGHEST-ALGEBRAIC container bound, §15.43.4 r2). The VALUE paths (store, display, relation) keep the
/// full [0, 2^128) range via the runtime's <c>UInt128</c> overloads; the ARITHMETIC paths funnel through
/// <c>CobolNum.Widen</c> (loud beyond the documented Int128 intermediate — CONFORMANCE.md §4.2.16), never a
/// silent wrap. Mutually exclusive with <see cref="Dec"/> and <see cref="Real"/>.</param>
/// <param name="Approximate">Only with <see cref="Real"/>: the binary64 is an implementor-defined APPROXIMATION the
/// ENGINE produced — a floating-math function's returned value (§15.4.1: "the characteristics and representation of
/// the returned value are defined by the implementor"), a non-integer native power (§8.8.1.3: under native arithmetic the method of evaluation is the implementor's) — and not the content
/// of a floating-point data item or a floating-point carrier literal. The distinction is the lane's whole
/// question (kb/Work PB1641): an operand "described with usage float-long" brings its expression into IEEE binary64
/// (§14.9.2.4 GR4's exemption for ADD, §14.9.44.4 GR4's for SUBTRACT, numeric design D16 for the rest), but a returned value is described with no usage at all, so
/// beside fixed-point operands nothing licenses binary64 (for ADD / SUBTRACT: "enough places shall be carried so as
/// not to lose significant digits") and <c>NumericRenderer.CombineCore</c> lifts it to the SDIDI through the
/// shortest-round-trip conversion (the implementor-defined float→SDIDI conversion of §8.8.1.5.1) instead of
/// dragging <c>13.2</c> into binary64 (where it is 13.199999999999999 and a truncating store gives 17.1 for
/// <c>13.2 + FUNCTION SQRT(16)</c>).</param>
/// <param name="Digits">An UPPER BOUND on the decimal digits of the UNSCALED value of an exact (non-<see cref="Dec"/>,
/// non-<see cref="Real"/>) operand, or 0 when it is not known (kb/Work PB1143 / PB621). The bound is what lets the
/// renderer decide at COMPILE time whether an operation on scaled <see cref="Int128"/> values can leave the
/// carrier — a product of an N-digit and an M-digit unscaled value is below 10^(N+M) — and fall to the SDIDI,
/// whose exponent is carried at run time, exactly where it can and never otherwise. Producers that know it set it
/// (a field read: the PICTURE's digit count; a literal: its digit count; the exact additive, multiplicative and
/// aligned results derived from those); every other producer leaves it 0 and keeps the carrier's checked
/// behaviour.</param>
/// <param name="Wide">True when <see cref="Expr"/> is a <c>CobolWide</c>-typed EXACT 256-bit intermediate of the NATIVE
/// lane (kb/Work PB1900): the unscaled value at the compile-time <see cref="Scale"/>, with <see cref="Digits"/> its
/// (always known, at most <c>CobolWide.MaxDigits</c>) magnitude bound. ⛔ IT NEVER LEAVES <c>NumericRenderer</c>: the
/// renderer's public entries settle it — into the receiver's scale and mode at a final transfer, into the SDIDI
/// otherwise (or the consumer opts in with <c>keepWide</c> and settles it itself) — so it is deliberately NOT a
/// <see cref="NumXCarrier"/>, and <see cref="Carrier"/> throws on it. Mutually exclusive with <see cref="Dec"/>,
/// <see cref="Real"/> and <see cref="U"/>.</param>
internal readonly record struct NumX(string Expr, int Scale, bool Dec = false, bool Real = false, bool U = false,
    bool Approximate = false, int Digits = 0, bool Wide = false)
{
    /// <summary>WHICH carrier <see cref="Expr"/> is typed as — the one question every total-over-carriers dispatch
    /// (<c>IntrinsicRenderer.IntegerIntake</c>, the landings, the stores) asks, answered once from the mutually
    /// exclusive flags. <see cref="NumXCarrier"/> is the enumeration a drift test walks to prove such a dispatch
    /// accepts every carrier the renderer can produce.</summary>
    public NumXCarrier Carrier => Wide ? throw new InvalidOperationException(
            "an exact wide intermediate (NumX.Wide) is settled inside NumericRenderer and is not a carrier a consumer dispatches on (kb/Work PB1900)")
        : U ? NumXCarrier.UnsignedWide : Dec ? NumXCarrier.Sdidi : Real ? NumXCarrier.Binary64 : NumXCarrier.Scaled;
}

/// <summary>The carriers a <see cref="NumX"/> expression can be typed as (see <see cref="NumX.Carrier"/>): the exact
/// scaled <see cref="Int128"/>, the unsigned-wide <c>UInt128</c> lane, the <c>CobolDec</c> SDIDI, and binary64.</summary>
internal enum NumXCarrier { Scaled, UnsignedWide, Sdidi, Binary64 }

/// <summary>Small text utilities shared by every backend emitter: loud-failure guards, literal escaping, and the
/// numeric-literal → unscaled-<c>long</c> conversions.</summary>
internal static class EmitText
{
    /// <summary>A C# statement that fails loud at run time for an unsupported construct (COBOLNET_DESIGN §1.4).</summary>
    public static string LoudStmt(string feature) => $"NotImplemented.Run({CsLiteral(feature)});";

    /// <summary>A C# expression (typed <paramref name="csType"/>) that fails loud at run time when reached.</summary>
    public static string LoudValue(string csType, string feature) =>
        $"NotImplemented.Value<{csType}>({CsLiteral(feature)})";

    /// <summary>Render a .NET string as a safely-escaped C# string literal.</summary>
    public static string CsLiteral(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    /// <summary>Render a char as a C# char literal, ALWAYS in the <c>'\uXXXX'</c> escape form. One form for every
    /// character means the NUL, the space and U+FFFF — the three the §11.9.10.4 GR5 fill map produces — need no
    /// per-character casing, and no emitted literal can ever contain a raw control character or a quote.</summary>
    public static string CsCharLiteral(char c) => $"'\\u{(int)c:X4}'";

    /// <summary>The C# expression evaluating to file <paramref name="f"/>'s runtime connector key — the ONE way a
    /// connector is addressed at OPEN/READ/WRITE/CLOSE/registration/STATUS/USE-selection (feedback_one_mechanism_per_job).
    /// For a program, factory, or EXTERNAL file it is the qualified-name literal; for a per-object instance file
    /// (M2-OO-1i) it is the object's minted-key field <c>this.__fkey_X</c> (ISO §9.1.4 — one connector per object
    /// instance). Until an instance file sets <see cref="FileModel.InstanceKeyField"/> this is byte-identical to
    /// <c>CsLiteral(f.CobolName)</c>, so routing every connector-key site through this helper is behaviour-neutral
    /// for all existing files (the refactor is proven by the full battery, not a new golden).</summary>
    public static string FileKeyExpr(FileModel f) =>
        f.InstanceKeyField is { } fld ? $"this.{fld}" : CsLiteral(f.CobolName);

    // FigurativeFill lives in FigurativeConstants.Fill(kind, null) since P7 Step 4.

    /// <summary>The character value of a figurative <c>ALL literal-1</c> in a WIDTH-SPECIFIED context (a VALUE clause /
    /// a fixed-length receiver / a compared-with operand; ISO §8.3.3.6.4 GR2): the literal is repeated character by
    /// character until its length is ≥ <paramref name="width"/>, then truncated from the right to <paramref
    /// name="width"/> (or 1, whichever is greater). An empty literal yields spaces. (A length-UNSPECIFIED context —
    /// DISPLAY / STOP / STRING — uses the literal once per GR3c, NOT this.)
    /// <para>⛔ THE RULE ITSELF LIVES IN THE RUNTIME (<c>CobolString.FigToWidth</c>, kb/Work PB297) — a relation
    /// condition sizes the same figurative at RUNTIME (the associated operand may be a reference-modified or
    /// function-result item whose character-position count is not a compile-time property), and the constant fold
    /// must not be a second copy of GR2 that can drift from it. The call goes through
    /// <see cref="RuntimeApi.FigToWidthFold"/> rather than naming the runtime type here, because the P7 Step 4b
    /// ratchet makes <c>RuntimeApi</c> the ONE CodeGen file that names the runtime.</para></summary>
    public static string RepeatToWidth(string literal, int width) => RuntimeApi.FigToWidthFold(literal, width);

    // The ALL literal-1 form (ISO §8.3.3.6.2 Format 6 / §8.3.3.6.4 GR9) is no longer forwarded from here: every
    // CodeGen reader of a VALUE / level-88 operand now asks FigurativeConstants.Classify, which parts Formats 1-5
    // from Format 6 in ONE place and delegates the Format-6 half to CobolLiteral.AllLiteralRaw (kb/Work PB461).
    // A forward with no callers is a second name for a rule, and this rule had five of those.

    /// <summary>⛔ THE ONE exact unscaled decomposition of a numeric literal, in EITHER notation: the signed
    /// unscaled digit string and the fraction-digit SCALE, so that the literal's value is exactly
    /// <c>digits × 10^−scale</c>. A FIXED-POINT literal is its digits at its decimal-point position (ISO
    /// §8.3.3.3.2 rule 4 — "The value of a fixed-point numeric literal is the algebraic quantity represented by
    /// the characters in the fixed-point numeric literal"); a FLOATING-POINT literal decomposes to the SAME
    /// KIND OF PAIR, because ISO §8.3.3.3.3 rule 5 makes its value "the algebraic product of the value of its
    /// significand and the quantity derived by raising ten to the power of the exponent" — an EXACT scaled
    /// integer, never an approximation.
    /// <para>⛔ NEVER A TEXT EXPANSION. The pair comes from <c>NumericLiteral.TryParseExact</c>'s (significand,
    /// power of ten). Expanding the value to digit text would build a string as long as the literal's decade
    /// bound allows, and that bound is the SDIDI intermediate exponent range (owned solely by
    /// <c>ArithmeticModes.IntermediateExponentRange</c>, which COBOLNET1661 enforces on the literal at bind) —
    /// thousands of characters for one argument. The SCALE simply goes NEGATIVE instead — <c>1.0E+300</c> is 1
    /// at scale −300 — which <c>CobolNum.Rescale</c> already carries (it is the same negative scale a PICTURE-P
    /// trailing-scaled item uses).</para>
    /// <para>The pair is normalized toward a NON-NEGATIVE scale while the unscaled magnitude still fits
    /// <see cref="Int128"/>, so <c>1.5E+3</c> decomposes exactly as <c>1500</c> does. That is the visible form of
    /// owner decision D-B's invariant — a literal's NOTATION never changes the VALUE it contributes — now true
    /// on the argument lane as well.</para>
    /// Returns false for text that is not a canonical numeric literal; the binder has already diagnosed it
    /// (COBOLNET1661 for an out-of-range exponent, the §8.3.3.3.3 SR2/SR3 form checks for the rest).</summary>
    internal static bool TryUnscaledParts(string text, out string digits, out int scale)
    {
        string t = text.Trim().TrimStart('+');
        if (t.IndexOf('E') < 0 && t.IndexOf('e') < 0)
        {
            // The FIXED-POINT form, textually — byte-identical to what this helper has always produced (leading
            // zeros and the sign are preserved; IntLiteralX does the magnitude test).
            int dot = t.IndexOf('.');
            digits = dot < 0 ? t : t.Remove(dot, 1);
            scale = dot < 0 ? 0 : t.Length - dot - 1;
            return true;
        }
        digits = ""; scale = 0;
        if (!CobolNet.Common.NumericLiteral.TryParseExact(t, out Int128 sig, out int exp10)) return false;
        // Normalize toward scale ≥ 0 while the magnitude still fits the Int128 carrier (the guard keeps the
        // multiply from overflowing; a literal's significand is at most 36 digits per §8.3.3.3.3 rule 2).
        Int128 limit = Int128.MaxValue / 10;
        while (exp10 > 0 && sig >= -limit && sig <= limit) { sig *= 10; exp10--; }
        digits = sig.ToString(System.Globalization.CultureInfo.InvariantCulture);
        scale = -exp10;
        return true;
    }

    /// <summary>Render a numeric literal as a scaled integer: its digits as the unscaled value, its fractional
    /// digit count as the scale (e.g. <c>"3.5"</c> → <c>(35L, 1)</c>, <c>"-12"</c> → <c>(-12L, 0)</c>). A literal
    /// wider than 18 digits (legal to 31, ISO §8.3.3.3.2 — the 2002+ wide tier) emits an <c>Int128.Parse</c>
    /// since C# has no Int128 literal form.
    /// <para>⛔ A FLOATING-POINT literal is a scaled integer TOO (<see cref="TryUnscaledParts"/>, ISO §8.3.3.3.3
    /// rule 5) and this helper's contract IS that pair. It used to return the literal's own source text as a
    /// binary64 <c>Real</c> NumX, which is not a scaled integer at all, and the two consumers that render an
    /// OPERAND through this helper — the CALL argument lane and the INVOKE argument lane — then handed a C#
    /// <c>double</c> to a <c>ManagedPointer&lt;long|Int128&gt;.Cell(…)</c>: the generated C# did not compile, so
    /// conforming source was rejected by a RAW ROSLYN CS1503 with no COBOL diagnostic at all (kb/Work PB263).</para>
    /// <para>⛔ THIS IS NOT THE ARITHMETIC LANE. <c>NumericRenderer.LiteralNum</c> takes a canonical
    /// floating-point literal onto the SDIDI (<c>Dec</c>) lane BEFORE it reaches here (owner decision D-B), so
    /// the §8.8.1.3 intermediate-lane determination is untouched by this rendering; only a literal that is not
    /// canonical at all — which the binder has already diagnosed — still falls through to the <c>Real</c>
    /// form.</para></summary>
    public static NumX UnscaledLit(string text)
    {
        if (!TryUnscaledParts(text, out string digits, out int scale))
            return new NumX(text.Trim().TrimStart('+'), 0, Real: true);
        var (lit, u) = IntLiteralX(digits);
        // The literal's own digit count is the exact bound (leading zeros carry no magnitude): the sign is not a digit.
        int significant = digits.TrimStart('-', '+').TrimStart('0').Length;
        return new NumX(lit, scale, U: u, Digits: Math.Clamp(significant, 1, 39));   // a synthesized HIGHEST-ALGEBRAIC literal is 39 digits
    }

    /// <summary>The C# literal for an unscaled integer digit string: <c>…L</c> while it fits <see cref="long"/>
    /// (≤18 digits), else <c>Int128.Parse("…")</c>.</summary>
    public static string IntLiteral(string signedDigits) => IntLiteralX(signedDigits).Text;

    /// <summary>The carrier-aware form of <see cref="IntLiteral"/>. A SOURCE literal never exceeds 31 digits
    /// (ISO §8.3.3.3.2), but a COMPILER-SYNTHESIZED literal can: the HIGHEST-ALGEBRAIC fold of a 16-byte
    /// unsigned COMP-5 container is 2^128−1 (39 digits, §15.43.4 r2 — kb/Work R10 F73; the old unconditional
    /// <c>Int128.Parse</c> threw <c>OverflowException</c> at run time). A positive magnitude beyond
    /// <see cref="Int128.MaxValue"/> renders as <c>UInt128.Parse</c> and is flagged unsigned-wide (<c>U</c>) so
    /// the renderer routes it down the unsigned lane. (A NEGATIVE value never needs it — the most negative fold,
    /// −2^127, is exactly <see cref="Int128.MinValue"/>.)</summary>
    internal static (string Text, bool U) IntLiteralX(string signedDigits)
    {
        var c = IntLiteralCore(signedDigits);
        return (c.Text, c.U);
    }

    /// <summary>⛔ THE ONE carrier decision for an unscaled integer literal — the rendered C# text, the
    /// unsigned-wide flag, and the C# CARRIER TYPE that text is typed as, decided TOGETHER so they cannot
    /// disagree. The CALL / INVOKE argument lanes need the third fact: a <c>CobolArg</c>'s cell is a
    /// <c>ManagedPointer&lt;T&gt;</c> and <c>T</c> has to be the type the rendered expression actually has.
    /// <para>kb/Work PB263: the cell type used to be re-derived at the call site from a SOURCE-TEXT digit
    /// count, which counted a floating-point literal's EXPONENT digits as significand digits — <c>1.5E+3</c>
    /// "has 3 digits", so the site asked for a <c>long</c> cell and handed it a <c>double</c> expression. Two
    /// derivations of one fact, and they disagreed. Deriving the carrier HERE, from the same magnitude test that
    /// chooses the rendering, is what makes the next carrier tier automatic instead of a second edit.</para></summary>
    internal static (string Text, bool U, string Carrier) IntLiteralCore(string signedDigits)
    {
        string mag = signedDigits.TrimStart('-').TrimStart('0');
        if (mag.Length <= 18) return ($"{signedDigits}L", false, "long");
        if (signedDigits.StartsWith('-') || mag.Length < 39
            || System.Numerics.BigInteger.Parse(mag) <= (System.Numerics.BigInteger)Int128.MaxValue)
            return ($"Int128.Parse(\"{signedDigits}\")", false, "Int128");
        return ($"UInt128.Parse(\"{signedDigits}\")", true, "UInt128");
    }

    /// <summary>Render a numeric literal as a C# <c>long</c> holding its UNSCALED value at <paramref name="scale"/>
    /// fractional digits — e.g. <c>"3.5"</c> at scale 2 → <c>350L</c>, <c>"12"</c> at scale 0 → <c>12L</c>. A NEGATIVE
    /// scale (a PICTURE-P trailing-scaled item, e.g. <c>99P(4) VALUE 990000</c> at scale −4 → <c>99L</c>) drops the
    /// low <c>|scale|</c> integer digits (the stored value is a multiple of 10^|scale|).</summary>
    public static string UnscaledAtScale(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+');
        bool neg = t.StartsWith('-');
        if (neg) t = t[1..];
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        string digits;
        if (scale >= 0)
        {
            if (fracPart.Length < scale) fracPart = fracPart.PadRight(scale, '0');
            else if (fracPart.Length > scale) fracPart = fracPart[..scale];
            digits = intPart + fracPart;
        }
        else
        {
            // Negative scale: the implied point sits |scale| positions right of the stored digits, so the low
            // |scale| integer digits are dropped (they are the assumed-zero P positions).
            string all = intPart + fracPart;
            int drop = -scale;
            digits = all.Length > drop ? all[..^drop] : "0";
        }
        digits = digits.TrimStart('0');
        return IntLiteral($"{(neg ? "-" : "")}{(digits.Length == 0 ? "0" : digits)}");
    }
}
