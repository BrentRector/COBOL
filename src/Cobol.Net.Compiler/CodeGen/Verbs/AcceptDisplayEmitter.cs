// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The ACCEPT / DISPLAY verb emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>; the DISPLAY half moved in from the orchestrator partial, making the Step-5
/// filename honest). Every runtime-member fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class AcceptDisplayEmitter(EmitContext ctx, NumericRenderer num, MoveEmitter move)
{
    /// <summary>DISPLAY (ISO §14.9.11): shows the sign-aware image (deSign defaults false — the operational sign is
    /// part of the displayed zoned representation, unlike a move to an alphanumeric receiver). The UPON device routing
    /// (§14.9.11.3 SR2 / §14.9.11.4 GR8): a mnemonic bound to SYSERR writes the standard-error stream; CONSOLE / SYSOUT
    /// and the no-UPON default use the standard display device (standard output) — the latter path is byte-identical.</summary>
    public void EmitDisplay(BoundDisplay d)
    {
        // A VARIABLE-LENGTH group operand displays in the implementor-defined format §14.9.11.4 GR7 leaves to
        // us, documented as CONFORMANCE.md A.1 item 57 (kb/Work PB164): the generated CurrentImage() — fixed
        // members by the record-image member law, dynamic members at their CURRENT extent, following the
        // §15.50.4 r7 LENGTH-sum geometry, so LENGTH(G) equals the displayed width (a NATIONAL member contributes
        // the two bytes it occupies — D-N1; kb/Work PB327 dissolved the old one-character divergence. An ODO table
        // beside a dynamic member displays at its CURRENT count, and a table of variable-length elements - OCCURS
        // DEPENDING or dynamic-capacity, in a declared group or a CELL-BACKED one (kb/Work PB244) - shows each
        // occurrence's own current image: the gate is CurrentExtentImageCapable, the one operand capability).
        // DISPLAY-ONLY by design: GR7 is a DISPLAY-statement determination, so the shared group-sender arm
        // (WRITE/RELEASE) keeps the narrower record gate (DataItem.RecordImageCapable).
        // ⛔ `vp is not RedefViewPlace`: a Tier-B class-tier VIEW's Read() is its string WINDOW — spelling
        // .CurrentImage() on it is CS1061 on `string` (the PB176 skeptic round; whether a dynamic-length
        // member under REDEFINES is even legal is kb/Work PB177's screen question — the emitter defends
        // regardless, per the GroupImage doc's own law about window shapes).
        // A CELL-BACKED variable-length group view (an EXTERNAL record, an ADDRESS-OF-taken record — kb/Work
        // PB1026) is the exception: its view composes the same current-extent image from the cell, through the
        // same dispatcher (PlaceRenderer.VarGroupCurrentImage).
        var parts = d.Operands.Select(o =>
            o is BoundFieldOperand { Place: { Item.IsGroup: true, ImageCapable: false } vp }
                && (vp is not RedefViewPlace || vp is RedefViewPlace { Coding: VarGroupWindow })
                && vp.Item.CurrentExtentImageCapable
            ? PlaceRenderer.VarGroupCurrentImage(vp, "DISPLAY of", transfer: true)
            // A strongly-typed group with a class pointer/object leaf (kb/Work PB244): a legal identifier-1
            // (§14.9.11.3 SR1 bars only an item OF class pointer/object; a strongly-typed group's class is its
            // type-name, §8.5.2.1) whose GR1 device conversion is ours to define — CONFORMANCE.md A.1 item 56:
            // its STORAGE image, each such leaf as its 8 positions (a pointer's storage image, kb/Work PB1071; an object's reserved placeholder, D-SLOT). The ONE-WAY
            // transfer reader (OperandText.AsTransferString), so comparison and read-back keep refusing a
            // non-injective, non-invertible image; an intrinsic function's string argument asks the same reader.
            : OperandText.AsTransferString(o, num, "DISPLAY of")).ToList();
        string image = parts.Count == 0 ? "\"\"" : string.Join(" + ", parts);
        string sink = d.ToStdErr ? "System.Console.Error" : "System.Console";
        ctx.Writer.Line(d.NoAdvancing ? $"{sink}.Write({image});" : $"{sink}.WriteLine({image});");
    }

    /// <summary>ACCEPT (ISO §14.9.1). Format 1 (device) is the GR1–GR4 transfer: 80-character card-image records
    /// from standard input (<c>AcceptSource.Device</c>), stored ALIGNED LEFT by SIZE — explicitly NOT the MOVE
    /// rules (JUSTIFIED is irrelevant; GR4a left-aligned fill / GR4b leftmost truncation). Format 2 (temporal) is
    /// the GR6 transfer: the clock value as a conceptual UNSIGNED INTEGER USAGE DISPLAY item of fixed width
    /// (GR7–GR12) stored BY THE MOVE RULES — the bound implicit MOVE <see cref="BoundAccept.Store"/>, which the
    /// MOVE emitter renders (kb/Work PB887). (The legacy stored temporal text left-justified-raw for every
    /// receiver — a §14.9.1.4 GR6 deviation its exact-width NIST receivers never exposed.)</summary>
    public void EmitAccept(BoundAccept a)
    {
        if (a.Kind == AcceptKind.Device) EmitAcceptDevice(a.Target);
        else EmitAcceptTemporal(a);
    }

    /// <summary>Format 1 — the device transfer (ISO §14.9.1.4 GR1–GR5). <c>AcceptSource.Device(n)</c> returns
    /// EXACTLY n characters (left-aligned, space-padded, excess ignored), so each receiver shape stores it whole:
    /// a character group distributes via <c>FromImage</c>; an elementary string field takes it directly; a native
    /// numeric receiver converts via <c>ParseDisplay</c> (GR1 — conversion is implementor-defined; ours
    /// decodes the zoned/separate-sign image per the receiver's profile, non-digits contributing no digit).</summary>
    private void EmitAcceptDevice(Place target)
    {
        var w = ctx.Writer;
        var item = target.Item;

        // A ref-modified receiver: the slice length IS the transfer size; the splice left-justifies/space-fills
        // into the slice (§8.4.3.3.4 GR6), which coincides with the GR4 left-aligned store.
        // ⛔ THE OMITTED-LENGTH WIDTH READS OperandPic, NOT RAW Pic — the THIRD arm of kb/Work PB173's pad family
        // (the other two are PlaceRenderer.Write's boolean ref-mod pad and MoveEmitter's RefModSlice figurative
        // fill). §8.4.3.3.4 GR5c: "If length is not specified, the unique data item extends from and includes the
        // position identified by leftmost-position up to and including the rightmost POSITION of the data item
        // referenced by identifier-1", and GR5a fixes what a POSITION is: "If the usage of identifier-1 is bit,
        // positions used in evaluation are bit positions". `Pic` is NULL for any group, so a BIT group fell back
        // to `ImageWidth` — ceil(m/8) PACKED CHARACTERS — and every omitted-length ACCEPT into a bit-group slice
        // transferred too few characters at EVERY start (an 8-bit group's `ACCEPT XM(3:)` computed 1 - 3 + 1 = -1;
        // its `ACCEPT XM(1:)` computed 1, storing one character into an 8-position slice and zero-filling the
        // rest). `OperandPic` is the ONE category/position reader (DataItem §D20): it answers the bit group's
        // as-if PICTURE 1(m) length, the same units RefModPlace.Category and the write-side pad already use.
        if (target is RefModPlace rm)
        {
            // The ONE ref-mod narrowing (RuntimeApi.RefModStart / RefModLength — saturating, kb/Work PB1033); the
            // omitted-length width is computed in `long` so a saturated start cannot overflow it, and floored at
            // zero — the slice write below raises EC-BOUND-REF-MOD for that start (§8.4.3.3.4 item 5c).
            string len = rm.Length is { } l
                ? RuntimeApi.RefModLength(l)
                : $"(int)System.Math.Max(0L, {rm.Inner.Item.OperandPic?.Length ?? rm.Inner.Item.ImageWidth} - (long){RuntimeApi.RefModStart(rm.Start)} + 1)";
            w.Line(PlaceRenderer.Write(target, $"AcceptSource.Device({len})"));
            return;
        }

        // ⛔ THE TRANSFER SIZE IS THE RECEIVER'S, AT EXECUTION — §14.9.1.4 GR3/GR4 are written over "the size of the
        // receiving data item", and an ANY LENGTH receiver's size is its carrier's (§13.18.2.4 GR1 b)), never the one
        // symbol its PICTURE spells (§13.18.2.3 SR1). Every arm below, the group arm included, reads this ONE
        // expression (ReceivingStore.CharacterPositions — a group's is its character positions, a bit / national
        // group's its as-if positions); each used to read the declared width, so an ANY LENGTH receiver bound to a
        // 6-character argument stored ONE character of the record (kb/Work PB1013).
        string size = ReceivingStore.CharacterPositions(target);

        if (item.IsGroup)
        {
            // A group receiver takes the device characters positionally — the ONE group-value store: an alphanumeric
            // group's character image (§14.9.25.4 GR4), and for a BIT / NATIONAL group the ELEMENTARY item of its
            // as-if PICTURE (§13.18.29.4 GR1b/GR2b) — m boolean or m national positions, converted exactly as the
            // elementary arms below convert them, NOT m characters poured through the 2m-byte / ceil(m/8)-byte
            // storage image (kb/Work PB1653: `ACCEPT NG` of "HELLO" into a national group stored five characters as
            // ten UTF-16BE bytes and read back as five CJK characters).
            string device = item.GroupUsage is GroupUsage.Bit
                ? $"AcceptSource.DeviceBoolean({size})"
                : $"AcceptSource.Device({size})";
            w.Line(PlaceRenderer.WriteGroupValue(target, device, "ACCEPT into group"));
            return;
        }

        var pic = item.Pic!;
        switch (pic)
        {
            case { Category: PicCategory.Numeric, IsFloat: false }:
                // The DEVICE window is CHARACTERS — digit count + a SIGN SEPARATE position (ISO §13.18.52
                // GR6a) — never the item's byte width: a PIC 9(4) COMP receiver reads FOUR typed digits and
                // stores TWO bytes (V59). EVERY numeric receiver converts (§14.9.1.4 GR1 — the documented
                // determination this emitter's own doc states: decode the typed characters per the
                // receiver's profile), then an image-stored/windowed receiver re-encodes through the ONE
                // byte-form recipe. ⛔ kb/Work PB180: the old image arm stored the DEVICE CHARACTERS RAW
                // ("the characters ARE the storage") — true only of the pre-V59 zoned windows; against a
                // V59 byte-form window it spliced DisplayTextWidth characters into a StorageWidth window
                // (PIC 9(4) COMP under REDEFINES: "1234" became the two bytes 31 32 = 12594).
                string image = $"AcceptSource.Device({size})";
                string value = ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(image, item.ProfileName), item);
                w.Line(item.StoreAsImage || target is RedefViewPlace
                    ? PlaceRenderer.Write(target, RuntimeApi.NumFormatImage(value, item.ProfileName))
                    : PlaceRenderer.Write(target, value));
                return;
            case { Category: PicCategory.Numeric } fpic:   // COMP-1/COMP-2/FLOAT-* — a float receiver
                // §14.9.1.4 GR1 leaves the device conversion to the implementor, and §14.9.1.3 SR1 does not
                // exclude a float receiver, so this is legal source with a DOCUMENTED conversion (CONFORMANCE.md §7
                // DOC-A.1-1; kb/Work PB887 — this arm was a run-time LoudStmt that aborted the run unit): ONE
                // record read as the inverse of the DISPLAY image (AcceptSource.DeviceFloat — the NUMVAL-F format;
                // a non-conforming record is zero). The value lands in the receiver's carrier by a plain cast of a
                // value already converted ONCE into the receiver's own format (§14.6.8.3 rule 2; kb/Work PB1110:
                // DeviceFloat(single) — a binary32 receiver is never rounded through binary64). This is the device
                // conversion, NOT a MOVE (GR1–GR4 are explicitly not the MOVE rules), so no EC-DATA-OVERFLOW; a
                // WINDOWED receiver re-encodes its IEEE window bytes.
                string fvalue = $"({fpic.ClrType})AcceptSource.DeviceFloat({(fpic.IsSingle ? "true" : "false")})";
                w.Line(PlaceRenderer.Write(target, item.StoreAsImage
                    ? RuntimeApi.NumFormatImageFloat(fvalue, item.ProfileName, fpic.IsSingle)
                    : fvalue));
                return;
            case { Category: PicCategory.Boolean }:
                // A BOOLEAN device receiver (SR1 does not exclude it): GR1's implementor-defined conversion is
                // AcceptSource.DeviceBoolean — each transferred '1' converts to boolean one, EVERY other
                // character (a '0', a pad space, any device character) to boolean zero, so the §13.18.40.4
                // GR14 '0'/'1' representation invariant holds for any input (the old default-arm raw store put
                // pad SPACES into boolean storage — kb/Work PB139).
                w.Line(PlaceRenderer.Write(target, $"AcceptSource.DeviceBoolean({size})"));
                return;
            default:
                // Alphanumeric / alphabetic / national / edited: the characters store as-is (GR3/GR4 — no
                // editing; an edited PICTURE's Length counts every mask position). For a NATIONAL receiver the
                // GR1 conversion is DEFINED as the identity on the UTF-16 character substrate — one device
                // character per national position, so Device(pic.Length) is exact (kb/Work PB139).
                // A DYNAMIC-LENGTH receiver's size is its MAXIMUM size (D-DL2), and the transferred characters
                // become its content through the ONE receiving store (§8.5.1.10.4; kb/Work PB871) — the PICTURE's
                // single symbol (§13.18.19.3 SR1) used to make every ACCEPT store one character. An ANY LENGTH
                // receiver takes exactly its carrier's length of characters (kb/Work PB1013).
                w.Line(PlaceRenderer.Write(target, item.IsDynamicLength
                    ? ReceivingStore.Characters(item, $"AcceptSource.Device({size})", "")
                    : $"AcceptSource.Device({size})"));
                return;
        }
    }

    /// <summary>Format 2 — the temporal transfer (ISO §14.9.1.4 GR6–GR12). Two steps, and only the FIRST is
    /// ACCEPT's: the clock reading fills the GR7–GR12 conceptual item (an unsigned integer of usage display, the
    /// binder's compiler temp <see cref="BoundAccept.Conceptual"/>), then GR6's transfer "according to the rules for
    /// the MOVE statement" is the BOUND implicit MOVE <see cref="BoundAccept.Store"/>, rendered by the MOVE emitter.
    /// ⛔ This method carries no receiver-category arm (kb/Work PB887): it used to write its own numeric / edited /
    /// alphanumeric / national / float / group stores — a second copy of <c>MoveEmitter.ConvertSource</c> — and
    /// <c>ImplicitMoveConstructionDriftTests.TheAcceptTemporalTransfer_IsTheMoveEmitters</c> keeps it that way.</summary>
    private void EmitAcceptTemporal(BoundAccept a)
    {
        var conceptual = a.Conceptual!;
        var item = conceptual.Item;
        string call = a.Kind switch
        {
            AcceptKind.Date => "AcceptSource.Date()",                   // GR7 — YYMMDD
            AcceptKind.DateYYYYMMDD => "AcceptSource.DateYYYYMMDD()",   // GR8 — YYYYMMDD (2002+)
            AcceptKind.Day => "AcceptSource.Day()",                     // GR9 — YYDDD
            AcceptKind.DayYYYYDDD => "AcceptSource.DayYYYYDDD()",       // GR10 — YYYYDDD (2002+)
            AcceptKind.Time => "AcceptSource.Time()",                   // GR11 — HHMMSScc
            _ => "AcceptSource.DayOfWeek()",                            // GR12 — 1=Monday … 7=Sunday
        };
        // The clock value is an unsigned integer that fits the conceptual item by construction (each AcceptSource
        // reading is at most GR7–GR12's digit count), so the fill is the plain scale-0 numeric store.
        string stored = ArithmeticEmitter.Narrow(RuntimeApi.NumStore(call, "0", item.ProfileName), item);
        ctx.Writer.Line(PlaceRenderer.Write(conceptual, item.StoreAsImage
            ? RuntimeApi.NumFormatImage(stored, item.ProfileName)
            : stored));
        move.Emit(a.Store!);
    }
}
