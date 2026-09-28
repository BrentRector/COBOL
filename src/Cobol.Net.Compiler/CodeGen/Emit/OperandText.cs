// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>Renders a bound operand to its C# DISPLAY-image string (used by DISPLAY, MOVE-to-alphanumeric, and
/// string comparisons). A numeric field's image goes through <c>CobolNum.FormatDisplay</c> (sign-aware); an
/// alphanumeric field is its <see cref="Place"/> value.</summary>
internal static class OperandText
{
    /// <summary>A bound operand rendered as a C# <see cref="string"/> (its character image). When
    /// <paramref name="deSign"/> is set, a SIGNED numeric operand drops its operational sign (ISO §14.9.25.4 GR6a /
    /// §8.8.4.2.5 — a signed numeric used as / compared against an alphanumeric operand moves the de-signed magnitude
    /// digits, not the zoned/overpunch image). DISPLAY leaves it unset (it shows the sign-aware image).
    /// <paramref name="num"/> is the caller's per-unit expression renderer: an ALPHANUMERIC/NATIONAL-result
    /// intrinsic operand (ISO §15.2 type 1 — the one case that lets MOVE-to-alphanumeric, string comparisons,
    /// and group moves take FUNCTION operands) renders through its INSTANCE intrinsic channel (P7 Step 12 —
    /// the context-free static channel is deleted); deSign is a no-op for it (no operational sign).</summary>
    // The cached operand-visitors render the image with zero per-call allocation; a single cached IsString
    // visitor answers the text-comparison predicate. The generated IBoundOperandVisitor makes both
    // exhaustive (PHASE-07 Step 6f) — a new BoundOperand leaf is a COMPILE error, the loud `_ =>` defaults are gone.
    // The intrinsic-operand case is intercepted at the entry (it needs the PER-UNIT renderer, which the cached
    // static visitors cannot hold); the visitor's computed arm keeps the loud non-intrinsic case.
    // One cached instance per (deSign × SendingRef) pair — both carried on the instance so every call is
    // allocation-free. SendingRef names the §14.6.13.2 EXEMPT CONTEXT of the reads in this operand (see that
    // type): rule 3's float wrap (CobolFloat.Sending, EC-DATA-NOT-FINITE) and rule 2's fixed-point windowed wrap
    // (CobolNum.ParseImageSending / CobolNum.SendingImage, EC-DATA-INCOMPATIBLE) each read their OWN exemption
    // list off it. The table is INDEXED, not a hand-written ladder: a SendingRef member added tomorrow gets its
    // pair by construction rather than by somebody remembering to extend a switch (kb/Work PB230).
    private static readonly AsStringVisitor[] _asString = BuildVisitors();

    /// <summary>One visitor per (<c>deSign</c> × <see cref="SendingRef"/>) pair, indexed by the enum's VALUE.
    /// Written as an explicit fill rather than a <c>SelectMany</c> over <c>GetValues</c> so the contiguity it
    /// needs is CHECKED: a <see cref="SendingRef"/> whose values stopped being <c>0..n-1</c> throws here at type
    /// initialization instead of silently handing one context another's visitor.</summary>
    private static AsStringVisitor[] BuildVisitors()
    {
        var values = Enum.GetValues<SendingRef>();
        var a = new AsStringVisitor[values.Length * 2];
        foreach (var s in values)
        {
            a[(int)s * 2] = new AsStringVisitor(deSign: false, s);
            a[((int)s * 2) + 1] = new AsStringVisitor(deSign: true, s);
        }
        return a;
    }
    private static readonly IsStringVisitor _isString = new();

    private static AsStringVisitor Visitor(bool deSign, SendingRef sending) =>
        _asString[((int)sending * 2) + (deSign ? 1 : 0)];

