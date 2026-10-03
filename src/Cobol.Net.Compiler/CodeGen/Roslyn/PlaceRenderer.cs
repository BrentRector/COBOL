// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// The Roslyn backend's sole renderer of a <see cref="Place"/> lvalue to C# read/write text
/// (DESIGN-codegen-backend §2.3). Keeping ALL C# text on the backend side of the bind→emit seam is what makes the
/// bound tree backend-neutral (the G4 invariant): a <see cref="Place"/> carries STRUCTURE (an access path, subscript
/// <c>BoundExpr</c>s, a ref-mod span, resolved <see cref="DataItem"/>s), never call text, so a future CIL backend can
/// consume the same tree. Every runtime call this renderer emits routes through <see cref="RuntimeApi"/>.
/// <para>Rendering is context-free (it reads only the <see cref="Place"/>'s own structure + <see cref="RuntimeApi"/>),
/// so this is a <c>static</c> class like <see cref="RuntimeApi"/> — no <c>EmitContext</c> is threaded. It lives in
/// <c>CodeGen</c>, never <c>Binding</c>: the binder produces the structural <see cref="Place"/>, the backend renders
/// it (the binder never references this type — that would invert the layering the seam exists to enforce).</para>
/// <para><b>Migration (P7 Step 11, subtype-at-a-time).</b> Consumers are routed through <see cref="Read"/>/
/// <see cref="Write"/> FIRST (each routing is byte-identical — the shim below forwards to the legacy
/// <see cref="Place.Read"/>/<see cref="Place.Write"/>); THEN each <see cref="Place"/> subtype is converted to
/// structure one at a time, its rendering moving into an explicit arm here while the still-string subtypes keep
/// delegating. When every subtype and consumer is migrated, <see cref="Place.Read"/>/<see cref="Place.Write"/> are
/// deleted and the neutrality reflection test (DESIGN-codegen-backend §6 R5) locks the invariant.</para>
/// </summary>
internal static class PlaceRenderer
{
    /// <summary>⛔ THE ONE CODEGEN-TIME INTEGER COUNT READ — data-name-1 of an OCCURS DEPENDING table (§13.18.38.4
    /// GR7's "current number of occurrences"), a RECORD VARYING DEPENDING length, a report DEPENDING count — as the
    /// <c>long</c> the runtime positions by. A numeric item reads through its OWN profile
    /// (<c>CobolTable.Occ(x, _P_n)</c>): the count is a numeric SENDING item referenced during item identification,
    /// so §14.6.13.2 rule 2's EC-DATA-INCOMPATIBLE check applies to a count stored as its character image exactly
    /// as it does to a subscript (kb/Work PB1117 — the profile-less read decoded that image through a tolerant
    /// digit scan, so the condition was unreachable and a signed image's sign was dropped). Every emitter that
    /// needs a count calls THIS, never <c>RuntimeApi.TableOcc(Read(p), …)</c> by hand.</summary>
    public static string CountRead(Place p) =>
        RuntimeApi.TableOcc(Read(p), p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } ? p.Item.ProfileName : null);

    /// <summary>A C# expression that reads <paramref name="p"/>'s current value.</summary>
    public static string Read(Place p) => p switch
    {
        // A reference-modified slice inner(start:length) (ISO §8.4.3.3): a substring of the inner field's image.
        RefModPlace r => RuntimeApi.StrRefMod(Read(r.Inner), RmStart(r), RmLen(r), r.AllowZeroLength),
        // A NUMERIC item viewed as its character image (ISO §8.4.3.3.4 GR6 ref-mod; §13.18.45 a RENAMES span leaf):
        // the BYTES it occupies — its zoned digits for USAGE DISPLAY, its radix-2 / BCD bytes for BINARY / PACKED
        // (V59), which is what a span over it renames.
        // ⛔ A NATIVE FLOAT carrier takes the IEEE lane (kb/Work PB187): FormatImage has no float overload (the
        // runtime names the float lanes distinctly — an integer argument would be ambiguous between Int128 and
        // float), so this arm was a CS1503 on the first NATIVE float read through its image, e.g. a COMP-1
        // argument crossing to a windowed COMP-1 formal. An image-stored float is already its image.
        NumericImagePlace n => n.Inner.Item is { Pic.IsFloat: true, StoreAsImage: false }
            ? NumericRenderer.ImageOfCarrier(Read(n.Inner), n.Inner.Item)
            : RuntimeApi.NumFormatImage(Read(n.Inner), n.Inner.Item.ProfileName),
        // A GROUP viewed as its character image for reference modification (ISO §8.4.3.3.3 SR1 / §8.4.3.3.4 GR6 —
        // kb/Work PB70), READ: §8.4.3.3.4 GR5 makes the unique data item "a subset of the data item referenced by
        // identifier-1", so a ref-mod READ references the group as a SENDING operand and an occurs-depending group
        // sends only its CURRENT-count part under BOTH §13.18.38.4 GR8 arms — GR8a (data-name-1 outside) and GR8b
        // ("included in the same group and the group data item is referenced as a sending operand"). A position
        // past the count is therefore EC-BOUND-REF-MOD, never a read of the unused table area (kb/Work PB202: this
        // arm used to gate the slice on data-name-1 lying OUTSIDE and read the MAXIMUM image for a depending-inside
        // group). The receiving direction is GroupImageAs(…, Receiving) — the splice base in the Write arm below.
        GroupImagePlace g => GroupImageAs(g.Inner, AccessDir.Sending, "reference modification of group"),
        // A BIT GROUP reference-modified in BIT POSITIONS (ISO §8.4.3.3.4 GR5a; §8.4.3.3.3 SR1's last sentence;
        // §13.18.29.4 GR1b's as-if PICTURE 1(m)) — kb/Work PB173. AsBits() is the generated unpacked boolean
        // string (GroupImageCodec.EmitBitMethods, already emitted for every bit group); the RefModPlace above
        // then slices IT, so GR5a's units are structural rather than a per-site adjustment. ⛔ THROUGH THE ONE
        // BIT READER, which carries §13.18.38.4 GR8a's current-extent arm exactly as the GroupImagePlace arm
        // above does — the capability guard the image twin also carries is UNREACHABLE here and deliberately
        // absent: §13.18.29.3 SR1 bars a variable-length group from GROUP-USAGE and SR2 forces every subordinate
        // elementary item to be usage bit / category boolean, so a bit group is always image-capable.
        BitImagePlace b => SendingBits(b.Inner),
        // A NATIONAL GROUP reference-modified in NATIONAL POSITIONS (§8.4.3.3.4 GR5a's "otherwise … character
        // positions" — the item's OWN characters; §13.18.29.4 GR2b's as-if PICTURE N(m)) — kb/Work PB327. The
        // national twin of the bit arm above, through THE ONE national reader.
        NatImagePlace n => SendingNat(n.Inner),
        // A level-66 RENAMES alias (ISO §13.18.45): concatenate the spanned leaves' character images.
        RenamesPlace n => n.Leaves.Count == 1
            ? ReadRenamesLeaf(n.Leaves[0])
            : "(" + string.Join(" + ", n.Leaves.Select(ReadRenamesLeaf)) + ")",
        // An ODO group operand read plainly (not as a GR8 slice) is the struct lvalue — the inner member place.
        OdoGroupPlace o => Read(o.Inner),
        // A direct member/fixed-table access — the structural path rendered as a C# lvalue.
        MemberPlace m => RenderPath(m.Path, AccessDir.Sending),
        // A subscripted OCCURS DYNAMIC element read — the trailing DynTableSegment renders RefSending (§8.5.1.9.2).
        DynTablePlace d => RenderPath(d.Path, AccessDir.Sending),
        // A Tier-B REDEFINES view (§13.18.44): the (offset, width) character window over the class's ONE backing —
        // or, for a USAGE BIT member, its BIT window over the same backing (kb/Work PB203): §13.18.44.4 GR1 states
        // the storage association in BITS and §13.18.29.4 GR1c sends a bit group's members through §8.5.1.6.3, so
        // the member's positions are bit positions and its VALUE is the boolean carrier they hold — never the
        // characters of the byte that happens to contain them.
        RedefViewPlace { Coding: BitWindow b } v => RuntimeApi.BitsReadWindow(
            RenderPath(v.Backing, AccessDir.Sending), $"(int)({b.OffsetExpr})", b.Bits.ToString()),
        // A NATIONAL member of the class (kb/Work PB231 — RESIDUE-11): its window is still the byte window
        // above, but a national character position occupies TWO of those bytes (ISO §13.18.60.4 GR8 leaves the
        // size to the implementor; D-N1 pins two), so the read TRANSCODES the UTF-16BE pair back to the
        // member's value carrier — exactly as the bit arm reads a boolean carrier rather than the characters of
        // the byte that holds it. The offset is 0-based here (the runtime window helpers count from zero,
        // unlike the 1-based CobolString ref-mod the identity arm below uses).
        RedefViewPlace { Coding: NationalWindow n } v => RuntimeApi.NatReadWindow(
            RenderPath(v.Backing, AccessDir.Sending), $"(int)({v.OffsetExpr})", n.Positions.ToString()),
        // A POINTER-CLASS member of the class (kb/Work PB231 — the pointer third): its value is a MANAGED
        // REFERENCE, not bytes, so it rides the same storage area's SLOT at the same byte offset its reserved
        // bytes occupy. The null state is the item's own DefaultInitializer, which is how §14.9.3.4 GR9's
        // "data items of class object or class pointer in the allocated storage are initialized to null"
        // becomes a property of an unwritten slot rather than of a seeding loop. Reading the CELL (not the
        // backing string) is what makes the two halves provably one area — and it goes through the SAME cell
        // property whose GR3/GR4 deref guard the byte half already fires.
        RedefViewPlace { Coding: SlotWindow s } v => RuntimeApi.PtrSlotRead(
            RenderPath(s.Cell, AccessDir.Sending), $"(int)({v.OffsetExpr})",
            v.ViewItem.ElementType, v.ViewItem.Pic!.DefaultInitializer),
        // A DYNAMIC-LENGTH member of a cell-backed class (kb/Work PB1026): its content rides the cell's dynamic
        // slot (§8.5.1.10.3 — "located elsewhere"), so every description sharing the cell reads the same content.
        RedefViewPlace { Coding: DynSlotWindow d } => RuntimeApi.CellDynRead(RenderPath(d.Cell, AccessDir.Sending), d.Ordinal),
        // A VARIABLE-LENGTH GROUP of a cell-backed class (kb/Work PB1026): read as its contiguous image at its
        // current extent — §8.5.1.11.2, the same composition a declared group's CurrentImage() performs.
        RedefViewPlace { Coding: VarGroupWindow g } v => CellVarContiguous(v, g),
        RedefViewPlace v => ByteWindowRead(v),
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 GR15): a read-only view over the table's current capacity.
        CapacityRegisterPlace c => $"{RenderPath(c.Table, AccessDir.Sending)}.Capacity",
        // A REPORT SECTION sum counter (§13.18.54.4 GR1/GR4/GR12): RWCS engine state, read at the counter's own
        // scale and landed in the counter's own CLR carrier (kb/Work PB1666). The identity is the ENTRY's
        // ordinal, never GR5's data-name (kb/Work PB882).
        ReportSumCounterPlace s => RuntimeApi.ReportSumRead(s.ReportIndex, s.Depth, s.CounterId, s.RegisterItem.Pic!.ClrType),
        // A report's PAGE-COUNTER (ISO §8.4.3.15.4 GR1 — a temporary unsigned integer maintained per report):
        // RWCS engine state, scale 0. Reachable on the SENDING side only through a receiving place that is then
        // read back (a rounded/size-error resultant); the plain sending reference is BoundReportCounterRef, and
        // BOTH render through the one RuntimeApi spelling (kb/Work PB429).
        ReportPageCounterPlace pc => RuntimeApi.ReportCounterRead(pc.ReportIndex, pc.Depth, isPage: true),
        // The X3.23-1985 DEBUG-ITEM register / member (VCR 7.17): a read-only view over the program's __dbgItem.
        DebugRegisterPlace d => DebugRead(d.Member),
        // The predefined object reference EXCEPTION-OBJECT (ISO §8.4.3.6.4 GR1 — "references the current exception
        // object. If an exception object is not associated with the current exception, EXCEPTION-OBJECT is set to
        // null"; GR2 — "There is one instance of EXCEPTION-OBJECT in a run unit"), which IS the run unit's one
        // ExceptionState. The SAME expression SET … TO EXCEPTION-OBJECT already renders (OoEmitter), now reachable
        // from every sending position because the resolver knows the name (kb/Work PB922).
        ExceptionObjectPlace => RuntimeApi.ExceptionObjectRead,
        // A table(ALL) intrinsic argument (ISO §15.3; kb/Work PB62) is an ENUMERATION, never a single value — the
        // intrinsic argument-list renderers expand it (IntrinsicRenderer.ArgArray); reaching a read here is a
        // renderer that forgot to, and it must fail at compile time rather than emit an unbound index variable.
        TableAllPlace a => throw new System.InvalidOperationException(
            $"a table(ALL) argument over '{a.Element.Item.CobolName ?? a.Element.Item.CsName}' reached a single-value read — the argument-list renderer must enumerate it"),
        _ => throw Unhandled(p),
    };

    /// <summary>The C# read expression for a DEBUG-ITEM register member (X3.23-1985, VCR 7.17) — the C# text the
    /// structural <see cref="DebugRegisterMember"/> selector maps to (kept on the RENDERER, never in the Place).</summary>
    private static string DebugRead(DebugRegisterMember m) => m switch
    {
        DebugRegisterMember.Item => "__dbgItem.Image",
        DebugRegisterMember.Line => "__dbgItem.DebugLine",
        DebugRegisterMember.Name => "__dbgItem.DebugName",
        DebugRegisterMember.Sub1 => "__dbgItem.DebugSub1",
        DebugRegisterMember.Sub2 => "__dbgItem.DebugSub2",
        DebugRegisterMember.Sub3 => "__dbgItem.DebugSub3",
        DebugRegisterMember.Contents => "__dbgItem.DebugContents",
        _ => throw new System.InvalidOperationException($"unknown DEBUG-ITEM member '{m}'"),
    };

    /// <summary>A C# statement (with trailing <c>;</c>) that stores <paramref name="rhs"/> into <paramref name="p"/>.</summary>
    public static string Write(Place p, string rhs) => p switch
    {
        // Splice the new slice back into the inner field, preserving its width. A BOOLEAN receiver pads with
        // boolean-zero (§14.6.8.6; §8.4.3.3 GR5a); every other category keeps the space fill.
        // ⛔ THE PAD READS OperandPic, NOT RAW Pic (kb/Work PB173 — one of the TWO arms of this defect; the other
        // is MoveEmitter's RefModSlice figurative fill, nine files away, which made the identical raw-`Pic` read).
        // `Pic` is NULL for any GROUP, so a BIT-GROUP slice padded with SPACE where §14.6.8.6 requires the value
        // be "transferred … into the corresponding boolean positions of the receiving data item, with zero fill
        // or truncation to the right". `OperandPic` is the ONE category reader — it answers the bit group's as-if
        // PICTURE 1(m) (Boolean), which is what RefModPlace.Category on the very next line of Place.cs already
        // used; D20's consumer-switch sweep named that site and omitted these two, and that omission IS the bug.
        RefModPlace r => Write(r.Inner, RuntimeApi.StrSpliceInto(SpliceBase(r.Inner), RmStart(r), RmLen(r), rhs,
            r.Inner.Item.OperandPic is { Category: PicCategory.Boolean } ? "'0'" : null, allowZeroLength: r.AllowZeroLength)),
        // Decode the spliced image back into the typed field (via the FormatImage/StoreImage pair — the same
        // bytes the read produced, so a splice round-trips whatever the item's byte form is).
        // The float twin of the read arm above (kb/Work PB187) — the IEEE decode, narrowed to the carrier type.
        NumericImagePlace n => n.Inner.Item is { Pic.IsFloat: true, StoreAsImage: false }
            ? Write(n.Inner, NumericRenderer.CarrierOfImage(rhs, n.Inner.Item))
            : Write(n.Inner, RuntimeApi.NumStoreImage(rhs, n.Inner.Item.ProfileName, Read(n.Inner))),
        // The spliced group image goes back through the ONE group-image store (kb/Work PB70).
        GroupImagePlace g => WriteGroupImage(g.Inner, rhs, "reference modification into group"),
        // The spliced BOOLEAN string goes back through the generated FromBits, which distributes the boolean
        // positions to the subordinates (kb/Work PB173 — the receiving twin of the BitImagePlace read arm; the
        // same distribution the `MemberPlace { Item.IsAsIfElementary: true }` bit arm below performs for a whole
        // bit-group receiver, now reached through the ref-mod slice as well). ⛔ THROUGH THE ONE BIT WRITER,
        // which carries §13.18.38.4 GR8's DependingInside direction split exactly as WriteGroupImage does.
        BitImagePlace b => WriteBits(b.Inner, rhs),
        // The spliced NATIONAL string goes back through the generated FromNat (kb/Work PB327 — the receiving twin
        // of the NatImagePlace read arm, and the national twin of the bit arm above).
        NatImagePlace n => WriteNat(n.Inner, rhs),
        RenamesPlace n => WriteRenames(n, rhs),
        // A WHOLE bit-group receiver that also carries an occurs-depending table takes GR8's direction split
        // through the ONE bit writer — this arm must precede the plain ODO unwrap below, which would otherwise
        // hand the MemberPlace arm the MAXIMUM length for a GR8a (depending-outside) receiver (kb/Work PB173).
        OdoGroupPlace { Item: { IsAsIfElementary: true, GroupUsage: GroupUsage.Bit } } o => WriteBits(o, rhs),
        // The national twin, same reason: the ODO direction split belongs to the NATIONAL writer, not to the
        // byte-image one (kb/Work PB327).
        OdoGroupPlace { Item: { IsAsIfElementary: true, GroupUsage: GroupUsage.National } } o => WriteNat(o, rhs),
        OdoGroupPlace o => Write(o.Inner, rhs),
        // A bit / national GROUP receiving as an ELEMENTARY item (§13.18.29.4 GR1b/GR2b; D20/PB79): the value is
        // its as-if picture's string — the bit string for a bit group (the ONE bit writer's FromBits distributes
        // the boolean positions to the subordinates), the national character image for a national group (the ONE
        // group-image store). ⛔ BOTH structural shapes (kb/Work PB1411's sweep): an element of a dynamic-capacity
        // table that is a bit / national group is a DynTablePlace, and it used to fall to the plain dynamic-table
        // assignment below — `RefReceiving(i) = "<bits>"`, CS0029 on legal source. The writers reach it through
        // GroupTarget's RefReceiving, so the element is created (§8.5.1.9.3) and then distributed.
        Place { Item.IsAsIfElementary: true } m when m is MemberPlace or DynTablePlace => m.Item.GroupUsage is GroupUsage.Bit
            ? WriteBits(m, rhs)
            // ⛔ THE NATIONAL WRITER, not the byte-image one (kb/Work PB327): a national group's as-if PICTURE
            // N(m) value is m NATIONAL POSITIONS, and its image is the 2m BYTES they occupy.
            : WriteNat(m, rhs),
        // A member/fixed-table store — the structural path as an assignment target.
        MemberPlace m => $"{RenderPath(m.Path, AccessDir.Sending)} = {rhs};",
        // A dynamic-table store uses RefReceiving, which grows-and-seeds past the current capacity (§8.5.1.9.3).
        DynTablePlace d => $"{RenderPath(d.Path, AccessDir.Receiving)} = {rhs};",
        // Splice the member's BOOLEAN positions back into the class's ONE backing, leaving every other bit of it
        // untouched (kb/Work PB203). ⛔ Bit-granular splicing is REQUIRED, not an optimization: §8.5.1.6.3 puts two
        // same-level bit members at successive bit positions of the SAME byte, so a byte-granular store through
        // either would silently clobber the other. The value is first stored to exactly the member's boolean
        // position count with §14.6.8.6's boolean-ZERO fill.
        RedefViewPlace { Coding: BitWindow b } v => $"{RenderPath(v.Backing, AccessDir.Sending)} = " +
            $"{RuntimeApi.BitsWriteWindow(RenderPath(v.Backing, AccessDir.Sending), $"(int)({b.OffsetExpr})", RuntimeApi.StrStoreBoolean(rhs, b.Bits.ToString(), justifiedRight: false))};",
        // Splice the member's NATIONAL positions back into the class's ONE backing as UTF-16BE byte pairs,
        // leaving every other byte untouched (kb/Work PB231; §13.18.44.4 GR1 — one storage area, so a write
        // through one view must not disturb another's bytes). The receiving twin of the national read arm; the
        // fit to exactly the member's position count happens in POSITIONS inside the runtime helper, because
        // padding the BYTES would manufacture U+2020 characters instead of national spaces.
        RedefViewPlace { Coding: NationalWindow n } v => $"{RenderPath(v.Backing, AccessDir.Sending)} = " +
            $"{RuntimeApi.NatWriteWindow(RenderPath(v.Backing, AccessDir.Sending), $"(int)({v.OffsetExpr})", n.Positions.ToString(), rhs)};",
        // Store a POINTER-CLASS member into the area's MANAGED SLOT (kb/Work PB231) — the receiving twin of the
        // slot read. The BYTE image is deliberately untouched: the member's bytes there are reserved
        // placeholders, so a write through one description cannot disturb another view's characters
        // (§13.18.44.4 GR1 — one storage area, and every other member's positions are its own).
        RedefViewPlace { Coding: SlotWindow s } v =>
            $"{RuntimeApi.PtrSlotWrite(RenderPath(s.Cell, AccessDir.Sending), $"(int)({v.OffsetExpr})", rhs)};",
        // Store a DYNAMIC-LENGTH member's new content into the cell's dynamic slot (kb/Work PB1026) — the receiving
        // twin of the slot read. rhs already carries §8.5.1.10.4's receiving rule (CobolDynString.Store).
        RedefViewPlace { Coding: DynSlotWindow d } => $"{RuntimeApi.CellDynWrite(RenderPath(d.Cell, AccessDir.Sending), d.Ordinal, rhs)};",
        // A VARIABLE-LENGTH GROUP of a cell-backed class receiving a character value (kb/Work PB1026): the value is
        // a contiguous image (§8.5.1.11.2), decomposed by the ONE take step determination D-FRA states.
        RedefViewPlace { Coding: VarGroupWindow g } v => $"{RuntimeApi.CellVarStoreContiguous(RenderPath(g.Cell, AccessDir.Sending),
            $"(int)({v.OffsetExpr})", v.Width, g.DynBase, g.DynFixedAt, g.DynMax, g.DynFixedAt.Select(_ => 0), rhs)};",
        // Splice the new image back into the class's ONE backing, preserving its full width (§13.18.44).
        RedefViewPlace v => ByteWindowWrite(v, rhs),
        // Unreachable: SET Format 14 routes to BoundSetCapacity, and any other store into the CAPACITY register is
        // rejected COBOLNET1523 at bind time (§13.18.38 SR30–32). The backstop for a receiver path that forgot the gate.
        CapacityRegisterPlace => throw new System.InvalidOperationException(
            "the CAPACITY register is set only by SET Format 14 (ISO §13.18.38.3 SR30-32); a direct store must be "
            + "rejected COBOLNET1523 at bind time and never reach PlaceRenderer.Write"),
        // A REPORT SECTION sum counter as a RECEIVER — ISO §13.18.54.4 GR12: "It is permissible for procedure
        // division statements to alter the content of sum counters." The store goes to the RWCS engine, at the
        // counter's own scale (GR1); there is no storage to write (kb/Work PB840).
        ReportSumCounterPlace s => RuntimeApi.ReportSumWrite(s.ReportIndex, s.Depth, s.CounterId, rhs),
        // A report's PAGE-COUNTER as a RECEIVER — ISO §8.4.3.15.3 SR1 admits it wherever an integer data item
        // may appear, and SR3 bars only LINE-COUNTER from the receiving side. The store goes to the RWCS engine;
        // there is no storage to write (kb/Work PB429).
        ReportPageCounterPlace pc => RuntimeApi.ReportPageCounterWrite(pc.ReportIndex, pc.Depth, rhs),
        // Unreachable: a COBOL program never assigns to a DEBUG-* register (X3.23-1985 — the runtime populates it via
        // the injected debug trigger); a receiving-position use is rejected at bind time. The backstop for a
        // receiver path that forgot the gate.
        DebugRegisterPlace => throw new System.InvalidOperationException(
            "the X3.23-1985 DEBUG-ITEM register is read-only (the debug facility populates it); a store must be "
            + "rejected at bind time and never reach PlaceRenderer.Write"),
        // Unreachable by RULE: ISO §8.4.3.6.3 SR1 — "EXCEPTION-OBJECT shall not be specified as a receiving
        // operand" — is screened at the ONE receiving chokepoint (ExpressionBinder.ResolveReceiving) and at SET's
        // own receiver list (OoBinder). The backstop for a receiver path that forgot the gate (kb/Work PB922).
        ExceptionObjectPlace => throw new System.InvalidOperationException(
            "EXCEPTION-OBJECT shall not be specified as a receiving operand (ISO §8.4.3.6.3 SR1); a store must be "
            + "rejected at bind time and never reach PlaceRenderer.Write"),
        _ => throw Unhandled(p),
    };

    // Unreachable: every concrete Place subtype has an explicit arm above (there is no Place.Read()/Write() to fall
    // back to since the structural-Place migration completed). A new subtype without an arm trips this at run time.
    private static System.InvalidOperationException Unhandled(Place p) =>
        new($"CodeGen.PlaceRenderer has no arm for Place subtype '{p.GetType().Name}'");

    /// <summary>Render an <see cref="AccessPath"/> to a C# lvalue expression — a static/instance root field, then
    /// <c>.Member</c> access, <c>CobolTable.At(path, index)</c> for a fixed OCCURS, and <c>RefSending</c>/
    /// <c>RefReceiving</c> for an OCCURS DYNAMIC level (the accessor chosen from <paramref name="dir"/>: a read
    /// sends, a write receives). A subscript INDEX is the D10 transitional string carried on the table segment.</summary>
    public static string RenderPath(AccessPath ap, AccessDir dir)
    {
        string path = "";
        foreach (var seg in ap.Segments)
            path = seg switch
            {
                // A formal parameter's root renders through its *-ARG-OMITTED guard (kb/Work PB971).
                RootFieldSegment r => r.Guard is { } g ? g.Render(r.CsField) : r.CsField,
                MemberSegment m => path + "." + m.CsMember,
                // An OCCURS DEPENDING level carries §13.18.38.4 GR7's reference-time bound check when the
                // compilation group can enable EC-BOUND-ODO (kb/Work PB1268).
                FixedTableSegment { Odo: { } o } f => RuntimeApi.TableAtOdo(path, f.OneBasedIndex,
                    CountRead(o.Depending), o.MinOccurs, o.MaxOccurs),
                FixedTableSegment f => RuntimeApi.TableAt(path, f.OneBasedIndex),
                DynTableSegment d => $"{path}.{(dir == AccessDir.Sending ? "RefSending" : "RefReceiving")}({d.OneBasedIndex})",
                _ => path,
            };
        return path;
    }

    // A Tier-B view's 1-based window start = the 0-based offset expression + 1 (OffsetExpr is the D10 transitional string).
    private static string RvOffset(RedefViewPlace v) => $"(int)({v.OffsetExpr} + 1)";

    /// <summary>The BYTE window of a Tier-B view — its storage as one character per byte of the class's one backing,
    /// whatever the coding of its VALUE (§13.18.44.4 GR1: the same storage area, seen as bytes). The identity-coded
    /// member's value IS this string; a national GROUP's value is not (its m positions ride 2m of these bytes,
    /// <see cref="NationalWindow"/>), but its STORAGE image — what a file record, a CALL boundary copy or a group
    /// MOVE deposits positionally — still is (kb/Work PB1653).</summary>
    private static string ByteWindowRead(RedefViewPlace v) =>
        RuntimeApi.StrWindow(RenderPath(v.Backing, AccessDir.Sending), RvOffset(v), v.Width.ToString());

    /// <summary>The receiving twin of <see cref="ByteWindowRead"/>: splice <paramref name="image"/> into the class's
    /// ONE backing at the view's window, preserving its full width (§13.18.44).</summary>
    private static string ByteWindowWrite(RedefViewPlace v, string image) =>
        $"{RenderPath(v.Backing, AccessDir.Sending)} = " +
        $"{RuntimeApi.StrWindowInto(RenderPath(v.Backing, AccessDir.Sending), RvOffset(v), v.Width.ToString(), image)};";

    /// <summary>The contiguous image of a cell-backed variable-length group view (kb/Work PB1026).</summary>
    private static string CellVarContiguous(RedefViewPlace v, VarGroupWindow g) =>
        RuntimeApi.CellVarContiguous(RenderPath(g.Cell, AccessDir.Sending), $"(int)({v.OffsetExpr})", v.Width,
            g.DynBase, g.DynFixedAt);

    // The reference-modification start/length are `long`-valued expressions but the runtime takes `int` positions —
    // cast at the call site. Start/Length are the P5.11/D10 TRANSITIONAL string carrier (a rendered index expression);
    // they become BoundExpr when D10 removes the SUBSCRIPT lexer mode (PHASE 15) — see the PHASE-07 Step 11 plan.
    // The cast and the OMITTED-length sentinel are RuntimeApi's (the ONE definition, shared with the ref-modified
    // FUNCTION RESULT channel in IntrinsicRenderer — the two must render identical positions).
    private static string RmStart(RefModPlace r) => RuntimeApi.RefModStart(r.Start);
    private static string RmLen(RefModPlace r) => RuntimeApi.RefModLength(r.Length);

    /// <summary>Store into a multi-leaf RENAMES alias (ISO §13.18.45): store the value at the span width, then
    /// distribute the slices back into the leaves left to right (a write through the alias shows through every
    /// renamed item and vice versa — no second storage).</summary>
    private static string WriteRenames(RenamesPlace n, string rhs)
    {
        if (n.Leaves.Count == 1) return Write(n.Leaves[0], StorageToLeafCharacters(n.Leaves[0], rhs, n.Widths[0]));
        int width = n.Widths.Sum();   // each part's width — a partial part's is its slice, not the item's (kb/Work PB96)
        var sb = new System.Text.StringBuilder();
        sb.Append($"{{ string __ren = {RuntimeApi.StrStore(rhs, width.ToString())};");
        int off = 0;
        for (int i = 0; i < n.Leaves.Count; i++)
        {
            int w = n.Widths[i];
            sb.Append(' ').Append(Write(n.Leaves[i], StorageToLeafCharacters(n.Leaves[i], $"__ren.Substring({off}, {w})", w)));
            off += w;
        }
        return sb.Append(" }").ToString();
    }

    /// <summary>A spanned RENAMES leaf cell is of class national when the cell, or the slice of it, is — then its
    /// storage is UTF-16BE byte pairs (D-N1), two bytes per character position.</summary>
    private static bool IsNationalRenamesLeaf(Place leaf) =>
        leaf is RefModPlace r ? r.Category is PicCategory.National : leaf.Item.Pic is { Usage: Usage.National };

    /// <summary>One spanned leaf cell's contribution to the alias's image, which is a string of STORAGE bytes
    /// (§13.18.45.4 GR2 — the alias is an alphanumeric group over the record's storage): the cell's characters, a
    /// national cell serialized as its byte pairs — the same <c>CobolBits.NatBytes</c> every other storage image
    /// uses (kb/Work PB1665).</summary>
    private static string ReadRenamesLeaf(Place leaf) =>
        IsNationalRenamesLeaf(leaf) ? RuntimeApi.NatBytes(Read(leaf)) : Read(leaf);

    /// <summary>The inverse of <see cref="ReadRenamesLeaf"/> for the write through the alias: the leaf cell's
    /// characters out of its <paramref name="bytes"/>-wide slice of the alias's image
    /// (<c>CobolBits.NatReadWindow</c>, the one inverse of <c>NatBytes</c>).</summary>
    private static string StorageToLeafCharacters(Place leaf, string imageSlice, int bytes) =>
        IsNationalRenamesLeaf(leaf)
            ? RuntimeApi.NatReadWindow(imageSlice, "0", $"{bytes / RuntimeApi.BytesPerNational}")
            : imageSlice;

    /// <summary>A figurative-constant store into a reference-modified slice: <paramref name="seed"/> — a C# string
    /// expression holding one fill character, or the literal of <c>ALL literal-1</c> — is repeated character by
    /// character over EVERY position of the slice (ISO §8.3.3.6.4 GR2, sized against the §8.4.3.3.4 GR5 data item the
    /// reference modification denotes; kb/Work PB1655 — an ALL literal used to be spliced ONCE and space-padded).
    /// The slice's length is a run-time value, so the repetition is the runtime's (<c>SpliceInto(repeat: true)</c>).
    /// Threads <c>AllowZeroLength</c> exactly like the <see cref="Write"/> RefModPlace arm (review V31: omitting it
    /// made a figurative MOVE into a zero-length slice spuriously raise fatal EC-BOUND-REF-MOD under
    /// <c>&gt;&gt;REF-MOD-ZERO-LENGTH ON</c> — §8.4.3.3.4 GR5c allows the zero-length result, and §14.9.25.4 GR1
    /// makes the zero-length MOVE receiver a no-op, never a raise).</summary>
    public static string WriteFill(RefModPlace p, string seed) =>
        Write(p.Inner, RuntimeApi.StrSpliceInto(SpliceBase(p.Inner), RmStart(p), RmLen(p), seed,
            allowZeroLength: p.AllowZeroLength, repeat: true));

    /// <summary>The BASE a reference-modified RECEIVER splices into — the inner place read in the RECEIVING
    /// direction (kb/Work PB202). A ref-mod receiver's group is a §13.18.38.4 GR8 RECEIVING operand, so a
    /// depending-INSIDE occurs-depending group presents its MAXIMUM length (GR8b's second sentence — "If the
    /// group is a receiving operand, the maximum length of the group will be used"), while a depending-OUTSIDE
    /// one presents its current extent (GR8a) and its store is the ReceiveInto splice. Reading the base with the
    /// SENDING reader instead truncates a depending-inside group to its current extent, and the maximum-length
    /// store then writes that short image back over the whole group — the receiving twin of the read defect,
    /// on all three channels (image, bit, national).</summary>
    private static string SpliceBase(Place inner) => inner switch
    {
        GroupImagePlace g => GroupImageAs(g.Inner, AccessDir.Receiving, "reference modification into group"),
        BitImagePlace b => BitsAs(b.Inner, AccessDir.Receiving),
        NatImagePlace n => NatAs(n.Inner, AccessDir.Receiving),
        _ => Read(inner),
    };

    /// <summary>
    /// ⛔ THE ONE STORE OF A CHARACTER IMAGE INTO A GROUP RECEIVER (ISO §14.9.25.4 GR4 — a group receiver is
    /// "filled without consideration for the individual elementary or group items", no conversion): the generated
    /// <c>FromImage</c> distributes the image into the leaves; an occurs-depending group with data-name-1 OUTSIDE
    /// takes only its CURRENT-count part (§13.18.38 GR8a — the GR8a splice, positions past the count unmodified;
    /// data-name-1 INSIDE uses the maximum length, GR8b — the plain FromImage); a Tier-B REDEFINES view's image IS
    /// its character window; a group nested under an OCCURS DYNAMIC level distributes through the RECEIVING accessor
    /// (RefReceiving grows-and-seeds past the current capacity, §8.5.1.9.3 — never RefSending, which drops an
    /// out-of-capacity write into scratch). A variable-length group or a group with a pointer/object-class leaf has no image
    /// (<see cref="DataItem.IsImageCapable"/>; kb/Work PB164 + R40) and stays the loud Tier-C island (COBOLNET_DESIGN §4.2) —
    /// <paramref name="context"/> names the verb in that message.
    /// <para>V59 residue (DA5), written ONCE: this is a POSITIONAL character transfer INTO the group's storage — the
    /// same job a group MOVE does — so a COMP / PACKED group is admitted (its leaves decode their image slices) and
    /// "BYTES ARE NOT TEXT" does not apply (that rule governs RENDERING a COMP leaf's value as text, not writing
    /// characters positionally over its bytes). §14.9.43.4 GR3a makes STRING's transfer the alphanumeric MOVE
    /// rules, §14.9.22.3 SR1 names "an alphanumeric or national group item" a valid INSPECT identifier-1, ACCEPT
    /// and UNSTRING store by the same MOVE rules — so every verb that deposits an image into a group calls THIS
    /// (kb/Work PB70 removed five copies of the rule, each carrying this paragraph).</para>
    /// </summary>
    /// <summary>The FULL-image store into a group place — a STORAGE-BOUNDARY write (a file record area receiving a
    /// READ, a FILE STATUS group, the CALL formal's copy-in / copy-out), never the §13.18.38 GR8 sending/receiving
    /// slice: an occurs-depending wrapper is unwrapped and its inner takes the whole image (kb/Work PB80 — an FD
    /// record holding an ODO table is now an <see cref="OdoGroupPlace"/> over a class-tier window, and a
    /// <c>Read(record).FromImage(…)</c> spelled at the call site was CS1061 on the string).</summary>
    public static string WriteFullGroupImage(Place group, string image, string context) =>
        group is OdoGroupPlace o ? WriteFullGroupImage(o.Inner, image, context) : WriteGroupImage(group, image, context);

    public static string WriteGroupImage(Place group, string image, string context) => group switch
    {
        // A NATIONAL GROUP view's STORAGE image is its 2m-byte window; its Write is the m-position VALUE store
        // (kb/Work PB1653 — WriteGroupValue is the value channel).
        RedefViewPlace { Coding: NationalWindow, ViewItem.IsGroup: true } v => ByteWindowWrite(v, image),
        RedefViewPlace => Write(group, image),
        // A level-66 THROUGH alias is an alphanumeric GROUP item (ISO §13.18.45.4 GR2), so a §14.9.25.4 GR4
        // group-image store can land on one; distributing it into the spanned leaves IS the WriteRenames store
        // the Write switch already owns, never a second FromImage (kb/Work PB430).
        RenamesPlace => Write(group, image),
        _ when !group.ImageCapable => EmitText.LoudStmt(TierCIsland.Reason(group.Item, context)),   // the OPERAND's capability (kb/Work PB189)
        OdoGroupPlace odo when UsesCurrentExtent(odo, AccessDir.Receiving) => ReceiveInto(odo, image),   // GR8a
        OdoGroupPlace odo => WriteGroupImage(odo.Inner, image, context),   // GR8b — the maximum length, whatever the inner's storage shape
        _ => $"{GroupTarget(group)}.FromImage({image});",
    };

    /// <summary>⛔ THE ONE RECEIVER OF A GROUP-LEVEL STORE (kb/Work PB1411) — the struct a generated
    /// <c>FromImage</c> / <c>FromVarImage</c> / <c>FromContiguousImage</c> / <c>FromBits</c> / <c>FromNat</c> is
    /// called on. A group that is (or lies beneath) an element of a dynamic-capacity table is reached through the
    /// RECEIVING accessor: §8.5.1.9.3 — "When a data item in a dynamic-capacity table is referenced as a receiving
    /// item and the value of the subscript exceeds the current capacity of the table, a new element is
    /// automatically created and the capacity of the table is increased to the value given by the subscript" — so
    /// <c>RefReceiving</c> grows-and-seeds, where <see cref="Read"/>'s <c>RefSending</c> hands back benign scratch
    /// and the store is silently discarded. Every group writer used to spell its target as <c>Read(group)</c> and
    /// only <see cref="WriteGroupImage"/> carried a dynamic-table arm (the two-arm dispatch shape): a
    /// variable-length group element (<see cref="WriteVarGroupImage"/>) and a bit / national group element
    /// (<see cref="WriteBits"/> / <see cref="WriteNat"/>) never grew the table. <c>GroupStoreTargetDriftTests</c>
    /// holds every group writer to this one target.</summary>
    private static string GroupTarget(Place group) =>
        group is DynTablePlace dyn ? RenderPath(dyn.Path, AccessDir.Receiving) : Read(group);

    /// <summary>The character IMAGE of a group place, whatever its storage shape (kb/Work PB80): a record-struct
    /// group's generated <c>AsImage()</c>; a Tier-B / BASED class-tier window's <c>Read</c> (already the string
    /// window); an occurs-depending wrapper's inner image (the wrapper's own <see cref="SendingImage"/> is the GR8
    /// slice of this). THE ONE reader — a consumer that spells <c>.AsImage()</c> itself is wrong for the window shape.
    /// <para>⛔ Capability-guarded like its write twin <see cref="WriteGroupImage"/>, same arm order (kb/Work
    /// PB176 — the SEVENTH two-arm-dispatch instance: the write side staged the Tier-C loud while this read
    /// side emitted <c>.AsImage()</c> unconditionally, so a group with BOTH an OCCURS DEPENDING table and a
    /// dynamic member — legal source whose struct never receives AsImage — failed BACKEND compilation with
    /// CS1061 instead of the documented runtime loud).</para>
    /// <para><paramref name="context"/> names the operation in the Tier-C message, mirroring
    /// <see cref="WriteGroupImage"/>'s parameter of the same name — so a consumer can route through THE ONE
    /// reader and still keep its own site-specific reason (kb/Work PB178: the alternative, a consumer
    /// spelling <c>.AsImage()</c> itself to preserve its message, is exactly the copy this law forbids).</para></summary>
    /// <param name="transfer">True for a ONE-WAY TRANSFER consumer (DISPLAY, a MOVE's sending group - kb/Work
    /// PB244): the guard then asks <see cref="Place.TransferImageCapable"/>, which also admits a group with a class
    /// pointer/object leaf (its reserved placeholder positions). Comparison and every read-back leave it false.</param>
    public static string GroupImage(Place group, string context = "whole-group image of", bool transfer = false) => group switch
    {
        RedefViewPlace { Coding: NationalWindow, ViewItem.IsGroup: true } v => ByteWindowRead(v),   // the storage image — see ByteWindowRead (kb/Work PB1653)
        RedefViewPlace => Read(group),
        // The READ twin of WriteGroupImage's RenamesPlace arm (kb/Work PB907): a level-66 THROUGH alias is an
        // alphanumeric GROUP item (ISO §13.18.45.4 GR2) whose image IS the composed span string Read renders —
        // it has no record struct, so `.AsImage()` on it would not compile.
        RenamesPlace => Read(group),
        // ⛔ UNWRAP BEFORE THE GUARD (the PB176 skeptic round): an OdoGroupPlace may wrap a Tier-B
        // RedefViewPlace whose valid image IS the string window — a wrapper-first guard turned that
        // working read into a loud throw (CallStringRead hands the WRAPPER in, unlike the other callers).
        // The MemberPlace inner still meets the guard below with the exact predicate, so the CS1061 fix
        // holds: guard fires ⟺ the struct has no AsImage.
        OdoGroupPlace o => GroupImage(o.Inner, context, transfer),
        _ when !(transfer ? group.TransferImageCapable : group.ImageCapable) =>   // the OPERAND's capability (kb/Work PB189; PB244)
            EmitText.LoudValue("string", TierCIsland.Reason(group.Item, context)),
        _ => $"{Read(group)}.AsImage()",
    };

    /// <summary>⛔ THE ONE READER OF A VARIABLE-LENGTH GROUP'S ACTIVATION-BOUNDARY CARRIER (ISO §8.5.1.12;
    /// kb/Work PB204) — the <see cref="GroupImage"/> twin for a group that has no fixed record window. Same arm
    /// order and the same capability law: unwrap first, then the guard, so the guard fires exactly when the
    /// struct has no <c>AsVarImage</c> (<see cref="DataItem.CurrentExtentImageCapable"/> is what
    /// <c>RecordStructEmitter</c> emits it for), and the Tier-C loud is the ONE reason source.</summary>
    public static string VarGroupImage(Place group, string context = "activation-boundary image of") => group switch
    {
        OdoGroupPlace o => VarGroupImage(o.Inner, context),
        RedefViewPlace { Coding: VarGroupWindow g } v => RuntimeApi.CellVarCarrier(RenderPath(g.Cell, AccessDir.Sending),
            $"(int)({v.OffsetExpr})", v.Width, g.DynBase, g.DynFixedAt.Count),   // kb/Work PB1026
        _ when !group.Item.CurrentExtentImageCapable =>
            EmitText.LoudValue(RuntimeApi.VarGroupType, TierCIsland.Reason(group.Item, context)),
        _ => $"{Read(group)}.AsVarImage()",
    };

    /// <summary>The WRITE half of <see cref="VarGroupImage"/> — the §14.2.3 GR8 copy-back at an activation
    /// boundary, distributing the carrier's fixed run and its variable-length components back into the group's
    /// own members.</summary>
    public static string WriteVarGroupImage(Place group, string value, string context) => group switch
    {
        OdoGroupPlace o => WriteVarGroupImage(o.Inner, value, context),
        RedefViewPlace { Coding: VarGroupWindow g } v => $"{RuntimeApi.CellVarStoreCarrier(RenderPath(g.Cell, AccessDir.Sending),
            $"(int)({v.OffsetExpr})", v.Width, g.DynBase, g.DynMax, value)};",   // kb/Work PB1026
        _ when !group.Item.CurrentExtentImageCapable => EmitText.LoudStmt(TierCIsland.Reason(group.Item, context)),
        _ => $"{GroupTarget(group)}.FromVarImage({value});",
    };

    /// <summary>⛔ A VARIABLE-LENGTH GROUP'S CONTIGUOUS IMAGE at its current extent — ISO §8.5.1.11.2: "a
    /// variable-length data item behaves in all respects as though it were in fact contiguous with its neighbors
    /// whenever a procedural operation is applied to a group containing it". The generated <c>CurrentImage()</c>
    /// (the A.1 item 57 composer DISPLAY already uses), behind the same capability guard and unwrap as
    /// <see cref="VarGroupImage"/>. It is what a WRITE / REWRITE / RELEASE of a variable-length RECORD sends
    /// (determination D-FRA; kb/Work PB981).</summary>
    public static string VarGroupCurrentImage(Place group, string context) => group switch
    {
        OdoGroupPlace o => VarGroupCurrentImage(o.Inner, context),
        RedefViewPlace { Coding: VarGroupWindow } v => Read(v),   // the cell composition (kb/Work PB1026)
        _ when !group.Item.CurrentExtentImageCapable =>
            EmitText.LoudValue("string", TierCIsland.Reason(group.Item, context)),
        _ => $"{Read(group)}.CurrentImage()",
    };

    /// <summary>⛔ THE ONE §8.5.1.12 CARRIER OF A GROUP PAIRED WITH A VARIABLE-LENGTH GROUP — for every operation
    /// that takes a compatible pair component by component: the §14.9.25.4 GR9 MOVE's sending operand and both
    /// operands of the §8.8.4.2.17 comparison (kb/Work PB1467 moved it here from the MOVE emitter so the two
    /// cannot decompose a group differently). A variable-length group is its own composer; a FIXED-length group is
    /// its record image decomposed at the spans of ITS tables that correspond to <paramref name="other"/>'s
    /// dynamic-capacity tables (<c>VariableLengthCompatibility.CorrespondingSpans</c> — the PAIR's
    /// correspondence, §8.5.1.12.2; kb/Work PB965). Null when this implementation cannot compose the group's
    /// current extent (the caller emits its named loud). <paramref name="variableContext"/> /
    /// <paramref name="fixedContext"/> name the operand in a run-time reason, as <see cref="GroupImage"/>'s
    /// <c>context</c> does.</summary>
    public static string? VarGroupCarrier(Place g, DataItem other, string variableContext, string fixedContext) =>
        VariableLengthCompatibility.IsVariableLength(g.Item)
            ? g.Item.CurrentExtentImageCapable
                ? VarGroupImage(g, variableContext)
                : null
            : VariableLengthCompatibility.CorrespondingSpans(g.Item, other) is { } spans && g.ImageCapable
                ? RuntimeApi.VarGroupFromFixedImage(SendingGroupImage(g, fixedContext), SpanArray(spans))
                : null;

    /// <summary>The flat <c>(offset, width)</c> C# array literal <c>CobolVarGroup.FromFixedImage</c> /
    /// <c>ToFixedImage</c> take.</summary>
    public static string SpanArray(int[] spans) => $"new int[] {{ {string.Join(", ", spans)} }}";

    /// <summary>Each variable-length component's offset in a variable-length group's FIXED run — where
    /// <c>CobolVarGroup.Compare</c> interleaves the components with the fixed material (§8.8.4.2.17; kb/Work
    /// PB1467). Arm for arm the twin of <see cref="VarGroupImage"/>: the cell-backed window's layout is known at
    /// compile time, a record struct's is its generated contiguous layout.</summary>
    public static string VarGroupComponentOffsets(Place group) => group switch
    {
        OdoGroupPlace o => VarGroupComponentOffsets(o.Inner),
        RedefViewPlace { Coding: VarGroupWindow g } => $"new int[] {{ {string.Join(", ", g.DynFixedAt)} }}",
        _ => RuntimeApi.VarGroupComponentOffsets(Read(group)),
    };

    /// <summary>The EXTENT TABLE that travels beside <see cref="VarGroupCurrentImage"/> (determination D-FRA (v);
    /// kb/Work PB1053): where each variable-length component of the group ends in that image — the generated
    /// <c>CurrentExtents()</c>, or the cell's composition of the same table. Arm for arm the twin of
    /// <see cref="VarGroupCurrentImage"/>, so a record is never sent with a table computed from another shape than
    /// its image; a group whose image is the loud Tier-C island sends none (the statement is already loud).</summary>
    public static string VarGroupCurrentExtents(Place group) => group switch
    {
        OdoGroupPlace o => VarGroupCurrentExtents(o.Inner),
        RedefViewPlace { Coding: VarGroupWindow g } v => RuntimeApi.CellVarContiguousExtents(
            RenderPath(g.Cell, AccessDir.Sending), v.Width, g.DynBase, g.DynFixedAt, g.DynMax, g.DynStructure),
        _ when !group.Item.CurrentExtentImageCapable => "null",
        _ => $"{Read(group)}.CurrentExtents()",
    };

    /// <summary>The inverse of <see cref="VarGroupCurrentImage"/> — a record read into a variable-length
    /// record, decomposed by the generated <c>FromContiguousImage</c> (the ONE decomposition,
    /// <c>CobolContiguousLayout.Decompose</c>; determination D-FRA, kb/Work PB981) by the record's own
    /// <paramref name="extents"/> when they describe it (D-FRA (v), kb/Work PB1053) and by the take step
    /// otherwise. <paramref name="fixedForm"/> marks a record of a file of FIXED-LENGTH records (D-FRA (vi),
    /// <c>CobolContiguousLayout.ToFixedForm</c>): each member takes its maximum width and drops the space padding.</summary>
    public static string WriteVarGroupContiguous(Place group, string record, string extents, string context,
                                                 bool fixedForm = false) => group switch
    {
        OdoGroupPlace o => WriteVarGroupContiguous(o.Inner, record, extents, context, fixedForm),
        // the cell decomposition (kb/Work PB1026) — the same rule, over the cell's dynamic slots
        RedefViewPlace { Coding: VarGroupWindow g } v => $"{RuntimeApi.CellVarStoreContiguous(RenderPath(g.Cell, AccessDir.Sending),
            $"(int)({v.OffsetExpr})", v.Width, g.DynBase, g.DynFixedAt, g.DynMax, g.DynStructure, record, extents, fixedForm)};",
        _ when !group.Item.CurrentExtentImageCapable => EmitText.LoudStmt(TierCIsland.Reason(group.Item, context)),
        _ => $"{GroupTarget(group)}.FromContiguousImage({record}, {extents}{(fixedForm ? ", true" : "")});",
    };

    /// <summary>⛔ THE ONE READER OF A GROUP OPERAND'S IMAGE IN A <b>SENDING</b> CONTEXT — <see cref="GroupImage"/>,
    /// except that an occurs-depending group sends only its ISO §13.18.38.4 GR8 CURRENT-count part. Both GR8 arms
    /// agree on the sending direction: GR8a (data-name-1 outside the group) and GR8b (data-name-1 inside, "and the
    /// group data item is referenced as a sending operand") each say "only that part of the table area that is
    /// specified by the value of the data item referenced by data-name-1 at the start of the operation will be
    /// used"; only a RECEIVING operand takes the maximum length, which is <see cref="WriteGroupImage"/>'s
    /// <c>DependingInside</c> split.
    /// <para>Three consumers used to spell this dispatch themselves — <c>OperandText.FieldAsString</c>,
    /// <c>OperandText.AsStorageImage</c> and <c>NumericRenderer.FieldNumCore</c> — and the third had the ODO arm
    /// MISSING, so <c>Read(OdoGroupPlace o) =&gt; Read(o.Inner)</c> handed a Tier-B / BASED string window straight
    /// into <c>.AsImage()</c> (CS1061), and it returned the MAXIMUM image where GR8 wants the current extent
    /// (kb/Work PB178). One rule, one place.</para></summary>
    public static string SendingGroupImage(Place group, string context = "whole-group image of", bool transfer = false) =>
        GroupImageAs(group, AccessDir.Sending, context, transfer);

    /// <summary>⛔ THE §13.18.38.4 GR8 DIRECTION LAW, written once (kb/Work PB202): an occurs-depending group
    /// operand uses only its CURRENT-count part when it is SENDING (GR8a and GR8b agree) or when data-name-1 lies
    /// OUTSIDE it (GR8a, either direction); only a depending-INSIDE group as a RECEIVING operand takes the maximum
    /// length (GR8b's second sentence). Every channel reader below — image, bit, national — asks THIS, so the two
    /// directions cannot drift apart per channel again.</summary>
    private static bool UsesCurrentExtent(OdoGroupPlace o, AccessDir dir) =>
        dir == AccessDir.Sending || !o.DependingInside;

    /// <summary>A group operand's character image as seen in direction <paramref name="dir"/> — the GR8
    /// current-extent slice when <see cref="UsesCurrentExtent"/>, otherwise the full (maximum) image.</summary>
    private static string GroupImageAs(Place group, AccessDir dir, string context, bool transfer = false) =>
        group is OdoGroupPlace o && UsesCurrentExtent(o, dir) ? SendingImage(o, context, transfer) : GroupImage(group, context, transfer);

    /// <summary>⛔ <b>THE ONE READER OF A GROUP OPERAND'S SENDING VALUE</b> — the dispatch over ISO §13.18.29.4's
    /// three kinds of group, written once. A BIT group is "treated as though it were an elementary data item of
    /// usage bit … described with PICTURE 1(m)" (GR1b), so its value is its boolean positions
    /// (<see cref="SendingBits"/>); a NATIONAL group is the same with PICTURE N(m) (GR2b), so its value is its
    /// national positions (<see cref="SendingNat"/>); an ALPHANUMERIC group (GR3) sends its storage image
    /// (<see cref="SendingGroupImage"/>). Each arm carries §13.18.38.4 GR8's current-extent slice itself.
    /// <para>Three readers used to spell this dispatch, and one of them did not (kb/Work PB944's sibling sweep):
    /// <c>OperandText.FieldAsString</c> had the bit and national arms, while <c>NumericRenderer.FieldNumCore</c>
    /// decoded EVERY group through the alphanumeric image — so <c>MOVE NG TO PIC 9(3)</c> over a GROUP-USAGE
    /// NATIONAL group holding N"12" decoded the UTF-16BE BYTES 00 31 00 32 as digits, where §14.9.25.4 GR6 d) 3
    /// treats a national sender "as if it were an unsigned integer" of its character positions — and the
    /// GR1 zero-length-item test in <c>MoveEmitter</c> read a national group's STRUCT <c>.Length</c> (CS1061).</para></summary>
    public static string SendingGroupValue(Place group, string context = "whole-group image of") =>
        !group.Item.IsAsIfElementary ? SendingGroupImage(group, context)
        : group.Item.GroupUsage is GroupUsage.Bit ? SendingBits(group)
        : SendingNat(group);

    /// <summary>The SENDING character image of an occurs-depending GROUP operand (ISO §13.18.38 GR8 — only the
    /// current-count part: the maximum image truncated to the current extent, a prefix by SR22).</summary>
    public static string SendingImage(OdoGroupPlace p, string context = "whole-group image of", bool transfer = false) =>
        $"{GroupImage(p.Inner, context, transfer)}.Substring(0, {CharLengthExpr(p)})";

    /// <summary>⛔ THE ONE READER OF A BIT GROUP'S OPERAND VALUE (ISO §13.18.29.4 GR1b — "a bit group is treated as
    /// though it were an elementary data item of usage bit and class and category boolean described with PICTURE
    /// 1(m), where m is the bit length of the group"): the generated <c>AsBits()</c> boolean-position string, with
    /// §13.18.38.4 GR8a's CURRENT-extent truncation when an occurs-depending table lies beneath it. The bit twin of
    /// <see cref="SendingGroupImage"/>, and the reason the two must exist side by side is that they speak DIFFERENT
    /// ALPHABETS — <c>AsImage()</c> is ceil(m/8) PACKED characters, <c>AsBits()</c> is m boolean positions — so a
    /// consumer that reaches for the image reader on a bit group compares packed bytes against boolean literals
    /// (kb/Work PB173: <c>OperandText.FieldAsString</c>'s ODO early return did exactly that, and the character-unit
    /// extent it then computed was NEGATIVE, so the whole operand rendered as the empty string).
    /// <para>A Tier-B <c>RedefViewPlace</c> over a bit group has no struct to ask <c>AsBits()</c> of: its
    /// <see cref="BitWindow"/> read already IS the boolean-position string, so <see cref="BitValue"/> serves the
    /// view with its own read (kb/Work PB1653 — a national group's window is the same shape, and both reach every
    /// verb through <see cref="SendingGroupValue"/> now). §13.18.44.3 SR5 bars an occurs-depending table on either
    /// side of a REDEFINES, so an <c>OdoGroupPlace</c> over a view cannot carry one beyond a BASED record's.</para></summary>
    public static string SendingBits(Place group) => BitsAs(group, AccessDir.Sending);

    /// <summary>A bit group's boolean-position string as seen in direction <paramref name="dir"/> (the GR8 law
    /// is <see cref="UsesCurrentExtent"/>).</summary>
    private static string BitsAs(Place group, AccessDir dir) =>
        group is OdoGroupPlace o && UsesCurrentExtent(o, dir)
            ? $"{BitValue(o.Inner)}.Substring(0, {LengthExpr(o)})"
            : BitValue(group);

    /// <summary>A bit group's boolean-position string: the generated <c>AsBits()</c> of its record struct — or, for a
    /// Tier-B REDEFINES VIEW, the window's own read, which already IS that string (<see cref="BitWindow"/>; a view has
    /// no struct to ask).</summary>
    private static string BitValue(Place group) => group is RedefViewPlace ? Read(group) : $"{Read(group)}.AsBits()";

    /// <summary>⛔ THE ONE WRITER of a bit group's boolean-position string — <c>FromBits</c> distributes the
    /// positions to the subordinates. GR8a (data-name-1 OUTSIDE an occurs-depending table beneath the group)
    /// modifies only the current extent: the stored prefix is spliced over the live bit string, positions past the
    /// count untouched. GR8b (data-name-1 inside) keeps the maximum length, which is the plain arm.</summary>
    public static string WriteBits(Place group, string bits) => BitsWrite(group, bits, AccessDir.Receiving);

    /// <summary>The bit writer with the GR8 extent law of direction <paramref name="dir"/> (<see cref="UsesCurrentExtent"/>).</summary>
    private static string BitsWrite(Place group, string bits, AccessDir dir) =>
        group is OdoGroupPlace o && UsesCurrentExtent(o, dir)
            ? BitStore(o.Inner, RuntimeApi.StrWindowInto(BitTargetValue(o.Inner), "1", LengthExpr(o), bits, "'0'"))
            : BitStore(group, bits);

    /// <summary>Distribute a boolean-position string into a bit group: the struct's generated <c>FromBits</c>, or a
    /// Tier-B VIEW's window write (its <see cref="BitWindow"/> splices the positions into the class's backing).</summary>
    private static string BitStore(Place group, string bits) =>
        group is RedefViewPlace ? Write(group, bits) : $"{GroupTarget(group)}.FromBits({bits});";

    private static string BitTargetValue(Place group) => group is RedefViewPlace ? Read(group) : $"{GroupTarget(group)}.AsBits()";

    /// <summary>⛔ <b>THE ONE WRITER OF A GROUP OPERAND'S VALUE</b> — the receiving twin of
    /// <see cref="SendingGroupValue"/>, over the same three kinds of group (ISO §13.18.29.4): a BIT group takes its
    /// boolean-position string (<see cref="WriteBits"/>, GR1b), a NATIONAL group its national-position string
    /// (<see cref="WriteNat"/>, GR2b), an ALPHANUMERIC group its character image (<see cref="WriteGroupImage"/>, GR3).
    /// A verb that READS a group operand's value through <see cref="SendingGroupValue"/>, rewrites it and stores it
    /// back (INSPECT REPLACING/CONVERTING, STRING's receiver) must store through THIS, in the SAME alphabet it read:
    /// kb/Work PB1128 — both verbs stored a national group's national-position string through the BYTE-image writer,
    /// so `INSPECT NG CONVERTING N"A" TO N"Z"` over five N"A" positions wrote ten UTF-16 characters into ten bytes
    /// and the group read back as mojibake. <paramref name="dir"/> is the §13.18.38.4 GR8 extent law of the store:
    /// <see cref="AccessDir.Receiving"/> keeps GR8b's maximum length for a depending-INSIDE table;
    /// <see cref="AccessDir.Sending"/> splices over the CURRENT extent whatever the depending position — the store
    /// of a statement whose rules size the operand "as a sending data item" (INSPECT §14.9.22.4 GR1), so the value
    /// it writes back is exactly as long as the value it read.
    /// <para>A Tier-B REDEFINES view (<see cref="RedefViewPlace"/>) is written through its window's own
    /// <see cref="Write"/>, whatever its group kind, because that is the pair of what the operand readers read
    /// (<c>OperandText</c>'s view arm precedes the group dispatch, and this struct-less window has no
    /// <c>FromNat</c> / <c>FromBits</c>): the window's coding IS the value alphabet — characters for an alphanumeric
    /// group, boolean positions for a bit group, national positions for a national group (§13.18.29.4 GR1b/GR2b;
    /// kb/Work PB1653 — a national group's view was written as its BYTE window, so ten characters stood for five
    /// positions). The STORAGE image of a view is the other channel, <see cref="WriteGroupImage"/>.</para></summary>
    public static string WriteGroupValue(Place group, string value, string context, AccessDir dir = AccessDir.Receiving) =>
        group is RedefViewPlace ? Write(group, value)
        : !group.Item.IsAsIfElementary
            ? (group is OdoGroupPlace o && UsesCurrentExtent(o, dir) ? ReceiveInto(o, value) : WriteGroupImage(group, value, context))
        : group.Item.GroupUsage is GroupUsage.Bit ? BitsWrite(group, value, dir)
        : NatWrite(group, value, dir);

    /// <summary>⛔ THE ONE READER OF A NATIONAL GROUP'S OPERAND VALUE (ISO §13.18.29.4 GR2b — "a national group is
    /// treated as though it were an elementary data item of usage national … described with PICTURE N(m), where m
    /// is the length of the group"): the generated <c>AsNat()</c> national-position string, with §13.18.38.4 GR8a's
    /// CURRENT-extent truncation when an occurs-depending table lies beneath it. The national twin of
    /// <see cref="SendingBits"/>, and it exists for the same reason: <c>AsImage()</c> speaks BYTES (two per
    /// national position since kb/Work PB327 admitted a national leaf to the byte-addressed record codec) while
    /// this face speaks national positions, so a consumer reaching for the image reader on a national group would
    /// compare UTF-16BE bytes against national literals.
    /// <para>The ODO extent arrives in BYTES (<see cref="CharLengthExpr"/> slices the AsImage channel), so the
    /// national-position count is that extent over <c>CobolBits.BytesPerNational</c> — the ONE pinned size
    /// (§13.18.60.4 GR8 leaves it to the implementor; D-N1 pins two), never a literal 2 written here.</para></summary>
    public static string SendingNat(Place group) => NatAs(group, AccessDir.Sending);

    /// <summary>A national group's national-position string as seen in direction <paramref name="dir"/> (the GR8
    /// law is <see cref="UsesCurrentExtent"/>).</summary>
    private static string NatAs(Place group, AccessDir dir) =>
        group is OdoGroupPlace o && UsesCurrentExtent(o, dir)
            ? $"{NatValue(o.Inner)}.Substring(0, {NatLengthExpr(o)})"
            : NatValue(group);

    /// <summary>A national group's national-position string: the generated <c>AsNat()</c> of its record struct — or,
    /// for a Tier-B REDEFINES VIEW, the window's own read, which already IS that string (<see cref="NationalWindow"/>
    /// transcodes the 2m UTF-16BE bytes to m positions, kb/Work PB1653; a view has no struct to ask).</summary>
    private static string NatValue(Place group) => group is RedefViewPlace ? Read(group) : $"{Read(group)}.AsNat()";

    /// <summary>⛔ THE ONE WRITER of a national group's national-position string — <c>FromNat</c> re-serializes it
    /// to the group's byte image and distributes it. GR8a (data-name-1 OUTSIDE an occurs-depending table beneath
    /// the group) modifies only the current extent; GR8b keeps the maximum length, the plain arm — exactly
    /// <see cref="WriteBits"/>'s split.</summary>
    public static string WriteNat(Place group, string value) => NatWrite(group, value, AccessDir.Receiving);

    /// <summary>The national writer with the GR8 extent law of direction <paramref name="dir"/> (<see cref="UsesCurrentExtent"/>).</summary>
    private static string NatWrite(Place group, string value, AccessDir dir) =>
        group is OdoGroupPlace o && UsesCurrentExtent(o, dir)
            ? NatStore(o.Inner, RuntimeApi.StrWindowInto(NatTargetValue(o.Inner), "1", NatLengthExpr(o), value))
            : NatStore(group, value);

    /// <summary>Distribute a national-position string into a national group: the struct's generated <c>FromNat</c>,
    /// or a Tier-B VIEW's window write (<see cref="NationalWindow"/> re-serializes the positions as UTF-16BE into the
    /// class's backing).</summary>
    private static string NatStore(Place group, string value) =>
        group is RedefViewPlace ? Write(group, value) : $"{GroupTarget(group)}.FromNat({value});";

    private static string NatTargetValue(Place group) => group is RedefViewPlace ? Read(group) : $"{GroupTarget(group)}.AsNat()";

    /// <summary>An occurs-depending NATIONAL group's current extent in NATIONAL POSITIONS: the byte extent the
    /// AsImage channel slices at, divided by the pinned bytes-per-national-position (kb/Work PB327).</summary>
    private static string NatLengthExpr(OdoGroupPlace p) =>
        $"({CharLengthExpr(p)}) / {RuntimeApi.BytesPerNational}";

    /// <summary>A receiving store over an occurs-depending GROUP operand's CURRENT extent (GR8a — depending-outside):
    /// splice the stored prefix over the live image, leaving positions past the count unmodified. A zero current
    /// extent (OCCURS 0 TO n DEPENDING at count 0, §13.18.38 GR8a) is a no-op store, NOT a reference-modification
    /// violation — this is an image WINDOW the compiler chose (<see cref="RuntimeApi.StrWindowInto"/>), not a user
    /// ref-mod, so it never raises EC-BOUND-REF-MOD (review V48; kb/Work PB1707).</summary>
    public static string ReceiveInto(OdoGroupPlace p, string imageExpr) =>
        WriteGroupImage(p.Inner, RuntimeApi.StrWindowInto(GroupImage(p.Inner), "1", CharLengthExpr(p), imageExpr),
            "occurs-depending group receive");

    /// <summary>The C# <c>int</c> expression for an occurs-depending group operand's current POSITION extent (GR8):
    /// the fixed prefix plus data-name-1's clamped value × the element width, read at the operation site. The unit
    /// is the group's own — BIT positions for a subtree holding USAGE BIT leaves (§8.5.1.6.3; kb/Work PB173),
    /// character positions otherwise — so this is the extent the <c>AsBits</c>/<c>FromBits</c> channel slices at.</summary>
    public static string LengthExpr(OdoGroupPlace p) =>
        RuntimeApi.TableOdoExtent(CountRead(p.Depending), p.MinOccurs, p.MaxOccurs, p.FixedUnits, p.ElemUnits);

    /// <summary>The same extent in CHARACTER positions — <see cref="LengthExpr"/> rounded up (the identity when the
    /// positions already ARE characters). This is what the <c>AsImage</c>/<c>FromImage</c> channel slices at.</summary>
    public static string CharLengthExpr(OdoGroupPlace p) =>
        RuntimeApi.TableOdoExtentChars(CountRead(p.Depending), p.MinOccurs, p.MaxOccurs,
            p.FixedUnits, p.ElemUnits, p.PositionsPerCharacter);

    /// <summary>⛔ THE ONE RENDERER OF A TABLE'S OCCURRENCE COUNT (the <see cref="AllCount"/> model): the fixed
    /// OCCURS count (ISO §13.18.38.4 GR4); an occurs-depending table's data-name-1 value CLAMPED to
    /// [integer-1, integer-2] with EC-BOUND-ODO outside (GR7 — <c>CobolTable.OdoExtent</c> over a unit element,
    /// the same clamp the sending image applies); a dynamic-capacity table's current capacity (§8.5.1.9.1).
    /// <para>Promoted here from <c>IntrinsicRenderer</c> when INITIALIZE's per-occurrence loop became the second
    /// consumer (kb/Work PB393): a <c>table(ALL)</c> argument's range and an INITIALIZE loop bound are the SAME
    /// question about the SAME model, and the second spelling is what this repo's one-rule-one-place rule
    /// forbids.</para></summary>
    public static string OccurrenceCount(AllCount c) => c switch
    {
        AllCount.Fixed f => f.Occurs.ToString(),
        AllCount.Odo o => RuntimeApi.TableOdoExtent(CountRead(o.Depending), o.MinOccurs, o.MaxOccurs, 0, 1),
        AllCount.Capacity cap => Read(cap.Register),
        _ => throw new InvalidOperationException($"unknown occurrence count {c.GetType().Name}"),
    };
}
