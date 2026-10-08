// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound numeric expression / operand to a scale-tracked native-integer C# expression (<see cref="NumX"/>).
/// COBOL fixed-point arithmetic operates on the algebraic value regardless of representation (ISO §8.8.1): operands
/// are unscaled longs carrying their scale, the renderer aligns scales for ±, adds them for ×, and computes a
/// quotient at the RECEIVER's working scale (<see cref="ReceiverContext.Scale"/> — P7 Step 3: the receiver
/// travels by parameter into every public entry, never mutable context state) for ÷. The receiver's store
/// (truncation / capacity) is applied later by <c>CobolNum.Store</c>.
/// </summary>
internal sealed class NumericRenderer(EmitContext ctx, EcState ecState) : IBoundExprVisitor<NumX>, IBoundOperandVisitor<NumX>
{
    /// <summary>THE checked-kernel decision (kb/Work PB91): a render under an ON SIZE ERROR / EC-SIZE-checked
    /// ARITHMETIC statement (<c>_rcv.InSizeError</c>, threaded by ArithmeticEmitter) — and, since PB91, any
    /// RECEIVER-LESS render (a condition, an argument, a subscript, …) inside a statement that has EC-SIZE-*
    /// checking enabled (<see cref="EcState.SizeChecking"/>): §14.7.5 no-phrase rule 3 makes such an
    /// intermediate overflow EC-SIZE-OVERFLOW, and §14.6.13.1.3 its disposition fatal.</summary>
    private bool Checked => CheckedFor(_rcv);

    /// <summary><see cref="Checked"/> for an explicit receiver context — the final transfer of an exact wide value
    /// (<see cref="Settle"/>) is checked by the receiver it lands in, which a per-receiver settle names itself.</summary>
    private bool CheckedFor(in ReceiverContext rcv) => rcv.InSizeError || (rcv.Receiverless && ecState.SizeChecking);

    /// <summary>The intrinsic-function render dispatch (ISO §15; IntrinsicRenderer.cs) — created lazily because
    /// the two renderers are mutually recursive (an intrinsic renders its numeric arguments through THIS).</summary>
    internal IntrinsicRenderer Intrinsics => _intrinsics ??= new IntrinsicRenderer(ctx, this);
    private IntrinsicRenderer? _intrinsics;

    /// <summary>The declared PROGRAM COLLATING SEQUENCE tables (§12.3.7) — exposed so a bare figurative in a value
    /// context materializes the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), not the native pin. Null when no
    /// non-native sequence is declared, in which case <c>FigurativeConstants.Fill</c> returns the native pin
    /// (the byte-stable common case). The MOVE and relation paths already thread the same tables.</summary>
    internal AlphabetDef? Collating => ctx.Data.Collating;
    internal NationalAlphabetDef? NationalCollating => ctx.Data.NationalCollating;

    /// <summary>The EMITTING unit's own DECIMAL-POINT IS COMMA mode (§12.3.7.4 GR14 a; a contained program inherits it,
    /// §12.3.4 GR1) — for what the unit ITSELF evaluates and spells, never for an item's PICTURE (that is
    /// <c>PicInfo.DecimalPointIsComma</c>, kb/Work PB2554, and <c>DecimalPointModeDriftTests</c> holds the line): the
    /// literal-form text image of a numeric function's returned value (kb/Work PB2507).</summary>
    internal bool UnitDecimalPointIsComma => ctx.Data.DecimalPointIsComma;

    // Render/AsNum dispatch through the generated exhaustive visitors (PHASE-07 Step 6d): every BoundExpr / BoundOperand
    // leaf has a Visit below, so a new leaf is a COMPILE error here — the former loud `_ =>` defaults are gone.

    // ── The receiver context (P7 Step 3). Every PUBLIC entry REQUIRES the caller's ReceiverContext and
    // stores it here; the private Visit recursion reads the field. The generated single-arg visitors cannot
    // thread a parameter (Accept<T> has no TArg), and one render tree serves exactly ONE receiver — the context
    // is constant across the recursion — so a freshly-assigned-at-every-entry field is equivalent to a
    // parameter: the H1 hazard (a render inheriting the PREVIOUS statement's receiver) is impossible because no
    // entry exists that does not take the context. (DESIGN-codegen-backend §2.5, updated for the landed
    // generated visitor.) ──
    private ReceiverContext _rcv = ReceiverContext.None;

    /// <summary>The §14.6.13.2 EXEMPT-CONTEXT of the sending reads in the sub-tree being rendered
    /// (<see cref="SendingRef"/> — the clause's exemption table as a value, kb/Work PB230). The two sending-read
    /// chokepoints in <see cref="FieldNumCore"/> ask it per rule: a float read emits the raw
    /// <c>(double)(Read(p))</c> instead of <c>CobolFloat.Sending(…)</c> when the context is exempt from rule 3
    /// (class/sign condition, same-usage MOVE, VALIDATE), and a FIXED-POINT windowed read emits the raw
    /// <c>ParseImage</c> instead of <c>ParseImageSending</c> when it is exempt from rule 2 (class condition,
    /// VALIDATE — a sign condition and a same-usage MOVE are NOT exempt there).
    /// <para>Re-entrant save/restore (like <see cref="_rcv"/>): propagates through the direct <c>.Accept</c>
    /// recursion of the exempt operand, and resets to <see cref="SendingRef.Normal"/> at a fresh
    /// <see cref="Render"/>/<see cref="AsNum"/> entry (an intrinsic argument read is a non-exempt reference —
    /// the narrow reading of the exemptions).</para></summary>
    private SendingRef _sending = SendingRef.Normal;

    /// <summary>True while rendering a division node whose quotient is transferred DIRECTLY to a single resultant
    /// identifier — the "final transfer" of §14.7.7 rule 3 NOTE 1. Set by the emit sites that own that transfer
    /// (a single-receiver COMPUTE RHS, a DIVIDE top-level division); an outermost division computes at the
    /// receiver's scale + ROUNDED mode in one exact step. A division NESTED inside a larger expression is NOT the
    /// final transfer (ROUNDED applies only to that final transfer, §14.7 NOTE 1; PROHIBITED tests only the
    /// resultant identifier, §14.7.4.3 GR7), so it always computes at the D2 guard scale with TRUNCATION — never
    /// inheriting the receiver's mode. Re-entrant save/restore like <see cref="_rcv"/>; <see cref="Visit(BoundBinary)"/>
    /// clears it for a node's children (they are never the final transfer) and restores it for the node's own
    /// combine. The OLD <c>ds == _rcv.Scale</c> heuristic in <see cref="Divide"/> was a wrong proxy for this — it
    /// let a nested integer-operand division inherit PROHIBITED (spurious size error) and let a multi-receiver root
    /// division miss PROHIBITED (CA5).</summary>
    private bool _outermost = false;

    /// <summary>The receiver of the render currently in progress — read by the mutually-recursive
    /// <see cref="IntrinsicRenderer"/> (an intrinsic's working scale/mode derive from the SAME receiver).</summary>
    internal ReceiverContext Receiver => _rcv;

    /// <summary>Whether the value now being rendered IS the value transferred to a single resultant identifier —
    /// <see cref="_outermost"/>, read by the mutually-recursive <see cref="IntrinsicRenderer"/> (kb/Work PB647:
    /// a float returned value's quantization is the final transfer's rounding exactly when this is true, and
    /// <see cref="ReceiverContext.FloatLanding"/> is the one place that decides what follows from it).</summary>
    internal bool Outermost => _outermost;

    /// <summary>Render a bound numeric expression as a scaled long, computed FOR <paramref name="rcv"/>.
    /// Save/restore makes every public entry RE-ENTRANT (P7 Step 12): the instance string channel renders an
    /// intrinsic's numeric arguments under <see cref="ReceiverContext.None"/> MID-render, and the outer render
    /// must resume under its own receiver — the H1 staleness class stays closed by construction.</summary>
    public NumX Render(BoundExpr e, in ReceiverContext rcv, SendingRef sending = SendingRef.Normal, bool outermost = false,
                       bool keepWide = false)
    {
        var saved = _rcv;
        var savedEx = _sending;
        var savedOut = _outermost;
        _rcv = rcv;
        _sending = sending;
        _outermost = outermost;
        try
        {
            NumX x = e.Accept(this);
            return keepWide ? x : Settle(x, rcv, outermost);
        }
        finally { _rcv = saved; _sending = savedEx; _outermost = savedOut; }
    }

    /// <summary>⛔ SETTLE AN EXACT WIDE INTERMEDIATE (<see cref="NumX.Wide"/>, kb/Work PB1900) — THE ONE PLACE it leaves
    /// the renderer. Every public entry (<see cref="Render"/>, <see cref="AsNum"/>, <see cref="Fold"/>,
    /// <see cref="Combine"/>) calls it on its result, so no consumer ever dispatches on a carrier it does not know:
    /// <list type="bullet">
    ///   <item><b>The final transfer to ONE fixed-point resultant</b> (<paramref name="outermost"/>, a receiver that is
    ///         neither receiver-less nor floating-point) — the exact value is rounded ONCE, at that resultant's scale and
    ///         with its mode (<c>CobolWide.ToUnscaled</c>, the kernel <c>CobolDec.MulAtScale</c> shares): §14.7.7 rule 3 NOTE 1 gives
    ///         ROUNDED to the final transfer only, and the receiver's own rounding must see every digit.</item>
    ///   <item><b>Anything else</b> (a receiver-less render, a float receiver, an intermediate with no transfer) — it is
    ///         lowered to the SDIDI by ROUND-TO-ODD (<see cref="LowerWide"/>), exactly the intermediate a nested product
    ///         was before the wide form existed, so whatever consumes it next rounds it in any mode as the exact value.</item>
    /// </list>
    /// A consumer that CAN use the exact form (a several-receiver COMPUTE's one initial evaluation, a relation) passes
    /// <c>keepWide</c> and settles it itself.</summary>
    public NumX Settle(NumX x, in ReceiverContext rcv, bool outermost)
    {
        if (!x.Wide) return x;
        return outermost && rcv.RoundsAtItsScale
            ? new NumX(RuntimeApi.WideToUnscaled(x.Expr, x.Scale, rcv.Scale, rcv.Rounding, CheckedFor(rcv)), rcv.Scale)
            : LowerWide(x);
    }

    /// <summary>An exact wide intermediate as the SDIDI (round-to-odd, 34 digits); every other value passes through.</summary>
    internal static NumX LowerWide(NumX x) =>
        x.Wide ? new NumX(RuntimeApi.WideToDec(x.Expr, x.Scale), 0, Dec: true) : x;

    /// <summary>The wide-lane spelling of an exact operand: a wide value as itself, an <c>Int128</c>-lane value lifted.</summary>
    private static string WideOperand(NumX x) => x.Wide ? x.Expr : RuntimeApi.WideFrom(x.Expr);

    /// <summary>The wide-lane spelling of an exact operand aligned UP to <paramref name="scale"/> (a sum's common scale).</summary>
    private static string WideAligned(NumX x, int scale) => RuntimeApi.WideUp(WideOperand(x), scale - x.Scale);

    /// <summary>The exact comparison (−1/0/+1 as a C# expression) of two operands of which at least one is an exact wide
    /// intermediate, or <c>null</c> when the pair has no exact form to compare on (a float, an SDIDI, an unknown bound: the
    /// caller then lowers both and compares as it always did). A relation (§8.8.4.2.4 — "a comparison is made with respect
    /// to the algebraic value of the operands") has a defined answer for every legal pair, which only the exact form gives
    /// for two nested products that differ below the SDIDI's 34th digit. No digit bound is needed: lifting an
    /// <c>Int128</c> value is exact and the comparison never aligns past the 256-bit range (<c>CobolWide.Compare</c>).</summary>
    public static string? CompareWide(NumX l, NumX r)
    {
        static bool Exact(NumX x) => !x.Real && !x.Dec && !x.U;   // the scaled Int128 lane or the wide form
        return (l.Wide || r.Wide) && Exact(l) && Exact(r)
            ? RuntimeApi.WideCompare(WideOperand(l), l.Scale, WideOperand(r), r.Scale)
            : null;
    }

    /// <summary>Render a bound expression that the FORMAT admits only as an OPERAND — PERFORM VARYING's FROM / BY
    /// (ISO §14.9.28.2: identifier, index-name or literal; the binder types them BoundExpr for convenience). A bare
    /// floating-point literal keeps its EXACT value (kb/Work PB99: `FROM 1.0E+3` set a 20-decimal item to
    /// 999.999…9161, the binary64 1E+23 product) — which is now what <see cref="LiteralNum"/> gives it in EVERY
    /// position, so this is plain <see cref="Render"/> under <see cref="ReceiverContext.None"/>.
    /// <para>⛔ THE OPERAND-vs-EXPRESSION SPLIT IS GONE (owner decision D-B, kb/Work PB156/PB195). It existed only
    /// because a literal rendered differently in the two positions; one rendering means one path, and the named
    /// method survives solely to document WHY the format admits no expression here.</para></summary>
    public NumX RenderOperandLike(BoundExpr e) => Render(e, ReceiverContext.None);

    /// <summary>Render a bound operand as a scaled native-integer value, computed FOR <paramref name="rcv"/>
    /// (re-entrant — see <see cref="Render"/>). <paramref name="sending"/> names the §14.6.13.2 exempt context,
    /// which suppresses whichever rule's checked read that context exempts.</summary>
    public NumX AsNum(BoundOperand op, in ReceiverContext rcv, SendingRef sending = SendingRef.Normal, bool outermost = false,
                      bool keepWide = false)
    {
        var saved = _rcv;
        var savedEx = _sending;
        var savedOut = _outermost;
        _rcv = rcv;
        _sending = sending;
        _outermost = outermost;
        try
        {
            NumX x = op.Accept(this);
            return keepWide ? x : Settle(x, rcv, outermost);
        }
        finally { _rcv = saved; _sending = savedEx; _outermost = savedOut; }
    }

    // ── IBoundExprVisitor<NumX> ──────────────────────────────────────────────────────────────────────────────
    public NumX Visit(BoundNumLiteral n) =>
        n.Carrier is { } carrier && _rcv.Real && !StandardDecimal ? CarrierLiteral(n.Text, carrier) : LiteralNum(n.Text);