    public static string AsString(BoundOperand op, NumericRenderer num, bool deSign = false, SendingRef sending = SendingRef.Normal) =>
        op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } ic }
            ? num.Intrinsics.RenderString(ic)
        // A NUMERIC-result intrinsic in a string context — DISPLAY FUNCTION ORD(C), MOVE FUNCTION MAX(…) TO a
        // PIC X item. §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so every "identifier-1"
        // position admits one unless a syntax rule excludes it (§14.9.11.3 SR1 excludes only message-tag, object
        // and pointer), and §15.4 puts the returned value in a temporary elementary data item. Intercepted at the
        // ENTRY beside the string channel because it needs the PER-UNIT renderer, which the cached static
        // visitors cannot hold. Before this, ONLY the compile-time-FOLDED cases worked — a fold turns the call
        // into a numeric literal, which is why FUNCTION LENGTH printed and FUNCTION ORD threw.
        : op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Numeric } nic }
            ? NumericIntrinsicText(num, nic, deSign)
        // A function-identifier the binder FOLDED into an arithmetic sum — FUNCTION LENGTH / BYTE-LENGTH of a
        // variable-length group is §15.50.4 r7's fixed-plus-dynamic-leaves sum (kb/Work PB61): still a numeric
        // function's returned value in a string position, rendered in the same item-92 literal form. No general
        // format admits a bare arithmetic expression here, so nothing else can reach this arm.
        : op is BoundComputedOperand { Expr: BoundBinary } sum
            ? NumericExprText(num, sum.Expr, deSign)
        // A bare figurative (HIGH-VALUE/LOW-VALUE/SPACE/…) in a DISPLAY/STRING/STOP value position is an
        // alphanumeric value (§8.3.3.6.4 GR1) of one character (GR3b). Materialize it through the declared collating
        // tables so HIGH-/LOW-VALUE is the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), matching the MOVE and
        // relation paths — not the native pin. Intercepted at the ENTRY (like the intrinsic channel) because the
        // AsStringVisitor has no access to the renderer's collating context; cat=null ⇒ the alphanumeric PCS applies.
        : op is BoundFigurative fig
            ? $"new string({FigurativeConstants.Fill(fig.Kind, num.Collating, null, num.NationalCollating)}, 1)"
        // A boolean EXPRESSION operand's string is its '0'/'1' image through the ONE boolean renderer (kb/Work PB65
        // — a legal intrinsic argument, §8.4.3.2.3 SR8, and any other string position a boolean expression reaches);
        // intercepted at the ENTRY because the renderer needs the per-unit NumericRenderer (shift counts).
        : op is BoundBoolOperand bo
            ? BooleanRenderer.Render(bo.Expr, num, sending)
            : op.Accept(Visitor(deSign, sending));

    /// <summary>The text image of a NUMERIC-result intrinsic (DA2), across all three shapes a numeric
    /// intermediate can take. A FLOAT-valued function renders through the same <c>CobolFloat.Display</c> a float
    /// ITEM does (shortest round-trip, §14.9.11.4 GR1 implementor-defined) — the two must agree, since
    /// <c>FUNCTION SQRT(2)</c> and a COMP-2 item holding that value are the same value; an SDIDI intermediate and
    /// an ordinary scaled value share <c>FormatFunctionText</c>. No float SENDING guard here: the value is a
    /// freshly computed temporary, not a stored item that could hold a NaN deposited earlier.
    /// <para>⛔ <see cref="ReceiverContext.None"/>, NEVER the ambient receiver — the same convention
    /// <c>IntrinsicRenderer.ArgNum</c> uses for the string channel. A text context HAS no numeric receiver, and an
    /// intrinsic's working scale is <c>max(Receiver.Scale, 9)</c>, so reading the ambient one made a function's
    /// printed text depend on whatever arithmetic statement happened to run BEFORE it.</para>
    /// <para>⛔ <paramref name="deSign"/> is honoured, because §14.9.25.4 GR6a is a GENERAL rule carrying no
    /// implementor latitude: "If the sending operand is described as being signed numeric, the operational sign is
    /// not moved". The §15.4.1 / §14.9.11.4 GR1 latitude covers the FORM of the text (padding, radix), never
    /// whether the sign travels. So a de-signing caller — MOVE to an alphanumeric/national/edited receiver, a text
    /// relation (§8.8.4.2.5 routes it through the MOVE rules), INSPECT — gets the MAGNITUDE, exactly as a signed
    /// FIELD operand does via <see cref="FieldAsString"/>. Without this, a signed item and a function returning
    /// the same value rendered differently in identical statements.</para></summary>
    private static string NumericIntrinsicText(NumericRenderer num, BoundIntrinsicCall ic, bool deSign) =>
        NumericExprText(num, ic, deSign);

    /// <summary>The item-92 text form of a numeric function-identifier's value, whatever bound shape the binder
    /// left it in — the intrinsic call itself, or a fold's arithmetic sum (<see cref="AsString"/>).</summary>
    private static string NumericExprText(NumericRenderer num, BoundExpr expr, bool deSign)
    {
        NumX x = num.Render(expr, ReceiverContext.None);
        return x.Real ? RuntimeApi.FloatDisplay(x.Expr)
             : x.Dec ? RuntimeApi.DecFunctionText(x.Expr, deSign)
             : RuntimeApi.NumFormatFunctionText(x.Expr, x.Scale, deSign);
    }

    /// <summary>A data item's character image directly from its <see cref="Place"/> — the num-free entry for
    /// callers that hold a Place (a FIELD can never be an intrinsic operand, so no per-unit renderer is
    /// needed). Same rendering as <see cref="AsString"/> over a field operand.</summary>
    public static string FieldImage(Place p, bool deSign = false, SendingRef sending = SendingRef.Normal) => FieldAsString(p, deSign, sending);

    /// <summary>The RAW-STORAGE byte image of a field — CONVERT's ANY source channel (§15.19.3 r7: "argument-1
    /// shall be of any usage … It is not necessary for the contents to be valid according to the usage" — the
    /// item's STORAGE BITS, never its display image; fix-queue PB59 family 5b / AR-15.19.3-7 leg b). The
    /// convention is char==byte (the <c>CobolBits.Pack</c> image convention), one representation per leaf shape,
    /// each THE mechanism that already defines that shape's storage (<c>GroupImageCodec.AsImageOf</c>'s recipes —
    /// never a second codec): a group is its generated <c>AsImage()</c>; a national leaf its UTF-16BE bytes
    /// (<c>CobolBits.NatBytes</c>, D-N1); a USAGE BIT leaf its packed bits (<c>CobolBits.Pack</c> — which also
    /// materializes §15.19.4 r2's trailing zero-bit pad); a zoned/character leaf its carrier (the sign-AWARE
    /// image — storage, so no GR6a de-sign); a BINARY/PACKED/COMP-5 leaf its radix-2/BCD bytes
    /// (<c>CobolNum.FormatImage</c>, V59/§14.4); a float leaf its IEEE interchange bytes
    /// (<c>CobolNum.FormatImageFloat</c> — kb/Work PB164 wave 2); an INDEX leaf its 8-byte occurrence-number
    /// bytes (R40). Only a VARIABLE-LENGTH group stages LOUD rather than returning a plausible wrong image;
    /// pointer/object shapes are unreachable — the §15.19.3 r7 bind screen rejected them.</summary>
    /// <summary>⛔ THE RECORD-AREA IMAGE — what a WRITE / REWRITE / RELEASE sends to a connector and what a
    /// READ / RETURN distributes back (kb/Work PB327). It IS <see cref="AsStorageImage"/>, because a file record
    /// is the record area's STORAGE: ISO §14.9.30.4 GR14/GR15 state the short/long-record rules over "the number
    /// of BYTES in the record" and §9.1.6 records the record size as a fixed file attribute in bytes. These sites
    /// used to send <see cref="AsString"/> — the OPERAND text — which agreed with storage for every leaf shape
    /// except the ones whose carrier is not their bytes: a NATIONAL record area wrote one byte per position
    /// (measured: `01 L-REC PIC N(5).` holding N"AB" wrote `41 42` where D-N1's representation is
    /// `00 41 00 42`), and a USAGE BIT one wrote its '0'/'1' carrier into a ceil(n/8)-byte record. A GROUP record
    /// took the same string either way, which is exactly why the elementary half went unnoticed.</summary>
    /// <para>⛔ AN OUT-OF-LINE RECORD (<c>FileModel.IsOutOfLineRecord</c> — determination D-FRA, kb/Work PB981)
    /// has no character window, so its record image is stated by the determination rather than read from one: a
    /// VARIABLE-LENGTH GROUP sends its contiguous image at its current extent (ISO §8.5.1.11.2 — the
    /// <c>CurrentImage()</c> composer; §13.18.43.4 GR13 c) sizes it), a DYNAMIC-LENGTH record its content at its
    /// current length (§8.5.1.10.4 — a sending operand "is treated as a fixed-length data item whose length is
    /// the dynamic-length elementary item's current length"; the storage arm below already renders exactly that),
    /// and a POINTER-CLASS record the zero-length image — it has no character image (A.1 items 210/216), so there
    /// is nothing of it for a file to carry.</para></summary>
    public static string RecordAreaImage(Place record) =>
        IsVarGroupRecord(record)
            ? PlaceRenderer.VarGroupCurrentImage(record, "record-area image of")
        : record.Item.IsElementary && SlotWindow.CarriedBySlot(record.Item) ? "\"\""
        : AsStorageImage(record, "record-area image of");

    /// <summary>⛔ THE EXTENT TABLE that travels beside <see cref="RecordAreaImage"/> (docs/CONFORMANCE.md §3
    /// determination D-FRA (v); kb/Work PB1053), or null when the record has none to send. A VARIABLE-LENGTH GROUP
    /// record's image is its contiguous composition (§8.5.1.11.2), which carries no marker of where a dynamic member
    /// ends, so it is sent with the table that says so (<see cref="PlaceRenderer.VarGroupCurrentExtents"/>); every
    /// other record — a character-window record, a dynamic-length elementary record (its image IS its one
    /// component), a pointer record — has nothing to describe. Decided by the SAME predicate as the image's own
    /// arm, so an image and its table can never come from different shapes. Every emitter that hands a record-area
    /// image to the runtime (WRITE, REWRITE, RELEASE, and the keyed READ / START / DELETE whose key the record's
    /// members may precede) passes this beside it.</summary>
    public static string? RecordAreaExtents(Place record) =>
        IsVarGroupRecord(record) ? PlaceRenderer.VarGroupCurrentExtents(record) : null;

    /// <summary>Is <paramref name="record"/> a variable-length GROUP record — the one arm of
    /// <see cref="RecordAreaImage"/> and <see cref="RecordAreaExtents"/> whose image is a contiguous composition.</summary>
    private static bool IsVarGroupRecord(Place record) =>
        record.Item.IsGroup && FileModel.IsVariableLengthRecord(record.Item);

    /// <summary>THE CURRENT RECORD's character image — the sending operand of a <c>READ … INTO</c> /
    /// <c>RETURN … INTO</c> implicit MOVE (ISO §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b): the record area's image
    /// (through THE ONE record-area channel above) sliced to the §13.18.43.4 GR16 byte count. The slice starts at
    /// position 1 because the record occupies the leading positions of the area; the area's remaining positions
    /// are padding, and sending them is what kb/Work PB339 fixes.
    /// <para>ZERO IS A LEGAL LENGTH here and must not raise EC-BOUND-REF-MOD: GR16's closing sentence is "If the
    /// number of bytes determined as above is zero, the record is a zero-length item", and §14.9.25.4 GR1/GR2
    /// then space-fill the receiver — which is exactly what a zero-length ref-mod result moves.</para></summary>
    public static string CurrentRecordImage(BoundCurrentRecord n) =>
        RuntimeApi.StrRefMod(RecordAreaImage(n.Area), "1", CurrentRecordLength(n), allowZeroLength: true);

    /// <summary>§13.18.43.4 GR16's two arms, rendered — "the number of bytes in the current record that
    /// participate as the sending operands in the implicit MOVE statement": GR16 a) "If data-name-1 is specified,
    /// by the content of the data item referenced by data-name-1" (the SAME expression GR13 a) renders for
    /// RELEASE/REWRITE/WRITE — <c>SequentialIoEmitter.VaryingLengthArg</c>; one rule text, one renderer), and
    /// GR16 b) "If data-name-1 is not specified, by the value that would have been moved into the data item
    /// referenced by data-name-1 had data-name-1 been specified" — GR15's just-read length, which the runtime
    /// already holds per connector (a READ) or per sort store (a RETURN).</summary>
    public static string CurrentRecordLength(BoundCurrentRecord n) =>
        n.Depending is { } dep
            ? RuntimeApi.HostInt32(RuntimeApi.TableOcc(PlaceRenderer.Read(dep)))
            : $"(int){(n.File.IsSortMerge
                ? RuntimeApi.SortLastReturnedLength(EmitText.FileKeyExpr(n.File))
                : RuntimeApi.FileLastReadLength(EmitText.FileKeyExpr(n.File)))}";

    /// <summary>⛔ <b>THE SENDING OPERAND OF A NON-ELEMENTARY MOVE — ISO §14.9.25.4 GR4, asked in BOTH
    /// directions</b> (kb/Work PB430). GR4: "Any move that is not an elementary move, and does not reference a
    /// variable-length group, is treated exactly as if it were an alphanumeric to alphanumeric elementary move,
    /// EXCEPT THAT THERE IS NO CONVERSION OF DATA FROM ONE FORM OF INTERNAL REPRESENTATION TO ANOTHER." The
    /// operand TEXT (<see cref="AsString"/>) is the wrong reader for that clause on an ELEMENTARY sender: it
    /// renders a COMP-3 / COMP / COMP-5 / floating-point item as its zoned DISPLAY digits, which is precisely a
    /// conversion of internal representation — and makes the item occupy the wrong number of character positions
    /// in the receiving area.
    /// <para>The compiler already owned the right answer, in the OTHER direction: the group-sender arm deposits
    /// the pinned representation bytes of exactly these items, through <see cref="AsStorageImage"/>'s per-shape
    /// recipes. This is that one codec, so the two arms of the group move cannot answer the same question
    /// differently (feedback_two_arm_dispatch). A LITERAL or figurative sender keeps the operand text: it has no
    /// internal representation to preserve — GR4's own first sentence names "a literal OR an elementary item" as
    /// the sending side, and §8.3.3.6.4 GR2 sizes a figurative from the receiver.</para>
    /// <para>A <c>BoundCurrentRecord</c> also keeps <see cref="AsString"/>, which routes it to
    /// <see cref="CurrentRecordImage"/> — the record-area STORAGE image sliced to §13.18.43.4 GR16's byte count,
    /// i.e. this same channel plus the rule only that operand has (kb/Work PB327/PB339).</para></summary>
    public static string NonElementaryMoveSender(BoundOperand op, NumericRenderer num, string context) => op switch
    {
        // A MOVE's sending group is a ONE-WAY transfer (kb/Work PB244): its characters go to the receiver and are
        // never read back into it, so a strongly-typed group with a class pointer/object leaf sends its storage
        // image - the leaf's reserved placeholder positions - under §14.9.25.4 GR4.
        BoundFieldOperand f => AsStorageImage(f.Place, context, transfer: true),
        _ => AsString(op, num, deSign: false),
    };

    /// <param name="context">Names the operation in the Tier-C loud message — the same parameter
    /// <c>PlaceRenderer.GroupImage</c> carries, so a caller can route through THE ONE storage channel and keep
    /// its own site-specific reason (kb/Work PB178's law, PB327's second caller).</param>
    /// <param name="transfer">A ONE-WAY transfer consumer (kb/Work PB244) - see <c>PlaceRenderer.GroupImage</c>.</param>
    public static string AsStorageImage(Place p, string context = "raw-storage image (CONVERT ANY) of", bool transfer = false)
    {
        // §8.4.3.3.4 GR6 — a ref-mod view is an elementary item over the underlying item's characters; its
        // storage is the slice's characters (a NATIONAL slice keeps category national, so its bytes are UTF-16BE).
        // ⛔ A BIT-USAGE SLICE IS THE THIRD CASE, AND IT WAS MISSING (kb/Work PB173). §8.4.3.3.4 GR5a gives a
        // usage-bit operand BIT positions, and GR6 preserves its class, category and usage — so the slice's
        // '0'/'1' carrier is NOT its storage, exactly as it is not for the whole item three arms below. §15.19.3
        // r7 asks for the item's storage BITS and §15.19.4 r2 then pads: "If the number of bits in argument-1 is
        // not a multiple of those needed for a single alphanumeric character, the trailing portion needed to make
        // up a complete multiple is padded with zero bits". Measured before the fix: `CONVERT(XM(1:3) ANY ANUM
        // HEX)` returned 313130 — the three CHARACTERS '1','1','0' — where the unsliced `CONVERT(XM ANY ANUM
        // HEX)` correctly returned CA. Both the bit-GROUP slice (new with this place) and the ELEMENTARY USAGE
        // BIT slice (which predates it) take this arm, and both were wrong; one arm fixes both.
        // ⚠ A DISPLAY-FORM boolean slice must NOT pack: §13.18.60.4 GR7 makes usage DISPLAY "an alphanumeric
        // coded character set", one character per boolean position, so its carrier IS its storage. That is why
        // the test reads the USAGE and not the category (measured: `PIC 1(8)` without USAGE BIT correctly
        // returns 3131303031303130 sliced and unsliced alike, and must keep doing so).
        if (p is RefModPlace rmp)
            return rmp.Category is PicCategory.National ? RuntimeApi.NatBytes(PlaceRenderer.Read(p))
                : rmp.Inner.Item.OperandPic is { Category: PicCategory.Boolean, Usage: Usage.Bit }
                    ? RuntimeApi.BitsPackAll(PlaceRenderer.Read(p))
                    : PlaceRenderer.Read(p);
        // A GROUP's raw storage IS its character image — through THE ONE SENDING READER, never a self-spelled
        // `.AsImage()`. That routing is the fix for kb/Work PB178: a Tier-B REDEFINES group VIEW is fully
        // image-CAPABLE and its Read() is ALREADY the (offset, width) window over the class backing
        // (§13.18.44.4 GR1 — one storage area, so the view's raw storage IS that window, never a second
        // encoding), so the capability guard this arm already had was the WRONG AXIS and
        // `CobolStr.RefMod(...).AsImage()` was a backend CS1061. `SendingGroupImage` owns all four arms —
        // the RedefViewPlace window, the OdoGroupPlace §13.18.38 GR8 current-extent slice (the arm this site
        // used to spell itself, one line above), the capability guard, and the plain struct image.
        if (p.Item.IsGroup) return PlaceRenderer.SendingGroupImage(p, context, transfer);
        // ⛔ USAGE NATIONAL IS A SERIALIZATION OF THE CHARACTER IMAGE, NOT AN ALTERNATIVE TO IT (design D-N7 —
        // the group codec's AsImageOf composes the same way, kb/Work PB646). The category decides what the
        // item's CHARACTERS are — for category national its carrier, for the national-form NUMERIC of
        // §13.18.60.3 SR12 the zoned digit run FormatImage writes — and the usage decides only that each
        // character occupies two bytes, UTF-16BE (D-N1). A national arm keyed on the CATEGORY alone sent a
        // national-form numeric as one byte per digit (kb/Work PB1556: `01 R PIC 9(3) USAGE NATIONAL` wrote
        // `37 38 39 20 20 20` into its 6-byte record).
        if (ElementaryCharacterImage(p) is not { } chars)
            return EmitText.LoudValue("string", TierCIsland.Reason(p.Item, context));
        return p.Item.Pic is { Usage: Usage.National } ? RuntimeApi.NatBytes(chars) : chars;
    }

    /// <summary>An elementary item's storage in its OWN character alphabet — <see cref="AsStorageImage"/>
    /// serializes it (a national item's characters become byte pairs; every other item's are its bytes already).
    /// Null for a shape with no storage image (pointer/object — the Tier-C island).</summary>
    private static string? ElementaryCharacterImage(Place p)
    {
        return p.Item.Pic switch
        {
            { Category: PicCategory.National } => PlaceRenderer.Read(p),
            // A USAGE BIT leaf packs its own carrier (an elementary operand is a run of one — §8.5.1.6.3's
            // shared-byte runs exist only INSIDE a group image, which the group arm above owns).
            { Category: PicCategory.Boolean, Usage: Usage.Bit } pic =>
                RuntimeApi.BitsPack(PlaceRenderer.Read(p), pic.Length.ToString()),
            // Display-form boolean, alphanumeric, edited: the carrier IS the storage, one char per byte.
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.Boolean } =>
                PlaceRenderer.Read(p),
            // A numeric leaf stored as its image: the carrier ALREADY HOLDS the storage bytes, whatever byte form
            // they are — zoned digits, radix-2, BCD, or the IEEE interchange bytes of a windowed float. §15.19.3 r7
            // asks for the item's STORAGE, and a windowed item's window IS its storage, so it passes through
            // untouched. ⛔ THIS ARM MUST PRECEDE the native byte-form arms below (the same ordering pin
            // NumericRenderer's windowed-float arm carries): with an `IsFloat: false` conjunct here a windowed
            // float fell through to FormatImageFloat(<string window>) — a backend CS1503, the ONE shape
            // §15.19.3 r7's ANY source format can reach.
            { Category: PicCategory.Numeric } when p.Item.StoreAsImage => PlaceRenderer.Read(p),
            // A native numeric leaf: the bytes it occupies at a byte boundary (zoned digits, radix-2, BCD,
            // or the wave-2 IEEE forms — PicInfo.HasImageByteForm, THE ONE image predicate; kb/Work PB164) —
            // the same recipes the group codec uses, the float family through its distinctly-named lane.
            { HasImageByteForm: true, IsFloat: true } =>
                RuntimeApi.NumFormatImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName, p.Item.Pic!.IsSingle),
            { HasImageByteForm: true } =>
                RuntimeApi.NumFormatImage(PlaceRenderer.Read(p), p.Item.ProfileName),
            _ => null,
        };
    }

    /// <summary>True if an operand is compared as text (an alphanumeric literal, or an alphanumeric/edited/group
    /// field — an alphanumeric group item "has class and category alphanumeric", ISO §8.5.2.1, and §8.8.4.2.1 makes
    /// it "treated as an elementary alphanumeric data item" for comparison; §8.8.4.2.3 SR2 admits it as a relation
    /// operand). ⚠ §8.8.4.1.1 DOES NOT EXIST (kb/Work PB182 — the repo-wide sweep).</summary>
    public static bool IsString(BoundOperand op) => op.Accept(_isString);

    /// <summary>⛔ BYTES ARE NOT TEXT (V59). An image-STORED BINARY/PACKED leaf holds its radix-2 / BCD bytes, and
    /// those bytes are not the item's alphanumeric text: an ELEMENTARY numeric operand used as text is "treated as
    /// though it were moved to an alphanumeric data item" (ISO §14.9.25.4 GR6, and §8.8.4.2.2 for a numeric ↔
    /// nonnumeric comparison), which yields its DIGITS. So decode the stored bytes and re-render the DISPLAY image.
    /// Returns null for every ZONED item — there the stored image IS the text, and passing the window through
    /// verbatim also preserves the incompatible content a group MOVE can legitimately deposit (spaces in a numeric
    /// leaf), which a decode-and-reformat round trip would silently turn into zeros.
    /// <para>A GROUP operand is the opposite case and is handled before this: §8.5.2.1 makes it class and category
    /// alphanumeric over the items' REPRESENTATION, so its text IS the record image, bytes and all.</para>
    /// <para>⛔ EVERY byte form the window can hold decodes here, keyed on the item's own carrier lane — the
    /// decode is the exact inverse of the encode the group codec / REDEFINES backing used, so a WINDOWED leaf
    /// and its NATIVE twin print identical text. §13.18.44.4 GR1 associates the two descriptions with ONE
    /// storage area, so "same value, two descriptions, two different printed images" is never a defensible
    /// answer, whatever latitude §14.9.11.4 GR1 leaves the implementor over the FORM of that text (WiseOwl COBOL's
    /// determination: docs/CONFORMANCE.md Annex A.1 items 56/92). The three lanes below are the three carriers
    /// a byte-form numeric leaf can have, and they mirror <c>NumericRenderer.FieldNumCore</c>'s windowed arms
    /// one for one — the float lane FIRST (its <c>IsFloat</c> exclusion is what printed raw IEEE bytes), then
    /// the UInt128 lane (a signed decode of a 16-byte unsigned window is a silent wrong answer above
    /// <c>Int128.MaxValue</c>'s bit pattern), then the signed Int128 lane every other form rides. An 8-byte
    /// UNSIGNED window needs no twin: <c>ParseImage</c> already returns its full [0, 2^64) range as an
    /// Int128.</para></summary>
    private static string? NonTextBytes(Place p, bool deSign, SendingRef sending)
    {
        if (p.Item.Pic is not { Category: PicCategory.Numeric } pic
            || pic.ByteForm is NumericByteForm.Zoned or NumericByteForm.None) return null;
        // A FLOAT window holds the item's IEEE interchange bytes (§13.18.60.4 GR13–GR15, the wave-2 pin): decode
        // through the distinctly-named float lane and render through the SAME CobolFloat.Display the native float
        // arm below uses, including the CobolFloat.Sending guard (EC-DATA-NOT-FINITE, §14.6.13.2 item 3) unless
        // the caller is an exempt context. deSign is a no-op: §14.9.25.4 GR6a speaks of an item "described as
        // being signed numeric" — a float item has no PICTURE and no operational sign to drop.
        if (pic.IsFloat)
        {
            string dec = RuntimeApi.NumParseImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName, binary32Carrier: pic.IsSingle);
            return RuntimeApi.FloatDisplay(sending.FloatChecked() ? RuntimeApi.FloatSending(dec) : dec);
        }
        // A 16-byte UNSIGNED BinaryCapacity window (UInt128 carrier, kb/Work R10): the unsigned parse twin
        // reinterprets the signed lane's Int128 bit-identically, and the U-named format lane keeps the full
        // [0, 2^128) range — picked by NAME, never by overload (see CobolNum.FormatDisplayU).
        if (pic.IsUnsignedWideBinary)
        {
            string uv = RuntimeApi.NumParseImageU128(PlaceRenderer.Read(p), p.Item.ProfileName, sending.FixedPointChecked());
            return deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplayU({uv}, {pic.Digits})", pic)
                : RuntimeApi.NumFormatDisplay(uv, p.Item.ProfileName, u: true);
        }
        string value = RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending.FixedPointChecked());
        // GR6a: an alphanumeric move/compare drops the operational sign — the magnitude digits, never the
        // BinaryMinus form (which is VARIABLE width and would shift a fixed receiver).
        return deSign
            ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(value, pic.Digits), pic)
            : RuntimeApi.NumFormatDisplay(value, p.Item.ProfileName);
    }

    /// <summary>
    /// ⛔ THE VERBATIM CHARACTER READ of a place whose STORED IMAGE IS ITS VALUE — checked per ISO §14.6.13.2
    /// for the item's CATEGORY, which is the one thing the three verbatim-return sites in
    /// <see cref="FieldAsString"/> must not each decide for themselves. They used to: the REDEFINES-view return,
    /// the <c>StoreAsImage</c> return and the elementary switch all spelled a bare <c>PlaceRenderer.Read(p)</c>,
    /// and a rule that applies to one CATEGORY across all three of them has no place to live in that shape. It is
    /// written here once (kb/Work PB230).
    /// </summary>
    /// <list type="bullet">
    /// <item>NUMERIC — rule 2, via <c>CobolNum.SendingImage</c>. The window goes on VERBATIM: its stored image
    /// IS its text, and a decode-and-reformat round trip would silently turn the very incompatible content the
    /// rule exists to report into zeros.</item>
    /// <item>BOOLEAN — rule 1, via <c>CobolBool.Sending</c>. The string half of the boolean chokepoint pair
    /// (<c>BooleanRenderer</c>'s <c>BoundBoolRef</c> is the value half): <c>DISPLAY B</c> references a boolean
    /// item's content exactly as <c>COMPUTE R = B</c> does.</item>
    /// <item>Alphanumeric / numeric-edited / national — not subjects of the clause (its rules name a boolean
    /// sending item, a numeric one, and a de-editing MOVE's numeric-edited SENDER, which is the de-edit's own
    /// site and not this text read), so they read RAW.</item>
    /// </list>
    private static string SendingVerbatim(Place p, SendingRef sending) =>
        !sending.FixedPointChecked() ? PlaceRenderer.Read(p)
        : p.Item.Pic?.Category switch
        {
            PicCategory.Numeric => RuntimeApi.NumSendingImage(PlaceRenderer.Read(p), p.Item.ProfileName, true),
            PicCategory.Boolean => RuntimeApi.BoolSending(PlaceRenderer.Read(p)),
            _ => PlaceRenderer.Read(p),
        };

    private static string FieldAsString(Place p, bool deSign = false, SendingRef sending = SendingRef.Normal)
    {
        // A reference-modified result is an elementary ALPHANUMERIC item regardless of the underlying item's
        // category (ISO §8.4.3.3.4 GR6) — its Read() is already the character slice (a numeric inner goes through
        // NumericImagePlace), never the numeric format path.
        if (p is RefModPlace) return PlaceRenderer.Read(p);
        // A Tier-B REDEFINES view's Read() is already its character-image window (a string), for a group or an
        // elementary view alike — use it directly (no .AsImage(), no FormatDisplay). EXCEPT for a NUMERIC view
        // whose bytes are not text: every non-zoned byte form (binary / packed / COMP-5 / IEEE float) decodes
        // through NonTextBytes and re-renders its DISPLAY image, so the view prints what its NATIVE twin prints
        // (§13.18.44.4 GR1 — one storage, two descriptions). The zoned case falls through to the window verbatim,
        // and a signed zoned view that is the de-signed source of an alphanumeric move/compare re-emits its
        // magnitude digits (ISO §14.9.25.4 GR6a) exactly as the StoreAsImage branch below does.
        // ⛔ "A Tier-B view's Read() is its character image" IS THE ARM'S PREMISE, so it is asked rather than
        // assumed (kb/Work PB231). It holds for the identity coding, for a BIT window (the boolean carrier) and
        // for a NATIONAL window (the transcoded characters) — and NOT for a SlotWindow, whose read is a managed
        // pointer/object reference and not a string. A SlotWindow member therefore falls THROUGH to the
        // category switch below, where a class-pointer operand meets exactly the same arm its non-cell-backed
        // twin meets: `01 P USAGE POINTER BASED.` and `01 P USAGE POINTER.` must produce the same text for the
        // same statement, whatever that text is. `RedefViewPlace.ReadsCharacterImage` states the premise once.
        if (p is RedefViewPlace { ReadsCharacterImage: true })
            return NonTextBytes(p, deSign, sending) is { } rvBytes ? rvBytes
                : deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } rvp
                ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending.FixedPointChecked()), rvp.Digits), rvp)
                // The zoned window goes through VERBATIM, checked on the way — see SendingVerbatim.
                : SendingVerbatim(p, sending);
        // A group operand's character image is the generated AsImage(): each string-stored leaf contributes its
        // characters; a DISPLAY leaf its zoned digits; and a BINARY/PACKED leaf ⛔ ITS TRUE BYTES — radix-2
        // two's complement of StorageWidth, or BCD with a trailing sign nibble (V59). Implementor-defined
        // territory (§8.8.4.2.1 + §8.8.4.2.7: a group operand is class alphanumeric compared over its
        // REPRESENTATION, and §13.18.60.4 GR4/GR11 leave a binary/packed item's representation, including its
        // sign, to the implementor — COBOLNET_DESIGN §14.4 and docs/CONFORMANCE.md items 205–215 are the ONE
        // total definition).
        // ⚠ THIS COMMENT USED TO SAY "its zoned decimal digit image … with a trailing-overpunch sign", which was
        // true of the PRE-V59 image and is now false — the bytes are not digits. Corrected rather than left, since
        // a stale comment describing the old representation is exactly how the two-predicate residue below spread.
        // Only a VARIABLE-LENGTH group or a group with a pointer/object-class leaf has no image and stays loud
        // (kb/Work PB164 + R40 — there is no "Tier-C island" left for a numeric leaf: every numeric usage has a
        // pinned byte form). This is the WRITE / RELEASE / DISPLAY / compare sender path. (A pointer/object-leafed
        // group's ONE-WAY transfer image is served before this by the DISPLAY and MOVE-sender arms — kb/Work PB244;
        // this two-way path keeps refusing it, since compare needs an injective image.)
        // A BIT GROUP operates as an elementary boolean item of PICTURE 1(m) (§13.18.29.4 GR1b; D20/PB79): its
        // operand value is its BIT STRING (the subordinates' boolean positions concatenated), never the packed
        // byte image AsImage yields; a NATIONAL group's operand value IS its character image (GR2b).
        // ⛔ THIS TEST PRECEDES THE OdoGroupPlace ARM, AND THE ORDER IS THE FIX (kb/Work PB173): the ODO early
        // return used to sit above it and steal every bit group with an occurs-depending table into the CHARACTER
        // reader — two different alphabets on one operand (`IF G = B"1100"` compared packed bytes against a boolean
        // literal), and the character-unit extent it then computed was NEGATIVE for a sub-byte element, so the
        // operand rendered as the EMPTY string. `SendingBits` is the ONE bit reader and carries the GR8a
        // current-extent arm itself, so the ODO shape is served here rather than routed past.
        // ⛔ THE NATIONAL TWIN, AND IT IS THE SAME DEFECT SHAPE ONE CATEGORY OVER (kb/Work PB327). A national
        // group operates as an elementary NATIONAL item of PICTURE N(m) (§13.18.29.4 GR2b), so its operand value
        // is its m national CHARACTER positions — never AsImage()'s 2m UTF-16BE bytes, which is what AsImage()
        // became when a national leaf was admitted to the byte-addressed record codec.
        // An occurs-depending GROUP operand SENDS only the current-count part (ISO §13.18.38 GR8 — "that part of the
        // table area specified by data-name-1 at the start of the operation"); a zero count with no fixed prefix is
        // the zero-length item of §8.5.4. This is the read side of every quadrant (MOVE/compare/INSPECT/STRING/
        // UNSTRING source); the receiving direction split lives at the store sites.
        // All three kinds of group, and the GR8 current-extent arm of each, are THE ONE group value reader's
        // (PlaceRenderer.SendingGroupValue — kb/Work PB178's one-reader law, which BoundaryImageChannelTests holds).
        if (p.Item.IsGroup) return PlaceRenderer.SendingGroupValue(p);
        // ⛔ THE §15.4 TEMPORARY OF A NUMERIC FUNCTION'S RETURNED VALUE IS TEXT IN THE FUNCTION'S OWN FORM — DOC-A.1-92's
        // literal form, exactly as NumericIntrinsicText renders the un-materialized call, so a materialized function
        // (the MOVE multi-receiver hoist, an EVALUATE subject, an INVOKE argument) and the call it froze produce the
        // same characters in a character receiver (kb/Work PB1007 — the wide description's zero-padded digits did not).
        if (p.Item is { IsFunctionReturnedValue: true, Pic: { Category: PicCategory.Numeric, IsFloat: false } fvp })
            return RuntimeApi.NumFormatFunctionText(PlaceRenderer.Read(p), fvp.Scale, deSign);
        // A numeric-DISPLAY leaf stored as its character image is already a string holding the (sign-aware) image; when
        // it is the de-signed source of an alphanumeric move/compare, decode and re-emit the magnitude digits (GR6a).
        if (p.Item.StoreAsImage)
            return NonTextBytes(p, deSign, sending) is { } siBytes ? siBytes
                : deSign && p.Item.Pic is { Category: PicCategory.Numeric, Signed: true } sip
                ? PExpand(RuntimeApi.NumFormatUnsignedDisplay(RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending.FixedPointChecked()), sip.Digits), sip)
                // The StoreAsImage twin of the RedefViewPlace arm above — same rule, same one reader.
                : SendingVerbatim(p, sending);
        return p.Item.Pic switch
        {
            // ISO §14.9.25.4 GR6a: a signed numeric moved to / compared as an alphanumeric item drops its operational
            // sign — the de-signed magnitude digits (FormatUnsignedDisplay), not the zoned/overpunch image. FormatDisplay
            // already yields these for an unsigned item, so deSign on an unsigned numeric is a no-op.
            // A UInt128-carrier item (kb/Work R10) renders through the U-named lane — picked by name, never by
            // overload (an int constant converts implicitly to both Int128 and UInt128; see CobolNum.FormatDisplayU).
            { IsUnsignedWideBinary: true } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplayU({PlaceRenderer.Read(p)}, {pic.Digits})", pic)
                : RuntimeApi.NumFormatDisplay(PlaceRenderer.Read(p), p.Item.ProfileName, u: true),
            { Category: PicCategory.Numeric, IsFloat: false } pic => deSign
                ? PExpand($"CobolNum.FormatUnsignedDisplay({PlaceRenderer.Read(p)}, {pic.Digits})", pic)
                : $"CobolNum.FormatDisplay({PlaceRenderer.Read(p)}, {p.Item.ProfileName})",
            // A float item (COMP-1/2/FLOAT-*, D16): DISPLAY renders the algebraic value via CobolFloat.Display
            // (invariant-culture shortest round-trip, §14.9.11 GR1 implementor-defined) — never a bare .ToString().
            // The sending read is wrapped in CobolFloat.Sending (raises EC-DATA-NOT-FINITE for NaN/±Inf under checking,
            // §14.6.13.2 item 3) UNLESS this is an exempt context — SendingRef.FloatChecked() false.
            { Category: PicCategory.Numeric } => sending.FloatChecked()
                ? RuntimeApi.FloatDisplay(RuntimeApi.FloatSending(PlaceRenderer.Read(p)))
                : RuntimeApi.FloatDisplay(PlaceRenderer.Read(p)),
            // ⛔ THE BOOLEAN ARM IS SPLIT OUT, and the reason is the defect this whole change is about: it used to
            // ride the alphanumeric/national arm, and a shared arm cannot carry a rule that applies to only one of
            // its categories. §14.6.13.2 rule 1 makes a boolean sending item whose content is not all '0'/'1'
            // EC-DATA-INCOMPATIBLE, and `DISPLAY B` references that content exactly as `COMPUTE R = B` does — this
            // is the STRING half of the boolean chokepoint pair, the twin of BooleanRenderer's numeric-channel half
            // (kb/Work PB230). Fixing one channel and not the other is the two-arm shape, again.
            { Category: PicCategory.Boolean } => SendingVerbatim(p, sending),
            // National items are string-stored (D-N1) — the value IS the character image; an alphanumeric or
            // numeric-edited item is its own storage. None of the three is a subject of §14.6.13.2 (its rules name
            // a BOOLEAN sending item, a NUMERIC one, and a de-editing MOVE's numeric-edited SENDER — the last of
            // which is the de-edit's own site, not this text read).
            { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National } => PlaceRenderer.Read(p),
            // ⛔ NO FALL-THROUGH, AND ITS ABSENCE IS THE POINT (kb/Work PB664). The categories that reach here
            // are the four the model gives a MANAGED carrier — data-pointer, program-pointer, function-pointer
            // and object reference — and none of them has a character image: §8.5.2.1 Table 2 puts them in class
            // pointer and class object, and every statement that asks for one is refused at BIND (MOVE
            // §14.9.25.3 SR1 → COBOLNET0809; DISPLAY §14.9.11.3 SR1 → COBOLNET1694; INSPECT §14.9.22.3 SR1 and
            // UNSTRING §14.9.48.3 SR2 → COBOLNET1626/1651; STRING §14.9.43.3 SR1 → COBOLNET1626; a relation
            // §8.8.4.2.3 SR5 → COBOLNET0869). This arm used to be `Read(p).ToString()`, which turned that
            // missing screen into TEXT: `MOVE P TO A66` stored the first characters of the CLR carrier's type
            // name, and so did `STRING P …` for a year after the MOVE arm was fixed. A wrong answer is worse
            // than a crash, so the renderer now states the invariant instead of inventing a value.
            _ => throw new InvalidOperationException(
                $"no character image for category {p.Item.Pic?.Category.ToString() ?? "(none)"} on '"
                + $"{p.Item.CobolName ?? "FILLER"}' — a class pointer/object operand must be refused at bind "
                + "(ISO §8.5.2.1 Table 2); reaching the sending-image renderer is a missing syntax-rule screen"),
        };
    }

    /// <summary>The sending character image of a numeric item whose PICTURE has <c>P</c> scaling positions: the Ps
    /// are counted in the sending size and are ZEROS (ISO §13.18.40.3 symbol-P operations item b; §14.9.25.4 GR6a)
    /// — appended for trailing P (a negative scale), prepended for leading P (scale &gt; digit count). The zero
    /// runs are compile-time constants, so they concatenate as literals.</summary>
    private static string PExpand(string digitsExpr, PicInfo pic)
    {
        int trailing = pic.Scale < 0 ? -pic.Scale : 0;
        int leading = pic.Scale > pic.Digits ? pic.Scale - pic.Digits : 0;
        if (trailing > 0) return $"({digitsExpr} + \"{new string('0', trailing)}\")";
        if (leading > 0) return $"(\"{new string('0', leading)}\" + {digitsExpr})";
        return digitsExpr;
    }

    /// <summary>A numeric LITERAL's magnitude text — its own characters with the §8.3.3.3.2 rule-2 leading sign
    /// removed, which leaves exactly the "number of digits in the integer" character positions §8.8.4.2.5 asks
    /// for (leading zeros are digits and stay). The literal's own text is otherwise untouched.</summary>
    private static string DeSign(string text) =>
        text.Length > 0 && text[0] is '+' or '-' ? text[1..] : text;

    /// <summary>The operand→DISPLAY-image dispatch (PHASE-07 Step 6f). <paramref name="deSign"/> carried on the
    /// instance so the two cached instances need no per-call allocation. Each Visit is the former <c>AsString</c>
    /// switch arm verbatim.</summary>
    private sealed class AsStringVisitor(bool deSign, SendingRef sending) : IBoundOperandVisitor<string>
    {
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        // ⛔ A SIGNED numeric LITERAL de-signs exactly as a signed numeric ITEM does (kb/Work PB400). §14.9.25.4
        // GR6a — "If the sending operand is described as being signed numeric, the operational sign is not
        // moved" — is a rule about the SENDING VALUE, not about how it was written, and §8.8.4.2.5 routes a text
        // relation through those same MOVE rules, moving the integer operand to an item "of the same length in
        // terms of character positions as the NUMBER OF DIGITS in the integer" — a count the sign is not part of.
        // This arm ignored deSign while the FIELD arm beside it honoured it, so `MOVE WS-S TO WS-X` over
        // PIC S9 VALUE -5 gave "5 " and `MOVE -5 TO WS-X` gave "-5": the same value, two answers, one of them
        // wrong. It went unmeasured for as long as it did because the RELATION half of the arm was unreachable —
        // a signed literal was classified as an arithmetic expression, so it could never arrive here at all.
        public string Visit(BoundNumericLiteral n) => EmitText.CsLiteral(deSign ? DeSign(n.Text) : n.Text);
        public string Visit(BoundFieldOperand n) => FieldAsString(n.Place, deSign, sending);
        // THE CURRENT RECORD (ISO §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) — kb/Work PB339): the record area's
        // image sliced to the §13.18.43.4 GR16 byte count. deSign is moot (the operand is alphanumeric by
        // designation, never a signed numeric item), and so is the SendingRef exempt context (a record area's
        // image is characters, never a float or windowed fixed-point READ).
        public string Visit(BoundCurrentRecord n) => CurrentRecordImage(n);
        // A bare figurative is intercepted PCS-aware at AsString's ENTRY (the collating context lives on the
        // renderer, not this visitor); this arm is the unreachable native-pin fallback the visitor interface requires.
        public string Visit(BoundFigurative n) => $"new string({FigurativeConstants.Fill(n.Kind, null)}, 1)";   // DISPLAY shows one occurrence (GR3)
        public string Visit(BoundPredefinedNull n) =>
            throw new InvalidOperationException("the predefined NULL reached a character rendering: only a §8.4.3.10.3 SR1 context "
            + "binds it (ExpressionBinder.NullAdmittingOperand), and every such context renders it itself (kb/Work PB1427)");
        public string Visit(BoundAllLiteral n) => EmitText.CsLiteral(n.Literal);                          // length-unspecified: the literal once (GR3c)
        // EVERY intrinsic-result operand is intercepted at AsString's ENTRY (it needs the per-unit INSTANCE
        // renderer — P7 Step 12): alphanumeric/national/boolean through the string channel, numeric through
        // NumericIntrinsicText (DA2). What reaches this arm is a computed operand that is NOT an intrinsic — an
        // arithmetic expression in a string position, which no general format admits — so it stays loud.
        public string Visit(BoundComputedOperand n) =>
            EmitText.LoudValue("string", "computed expression in a string context");
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        // An address-identifier (kb/Work PB1021) is class pointer: the binder admits it only as a relation operand, which
        // ConditionRenderer's pointer arms render, and an INVOKE argument, which OoEmitter renders — never a
        // character or numeric value. Reaching this is a binder hole, so it is LOUD, never a guessed value.
        public string Visit(BoundAddressOperand n) => EmitText.LoudValue("string", "address-identifier as a character operand");
        // A boolean EXPRESSION operand is intercepted at AsString's ENTRY (it needs the per-unit renderer); this
        // cached visitor arm is the unreachable backstop.
        public string Visit(BoundBoolOperand n) => EmitText.LoudValue("string", $"bound operand '{nameof(BoundBoolOperand)}'");
    }

    /// <summary>The "compared as text?" predicate (PHASE-07 Step 6f) — the former <c>IsString</c> switch, with the
    /// four <c>_ => false</c> leaves now explicit.</summary>
    private sealed class IsStringVisitor : IBoundOperandVisitor<bool>
    {
        public bool Visit(BoundStringLiteral n) => true;
        public bool Visit(BoundAllLiteral n) => true;   // ALL "literal" is an alphanumeric figurative
        public bool Visit(BoundFieldOperand n) => n.Place.Item.IsGroup || n.Place.Item.Pic?.Category
            is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;
        // The current record is an ALPHANUMERIC value — §14.9.30.4 GR4 b) calls the implied move "an
        // alphanumeric group move" and §13.18.43.4 GR16 counts its size in BYTES.
        public bool Visit(BoundCurrentRecord n) => true;
        // An intrinsic result compares by its §15.2 function type: alphanumeric functions are class/category
        // alphanumeric (IF107A's `IF FUNCTION CURRENT-DATE >= TEMP1` is a STRING comparison); a boolean
        // function's '0'/'1' image compares as text likewise (§8.8.4.3 over the D-B1 substrate);
        // numeric/integer functions stay numeric operands. A computed operand that is NOT an intrinsic is not text.
        public bool Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall ic
                && ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean;
        public bool Visit(BoundFigurative n) => false;
        public bool Visit(BoundPredefinedNull n) => false;   // an identifier of class pointer / object, never a string
        public bool Visit(BoundNumericLiteral n) => false;
        public bool Visit(BoundOperandError n) => false;
        public bool Visit(BoundAddressOperand n) => false;   // class pointer — never string-carried (kb/Work PB1021)
        public bool Visit(BoundBoolOperand n) => false;
    }
}