    /// <summary>A literal that DENOTES a binary float carrier value (<see cref="BoundNumLiteral.Carrier"/> — kb/Work
    /// PB1196), rendered as that exact value on the binary64 lane where the expression evaluates there: under native
    /// arithmetic into an all-floating-point resultant set (<see cref="ReceiverContext.Real"/>, D16). The text rounds
    /// to the carrier value (it is that value's image), and the bits are materialized through the ONE opaque
    /// from-bits call. Elsewhere — a relation, a fixed-point resultant, a standard mode — the literal keeps its
    /// decimal lane, so an exact decimal comparison against the extreme is unchanged.</summary>
    private static NumX CarrierLiteral(string text, BinaryFloatCarrier carrier)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const System.Globalization.NumberStyles Float = System.Globalization.NumberStyles.Float;
        return carrier == BinaryFloatCarrier.Binary32
            ? new NumX($"(double){RuntimeApi.FloatFromBits(BitConverter.SingleToUInt32Bits(float.Parse(text, Float, inv)), single: true)}", 0, Real: true)
            : new NumX(RuntimeApi.FloatFromBits(BitConverter.DoubleToUInt64Bits(double.Parse(text, Float, inv)), single: false), 0, Real: true);
    }

    /// <summary>⛔ THE ONE numeric-literal → <see cref="NumX"/> rendering, for EVERY position and EVERY arithmetic
    /// mode (owner decision D-B, 2026-08-30; kb/Work PB156 + PB195). A FLOATING-POINT literal is its EXACT
    /// ISO §8.3.3.3.3 rule-5 value — "the algebraic product of the value of its significand and the quantity derived
    /// by raising ten to the power of the exponent" — carried on the Dec (SDIDI) lane; a fixed-point literal is the
    /// scaled-integer rendering of <see cref="EmitText.UnscaledLit"/>, unchanged.
    /// <para>WHY THE NATIVE ARM IS EXACT TOO. §14.9.2.4 GR4 (and its §14.9.44.4 GR4 SUBTRACT twin) says: "When
    /// native arithmetic is in effect and none of the operands is described with usage binary-char, binary-short,
    /// binary-long, binary-double, float-short, float-long, or float-extended, enough places shall be carried so as
    /// not to lose any significant digits during execution." A LITERAL is not "described with usage" anything, and
    /// §14.7.7 rule 2 proves the drafters spell a float literal out as its OWN bullet when they mean to include it —
    /// GR4 does not. So `ADD 1.0E+0 TO W` over a PIC 9(20) holding 12345678901234567890 owed …891 and a binary64
    /// operand delivered …168. Numeric design D16's §8.8.1.3 latitude is therefore NARROWED to what it always
    /// described: an expression touching a float ITEM or a float RECEIVER still evaluates in IEEE binary64 (the
    /// <c>Real</c> lane keeps precedence in <see cref="CombineCore"/> — untouched by D-B).</para>
    /// <para>⛔ THE INVARIANT THIS BUYS, STATED EXACTLY (it was written as "a literal's NOTATION never changes the
    /// arithmetic — only its VALUE does", and that sentence is FALSE — kb/Work PB274): the VALUE a literal
    /// contributes is its exact §8.3.3.3.3 rule-5 value, in every position and every arithmetic mode, and no
    /// operand ever arrives rounded to binary64. `ADD 1.0E+0` computes what `ADD 1.0` computes and
    /// `2 ** 0.5E+0` what `2 ** 0.5` computes, because those are the same VALUES.</para>
    /// <para>What the notation still selects, under NATIVE arithmetic, is the INTERMEDIATE LANE, and that is a
    /// published §8.8.1.3 determination rather than an invariant (CONFORMANCE.md A.1 item 82): a float literal
    /// operand routes the statement onto the decimal128 lane (34 significant digits) where a fixed-point literal
    /// keeps the Int128 one (~38), so `A * B + 0.0E+0` and `A * B + 0.0` differ for 18-digit A and B — and
    /// INTERMEDIATE ROUNDING (§11.9.11) reaches the first and not the second. See <see cref="CombineCore"/>.</para>
    /// <para>Residual, documented (CONFORMANCE.md §7 / A.1 item 82): §8.3.3.3.3 rule 2 admits a 36-digit
    /// significand and the SDIDI carrier holds 34, so a 35th/36th significand digit rounds at
    /// <c>CobolDec.FromParsed</c>.</para>
    /// <para>Every consumer of the Dec lane already exists — the PB84 landing/store funnel; <see cref="Real"/>
    /// converts it where a binary64 body wants a double; <see cref="DecOperand"/> where an SDIDI body does.</para></summary>
    private NumX LiteralNum(string text) =>
        CobolNet.Common.NumericLiteral.IsFloatingPointForm(text)
            && CobolNet.Common.NumericLiteral.TryParseExact(text, out var sig, out int exp10)
        ? new NumX(RuntimeApi.DecFromParsedLiteral(sig, exp10, IntermediateMode), 0, Dec: true)
        : EmitText.UnscaledLit(text);
    public NumX Visit(BoundNumRef n) => FieldNum(n.Place);
    public NumX Visit(BoundIndexRef n) => new(n.IndexField, 0);   // an index IS its 1-based occurrence number (§3.5)
    // The LINAGE-COUNTER register (ISO §8.4.3.14 GR1): an unsigned INTEGER read from the file connector —
    // runtime-sourced (only the I-O control system modifies it, §13.18.34 GR7b), scale 0.
    public NumX Visit(BoundLinageCounterRef n) => new($"CobolFile.LinageCounter({EmitText.FileKeyExpr(n.File)})", 0);
    // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15.4 GR1): unsigned integers read from the report's engine instance,
    // scale 0. This ONE case serves both the relation-condition and MOVE-source paths (both route through the
    // renderer). The member name comes from RuntimeApi, which the RECEIVING side (ReportPageCounterPlace, §8.4.3.15.3
    // SR1) also reads — one spelling for the two directions (kb/Work PB429).
    public NumX Visit(BoundReportCounterRef n) =>
        new(RuntimeApi.ReportCounterRead(n.Report.CsIndex, n.Depth, n.IsPage), 0);
    // A SUM counter read (ISO §13.18.54.4 GR4 — the counter is its printable entry's source item): an unscaled
    // integer at the counter's PICTURE-derived scale (GR1), engine-sourced.
    // A report VARYING counter read (ISO §13.18.64.4 GR3/GR4): the compose-local integer counter, scale 0.
    public NumX Visit(BoundReportVaryingRef n) => new(n.CsName, 0);
    // An OCCURS DEPENDING table's current extent (ISO §13.18.38 GR8; the §15.50.4 r4b / §15.14.4 r2b channel,
    // kb/Work PB61): CobolTable.OdoExtent over data-name-1's current count — scale 0, EC-BOUND-ODO inside.
    public NumX Visit(BoundOdoExtent n)
    {
        string extent = RuntimeApi.TableOdoExtent(PlaceRenderer.CountRead(n.Depending),
            n.MinOccurs, n.MaxOccurs, n.FixedWidth, n.ElemWidth);
        // A BASED entry not associated with actual data answers GR8b's maximum (§15.50.4 r4a / §15.14.4 r2a —
        // kb/Work PB80); the null test comes FIRST, before data-name-1 (possibly inside the entry) is read.
        return new(n.BasedAddress is { } addr
            ? $"({RuntimeApi.PtrIsNull(addr)} ? (long){n.FixedWidth + (long)n.MaxOccurs * n.ElemWidth} : {extent})"
            : extent, 0);
    }
    public NumX Visit(BoundBinary n)
    {
        // A binary node's CHILDREN are never the final transfer (a sub-expression operand), so they render with
        // _outermost cleared; the node's OWN combine (which may be the final-transfer division) sees the value
        // that reached this node. §14.7.7 rule 3 NOTE 1 — ROUNDED/PROHIBITED bind to the final transfer only.
        bool outer = _outermost;
        _outermost = false;
        var l = n.Left.Accept(this);
        var r = n.Right.Accept(this);
        _outermost = outer;
        return CombineCore(l, n.Op.ToString(), r);
    }
    // ⛔ EVERY recursion clears _outermost, not only BoundBinary's (kb/Work PB129 — GR-8.8.1.2-1): a division
    // under a unary minus, an exponent base, or a computed-operand wrapper is NOT the final transfer, so it
    // must compute at the D2 guard scale with truncation — the receiver's ROUNDED mode binds to the final
    // transfer only (§14.7.4.3 GR3–GR10; §8.8.1.2 r1 "treated as a single operand"). The leak reinstated the
    // exact pre-CA5 defect for `(1 / 3) ** 0` under PROHIBITED, and made `(A / B) ** 2` differ from
    // `1 * (A / B) ** 2` — multiplying by one changed the value.
    public NumX Visit(BoundNegate n)
    {
        bool outer = _outermost;
        _outermost = false;
        var v = n.Operand.Accept(this);
        _outermost = outer;
        return Negate(v);
    }
    public NumX Visit(BoundPower n)
    {
        bool outer = _outermost;
        _outermost = false;
        var b = IntegerValuedLiteral(n.Base) ?? n.Base.Accept(this);
        var e = IntegerValuedLiteral(n.Exp) ?? n.Exp.Accept(this);
        _outermost = outer;
        return Power(b, e, IntegerLiteralValue(n.Exp));
    }

    /// <summary>A numeric LITERAL whose algebraic VALUE is an integer the native carrier can hold, as a scale-0
    /// operand — whatever notation it was written in. <c>null</c> for anything else.
    /// <para>⛔ THE EXACT-INTEGER <c>**</c> ARM IS KEYED ON THE VALUE, NEVER ON THE SPELLING (kb/Work PB272).
    /// <see cref="Power"/>'s exact arm asks <c>e.Scale == 0 &amp;&amp; !e.Dec &amp;&amp; !e.Real</c>, all three of
    /// which are properties of how the operand was WRITTEN: <c>10 ** 30</c> took the exact Int128 power and
    /// <c>10 ** 30.0</c> (scale 1) and <c>10 ** 3.0E+1</c> (a Dec since D-B, a Real before it) both dropped to the
    /// §8.8.1.2 binary64 approximation — the same expression, three notations, and the answers differed from the
    /// 17th significant digit up. §8.3.3.3.3 rule 5 makes a floating-point literal's value "the algebraic product
    /// of the value of its significand and the quantity derived by raising ten to the power of the exponent" and
    /// nothing more (cite.py-verified), and §8.3.3.3.2's fixed-point form is likewise a value — so trailing zeros
    /// past the point are notation, not precision, and 30, 30.0 and 3.0E+1 are ONE operand.</para>
    /// <para>Not applied under a standard arithmetic mode: there every operand is an SDIDI by §8.8.1.5.2 r1 and
    /// this arm does not exist. Not applied past <see cref="Int128"/>: the exact arm's carrier cannot hold it, and
    /// the Dec lane it would otherwise reach is the right home.</para></summary>
    private NumX? IntegerValuedLiteral(BoundExpr x) =>
        !StandardDecimal && IntegerLiteralValue(x) is { } v
            ? new NumX(EmitText.IntLiteral(v.ToString(System.Globalization.CultureInfo.InvariantCulture)), 0)
            : null;

    /// <summary>The integer VALUE of a numeric literal operand (<see cref="IntegerValuedLiteral"/>'s test), or
    /// <c>null</c> when it is not a literal, is not an integer, or does not fit <see cref="Int128"/>. Read from the
    /// BOUND TREE, never from a rendered expression text — an operand does not render as its bare digits (a first
    /// cut of the exponent test did <c>long.TryParse(e.Expr)</c> and silently stopped matching). A
    /// <c>BoundNegate</c> wrapper is deliberately NOT unwrapped: the exponent <c>-2</c> is an expression, not a
    /// literal, and the exponent arms read its sign at run time.</summary>
    private static Int128? IntegerLiteralValue(BoundExpr x)
    {
        // `BoundNumLiteral` is the ONE literal shape an arithmetic expression's operands take — the operand-level
        // `BoundNumericLiteral` never reaches a `BoundPower`.
        string? text = x is BoundNumLiteral l ? l.Text : null;
        if (text is null || !CobolNet.Common.NumericLiteral.TryParseExact(text, out Int128 sig, out int exp10))
            return null;
        while (exp10 < 0 && sig % 10 == 0) { sig /= 10; exp10++; }   // trailing zeros are notation, not scale
        if (exp10 < 0) return null;                                   // a genuinely non-integer value
        for (int i = 0; i < exp10; i++)
        {
            if (Int128.Abs(sig) > Int128.MaxValue / 10) return null;   // past the exact arm's own carrier
            sig *= 10;
        }
        return sig;
    }

    public NumX Visit(BoundIntrinsicCall n) => Intrinsics.RenderNum(n);   // FUNCTION call (ISO §15)
    public NumX Visit(BoundExprError n) => new(EmitText.LoudValue("long", n.Feature), 0);

    /// <summary>A per-evaluation operand window (ISO §14.9.28.4 GR12; kb/Work PB437) reaching a rendering site
    /// that does NOT own its window. ⛔ LOUD, NEVER SILENTLY WRONG (§1.4): the node exists to make its function
    /// activations run once per SETTING or AUGMENTING operation, and the ONLY way to do that is to emit them as
    /// statements at that operation — which is what <c>ControlFlowEmitter.RenderPerEvaluation</c>, the node's one
    /// consumer, does. Rendering the inner expression here instead would silently drop the activations and read
    /// an uninitialized result temporary; a site that legitimately wants this window must call the consumer.
    /// </summary>
    public NumX Visit(BoundUdfEvaluatedExpr n) => new(EmitText.LoudValue("long",
        "a PERFORM VARYING per-evaluation operand window rendered outside its setting/augmenting operation "
        + "(ISO §14.9.28.4 GR12) — the activations must be emitted at the operation"), 0);

    // ── IBoundOperandVisitor<NumX> ───────────────────────────────────────────────────────────────────────────
    public NumX Visit(BoundNumericLiteral n) => LiteralNum(n.Text);   // the SAME rendering as the expression arm (D-B)
    public NumX Visit(BoundFieldOperand n) => FieldNum(n.Place);
    public NumX Visit(BoundComputedOperand n)
    {
        // An operand-wrapped expression is a nested operand, never the final transfer (PB129 — see the
        // BoundNegate note above).
        bool outer = _outermost;
        _outermost = false;
        var v = n.Expr.Accept(this);
        _outermost = outer;
        return v;
    }
    public NumX Visit(BoundFigurative n) => n.Kind == 'Z'
        ? EmitText.UnscaledLit("0")   // ZERO in a numeric context
        : new NumX(EmitText.LoudValue("long", $"figurative '{n.Kind}' in a numeric context"), 0);
    // An alphanumeric literal in a numeric context is an UNSIGNED integer (§14.9.25.3 Table 16 — the
    // alphanumeric→numeric move; NC105A's MOVE "12345" TO MOVE1), decoded exactly like an alphanumeric field. A
    // NATIONAL literal decodes the same way (§14.9.25.4 GR6d3 — national→numeric as an unsigned integer under the
    // Latin-1 identity); class BOOLEAN is not a numeric operand (§8.8.1) — loud.
    public NumX Visit(BoundStringLiteral n) => n.Category == PicCategory.Boolean
        ? new NumX(EmitText.LoudValue("long", "boolean literal in a numeric context (ISO §8.8.1 — class boolean is not a numeric operand)"), 0)
        : AlnumNum(EmitText.CsLiteral(n.Value), _sending);
    public NumX Visit(BoundOperandError n) => new(EmitText.LoudValue("long", n.Feature), 0);
    // An address-identifier (kb/Work PB1021) is class pointer: the binder admits it only as a relation operand, which
    // ConditionRenderer's pointer arms render, and an INVOKE argument, which OoEmitter renders — never a
    // character or numeric value. Reaching this is a binder hole, so it is LOUD, never a guessed value.
    public NumX Visit(BoundAddressOperand n) => new(EmitText.LoudValue("long", "address-identifier as a numeric operand"), 0);
    // THE CURRENT RECORD in a NUMERIC context (kb/Work PB339): an alphanumeric operand decoded as an unsigned
    // integer, exactly as the alphanumeric field arm below decodes one (§14.9.25.3 Table 16). ⛔ REACHABLE, not a
    // backstop: a FORMAT 3 `RECORD CONTAINS m TO n` file (whose §14.9.30.4 GR4 b) move carries no group-move
    // designation) with an ELEMENTARY 01 and `READ F INTO a-numeric-item` classifies MoveKind.Convert and lands
    // here — a loud arm would abort legal source at run time.
    public NumX Visit(BoundCurrentRecord n) => AlnumNum(OperandText.CurrentRecordImage(n), _sending);
    // BoundAllLiteral (ALL "x" in a numeric context) and BoundBoolOperand (a class-boolean operand) are not numeric
    // operands — the former loud `_ =>` default handled them; now explicit (byte-identical loud value; §8.8.1).
    public NumX Visit(BoundPredefinedNull n) =>
        throw new InvalidOperationException("the predefined NULL reached a numeric rendering: only a §8.4.3.10.3 SR1 context "
        + "binds it (ExpressionBinder.NullAdmittingOperand), and every such context renders it itself (kb/Work PB1427)");
    public NumX Visit(BoundAllLiteral n) => new(EmitText.LoudValue("long", $"bound operand '{nameof(BoundAllLiteral)}'"), 0);
    public NumX Visit(BoundBoolOperand n) => new(EmitText.LoudValue("long", $"bound operand '{nameof(BoundBoolOperand)}'"), 0);

    /// <summary>The scaled value of a data item place (its unscaled <c>long</c> value + its scale). A float item is
    /// truncated to <c>long</c> for now (mixed float/fixed arithmetic is a later slice). A non-numeric place (a group
    /// or an alphanumeric item used in a numeric context) fails loud rather than crashing the compiler (§1.4).
    /// The instance entry adds ONLY the numeric-edited de-edit (it needs the SPECIAL-NAMES emission config);
    /// every other branch lives in the context-free <see cref="FieldNumCore"/> so the static string-channel
    /// intrinsic renderer reads fields through the SAME single implementation (singular-pattern rule).</summary>
    public NumX FieldNum(Place p) =>
        // A table(ALL) intrinsic argument (ISO §15.3; kb/Work PB62) renders as its ELEMENT — the index variable's
        // slot in the subscript — which is what its enumeration lambda wraps; the list is never a single value.
        p is TableAllPlace all ? FieldNum(all.Element) :
        p.DenotedItem is not null && !p.Item.StoreAsImage
            // A FLOATING-POINT numeric-edited sender de-edits to its EXACT value — a CobolDec on the Dec lane (D21/PB66:
            // a significand with its own power of ten; never a double); EC-DATA-INCOMPATIBLE for impossible content.
            && p.Item.Pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true, EditMask: { } fem }
        ? new NumX($"CobolEdit.DeEditFloat({PlaceRenderer.Read(p)}, {EmitText.CsLiteral(fem)}{ctx.EditCfg(p.Item.Pic)}{(p.Item.BlankWhenZero ? ", blankWhenZero: true" : "")})", 0, Dec: true)
        : p.DenotedItem is not null && !p.Item.StoreAsImage
            // A format-2 (LOCALE) sender DE-EDITS through CobolLocaleEdit under the locale current NOW
            // (§13.18.40.5 r11; §14.6.13.2 r4 — impossible content is EC-DATA-INCOMPATIBLE); the scale is the
            // PICTURE's (PB64 T6). The second of the two dispatch sites PicInfo.LocaleEdit's doc names.
            && p.Item.Pic is { LocaleEdit: not null } lpic
        ? new NumX(RuntimeApi.LocaleDeEdit(lpic, PlaceRenderer.Read(p), p.Item.BlankWhenZero), lpic.Scale)
        : p.DenotedItem is not null && !p.Item.StoreAsImage
            // A numeric-edited sender DE-EDITS to its numeric value at the mask's scale (ISO §14.9.25.4 GR5 — the
            // COBOL-85 de-editing move; the runtime walks the image against the mask's digit positions).
            && p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } dem }
        ? new NumX($"CobolEdit.DeEdit({PlaceRenderer.Read(p)}, {EmitText.CsLiteral(dem)}{ctx.EditCfg(p.Item.Pic)}{(p.Item.BlankWhenZero ? ", blankWhenZero: true" : "")})",
            p.Item.Pic.ReceiverScale())
        : FieldNumCore(p, _sending);

    /// <summary>⛔ THE ONE RENDERING OF AN ALPHANUMERIC OR NATIONAL OPERAND READ IN A NUMERIC CONTEXT (ISO
    /// §14.9.25.4 GR6 d) 3 — such an operand "is treated as if it were an unsigned integer of category numeric",
    /// over its rightmost 31 character positions; §14.9.25.3 Table 16 makes the move valid). Five distinct
    /// operand shapes reach it — an alphanumeric/national LITERAL, a FIELD, a reference-modified result
    /// (§8.4.3.3.4 GR6), a GROUP item's image (§8.5.2.1) and the current RECORD area — and every one of them is
    /// the SAME rule, so they render through one call rather than five copies of the same interpolation.
    /// <para>The <paramref name="sending"/> context decides the CHECKED form (GR6 d) 1's EC-DATA-INCOMPATIBLE,
    /// kb/Work PB844) through <see cref="SendingRefRules.AlphanumericChecked"/> — writing that question once here
    /// is what keeps the next operand shape automatic. Both halves were absent before kb/Work PB426: the size
    /// rule (a 40-character sender wrapped the Int128 carrier) and the raise.</para></summary>
    private static NumX AlnumNum(string imageExpr, SendingRef sending) =>
        new(RuntimeApi.NumFromAlphanumeric(imageExpr, sending.AlphanumericChecked()), 0);

    /// <summary>⛔ THE ONE WINDOWED NUMERIC READER — the value of a numeric leaf stored as its CHARACTER IMAGE
    /// (<see cref="DataItem.StoreAsImage"/>: whole-group-aliased / Tier-B), decoded from <paramref name="image"/>
    /// through the leaf's own profile. Three lanes, chosen by the PICTURE, and every reader that needs a windowed
    /// leaf's VALUE — the numeric renderer's own place arm and the table SORT's key comparer (kb/Work PB186) —
    /// reaches them HERE, so a new lane lands once:
    /// <list type="bullet">
    /// <item>FLOAT — the window holds the item's IEEE interchange bytes and decodes through the distinctly-named
    ///   float lane, wrapped in CobolFloat.Sending (EC-DATA-NOT-FINITE, §14.6.13.2 item 3) unless exempt.</item>
    /// <item>UNSIGNED 16-BYTE BINARY (<see cref="PicInfo.IsUnsignedWideBinary"/>) — ParseImageU128 reinterprets
    ///   bit-identically into the UInt128 lane, which keeps the item's full [0, 2^128) container range (kb/Work
    ///   R10); decoded SIGNED, a value at or above 2^127 turns negative — the scout's silent wrong answer, and a
    ///   wrong ORDER in a SORT. There is deliberately no 8-byte twin: ParseImage already returns an unsigned item's
    ///   full width as a non-negative Int128 and every ulong fits Int128, so only the 16-byte range needs it.</item>
    /// <item>EVERY OTHER FORM — the signed Int128 lane, THE FIXED-POINT SENDING-READ CHOKEPOINT (kb/Work PB230): only
    ///   a WINDOWED leaf can hold content that fails its own numeric class condition (a native-carrier leaf can
    ///   only hold digits, which is exactly why ConditionRenderer folds `IS NUMERIC` on one to the constant true),
    ///   so this is where ISO §14.6.13.2 rule 2's EC-DATA-INCOMPATIBLE can arise: under checking
    ///   ParseImageSending raises for content that "would evaluate to false in a numeric class condition"; with
    ///   checking off, or in an exempt context, the decode stays the tolerant deterministic one the standard's
    ///   "undefined" permits.</item>
    /// </list></summary>
    internal static NumX WindowedNum(string image, DataItem item, PicInfo pic, SendingRef sending) => pic switch
    {
        { IsFloat: true } => new NumX(
            sending.FloatChecked(pic)
                ? RuntimeApi.FloatSending(RuntimeApi.NumParseImageFloat(image, item.ProfileName, binary32Carrier: false))
                : RuntimeApi.NumParseImageFloat(image, item.ProfileName, binary32Carrier: false),
            0, Real: true),
        { IsUnsignedWideBinary: true } =>
            new NumX(RuntimeApi.NumParseImageU128(image, item.ProfileName, sending.FixedPointChecked()), pic.Scale, U: true),
        _ => new NumX(RuntimeApi.NumParseImage(image, item.ProfileName, sending.FixedPointChecked()), pic.Scale),
    };

    /// <summary>The context-free numeric read of a place (every branch of <see cref="FieldNum"/> except the
    /// numeric-edited de-edit, which stays loud here — it requires the instance emission config).
    /// <paramref name="sending"/> names the §14.6.13.2 exempt context of this reference
    /// (<see cref="SendingRef"/>): rule 3's checked float read (<see cref="CobolNet.Runtime.CobolFloat.Sending(double)"/>)
    /// and rule 2's checked FIXED-POINT windowed read (<see cref="CobolNet.Runtime.CobolNum.ParseImageSending"/>)
    /// each consult their OWN exemption list off it, which is why this is not a bool — a sign condition and a
    /// same-usage MOVE are exempt from rule 3 and NOT from rule 2 (kb/Work PB230). The static string-channel
    /// intrinsic-arg reuse keeps <see cref="SendingRef.Normal"/> (a non-exempt reference).</summary>
    internal static NumX FieldNumCore(Place p, SendingRef sending = SendingRef.Normal) => p is TableAllPlace all ? FieldNumCore(all.Element, sending) : p is RefModPlace
        // A reference-modified result is ALPHANUMERIC (ISO §8.4.3.3.4 GR6) — in a numeric context it decodes as an
        // unsigned integer exactly like an alphanumeric field (§14.9.25.3 Table 16).
        ? AlnumNum(PlaceRenderer.Read(p), sending)
        : p.Item.Pic switch
    {
        // A GROUP operand in a remaining numeric context (an arithmetic operand, a subscript…) decodes its
        // alphanumeric IMAGE as an UNSIGNED integer (an alphanumeric group item "has class and category
        // alphanumeric", §8.5.2.1 — NOT the phantom §8.8.4.1.1, kb/Work PB182; the
        // deterministic digit decode §14.6.13.2 permits for incompatible content). NOTE: a MOVE never reaches
        // this branch — a group SENDER makes the move a GROUP move (§14.9.25.4 GR4: no conversion; classified
        // at EmitMove → EmitGroupToElementaryMove), never a numeric decode of the image (the pre-fix NC105A
        // MOVE MOVE43 TO MOVE3 mis-derivation). A mixed-usage (COMP-leaf) group is an ORDINARY image-capable
        // group now — its leaves' pinned bytes ARE the image (the Step D arm-1 dissolution).
        // ⛔ IsImageCapable, and correct ONLY because strict conformance now REJECTS a group arithmetic operand
        // (DA6 — §8.8.1.1, COBOLNET0844 in ExpressionBinder). Reaching here therefore means --permissive was
        // requested, and the leniency must be CONSISTENT: before, a group of PIC X leaves computed while a group of
        // PIC 9 leaves threw at run time, so the operand whose digits were unambiguous failed and the merely-textual
        // one succeeded. Migrating this arm while strict still ACCEPTED the construct would have extended acceptance
        // of illegal source instead of fixing anything — the two changes are correct only together. A
        // group with a pointer/object-class leaf (or a variable-length group) still has no image and stays
        // loud even under --permissive (kb/Work PB164 + R40 — an INDEX-leaf group images now).
        // ⛔ THROUGH THE ONE SENDING READER, never a self-spelled `.AsImage()` (kb/Work PB178, the sibling of
        // OperandText.AsStorageImage's defect and found in the same sweep). This site used to inline
        // `p is RedefViewPlace ? Read(p) : Read(p).AsImage()`, which had TWO independent holes:
        //   (a) NO OdoGroupPlace unwrap — `Read(OdoGroupPlace o) => Read(o.Inner)`, so an ODO wrapper over a
        //       Tier-B / BASED string-canonical window (what `ReferenceResolver`'s
        //       `WrapIfOdoGroup(new RedefViewPlace(...))` builds for a BASED record carrying an ODO table)
        //       rendered `<string window>.AsImage()` → backend CS1061. MEASURED, not deduced: strict
        //       conformance rejects a group arithmetic operand (COBOLNET0844 below), so the reach is
        //       `--permissive`, which is a SUPPORTED mode — `COMPUTE X = <BASED ODO group> + 1 --permissive`
        //       was the probe.
        //   (b) the MAXIMUM image where §13.18.38.4 GR8 wants the CURRENT-count part. A numeric decode of a
        //       group's image is a SENDING reference of that group, and GR8a/GR8b both give a sending operand
        //       the current extent — which its sibling `OperandText.FieldAsString` already got right. A
        //       wrong ANSWER, independent of (a)'s crash.
        // The capability guard now lives in the ONE reader too (it stages the same Tier-C loud), so the
        // former `when p.Item.IsImageCapable` arm and its hand-written twin below collapse into this one.
        // ⛔ …and through the ONE GROUP VALUE reader, never the image reader alone (kb/Work PB944's sibling
        // sweep): a NATIONAL group is Table 16's National row (§13.18.29.4 GR2b) and §14.9.25.4 GR6 d) 3 decodes
        // its CHARACTER positions, not the UTF-16BE bytes its image carries.
        null => AlnumNum(PlaceRenderer.SendingGroupValue(p, "numeric use of group item"), sending),
        // ⛔ EVERY WINDOWED (StoreAsImage) numeric leaf decodes through THE ONE windowed reader, WindowedNum above —
        // float, unsigned-wide and the signed Int128 lane alike (kb/Work PB186: the table SORT comparer used to spell
        // its own signed-only copy). It precedes the native float arm: with the order reversed, a Tier-B float view
        // rendered `(double)(<string window>)` (the Step D arm-1 dissolution — the design scout's pre-loaded CS0030).
        { } pic when p.Item.StoreAsImage => WindowedNum(PlaceRenderer.Read(p), p.Item, pic, sending),
        // A float leaf (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED, D16) enters the arithmetic pipeline as a native
        // IEEE double — NOT truncated to (long) at scale 0 (the pre-D16 stub that silently dropped the fraction). The
        // sending read is wrapped in CobolFloat.Sending (raises EC-DATA-NOT-FINITE for NaN/±Inf under checking, §14.6.13.2
        // item 3) UNLESS this is an exempt context (sign condition / same-usage MOVE — SendingRef.FloatChecked()
        // false = raw read; note rule 2's arm in WindowedNum reads its OWN, shorter exemption list off the same value).
        { IsFloat: true } => new NumX(
            sending.FloatChecked(p.Item.Pic!) ? RuntimeApi.FloatSending($"(double)({PlaceRenderer.Read(p)})") : $"(double)({PlaceRenderer.Read(p)})",
            0, Real: true),
        // (A COPY of a float item's content — not an arithmetic read — is FloatCarrierRead below: this arm and
        // WindowedNum's float lane widen to binary64 because arithmetic evaluates there (D16), and a widening
        // quiets a signaling NaN.)
        // An alphanumeric operand in a numeric context is an UNSIGNED integer (ISO §14.9.25.4 GR6) — never the raw
        // string read (which would emit uncompilable C#, the bind-success ⇒ compilable invariant). A NATIONAL
        // operand decodes identically (GR6d3 — its digit characters are the Latin-1 digits under D-N4);
        // class BOOLEAN is not a numeric operand (§8.8.1; Table 16 Boolean→Numeric = No) — loud.
        { Category: PicCategory.Alphanumeric or PicCategory.National } => AlnumNum(PlaceRenderer.Read(p), sending),
        { Category: PicCategory.Boolean } =>
            new NumX(EmitText.LoudValue("long", $"boolean operand '{p.Item.CobolName}' in a numeric context (ISO §8.8.1 — class boolean is not a numeric operand)"), 0),
        // The numeric-edited de-edit lives on the INSTANCE entry (it needs the SPECIAL-NAMES config); a static
        // caller (the string-channel intrinsic renderer) reaching one is a staged-out shape — loud (§1.4).
        { Category: PicCategory.NumericEdited } =>
            new NumX(EmitText.LoudValue("long", $"numeric-edited operand '{p.Item.CobolName}' in a context-free numeric read"), 0),
        // A 16-byte UNSIGNED BinaryCapacity item (UInt128 carrier — kb/Work R10): the read enters the renderer
        // on the unsigned-wide lane. Value paths (store/display/relation) keep the full [0, 2^128) range via the
        // runtime's UInt128 overloads; arithmetic funnels through CobolNum.Widen (loud beyond the Int128
        // intermediate), never a silent wrap.
        { IsUnsignedWideBinary: true } pic => new NumX(PlaceRenderer.Read(p), pic.Scale, U: true, Digits: pic.UnscaledDigitBound),
        // An 8-byte UNSIGNED BinaryCapacity item (ulong carrier): every ulong value fits the Int128 engine — the
        // read is lifted at THIS one site so downstream text (comparisons against long literals, raw-expr
        // alignment) never mixes ulong with long, which C# rejects as ambiguous.
        { IsUnsignedLongBinary: true } pic => new NumX($"(Int128)({PlaceRenderer.Read(p)})", pic.Scale, Digits: pic.UnscaledDigitBound),
        // Every exact item read states how many digits its unscaled value can have (PicInfo.UnscaledDigitBound), so
        // the carrier decisions downstream (a product, an aligned list) are made from a fact and not from hope.
        { } pic => new NumX(PlaceRenderer.Read(p), pic.Scale, Digits: pic.UnscaledDigitBound),
    };

    /// <summary>Left-fold a list of bound expressions with <c>+</c> (the addends of an ADD / minuends of a SUBTRACT).
    /// <para>⛔ SAVES AND RESTORES the ambient receiver, exactly as <see cref="Render"/> and <see cref="AsNum"/> do.
    /// It did not, and that was a real defect rather than a tidiness point: <c>_rcv</c> is a per-unit MUTABLE field,
    /// so an ADD left its receiver LATCHED on the renderer and the next receiver-less render — a numeric FUNCTION in
    /// a DISPLAY or a text MOVE — silently inherited it. `DISPLAY FUNCTION SQRT(2)` printed `1.414213562`, then
    /// `1.414213562373` after an unrelated `ADD 1 TO R`, because the intrinsic's working scale is
    /// <c>max(Receiver.Scale, 9)</c>. A public entry that mutates ambient state must restore it or the
    /// next caller reads someone else's context.</para></summary>
    public NumX Fold(IReadOnlyList<BoundExpr> xs, in ReceiverContext rcv)
    {
        var saved = _rcv; bool savedOut = _outermost;
        _rcv = rcv;
        _outermost = false;   // an ADD/SUBTRACT operand is never a final-transfer division (its result feeds the fold)
        try
        {
            if (xs.Count == 0) return new NumX("0L", 0);
            NumX acc = xs[0].Accept(this);
            for (int i = 1; i < xs.Count; i++) acc = CombineCore(acc, "+", xs[i].Accept(this));
            return Settle(acc, rcv, outermost: false);
        }
        finally { _rcv = saved; _outermost = savedOut; }
    }

    /// <summary>Combine two scaled values with a COBOL operator, tracking the result scale (ISO §8.8.1). EVERY
    /// operation runs in <see cref="Int128"/> — the carrier (COBOLNET_DESIGN §18 #4 / numeric design D1): a product
    /// of two 18-digit operands is 36 digits and an aligned sum 19+, both past the long range MID-computation even
    /// when the final receiver fits. The leading <c>(Int128)</c> cast forces wide arithmetic whatever the leaf
    /// types; storage stays narrow (the store path truncates/rounds once, at the receiver).</summary>
    public NumX Combine(NumX a, string op, NumX b, in ReceiverContext rcv, bool outermost = false)
    {
        // Saves/restores the ambient receiver for the same reason Fold does — see its remark.
        var saved = _rcv; bool savedOut = _outermost;
        _rcv = rcv;
        _outermost = outermost;
        try { return Settle(CombineCore(a, op, b), rcv, outermost); }
        finally { _rcv = saved; _outermost = savedOut; }
    }

    private NumX CombineCore(NumX a, string op, NumX b)
    {
        // The unsigned-wide funnel (kb/Work R10): an ARITHMETIC operand enters the engine's documented Int128
        // intermediate through CobolNum.Widen — a value beyond it raises the size-error condition (ON SIZE ERROR
        // catches it; without the phrase it is loud), never a silent two's-complement wrap. The VALUE paths
        // (store/display/relation) never come through here and keep the full [0, 2^128) range.
        a = DeU(a);
        b = DeU(b);
        // ⛔ AN APPROXIMATION THE ENGINE RETURNED IS NOT A FLOAT OPERAND (kb/Work PB1641). D16 evaluates an
        // expression in binary64 when an operand is a floating-point DATA ITEM, a float literal carrier or a float
        // RECEIVER — the operands §14.9.2.4 GR4 and §14.9.44.4 GR4 (ADD and SUBTRACT; the other statements' native
        // rule is the implementor's, §8.8.1.3) except from "enough places shall be carried so as not to lose
        // significant digits": those with usage binary-char, binary-short, binary-long, binary-double, float-short,
        // float-long or float-extended (§14.7.7 r2 lists them, a floating-point literal and an intrinsic function as
        // separate bullets). A function's returned value is described with no usage at all — §15.4.1 leaves "the
        // characteristics and representation of the returned value" to the implementor — and a non-integer native
        // power is an engine-computed approximation too (§8.8.1.3: under native arithmetic the method of
        // evaluation is the implementor's). Beside operands that are NOT floats nothing licenses carrying them in
        // binary64, and doing so loses a digit: `COMPUTE R = 13.2 + FUNCTION SQRT(16)` into PIC 99V9 stored 17.1
        // (13.199999999999999 + 4.0 truncated) where SQRT(16) alone is exactly 4 and the sum is 17.2. So the
        // approximation enters the arithmetic through its shortest-round-trip decimal (the §8.8.1.5.1
        // implementor-defined float→SDIDI conversion, defined here as shortest round-trip, which every standard mode
        // already applies to a float operand — the same VALUE the binary64 carries) and the operation runs on the
        // decimal lane; a float ITEM, a float literal carrier or a float receiver keeps the whole expression in
        // binary64 exactly as before (the approximation then stays a double beside it). GnuCOBOL, the precedent where
        // the ISO text is silent, evaluates every intrinsic result as a decimal.
        // Every producer of an Approximate Real is IntrinsicRenderer.RenderFloatNative / NUMVAL-F's receiver-less arm
        // and Power's non-integer arm; FloatItemLane is the ONE statement of "does a float operand own this lane".
        if (!StandardDecimal && !FloatItemLane(a, b))
        {
            a = LiftApproximation(a);
            b = LiftApproximation(b);
        }
        // STANDARD / STANDARD-DECIMAL arithmetic (§8.8.1.5): every operation of an arithmetic expression
        // evaluates in SDIDI form (decimal128 semantics), rounded per-op to 34 significant digits with the
        // INTERMEDIATE ROUNDING mode (§11.9.11) and range-checked at the decimal128 bounds (§8.8.1.5.2 r2);
        // the receiver's ROUNDED applies only at the final transfer (§14.7 NOTE 1). This branch runs BEFORE
        // the D16 float branch: under a standard mode a FLOATING-POINT operand converts into SDIDI form via
        // the §8.8.1.5.1 implementor-defined conversion (CobolDec.FromDouble — the shortest-round-trip decimal
        // identity of the IEEE value; see DecOperand) and the operation itself is the SDIDI one.
        // ⛔ AND A NATIVE OPERATION WITH AN SDIDI-CARRIED OPERAND EVALUATES ON THE SDIDI TOO (kb/Work PB69). The
        // Dec producers under NATIVE arithmetic are THREE, not one (kb/Work PB273 — this sentence named only the
        // first for two waves, and D-B made the third the common case):
        //   · an integer power (PowNativeIntDec — exact while it fits the carrier, the owner-decided double
        //     approximation past it, the reciprocal for a negative exponent);
        //   · a floating-point numeric-EDITED sender, lifted onto the Dec lane by FieldNum in every mode;
        //   · a FLOATING-POINT LITERAL operand (LiteralNum — since PB99 for an operand, and since owner decision
        //     D-B for every position), which is now much the most common of the three.
        // The reason is the same for all three and is why the branch tests the CARRIER: a Dec has no compile-time
        // scale, and landing it into Int128 at the operation's static scale truncated `2 ** -2 + 1` to 1 (the
        // additive arm) and did not compile at all for `*` and `/` (a raw Dec into an Int128 slot — CS1503). The
        // SDIDI carries the exact power, the approximation and the literal's exact value alike, so the operation
        // computes there and the result lands ONCE at the receiver (TryStore(CobolDec), checked). The float lane
        // keeps precedence when a float operand or a float receiver is present (native float arithmetic is IEEE,
        // D16).
        // ⚠ COST, AND IT IS A PUBLISHED DETERMINATION (§8.8.1.3 leaves "the method of evaluating an arithmetic
        // statement" to the implementor; CONFORMANCE.md A.1 item 82): this lane carries 34 significant digits
        // where CombineNative's Int128 carries ~38, so an expression whose exact intermediate needs 35–38 digits
        // rounds here and not there. That is observable — `A * B + 0.0E+0` differs from `A * B + 0.0` for
        // 18-digit A and B — so it is NOT covered by "a literal's notation never changes the arithmetic"; the
        // literal's VALUE is exact either way, and the LANE is the determination. It also means INTERMEDIATE
        // ROUNDING (§11.9.11) reaches a native statement carrying a float literal, PROHIBITED included.
        // MOD/REM keep exact integers exact through their own integer fast path.
        if (StandardDecimal || ((a.Dec || b.Dec) && !a.Real && !b.Real && !_rcv.Real))
            return DecBinary(op, a, b);
        // D16 (NATIVE arithmetic): any expression with ≥1 float operand evaluates ENTIRELY in IEEE binary64 (a
        // single-precision operand widens exactly) — native COBOL float arithmetic is IEEE binary, never decimal
        // (§8.8.1.3 implementor-defined; STANDARD-BINARY is obsolete, 2023 §8.8.1.4.1 NOTE). +,-,*,/ are native
        // double ops.
        if (a.Real || b.Real || _rcv.Real) return CombineReal(a, op, b);
        return CombineNative(a, op, b);
    }

    /// <summary>One operation on the SDIDI lane at the program's INTERMEDIATE ROUNDING mode — the operands lifted by
    /// <see cref="DecOperand"/> (a fixed-point value exactly, a float by its shortest round-trip decimal, an exact wide
    /// intermediate by round-to-odd). The ONE spelling of the four operators for every caller that takes an operation
    /// there: the standard-decimal engine, an SDIDI-carried native operand (kb/Work PB69) and an exact wide operand whose
    /// operation has no wide form (kb/Work PB1900).</summary>
    private NumX DecBinary(string op, NumX a, NumX b) => op switch
    {
        "+" => new NumX($"CobolDec.Add({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
        "-" => new NumX($"CobolDec.Sub({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
        "*" => new NumX($"CobolDec.Mul({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
        "/" => new NumX($"CobolDec.Div({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
        _ => a,
    };

    /// <summary>Does a FLOAT operand own this operation's lane — a floating-point data item or carrier literal
    /// (<c>Real</c> and not <see cref="NumX.Approximate"/>) or a floating-point receiver (kb/Work PB1641)? When none
    /// does, an <see cref="NumX.Approximate"/> operand is a returned value beside non-float operands and
    /// <see cref="LiftApproximation"/> takes it to the SDIDI.</summary>
    private bool FloatItemLane(NumX a, NumX b) =>
        _rcv.Real || (a.Real && !a.Approximate) || (b.Real && !b.Approximate);

    /// <summary>An engine-produced binary64 approximation as the SDIDI of its shortest round-trip decimal
    /// (<see cref="DecOperand"/>'s conversion — the VALUE is unchanged); every other operand passes through.</summary>
    private static NumX LiftApproximation(NumX x) =>
        x is { Real: true, Approximate: true } ? new NumX(RuntimeApi.DecFromDouble(x.Expr), 0, Dec: true) : x;

    /// <summary>The binary64 lane of <see cref="CombineCore"/> — the floating arm of the native dispatch whose other
    /// arm is <see cref="CombineNative"/>, and it owes the SAME size-error rules (kb/Work PB1147, PB1581; the
    /// two-arm shape). Where checking is enabled (<see cref="Checked"/>) each operation is the checked runtime twin
    /// of the scaled carrier's: a zero divisor is EC-SIZE-ZERO-DIVIDE (§14.7.5 case 2, as <c>DivideOrThrow</c>), and
    /// a result outside binary64 is EC-SIZE-OVERFLOW / -UNDERFLOW (case 5, as <c>MulChecked</c>/<c>AddChecked</c>).
    /// Unchecked, the sum, difference and product are the bare IEEE operators, exactly as the scaled lane's unchecked
    /// operators wrap; the QUOTIENT is <c>CobolFloat.Div</c>, because a zero divisor is case 2 whether or not checking
    /// is enabled — the same rule <c>CobolNum.Divide</c> and <c>CobolDec.Div</c> keep (kb/Work PB1605; the bare
    /// <c>/</c> stored +Infinity and ran on, where CONFORMANCE.md DOC-A.1-70 terminates).</summary>
    private NumX CombineReal(NumX a, string op, NumX b)
    {
        string x = Real(a), y = Real(b);
        string? fn = (op, Checked) switch
        {
            ("/", false) => nameof(CobolFloat.Div),
            ("+" or "-" or "*", false) => null,
            ("+", true) => nameof(CobolFloat.AddChecked),
            ("-", true) => nameof(CobolFloat.SubChecked),
            ("*", true) => nameof(CobolFloat.MulChecked),
            ("/", true) => nameof(CobolFloat.DivChecked),
            _ => throw new InvalidOperationException($"no binary64 operator '{op}'"),
        };
        return fn is null
            ? new NumX($"({x} {op} {y})", 0, Real: true)
            : new NumX($"{nameof(CobolFloat)}.{fn}({x}, {y})", 0, Real: true);
    }

    // Plain STANDARD arithmetic (2002; obsolete 2014, removed 2023 — Annex E.2 item 21) uses the standard
    // intermediate data item, which for these operands IS the standard DECIMAL form — STANDARD routes to the
    // same CobolDec engine as STANDARD-DECIMAL (DataBinder.BindDeclarations documents the bind-side rationale).
    // ⛔ The SET lives in ONE place — ArithmeticModes.IsDecimalEngine (kb/Work PB194): two other sites had written
    // it out again and both had dropped ARITHMETIC IS STANDARD from it.
    internal bool StandardDecimal => ArithmeticModes.IsDecimalEngine(ctx.Data.Options.Arithmetic);

    internal string IntermediateMode => $"CobolRounding.{ctx.Data.Options.IntermediateRounding}";

    /// <summary>Render an operand in SDIDI form: an already-decimal intermediate passes through; a fixed-point
    /// value lifts EXACTLY via <c>CobolDec.From</c> (≤31 digits always representable, §8.8.1.5.2); a FLOAT
    /// (Real) operand converts via <c>CobolDec.FromDouble</c> — the §8.8.1.5.1 implementor-defined float→SDIDI
    /// conversion (the shortest round-trip decimal, ≤17 digits, always exact in the 34-digit significand).</summary>
    // An unsigned-wide operand funnels through DeU first: the SDIDI's 34-digit significand cannot hold a
    // 39-digit value either (§8.8.1.5.2 r2 would range-check it out), so the failure is the size-error condition.
    public string DecOperand(NumX x) => DeU(x) switch
    {
        { Dec: true } d => d.Expr,
        { Real: true } r => RuntimeApi.DecFromDouble(r.Expr),
        { Wide: true } w => RuntimeApi.WideToDec(w.Expr, w.Scale),   // the lowering: round-to-odd, 34 digits (kb/Work PB1900)
        var v => $"CobolDec.From({v.Expr}, {v.Scale})",
    };

    private NumX CombineNative(NumX a, string op, NumX b) => op switch
    {
        "+" or "-" => CombineAdditive(a, op, b),
        "*" => Multiply(a, b),
        "/" => Divide(a, b),
        _ => a,
    };

    /// <summary>⛔ MULTIPLICATION ON THE EXACT CARRIER, DECIDED FROM THE OPERANDS' DIGIT BOUNDS (kb/Work PB1143).
    /// Scales add, so the product of an N-digit and an M-digit UNSCALED value is below 10^(N+M) — and that, not the
    /// operands' values, is what leaves the <see cref="Int128"/> carrier: <c>PIC 9V9(30)</c> times <c>PIC 9V9(30)</c>
    /// is the product 3.375 of 1.5 and 2.25, and the product of their 31-digit unscaled values is 61 digits. The
    /// emitter multiplied them unchecked, so a legal statement (§14.7.7 r2a limits the COMPOSITE of operands to 31
    /// digits, not the unscaled product) wrapped modulo 2^128 and stored a wrong product with no condition —
    /// §14.9.26.4 GR1/GR2: "The product of the multiplier and the multiplicand is stored".
    /// <list type="bullet">
    ///   <item><b>Both bounds known and N+M ≤ 38</b> — the product provably fits: a bare multiply, with no
    ///         overflow check even under a size-error phrase (it cannot overflow).</item>
    ///   <item><b>Both bounds known and N+M &gt; 38, and this product IS the final transfer to a resultant</b>
    ///         (<see cref="_outermost"/> — a single-receiver COMPUTE, MULTIPLY BY, MULTIPLY GIVING, and EACH receiver of a
    ///         several-receiver MULTIPLY GIVING or product-rooted COMPUTE, which render per receiver like DIVIDE from
    ///         operands evaluated once) — the exact 256-bit product is rounded ONCE to the resultant's scale with the
    ///         resultant's own mode
    ///         (<c>CobolDec.MulAtScale</c>, the multiplicative twin of the outermost division). It does not matter how
    ///         wide the resultant is: a 16-byte COMP-5 item owns a 38-digit container (§13.18.60.4 GR12), where an
    ///         SDIDI product keeps 34 and the receiver's ROUNDED phrase — PROHIBITED included — would never see the tail
    ///         (kb/Work PB1143's review finding N1: <c>E * F</c> of two 20-digit operands into <c>PIC S9(31) COMP-5</c>
    ///         stored …5370000 for …5361999). A product past the Int128 carrier at that scale is the size error
    ///         condition, never a rounding.</item>
    ///   <item><b>Both bounds known and N+M &gt; 38, NESTED in a larger expression or in a receiver-less one</b> — it is not
    ///         the final transfer, so it keeps EVERY digit: the exact 256-bit product as a <see cref="NumX.Wide"/> value
    ///         (<c>CobolWide.Mul</c>), whenever the bounds prove it fits <c>CobolWide.MaxDigits</c> (77) digits — kb/Work PB1900.
    ///         The implementor's intermediate (§8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an
    ///         arithmetic expression") follows GnuCOBOL, whose <c>cob_decimal_mul</c> / <c>cob_decimal_add</c> are exact: `A * B - C * D`
    ///         over PIC 9(21) operands is exactly 1, where the 34-digit SDIDI this arm used to form rounded each 41-digit product
    ///         and the cancellation kept only the rounding error (stored 10000000). The sums, differences and negations above it
    ///         stay exact on the same form (<see cref="CombineAdditive"/>, <see cref="Negate"/>), and the value is SETTLED once, at
    ///         the public entry (<see cref="Settle"/>): into the receiver's scale and mode at the final transfer — §14.7.7 rule 3
    ///         NOTE 1, ROUNDED sees every digit, for a 16-byte COMP-5 receiver as for a PICTURE-limited one — else into the SDIDI.
    ///         A product whose bound passes 77 digits, or whose other operand has no bound, is formed on the SDIDI as before
    ///         (<c>CobolDec.MulToOdd</c>: the exact product reduced to 34 significant digits by ROUND-TO-ODD, which keeps an odd
    ///         last digit whenever digits were dropped, so a receiver of at most 32 digit positions — every PICTURE-limited one,
    ///         §13.18.40.3 SR14 caps them at 31 — rounds that value, in every mode, exactly as it would round the exact product).
    ///         A quotient above a wide value is formed there too (<c>CobolDec.DivToOdd</c>): GnuCOBOL's own divide is inexact
    ///         (<c>shift_decimal</c> then a truncating <c>mpz_tdiv_q</c>), so the precedence does not demand an exact quotient.</item>
    ///   <item><b>A bound unknown</b> (an intrinsic's value, a windowed view, a counter) — the carrier's own
    ///         behaviour, now NEVER a silent wrap: <c>CobolNum.MulChecked</c> raises the size error condition at the
    ///         Int128 escape boundary (§14.7.5 case 5, A.1 item 179) in every statement, not only under a phrase.</item>
    /// </list></summary>
    private NumX Multiply(NumX a, NumX b)
    {
        bool known = a.Digits > 0 && b.Digits > 0;
        if (known && (a.Wide || b.Wide || a.Digits + b.Digits > ReceiverContext.IntermediateDigits))
        {
            // The final transfer to ONE fixed-point resultant: exact, rounded once at ITS scale with ITS mode. A
            // receiver-less or floating-point context has no such transfer (the bound alone cannot tell the value's
            // magnitude), and a nested product is not it either.
            if (_outermost && _rcv.RoundsAtItsScale && !a.Wide && !b.Wide)
                return new NumX(RuntimeApi.DecMulAtScale(a.Expr, a.Scale, b.Expr, b.Scale, _rcv.Scale, _rcv.Rounding, Checked), _rcv.Scale);
            // ⛔ A NESTED PRODUCT KEEPS EVERY DIGIT (kb/Work PB1900): the exact wide form, whenever the operands' digit
            // bounds prove the exact product fits it. Rounding it to 34 digits here — as the SDIDI below does — left
            // `A * B - C * D` over PIC 9(21) operands only the rounding error of two nearly equal 41-digit products.
            int digits = a.Digits + b.Digits;
            if (digits <= RuntimeApi.WideMaxDigits)
                return new NumX(RuntimeApi.WideMul(WideOperand(a), WideOperand(b)), a.Scale + b.Scale, Digits: digits, Wide: true);
            return new NumX(RuntimeApi.DecMulToOdd(DecOperand(a), DecOperand(b)), 0, Dec: true);
        }
        // A wide operand beside one of unknown bound (an intrinsic's value, a windowed view): the product cannot be proved
        // to fit the wide form, so it is formed on the SDIDI by round-to-odd like every nested product past the carrier.
        if (a.Wide || b.Wide)
            return new NumX(RuntimeApi.DecMulToOdd(DecOperand(a), DecOperand(b)), 0, Dec: true);
        string product = known
            ? $"((Int128)({a.Expr}) * ({b.Expr}))"
            : $"CobolNum.MulChecked({a.Expr}, {b.Expr})";
        return new NumX(product, a.Scale + b.Scale, Digits: known ? a.Digits + b.Digits : 0);
    }

    /// <summary>Guard digits past the deepest receiver/operand scale for a division NESTED inside a larger
    /// expression (numeric design D2): rounding happens ONCE, at the receiver, so the nested quotient must carry
    /// enough fraction headroom for the operations above it. 14 reproduces the legacy decimal accumulator's
    /// ~28-significant-digit behavior the golden corpus encodes.</summary>
    private const int DivGuardDigits = 14;

    /// <summary>Division quotient (ISO §8.8.1 / §14.7.4). An OUTERMOST division (<see cref="_outermost"/> — the
    /// quotient IS the value transferred to the single resultant identifier) is computed DIRECTLY at the receiver
    /// scale and rounded with the receiver's mode in ONE exact step (<c>CobolNum.Divide</c> → <c>RoundDiv</c> uses
    /// the true integer remainder, so no guard digits are needed; ROUNDED/PROHIBITED apply here, the final transfer,
    /// §14.7.7 rule 3 NOTE 1 / §14.7.4.3 GR7). A division NESTED inside a larger expression is NOT the final
    /// transfer — it ALWAYS computes at the D2 guard scale with TRUNCATION (never the receiver's mode), clamped so
    /// the Int128 radix alignment (dividend digits ≤ 18 + the alignment exponent) cannot exceed the wide engine's
    /// 38 digits — and the single receiver store performs the one rounding. The intermediate's precision is
    /// implementor-defined (§8.8.1.3); carrying full guard precision keeps it accurate and is size-error-free
    /// (§14.7.5 enumerates no native intermediate-inexactness case; only a zero divisor, case 2, still raises).</summary>
    private NumX Divide(NumX a, NumX b)
    {
        // An exact wide operand has no quotient form of its own (§8.8.1.3 leaves the intermediate to the implementor, and
        // GnuCOBOL's own divide is inexact: shift_decimal then a truncating mpz_tdiv_q, numeric.c:2259-2261): the quotient
        // forms on the SDIDI by ROUND-TO-ODD (CobolDec.DivToOdd), so the one rounding at the receiver sees any tail.
        // ⛔ AND SO DOES THE FINAL TRANSFER INTO A FLOATING-POINT numeric-edited RESULTANT (kb/Work PB2638): it has no
        // fraction scale to compute the quotient AT — the value normalizes into the mask and the store rounds its
        // SIGNIFICAND by the receiver's mode — so the guard-scale branch below, whose scale is only the mask's
        // working-scale hint, lost every digit of a small quotient (`1 / 3E24` into `+9.99E+99` stored +0.00E+00)
        // and showed the store a truncated tail that cannot break a rounding tie. The round-to-odd SDIDI keeps
        // 34 significant digits and the inexact marker, which is all that rounding (and PROHIBITED) reads.
        if (a.Wide || b.Wide || (_outermost && _rcv.FloatEdited))
            return new NumX(RuntimeApi.DecDivToOdd(DecOperand(a), DecOperand(b)), 0, Dec: true);
        int ds;
        CobolRounding mode;
        if (_outermost && _rcv.RoundsAtItsScale)
        {
            // The final transfer: compute at the resultant's scale + ROUNDED mode. DivideOrThrow detects a
            // PROHIBITED-inexact quotient via the exact integer remainder (§14.7.4.3 GR7 — tests the resultant).
            ds = _rcv.Scale;
            mode = _rcv.Rounding;
        }
        else
        {
            // Nested intermediate: full guard precision + truncation, clamped to the wide engine's alignment
            // headroom (exponent = b.Scale + ds − a.Scale must keep dividend-digits + exponent ≤ 38; 18-digit
            // operands ⇒ exponent ≤ 20). A nested quotient never inherits the receiver's mode — rounding happens
            // once, at the receiver store (§14.7 NOTE 1).
            int baseScale = Math.Max(_rcv.Scale, Math.Max(a.Scale, b.Scale));
            int maxExp = 20;
            int guard = Math.Min(DivGuardDigits, maxExp - (b.Scale + baseScale - a.Scale));
            ds = baseScale + Math.Max(0, guard);
            mode = CobolRounding.Truncation;
        }
        // Both kernels raise a zero divisor (ISO §14.7.5 case 2 — checked or not, kb/Work PB1605); the checked
        // DivideOrThrow adds the PROHIBITED-inexact quotient, which only a checked context owes (§14.7.4.3 r7).
        string fn = Checked ? "DivideOrThrow" : "Divide";
        return new NumX($"CobolNum.{fn}({a.Expr}, {a.Scale}, {b.Expr}, {b.Scale}, {ds}, CobolRounding.{mode})", ds);
    }

    private NumX CombineAdditive(NumX a, string op, NumX b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        // The result's digit bound (NumX.Digits): both operands aligned to s, plus one carry digit — known only when
        // both bounds are, and "unknown" (0) once it passes the carrier's own 38.
        int digits = a.Digits > 0 && b.Digits > 0
            ? Math.Max(a.Digits + (s - a.Scale), b.Digits + (s - b.Scale)) + 1 : 0;
        // ⛔ A SUM OR DIFFERENCE WITH AN EXACT WIDE OPERAND STAYS EXACT (kb/Work PB1900) while its digit bound fits the
        // wide form — the cancellation of two nested products is the case the form exists for. A bound that does not fit
        // (or is unknown) lowers both operands to the SDIDI, whose round-to-odd addition keeps the inexact marker.
        if (a.Wide || b.Wide)
            return digits is > 0 and <= RuntimeApi.WideMaxDigits
                ? new NumX(RuntimeApi.WideAdditive(WideAligned(a, s), op == "-", WideAligned(b, s)), s, Digits: digits, Wide: true)
                : DecBinary(op, a, b);
        if (digits > ReceiverContext.IntermediateDigits) digits = 0;
        // Under an ON SIZE ERROR phrase the sum/difference is overflow-checked at the Int128 ENGINE boundary
        // (AddChecked/SubChecked → OverflowException → the size error condition, §14.7.5 case 5) — the exact
        // MulChecked contract. Reachable: HIGHEST-ALGEBRAIC of PIC S9(19) COMP-5 is Int128.MaxValue itself
        // (kb/Work R10), where an unchecked `+ 1` wraps to the container's far end and stores with no error.
        // Without the phrase it stays the bare unchecked operator, like every other engine op.
        if (Checked)
            return new NumX($"CobolNum.{(op == "+" ? "AddChecked" : "SubChecked")}({Align(a, s)}, {Align(b, s)})", s, Digits: digits);
        return new NumX($"((Int128)({Align(a, s)}) {op} ({Align(b, s)}))", s, Digits: digits);
    }

    /// <summary>⛔ A FLOAT ITEM'S CONTENT ON ITS OWN CARRIER — <c>float</c> for binary32, <c>double</c> for
    /// binary64 — for a COPY of that content, never for arithmetic (kb/Work PB961). The arithmetic read
    /// (<see cref="FieldNumCore"/>'s float arms) widens to binary64 because D16 evaluates there, and an ISO/IEC
    /// 60559 widening QUIETS a signaling NaN; a transfer §14.9.25.4 GR6 c) makes "without change" must not take
    /// it. A windowed (image-stored) item decodes through the binary32 carrier lane
    /// (<c>CobolNum.ParseImageSingle</c>). The §14.6.13.2 rule 3 read check is decided by the ONE reader of that
    /// rule's exemption list, <see cref="SendingRefRules.FloatChecked"/>, exactly as on the arithmetic read.</summary>
    /// <summary>A NATIVE carrier's value decoded from its item's STORAGE image — the ONE crossing decode, a COPY of
    /// the content and never an arithmetic read, so a binary32 item takes the binary32 lane (kb/Work PB961). Every
    /// boundary that meets a native side and an image-carried side of the same numeric description asks it: the
    /// INVOKE copy-out and RETURNING into native storage (kb/Work PB970), the universal (object) crossing's box over
    /// an image-carried side and the numeric-image place's write arm (kb/Work PB187). It is the inverse of
    /// <see cref="ImageOfCarrier"/>; the fixed-point half is <c>CobolNum.StoreImage</c>, the write half of
    /// <c>FormatImage</c>.</summary>
    internal static string CarrierOfImage(string image, DataItem item) =>
        item.Pic!.IsFloat
            ? $"({item.Pic.ClrType}){RuntimeApi.NumParseImageFloat(image, item.ProfileName, binary32Carrier: item.Pic.IsSingle)}"
            : RuntimeApi.NumStoreImage(image, item.ProfileName, $"default({item.Pic.ClrType})");

    /// <summary>The item's STORAGE image encoded from a native carrier value — the inverse of
    /// <see cref="CarrierOfImage"/> (the float lane on its own encoder, keyed on the ITEM's width, kb/Work PB961).</summary>
    internal static string ImageOfCarrier(string value, DataItem item) =>
        item.Pic!.IsFloat
            ? RuntimeApi.NumFormatImageFloat(value, item.ProfileName, item.Pic.IsSingle)
            : RuntimeApi.NumFormatImage(value, item.ProfileName);

    internal static string FloatCarrierRead(Place p, SendingRef sending)
    {
        string raw = p.Item.StoreAsImage
            ? RuntimeApi.NumParseImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName, binary32Carrier: p.Item.Pic!.IsSingle)
            : PlaceRenderer.Read(p);
        return sending.FloatChecked(p.Item.Pic!) ? RuntimeApi.FloatSending(raw) : raw;
    }

    /// <summary>Rescale a value's unscaled long up to <paramref name="toScale"/> (widening only here → exact).
    /// <para>⛔ TOTAL OVER THE CARRIER KINDS, and it has to be: a <c>Real</c> (binary64) operand reaches the
    /// receiver-less sites — a subscript, a SET amount, a PERFORM VARYING FROM/BY, a report VARYING, a RETRY
    /// count — and without this arm the double expression was handed straight to a caller expecting a scaled
    /// integral, so legal source produced uncompilable C# (the PB2 shape). It was already reachable through a
    /// COMP-2 operand; PB13 widened it by keeping a receiver-less FLOAT-FAMILY result in binary64 too, so the
    /// arm is landed at the ONE choke point rather than at each of the forty-odd call sites
    /// (feedback_change_the_dispatch_not_the_callers). A float lands through the same
    /// <c>CobolFloat.ToScaled</c> every other float→fixed transfer uses — the saturation-SAFE one, because it
    /// lands AT the requested scale, so an out-of-range magnitude stays above the caller's capacity check
    /// instead of being rescaled back into range. TRUNCATION matches this helper's existing contract (alignment
    /// is not a ROUNDED transfer; §14.7.7 rule 3 NOTE 1 gives ROUNDED only to the final transfer).</para></summary>
    /// <para>⛔ AND THE <c>Dec</c> ARM IS THE ONE IT WAS MISSING (fix-queue PB32/PB14). The paragraph above
    /// declares this helper TOTAL over the carrier kinds; <see cref="NumX"/> has THREE — exact scaled
    /// <see cref="Int128"/>, the <c>CobolDec</c> SDIDI, and binary64 — and only two were written down. Under
    /// <c>ARITHMETIC IS STANDARD-DECIMAL</c> every arithmetic expression becomes a <c>Dec</c> carrier
    /// (<see cref="CombineCore"/>, <see cref="Power"/>), so a §15.3 type-10 arithmetic-expression argument — legal
    /// at 2014 and 2023 — reached <c>MaxScaled(params Int128[])</c> as a raw <c>CobolDec</c>, which has no
    /// conversion operator, and the user saw a raw Roslyn error on conforming COBOL:
    /// <c>COMPUTE R = FUNCTION MAX(A + B, B)</c> ⇒ <c>error CS1503: cannot convert from
    /// 'CobolNet.Runtime.CobolDec' to 'System.Int128'</c>. That is the PB2 shape on the Dec axis, and it is why
    /// this arm is placed BEFORE the <c>toScale == x.Scale</c> test rather than after: a <c>Dec</c> operand
    /// carries <c>Scale 0</c> by convention, so a scale-0 alignment would otherwise pass the <c>CobolDec</c>
    /// expression through untouched and reproduce the same failure.
    /// <para>⚠ THE LANDING IS EXACT ONLY TO <paramref name="toScale"/>. An SDIDI carries its exponent at RUN
    /// time, so there is no compile-time scale to preserve and the value lands at the argument list's common
    /// scale; a quotient with more fraction digits than that is truncated before the function sees it. That is a
    /// strictly smaller wrong than "does not compile" and it is not the end state — the §15.4.1 r1 answer is a
    /// Dec-carrier body, ledgered as PB38.</para></para></summary>
    /// <param name="mode">The rounding of the narrowing. TRUNCATION is this helper's contract and every
    /// value-semantics caller takes it (alignment is not a ROUNDED transfer; §14.7.7 rule 3 NOTE 1 gives ROUNDED only to
    /// the final transfer). The ONE caller that passes otherwise is the report SUM accumulation, where the
    /// alignment IS the final transfer: §13.18.54.4 GR3 adds each addend into the counter "consistent with the
    /// general rules of the ADD statement … or, in the case of an arithmetic expression, the COMPUTE statement",
    /// and GR4 gives that computation the clause's ROUNDED phrase (kb/Work PB852).</param>
    public static string Align(NumX x, int toScale, CobolRounding mode = CobolRounding.Truncation) =>
        // CHECKED: an intermediate consumer has no capacity check downstream, so a value past the carrier stays the
        // loud sentinel here — never the low-order digits a STORE may take (kb/Work PB77; the Dec arm below raises).
        x.Real ? RuntimeApi.FloatToScaled(x.Expr, $"{toScale}", mode, checkedLanding: true)
        // A Dec that the Int128 carrier cannot hold at this scale is a SIZE ERROR condition (EC-SIZE-OVERFLOW —
        // §14.7.5 case 5, A.1 item 179 "checked"; kb/Work PB69), never the low-order-digits landing a STORE may
        // use: an intermediate consumer has no capacity check downstream to catch a truncated value.
        : x.Dec ? RuntimeApi.DecToUnscaledIntermediate(x.Expr, $"{toScale}", mode)
        // Unsigned-wide (kb/Work R10): the receiver-less integral sites this helper feeds (a subscript, a SET
        // amount, PERFORM VARYING, RETRY, exit status) are Int128-lane consumers — the Widen funnel applies
        // (loud beyond the intermediate; a 39-digit subscript is not a computable position).
        : x.U ? Align(DeU(x), toScale, mode)
        : toScale == x.Scale ? x.Expr
        // ⛔ ESCAPE-CHECKED (fix-queue PB65): Align's consumers are VALUE-semantics sites — intrinsic argument
        // alignment, comparisons, subscript/status intake — where a silent Int128 wrap on widening handed MIN a
        // negative result over two positive arguments. RescaleEscape raises the size-error condition at the D1
        // escape boundary instead; the arithmetic store path keeps its documented wrap (item 179) and does not
        // come through here.
        : RuntimeApi.NumRescaleEscape(x.Expr, $"{x.Scale}", $"{toScale}", mode);

    /// <summary>Land an arithmetic-expression at scale 0 ROUNDED UP — the sibling of <see cref="Align"/> for the
    /// clauses that say "rounded up to the next whole number" rather than taking the value-semantics truncation.
    /// <para>⛔ THIS IS THE ONE PLACE THAT RULE IS WRITTEN, and its population is CLOSED, not sampled: a grep for
    /// "rounded up" over the whole standard returns exactly TWO normative sites — §14.7.9.3 GR1 (the RETRY
    /// phrase's arithmetic-expression-1, an n-TIMES count) and §14.9.3.4 GR1 (ALLOCATE's arithmetic-expression-1,
    /// a byte count). Both call here. Every other <c>Align(…, 0)</c> consumer was checked against its own clause
    /// and none of them says "rounded up" (EVALUATE / GO TO DEPENDING selectors, PERFORM VARYING and PERFORM n
    /// TIMES, relative RRN / START WITH LENGTH / key fields, SET amounts, boolean shift counts, report-writer
    /// counts, WRITE ADVANCING lines, and STOP RUN's exit status, whose §14.9.42 SR3 explicitly wants the integer
    /// truncation). A new site governed by a round-up clause belongs here, never in a second local ceiling —
    /// <c>NumericRoundUpSiteTests</c> is the drift test that keeps that true.</para>
    /// <para>The mode is <see cref="CobolRounding.TowardGreater"/>, which IS "rounded up to the next whole
    /// number" — round toward positive infinity — on every lane (<c>CobolFloat</c> uses <c>Math.Ceiling</c>,
    /// <c>CobolNum</c>/<c>CobolDec</c> add one only when the value is positive). It is deliberately NOT
    /// <c>AwayFromZero</c>, which agrees with it on positives but rounds −1.5 to −2 where the rule requires −1.
    /// Both clauses guard their own negatives — §14.9.3.4 GR2 shunts ≤ 0 to NULL and §14.7.9.3 GR4a shunts a
    /// negative or zero expression to the unsuccessful path before the rounded value is used — so the choice is
    /// behaviour-neutral at both of today's sites and simply correct at the next one.</para>
    /// <para>The lane structure mirrors <see cref="Align"/> exactly, including its checked landings: an
    /// intermediate consumer has no capacity check downstream, so a value past the carrier stays the loud
    /// sentinel here rather than becoming the low-order digits a STORE may take (kb/Work PB77/PB69/PB65).</para>
    /// <para>NOTE — ALLOCATE's NATIVE-FLOAT lane does NOT come through here, and that is not an oversight:
    /// <c>CobolPtr.AllocateReal</c> fuses GR1's <c>Math.Ceiling</c> with GR2's ≤ 0 ⇒ NULL and with the
    /// storage-not-available outcomes that only the ALLOCATE statement defines, so a NaN or an over-wide request
    /// answers "not available" instead of raising (kb/Work PB151). Routing it through a checked emitter-side
    /// landing would trade that defined outcome for a size-error condition. The ROUNDING RULE is still written
    /// once — here — and AllocateReal's ceiling is that rule realised inside the statement's own outcome
    /// machinery; the drift test knows about the exemption by name.</para></summary>
    public static string AlignRoundedUp(NumX x) =>
        x.Real ? RuntimeApi.FloatToScaled(x.Expr, "0", CobolRounding.TowardGreater, checkedLanding: true)
        : x.Dec ? RuntimeApi.DecToUnscaledIntermediate(x.Expr, "0", CobolRounding.TowardGreater)
        : x.U ? AlignRoundedUp(DeU(x))
        : x.Scale == 0 ? x.Expr
        : RuntimeApi.NumRescaleEscape(x.Expr, $"{x.Scale}", "0", CobolRounding.TowardGreater);

    /// <summary>Render a STOP RUN / GOBACK termination-status phrase to a C# <c>long</c> exit-status expression
    /// (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10): the status VALUE truncated to an integer at scale 0 when present
    /// (SR3 — an integer is passed to the OS), else the implementor error/normal indication ERROR ⇒ 1 / NORMAL ⇒ 0
    /// (§14.9.42.4 GR2/GR3; docs/CONFORMANCE.md §4.2.16). The value renders receiver-less (scale 0) exactly as a
    /// boolean-shift count or an intrinsic integer argument does.
    /// <para>⛔ THE VALUE IS AN OPERAND, NOT AN EXPRESSION (kb/Work PB169) — §14.9.42.2 writes the slot
    /// <c>{identifier-1 | literal-1}</c>. The NUMERIC arm is byte-identical to the former
    /// <c>Render(BoundExpr)</c> text by construction: <c>Visit(BoundNumericLiteral)</c> and
    /// <c>Visit(BoundNumLiteral)</c> both reduce to <c>EmitText.UnscaledLit</c> for an integer literal (SR3
    /// admits no other), and <c>Visit(BoundFieldOperand)</c> and <c>Visit(BoundNumRef)</c> are the same
    /// <c>FieldNum(Place)</c> call.</para></summary>
    public string ExitStatus(TerminationStatus st) =>
        st.Value is { } v ? RuntimeApi.HostInt64(Align(StatusNum(v), 0)) : st.Error ? "1L" : "0L";

    /// <summary>The GR5 numeric interpretation of a status operand. A NUMERIC operand (an integer literal-1, a
    /// numeric identifier-1) renders through the ordinary numeric channel; a NON-NUMERIC literal-1 of ANY form
    /// is interpreted through its CHARACTERS — docs/CONFORMANCE.md item 192's published determination, "its
    /// digit characters decode as an unsigned integer, a non-digit position contributing no digit"
    /// (<c>CobolNum.FromAlphanumeric</c>), the same decode <see cref="FieldNumCore"/> gives an alphanumeric or
    /// national identifier-1.
    /// <para>⛔ ONE ARM FOR EVERY NON-NUMERIC LITERAL SHAPE, THROUGH THE ONE CHARACTER-IMAGE READER (kb/Work
    /// PB216). The first cut read only <see cref="AsNum"/>, whose figurative / ALL-literal / boolean arms are
    /// LOUD — correctly so, because they answer the §8.8.1.1 ARITHMETIC question, and the status slot is not an
    /// arithmetic position. The consequence was that <c>STOP RUN WITH ERROR STATUS SPACE</c>,
    /// <c>… STATUS ALL "5"</c>, <c>… STATUS B"01"</c> and <c>… STATUS B"1" &amp; B"0"</c> — all conforming
    /// (§8.3.3.6.3 SR1 admits a figurative wherever 'literal' appears, and §14.9.42.3 SR3's conditional "if
    /// literal-1 is numeric" is the proof the slot is not restricted to a numeric literal) — compiled clean and
    /// died with <c>NotImplementedCobolFeatureException</c>. <see cref="OperandText.AsString"/> is THE ONE
    /// operand→character-image reader and already carries §8.3.3.6.4 GR3 b (a non-ALL figurative is ONE
    /// character — GR3's NOTE 2 names the STOP statement by name) and GR3 c (an <c>ALL literal-1</c> is
    /// literal-1 once), PCS-aware for HIGH-/LOW-VALUE. Routing the whole literal family through it is what makes
    /// the NEXT literal form automatic; <c>NumericRendererStatusOperandTests</c> holds that true.</para></summary>
    private NumX StatusNum(BoundOperand v) =>
        v is BoundStringLiteral or BoundFigurative or BoundAllLiteral
            // sending: false — the STOP status slot is not a MOVE into a numeric receiver; its numeric
            // interpretation is CONFORMANCE.md item 192's own determination, and §14.9.25.4 GR6 d) 1's
            // condition has no scope over it.
            ? new NumX(RuntimeApi.NumFromAlphanumeric(OperandText.AsString(v, this), sending: false), 0)
            : AsNum(v, ReceiverContext.None);

    /// <summary>Exponentiation (ISO §8.8.1.2). ⛔ THREE ARMS, DECIDED BY THE OPERANDS' CARRIERS: an INTEGER EXPONENT over an
    /// integer base (<c>PowNativeIntDec</c>) or over a scaled / SDIDI base (the exact wide lane or <c>PowNativeDec</c>,
    /// kb/Work PB2617) is EXACT — binary64 is never the method for a power that has an exact decimal value — and the rest
    /// is the arm below. A native-arithmetic exponentiation whose result has no exact
    /// representation is an IMPLEMENTOR-DEFINED approximation, computed in double and quantized through the ONE
    /// <c>CobolIntrinsics.FromDouble</c> at the landing <see cref="ReceiverContext.FloatLanding"/> chooses — the
    /// resultant identifier's scale + the statement's ROUNDED mode when the power IS the transfer, the
    /// <c>max(Receiver.Scale, 9)</c> working scale with TRUNCATION when it is a nested intermediate (kb/Work
    /// PB647: this arm carried the float family's hard-coded NEAREST-AWAY-FROM-ZERO and its defect with it, so
    /// <c>COMPUTE A = 3 ** 0.5</c> rounded where a no-phrase store must truncate). The previous
    /// scale-0 <c>(long)</c> truncation lost every fractional power result and turned the double artifact in
    /// <c>SQRT(10) ** 2</c> = 9.999999988 into 9 (IF136A F-SQRT-25); the 9-digit floor mirrors the float-intrinsic
    /// working scale (a receiver-less context renders at scale 0 — the P7.3 <see cref="ReceiverContext.None"/>).</summary>
    private NumX Power(NumX b, NumX e, Int128? literalExponent)
    {
        b = LowerWide(DeU(b));   // exponentiation is arithmetic — the unsigned-wide Widen funnel applies (kb/Work R10);
        e = LowerWide(DeU(e));   // an exact wide base/exponent has no power form: it enters on the SDIDI (kb/Work PB1900)
        // STANDARD / STANDARD-DECIMAL: exponentiation follows §8.8.1.5.4 — an integer exponent evaluates by
        // repeated SDIDI multiplication (r2a–r2d exactly; r2e's implementor-defined form for larger integers,
        // every step per §8.8.1.5.3), r3's reciprocal for a negative exponent, and the EC-SIZE-EXPONENTIATION
        // legs of §8.8.1.2 r6 / §8.8.1.5.4 r4; a float operand converts in per §8.8.1.5.1 (DecOperand). The ONE
        // runtime implementation is CobolDec.Pow.
        if (StandardDecimal)
            return new NumX(RuntimeApi.DecPow(DecOperand(b), DecOperand(e), IntermediateMode), 0, Dec: true);
        // ⛔ AN INTEGER BASE RAISED TO AN INTEGER EXPONENT IS EXACT, AND THE RECEIVER PLAYS NO ROLE AT ALL
        // (owner decision 2026-08-03; fix-queue PB18 + PB32 + PB65/RV-15.64.4-1). Three things turn on this arm:
        //   · `COMPUTE R = 10 ** 30` returned 1000000000000000071935427891953 where Int128 holds 10^30 exactly —
        //     the native technique contradicting our OWN documented one (numeric design D3).
        //   · It is the ROOT of PB32's receiver-shape defect: `A ** 2` was binary64 under DISPLAY / an IF subject
        //     and exact under COMPUTE, routing FUNCTION MOD to a DIFFERENT BODY (930000008 vs 930000007). A
        //     function's value must not depend on the SHAPE of its receiver (§15.4).
        //   · PB32's fix left the RECEIVER-BEARING arm forcing pscale = FloatWorkingScale (≥ 9) onto a result
        //     that is an INTEGER by construction, so the ×10⁹ landing pushed a 30-digit exact power past Int128
        //     inside PowNativeInt, whose double fallback then SATURATED — and FUNCTION MOD consumed the sentinel:
        //     `COMPUTE R = FUNCTION MOD(A ** 2, B)` printed 320612800 (13657001 owed) into S9(9)/S9(18)/S9(28)
        //     and the right answer into S9(31), the receiver's PICTURE alone selecting the value. The arms had
        //     merely SWAPPED (feedback_two_arm_dispatch, fifth sighting). The scale a non-negative integer power
        //     needs is 0 — for EVERY receiver — so both arms now land identically and the receiver is not read.
        // A scale-0 base to an integer exponent is scale 0 for ANY exponent, so the result scale is known without
        // knowing the exponent's value — which is exactly why this arm is restricted to a scale-0 base (a
        // scale-s base to the n has scale s·n, with no compile-time scale to give it).
        // ⚠ A NEGATIVE OR RUNTIME-ITEM EXPONENT CANNOT LAND AT A COMPILE-TIME SCALE: §8.8.1.2's reciprocal for a
        // negative exponent is not an integer (`2 ** -2` at scale 0 ⇒ 0 instead of 0.25 — a measured regression),
        // and a data-item exponent's sign is a run-time fact, so NO fixed scale serves both regimes — scale 9
        // corrupts the big positive powers (measured: `A ** X` with X = 2 gave the same 320612800), scale 0
        // truncates the reciprocals. The carrier that owns its scale AT RUN TIME is the SDIDI, so those shapes
        // return Dec: PowNativeIntDec computes the same owner-decided values (exact Int128 loop while it fits,
        // the documented double approximation past it / for the reciprocal) on the carrier every downstream
        // consumer already handles (the PB32/PB14 carrier-total sweep). Receiver-independent by construction.
        // ⛔ ONE ARM FOR BOTH EXPONENT SHAPES, AND IT IS THE DEC ONE (kb/Work PB69). The literal-exponent arm used
        // to return Int128 from PowNativeInt, whose past-the-carrier fallback SATURATED to Int128.MaxValue —
        // fine above a STORE's capacity check (the receiver reports SIZE ERROR), poison at every VALUE-semantics
        // consumer: `FUNCTION MOD(A ** 3, B)` answered from the sentinel (639816141 for 980012199), `A ** 4 >
        // A ** 3` was FALSE and `A ** 3 = A ** 4` TRUE (both saturated), `A ** 3 / A ** 2` was 1.7e8, and the SAME
        // expression spelled `A ** X` (the Dec arm) gave a THIRD number. The value that leaves the carrier is the
        // owner-decided double approximation, and the carrier that can hold it AND the exact Int128 fast path is
        // the SDIDI: PowNativeIntDec is exact while the power fits (CobolDec.From keeps the full significand — no
        // 34-digit rounding on construction), the approximation past it, receiver-independent by construction.
        // Consumers: a relation compares Decs exactly (CobolDec.Compare); an intrinsic with a Dec argument routes
        // to its SDIDI body under native too (IntrinsicRenderer.RenderNum); an Int128 landing that cannot hold the
        // value raises EC-SIZE-OVERFLOW (CobolDec.ToUnscaledIntermediate — A.1 item 179, "checked") instead of
        // returning modular digits.
        bool integerOperands = !b.Real && !e.Real && !b.Dec && !e.Dec && b.Scale == 0 && e.Scale == 0;
        if (integerOperands)
            return new NumX(RuntimeApi.Intrinsic("PowNativeIntDec", $"{b.Expr}, {e.Expr}"), 0, Dec: true);
        // ⛔ A FIXED-POINT OR SDIDI BASE RAISED TO AN INTEGER EXPONENT IS EXACT TOO (kb/Work PB2617). The integer-base arm
        // above was the only exact one, so `COMPUTE R = A ** 2` over `A PIC 9V9(10)` took the binary64 approximation
        // below (1.26215515697488189772 where `A * A` stores the exact 1.26215515697488187881), and the same program
        // answered differently for `A ** 2`, `A ** N` and `A * A` — the receiver-shape defect PB32 removed from the
        // integer base, one carrier over. §8.8.1.3 leaves native evaluation to the implementor and the precedence
        // follows GnuCOBOL (CLAUDE.md rule 1), whose cob_decimal_pow raises a decimal to an integer exponent by an
        // exact mpz_pow_ui with scale × n; binary64 is the approximation of a NON-integer exponent only. The test is
        // the exponent's CARRIER (scale 0, no Dec/Real: an integer by construction), like the arm above.
        //   · A literal exponent n ≥ 1 has the compile-time result scale (base scale × n) and digit bound (base digits ×
        //     n), so while that bound fits the exact wide lane the power IS a NumX.Wide (settled once at the public
        //     entry, like a nested product): `A ** 2` is `A * A` by construction, rounded once at the receiver.
        //   · Every other integer exponent (a data item, a negative or zero literal, a bound past the wide lane, a base
        //     with no digit bound) lands on the SDIDI, which owns its scale at run time: CobolIntrinsics.PowNativeDec
        //     is the same exact power (the wide lane at run time, then the round-to-odd chain), the §8.8.1.5.4 r3
        //     reciprocal for a negative exponent, and the §8.8.1.2 rule-6 screen.
        if (!b.Real && !e.Real && !e.Dec && e.Scale == 0 && (b.Dec || b.Scale != 0))
        {
            if (literalExponent is { } n && n >= 1 && b.Digits > 0 && !b.Dec && (long)b.Digits * (long)n <= RuntimeApi.WideMaxDigits)
                return n == 1
                    ? b
                    : new NumX(RuntimeApi.WidePow(WideOperand(b), (int)n), b.Scale * (int)n, Digits: b.Digits * (int)n, Wide: true);
            return new NumX(RuntimeApi.Intrinsic("PowNativeDec", $"{DecOperand(b)}, {e.Expr}"), 0, Dec: true);
        }
        // D16 (NATIVE): a float base/exponent OR a float receiver keeps the result FLOATING (native double) — skip
        // the FromDouble quantize-back that a pure fixed-point power needs, so a float ** stays in the float pipeline.
        // A receiver-less exponentiation keeps the binary64 result for the same reason the float-intrinsic family
        // does (PB13): §8.8.1.2 already makes this an implementor-defined approximation computed in double, and
        // with no receiver there is no scale to quantize TO — the ws = 9 stand-in saturated, so `IF 10 ** 30 =
        // 10 ** 31` evaluated TRUE. A fixed-point receiver still quantizes, at the capacity-capped working scale.
        // ⛔ BOTH ARMS NOW SCREEN §8.8.1.2 RULE 6 (PB28) — `PowNativeReal`, not a bare `System.Math.Pow`. The rule
        // is a GENERAL rule of arithmetic-expression evaluation and binds native `**` exactly as it binds the
        // SDIDI one, which `CobolDec.Pow` above has always honoured while every native arm ignored it.
        // ⛔ THE SAME QUANTIZER, SO THE SAME LANDING DECISION (PB13's sibling — feedback_scan_all_similar). A flat
        // max(Scale, 9) here silently saturated `COMPUTE R = 10 ** 30` into a PIC 9(31) exactly as it did for the
        // float-intrinsic family, and the hard-coded NEAREST-AWAY-FROM-ZERO mode made `COMPUTE A = 3 ** 0.5` round
        // where §14.7.4.3 rule 2 makes a no-phrase store TRUNCATE, exactly as it did there (kb/Work PB647).
        // ReceiverContext.FloatLanding is the one rule both consume, and it answers the WHETHER as well as the
        // scale and the mode: the resultant identifier's scale + the statement's mode for a final transfer into a
        // fixed-point resultant, NO QUANTIZATION AT ALL for a nested intermediate / a float receiver / a
        // receiver-less render. ⛔ THE RECEIVER-SHAPE HALF USED TO BE SPELLED HERE TOO (kb/Work PB653): this
        // method wrote `_rcv.Real || _rcv.Receiverless` out for itself, IntrinsicRenderer.RenderFloatNative wrote
        // the same pair out for itself, and NEITHER copy learned that a nested operand must keep its binary64 —
        // so `COMPUTE R = FUNCTION SQRT(10) ** 2` landed 9.999999998 where a COMP-2 base gave 10.000000000.
        // The decision now carries `Quantize` and this arm reads it (feedback_two_arm_dispatch, sixth sighting).
        var landing = _rcv.FloatLanding(_outermost);
        // Under size-error checking the binary64 power is range-checked like every other checked native operation
        // (§14.7.5 case 5 — kb/Work PB1581); unchecked it stays the bare approximation.
        string pow = RuntimeApi.Intrinsic(Checked ? "PowNativeRealChecked" : "PowNativeReal", $"{Real(b)}, {Real(e)}");
        // The binary64 power is an APPROXIMATION the engine produced (§8.8.1.3) unless an operand is itself a float
        // ITEM's content, which then owns the result (kb/Work PB1641 — see CombineCore's lane note).
        if (b.Real || e.Real || !landing.Quantize)
            return new NumX(pow, 0, Real: true, Approximate: !((b.Real && !b.Approximate) || (e.Real && !e.Approximate)));
        return new NumX(RuntimeApi.Intrinsic("FromDouble",
            $"{pow}, {landing.Scale}, {RuntimeApi.RoundingText(landing.Mode)}{CheckedFlag}"), landing.Scale);
    }

    private static NumX Negate(NumX x) =>
        x.Real ? new NumX($"(-({Real(x)}))", 0, Real: true, Approximate: x.Approximate)
        : x.Dec ? new NumX($"(new CobolDec(-({x.Expr}).Sig, ({x.Expr}).Exp))", 0, Dec: true)
        : x.Wide ? new NumX(RuntimeApi.WideNegate(x.Expr), x.Scale, Digits: x.Digits, Wide: true)
        : x.U ? new NumX($"(-{DeU(x).Expr})", x.Scale, Digits: x.Digits)   // negation is arithmetic — the Widen funnel applies
        : new($"(-{x.Expr})", x.Scale, Digits: x.Digits);

    /// <summary>The unsigned-wide → Int128-lane funnel (kb/Work R10): a <c>U</c> operand narrows through
    /// <c>CobolNum.Widen</c> (loud beyond the documented Int128 intermediate, CONFORMANCE.md §4.2.16); every
    /// other operand passes through unchanged. The ONE chokepoint every arithmetic path shares (internal: the
    /// CALL BY CONTENT computed-argument site funnels through the same rule).</summary>
    internal static NumX DeU(NumX x) => x.U ? new NumX(RuntimeApi.NumWiden(x.Expr), x.Scale, Digits: x.Digits) : x;

    /// <summary>A value's intake carrier KIND — the exact scaled <c>Int128</c>, the SDIDI or binary64 — after the
    /// unsigned-wide lane has funnelled through <see cref="DeU"/>: the ONE mapping both value-on-its-own-carrier
    /// intakes key on, the §15.3 type-6 integer argument (<c>IntrinsicRenderer.IntegerIntake</c>) and the
    /// subscript / reference-modifier position (<c>ArithmeticEmitter.EmitPositionValue</c>, kb/Work PB1890), so a new
    /// carrier is one arm here rather than a missed arm in one of them.</summary>
    internal static (RuntimeApi.IntegerArgCarrier Carrier, NumX Value) IntakeCarrier(NumX a)
    {
        a = DeU(a);
        return a.Carrier switch
        {
            NumXCarrier.Binary64 => (RuntimeApi.IntegerArgCarrier.Real, a),
            NumXCarrier.Sdidi => (RuntimeApi.IntegerArgCarrier.Dec, a),
            NumXCarrier.Scaled => (RuntimeApi.IntegerArgCarrier.Scaled, a),
            _ => throw new InvalidOperationException($"unsigned-wide operand survived the Widen funnel ({a.Carrier})"),
        };
    }

    /// <summary>
    /// Land a rendered intermediate into the exact <c>Int128</c> lane at the receiver's working scale — the ONE
    /// landing every value-semantics consumer of a <c>(Expr, Scale)</c> pair shares: an intrinsic argument, a CALL
    /// BY VALUE arithmetic-expression argument, a SET pointer UP/DOWN BY amount, an ALLOCATE character count. A
    /// native intermediate is already there; an SDIDI (<c>Dec</c>) intermediate — every arithmetic expression under
    /// a standard mode, and under NATIVE arithmetic an integer power (kb/Work PB69) — lands CHECKED at
    /// <c>rcv.WorkingScale(SdidiLandingScaleFloor)</c> (EC-SIZE-OVERFLOW past the carrier — §14.7.5 case 5, A.1 item 179
    /// "checked" — never the modular low-order digits a STORE may use, because a value-semantics consumer has no
    /// capacity check downstream); a float under a STANDARD mode is converted in first (§8.8.1.5.1 — the mode
    /// beats the float branch, COBOLNET_NUMERIC_DESIGN.md D3) and lands the same way. A float under NATIVE
    /// arithmetic stays binary64 — the consumer's own float arm applies (see <see cref="FixedLane"/> for the
    /// consumers that have none).
    /// <para>⛔ kb/Work PB84: after PB69 made <c>A ** 2</c> an SDIDI intermediate under native arithmetic, every
    /// consumer that read <c>x.Expr</c> as a native carrier — <c>(long)(x.Expr)</c> in the pointer and CALL BY
    /// VALUE emitters, the sign condition's <c>x.Expr &gt; 0</c> (NIST NC250A), the INVOKE BY CONTENT store —
    /// became a Roslyn error on conforming COBOL. The same consumers were ALREADY wrong for every STANDARD-DECIMAL
    /// expression; the SDIDI arm was written once here and the consumers now funnel through it, so the next carrier
    /// (or the next consumer) has one place to be right in.</para>
    /// </summary>
    public NumX Landed(NumX x, ReceiverContext rcv)
    {
        if (x.Real && StandardDecimal)
            x = new NumX(DecOperand(x), 0, Dec: true);
        if (!x.Dec) return x;
        int ws = rcv.WorkingScale(ReceiverContext.SdidiLandingScaleFloor);
        return new NumX(RuntimeApi.DecToUnscaledIntermediate(x.Expr, ws.ToString(), CobolRounding.Truncation), ws);
    }

    /// <summary><see cref="Landed"/> for a consumer that computes ONLY in the exact <c>Int128</c> lane and has no
    /// float arm of its own — the DIVIDE … REMAINDER subsidiary-quotient kernel (§14.9.12.4 GR7 — kb/Work PB85: a
    /// FLOAT-LONG dividend was a Roslyn error). A native float lands TRUNCATED at the receiver's float working scale
    /// (<see cref="ReceiverContext.FloatWorkingScale"/> — the D16 quantizer's one rule); an unsigned-wide read
    /// funnels through <see cref="DeU"/>; everything else is <see cref="Landed"/>.</summary>
    public NumX FixedLane(NumX x, ReceiverContext rcv) =>
        x.Real && !StandardDecimal ? new NumX(Align(x, rcv.FloatWorkingScale), rcv.FloatWorkingScale)
        : Landed(DeU(x), rcv);

    /// <summary>The value/scale/profile argument run that stores a rendered intermediate into a fixed-point
    /// receiver's <c>NumProfile</c> — the ONE place the carriers are told apart at a store (kb/Work PB84: the
    /// arithmetic store, the numeric MOVE and the INVOKE BY CONTENT expression each spelled it, and the third had
    /// no <c>Dec</c> arm): an SDIDI intermediate takes the <c>CobolNum.Store(CobolDec, profile)</c> overload (the
    /// §14.7 final transfer — whose <c>Store</c>/<c>TryStore</c> pair already tells the two landings apart, PB74); a
    /// float lands at the receiver scale through <c>ToScaled</c> (CHECKED — <paramref name="checkedLanding"/>, an
    /// ON SIZE ERROR / EC-SIZE store) or <c>ToScaledUnchecked</c> (a MOVE, the no-phrase store, INVOKE BY CONTENT —
    /// the low-order digits past the carrier, kb/Work PB77) with the receiver's ROUNDED mode (D16) and then stores as
    /// a native at that scale (rescale identity ⇒ no double rounding); a native passes its own scale (an exact-family
    /// intrinsic chose ITS landing form at the render — <c>IntrinsicRenderer.CheckedFlag</c>).</summary>
    public static string StoreArgs(NumX value, int recvScale, CobolRounding mode, string profile, bool checkedLanding) =>
        value.Dec ? $"{value.Expr}, {profile}"
        : value.Real ? $"{RuntimeApi.FloatToScaled(value.Expr, $"{recvScale}", mode, checkedLanding)}, {recvScale}, {profile}"
        : $"{value.Expr}, {value.Scale}, {profile}";

    /// <summary>The unscaled <c>Int128</c> a rendered intermediate lands as at a numeric-EDITED receiver's fraction
    /// scale (<paramref name="scale"/> — the mask's, or a format-2 LOCALE picture's: <c>PicInfo.ReceiverScale</c>)
    /// for an ARITHMETIC statement's final transfer, which the edit then formats: the edited receiver's sibling of
    /// <see cref="StoreArgs"/>, and the ONE place its carriers are told apart (kb/Work PB2163 — the masked and the
    /// LOCALE arms of <c>ArithmeticEmitter.StoreArith</c> each spelled this switch). The receiver's ROUNDED
    /// <paramref name="mode"/> applies here, at the receiver's scale, because the formatter only truncates.
    /// <paramref name="checkedLanding"/> is the statement's landing form (an ON SIZE ERROR / EC-SIZE store): a float
    /// lands through <c>ToScaled</c> or <c>ToScaledUnchecked</c> (the low-order digits past the carrier, kb/Work
    /// PB77 — the Real arm used to ignore the form); an SDIDI through <c>ToUnscaledChecked</c> or <c>ToUnscaled</c>
    /// (kb/Work PB74 — the unchecked form hands the formatter the low-order digits of an out-of-carrier value, which
    /// the mask then "fits"); a native at another scale through <c>RescaleChecked</c> or <c>RescaleStoreCap</c>
    /// (kb/Work PB639 — a widening past the carrier used to wrap before the capacity rule ran); an unsigned-wide
    /// value (a 16-byte unsigned COMP-5 item, kb/Work R10) through <c>RescaleCheckedU</c> or <c>RescaleStoreCapU</c>
    /// at EVERY scale, its own included, because the edit takes an <c>Int128</c> — it fell to the native arm and
    /// handed the <c>UInt128</c> over unconverted, CS1503 on accepted source, until this switch was made total over
    /// <see cref="NumXCarrier"/>. A MOVE into an edited receiver lands its non-native carriers here too, unchecked.</summary>
    public static string EditedLanding(NumX value, int scale, CobolRounding mode, bool checkedLanding) => value.Carrier switch
    {
        NumXCarrier.Binary64 => RuntimeApi.FloatToScaled(value.Expr, $"{scale}", mode, checkedLanding),
        NumXCarrier.Sdidi => checkedLanding ? RuntimeApi.DecToUnscaledChecked(value.Expr, $"{scale}", mode)
                                            : RuntimeApi.DecToUnscaled(value.Expr, $"{scale}", mode),
        NumXCarrier.UnsignedWide => RuntimeApi.NumRescaleStoreU(value.Expr, $"{value.Scale}", $"{scale}", mode, checkedLanding),
        NumXCarrier.Scaled => value.Scale == scale ? value.Expr
            : RuntimeApi.NumRescaleStore(value.Expr, $"{value.Scale}", $"{scale}", mode, checkedLanding),
        var c => throw new InvalidOperationException($"no edited landing for the {c} carrier"),
    };

    /// <summary>The EXPRESSION-POSITION store of a rendered intermediate into a fixed-point receiver —
    /// <see cref="StoreArgs"/> through <c>CobolNum.Store</c> (<c>StoreU</c> on the unsigned-wide lane, by name)
    /// with <paramref name="mode"/> (MOVE truncation by default, §14.6.8.2). Returns the receiver's stored
    /// unscaled integer expression.
    /// <para><paramref name="raiseOnSizeError"/> selects the RAISING kernel (<c>CobolNum.StoreOrRaise</c>) for
    /// the one caller shape that has neither an ON SIZE ERROR phrase nor an arithmetic statement to latch a
    /// flag in, yet whose statement HAS EC-SIZE checking enabled: the §14.2.3 GR9/GR10 argument crossing of an
    /// INVOKE, where "if the formal parameter is numeric" the transfer IS "a COMPUTE statement without the
    /// ROUNDED phrase" and §14.7.5's no-phrase rule 4 therefore sets EC-SIZE-TRUNCATION to exist (kb/Work
    /// PB640). A MOVE never passes it: §14.6.8.2 r4's alignment truncates by rule, and the MOVE statement is
    /// not in §14.7.5's list of statements the size error condition may occur as a result of.</para></summary>
    public static string StoreExpr(NumX value, int recvScale, string profile,
        CobolRounding mode = CobolRounding.Truncation, bool raiseOnSizeError = false)
    {
        // The float lane's own landing is checked TOO when the store raises: past the Int128 carrier
        // ToScaledUnchecked hands back the low-order digits (kb/Work PB77), which the capacity test would then
        // accept — §14.7.5 case 3 is a fact about the algebraic result (the same argument CobolArgAdapt's
        // checked float landing makes).
        string args = StoreArgs(value, recvScale, mode, profile, checkedLanding: raiseOnSizeError);
        return raiseOnSizeError
            ? RuntimeApi.NumStoreOrRaise(args, mode, value.U)
            : RuntimeApi.NumStoreRounded(args, mode, value.U);
    }

    /// <summary>The trailing <c>checkedLanding: true</c> argument for a runtime quantizer / exact-family parse rendered
    /// under ON SIZE ERROR / EC-SIZE checking (kb/Work PB77): the value's landing past the Int128 carrier is then the
    /// SATURATING one (the receiver's capacity check raises, PB13); a MOVE sender or a no-phrase store — the default —
    /// takes the low-order digits. ONE spelling for the float family's <c>FromDouble</c>, native <c>**</c>, and the
    /// NUMVAL family (<c>IntrinsicRenderer</c> reads it through <c>ReceiverContext.InSizeError</c>).</summary>
    internal string CheckedFlag => Checked ? ", checkedLanding: true" : "";

    // Internal (not private): the intrinsic renderer converts float-family arguments to double through THIS
    // one scaled-value→double conversion (ISO §15.4.1 native-arithmetic family; singular-pattern rule).
    // ⛔ A scaled operand routes through the runtime's CORRECTLY-ROUNDED CobolFloat.ScaledToDouble (kb/Work
    // PB115): the former `(double)(x) / <10^scale literal>` built its divisor by repeated multiplication —
    // exact only through 1e22 — so at scale ≥ 23 a LEGAL ASIN(|x| ≤ 1) argument (§15.10.3 r2) arrived one ulp
    // above 1.0 and evaluated NaN; and even a correct literal divisor rounds twice (the Int128 cast, then the
    // divide), which a boundary-sitting argument cannot afford.
    /// <summary>A NON-float sender's algebraic value converted to binary32 in ONE rounding (kb/Work PB1110):
    /// <c>CobolFloat.ScaledToSingle</c>, never <see cref="Real"/>'s binary64 narrowed by a cast. A scaled or
    /// standard-decimal operand only — a float intermediate is already a binary64 the caller narrows once.</summary>
    internal static string ScaledSingle(NumX x) =>
        x.Dec ? $"({x.Expr}).ToSingle()"
        : x.Wide ? $"({RuntimeApi.WideToDec(x.Expr, x.Scale)}).ToSingle()"
        : RuntimeApi.ScaledToSingle(x.Expr, x.Scale);

    internal static string Real(NumX x) =>
        x.Real ? x.Expr                                   // already a double-typed float intermediate (D16)
        : x.Dec ? $"({x.Expr}).ToDouble()"
        : x.Wide ? RuntimeApi.WideToDouble(x.Expr, x.Scale)
        : x.Scale == 0 ? $"(double)({x.Expr})"
        : RuntimeApi.ScaledToDouble(x.Expr, x.Scale);
}
