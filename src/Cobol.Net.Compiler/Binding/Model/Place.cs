// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE typed-lvalue model (COBOLNET_DESIGN §3.3 / §14.1): a resolved reference to a storage location, built once
/// by <see cref="ReferenceResolver"/> and consumed identically by every verb — MOVE, arithmetic,
/// INSPECT/STRING/UNSTRING, file READ INTO / WRITE FROM, and CALL-by-reference all go through this contract. A
/// <c>Place</c> is a BACKEND-NEUTRAL structural value (the G4 invariant): it carries an <see cref="AccessPath"/>,
/// resolved <see cref="DataItem"/>s, and (until PHASE 15's D10) transitional index/offset strings — never C# render
/// text. The C# read/write text is produced by <c>CodeGen.PlaceRenderer</c> (P7 Step 11); the binder never renders.
/// </summary>
public abstract record Place
{
    /// <summary>The analyzed PICTURE of the location (<see langword="null"/> for a group item).</summary>
    public abstract PicInfo? Pic { get; }

    /// <summary>The underlying bound data item this place refers to (carries category, scale, and the profile name).
    /// <para>⛔ IT ANSWERS "WHAT ATTRIBUTES AND WHAT STORAGE", NEVER "WHICH DATA ITEM" — <see cref="DenotedItem"/>
    /// answers the second question, and a screen that asks this one in its place gets the wrong answer for every
    /// DECORATED reference (kb/Work PB602).</para></summary>
    public abstract DataItem Item { get; }

    /// <summary>⛔ <b>THE DATA ITEM THIS REFERENCE DENOTES</b> — <see langword="null"/> when the reference denotes
    /// a data item that NO data description entry declares (kb/Work PB602). It is the OTHER half of
    /// <see cref="Item"/>, and the standard itself writes the two questions as two clauses of one sentence:
    /// ISO §13.18.45.4 GR1 — "all of the data attributes of data-name-2 become the data attributes of data-name-1
    /// <i>and the storage area occupied by data-name-2 becomes the storage area occupied by data-name-1</i>" — so
    /// a level-66 alias has ANOTHER item's attributes and ANOTHER item's storage while being a data item of its
    /// own. <see cref="Item"/> answers the attributes/storage half; this answers the identity half.
    /// <para><b>Ask it for every rule of the form "shall BE the data item …".</b> §14.9.41.3 SR5 ("For relative
    /// files, data-name-1, if specified, shall be the data item specified in the RELATIVE KEY clause in the
    /// associated file control entry") and SR6's record-key arm are identities over DATA ITEMS. Asked of
    /// <see cref="Item"/>, `START RLF KEY IS = WS-RK(1:2)` passed the screen and positioned the file on the
    /// relative record number the key's first two characters spell — a silent wrong answer — and a non-THROUGH
    /// `66 RK-ALIAS RENAMES WS-RK.` passed it too.</para>
    /// <para><b>And for "is this a WHOLE item" too, which is the same question negated.</b> §8.4.3.3.4 GR5:
    /// "Reference modification creates a unique data item that is a subset of the data item referenced by
    /// identifier-1" — a unique item no entry declares, so a reference-modified place denotes nothing declarable
    /// and answers <see langword="null"/>. That is what the hand-written <c>p is not RefModPlace</c> beside a
    /// <c>p.Item.…</c> test was approximating at a dozen sites; each of those is one reading of this property.</para>
    /// <para>⚠ A decoration that answers a question about the OPERAND rather than about its identity — an
    /// occurs-depending extent, an as-if-elementary image view, a table(ALL) enumeration — FORWARDS, because the
    /// reference still denotes the item underneath. The only kind whose decoration changes WHAT is denoted is
    /// <see cref="RefModPlace"/>, which overrides to <see langword="null"/>; the only reference whose NAME denotes
    /// another item is the non-THROUGH level-66 alias, which the resolver records in <see cref="DenotesAs"/>.
    /// <c>PlaceDenotedItemDriftTests</c> holds the roster, so a new <c>Place</c> kind is a failing test rather
    /// than a silent inherit.</para></summary>
    public virtual DataItem? DenotedItem => DenotesAs ?? Item;

    /// <summary>The data item the reference's own NAME denotes, when that is not the item whose attributes and
    /// storage this place carries — set by the ONE resolver, and today exactly the non-THROUGH level-66 RENAMES
    /// alias (kb/Work PB602).
    /// <para>ISO §13.18.45.4 GR1: "When the THROUGH phrase is not specified, all of the data attributes of
    /// data-name-2 become the data attributes of data-name-1 and the storage area occupied by data-name-2 becomes
    /// the storage area occupied by data-name-1." The attributes and the storage are shared, so the alias resolves
    /// to the renamed item's OWN place and every attribute, layout and codegen consumer keeps the answer it had;
    /// the NAME is not shared, so the place must still be able to say which data item was written. Carried as an
    /// init-only property rather than as another <c>Place</c> kind precisely because nothing about the ACCESS
    /// changes — a new kind would have to be re-handled in every renderer arm to render identically.</para>
    /// <para>⚠ It is not a general "rename this place" hook: a reference whose identity the DECORATION changes
    /// (reference modification) overrides <see cref="DenotedItem"/> instead, because there is no declared item to
    /// name.</para></summary>
    public DataItem? DenotesAs { get; init; }

    /// <summary>⛔ THE PLACE WITH EVERY <see cref="PlaceDecorator"/> LAYER REMOVED — the innermost STORAGE place
    /// (kb/Work PB393). A consumer that switches on the storage FORM (member access · REDEFINES window ·
    /// dynamic-table element) must ask this, not the operand place: a decoration answers a question about the
    /// OPERAND (its occurs-depending extent, its as-if-elementary image view, its reference modifier) and never
    /// changes where the storage is, so a switch over storage forms that meets a decorator falls into its
    /// default arm and rejects legal source. That is precisely how an occurs-depending group operand — an
    /// <see cref="OdoGroupPlace"/> wrapping a <see cref="MemberPlace"/> — reached INITIALIZE's and MOVE
    /// CORRESPONDING's <c>_ =&gt; null</c> and aborted the run unit. <c>PlaceDecoratorReachTests</c> is the drift
    /// test that keeps every decorator kind reaching a storage arm.
    /// <para>A consumer whose question the decoration ANSWERS (the operand's extent, its slice, its coding) must
    /// keep switching on the decorated place — this is for the storage-form switch only.</para></summary>
    public Place Undecorated => this is PlaceDecorator d ? d.Inner.Undecorated : this;

    /// <summary>⛔ <b>DOES THE OPERAND THIS PLACE REFERENCES HAVE A CHARACTER IMAGE?</b> (kb/Work PB189) — the
    /// capability question asked of the REFERENCED OPERAND, never of the declaring entry. The two differ for
    /// exactly one shape: a subscripted element of a DYNAMIC-capacity table. The dynamic axis belongs to the
    /// TABLE (§8.5.1.12.1 — "a group item whose data description has at least one dynamic-length elementary item
    /// or dynamic-capacity table as a subordinate item"), so the table entry's own
    /// <see cref="DataItem.IsImageCapable"/> is false, while ONE occurrence of it has no such subordinate and is an
    /// ordinary fixed-length item whose record struct carries <c>AsImage()</c>/<c>FromImage()</c> (emitted for
    /// <see cref="DataItem.ElementImageCapable"/>). A consumer that asked the entry refused a legal §14.9.11.4 GR4
    /// operand (DISPLAY T(3)) at run time although the image sat on the element's struct.
    /// <para>Decorators forward (the decoration never changes which occurrence is referenced);
    /// <see cref="DynTablePlace"/> overrides. Every image guard — <c>PlaceRenderer.GroupImage</c>,
    /// <c>WriteGroupImage</c> and the DISPLAY sender's current-extent arm — asks THIS.</para></summary>
    public virtual bool ImageCapable => Item.IsImageCapable;

    /// <summary>The operand-level twin of <see cref="DataItem.TransferImageCapable"/> (kb/Work PB244) — the
    /// ONE-WAY transfer image, asked of the REFERENCED operand for the same reason as <see cref="ImageCapable"/>.
    /// Only the transfer readers (DISPLAY, a MOVE's sending group) ask it; comparison and every read-back keep
    /// <see cref="ImageCapable"/>.</summary>
    public virtual bool TransferImageCapable => Item.TransferImageCapable;

    /// <summary>The operand-level twin of <see cref="DataItem.BoundaryImageCapable"/> (kb/Work PB189): the
    /// fixed half asks <see cref="ImageCapable"/> (the OPERAND), the variable-length half the entry's own
    /// current-extent composer.</summary>
    public bool BoundaryImageCapable => ImageCapable || Item.CurrentExtentImageCapable;
}

/// <summary>
/// The common base of the WRAPPING places (DESIGN-data-model §2.2 item 1): a decoration over one
/// <see cref="Inner"/> place that keeps the inner item's identity (<see cref="Pic"/>/<see cref="Item"/> forward).
/// Leaf places (<see cref="MemberPlace"/>, <see cref="DynTablePlace"/>, <see cref="RedefViewPlace"/>,
/// <see cref="CapacityRegisterPlace"/>) derive from <see cref="Place"/> directly. <see cref="RenamesPlace"/> stays
/// direct too — it composes N spanned leaves (no single inner) and its Pic/Item are the level-66 ALIAS's own, never a
/// forward (the DESIGN §2.2 item-1 derive list was over-inclusive on that member). The backend
/// <c>CodeGen.PlaceRenderer</c> renders each decorator's transformed access (a plain decorator renders as its inner).
/// </summary>
public abstract record PlaceDecorator(Place Inner) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;

    /// <inheritdoc/>
    public override bool ImageCapable => Inner.ImageCapable;

    /// <inheritdoc/>
    public override bool TransferImageCapable => Inner.TransferImageCapable;
}

/// <summary>
/// A direct member-access place: a static field or a (possibly nested, possibly subscripted) member of a
/// <c>record struct</c> — e.g. <c>WS_N</c>, <c>WS_REC.WS_NAME</c>, <c>CobolTable.At(TBL.ROWS, i).VAL</c>. The access
/// <paramref name="Path"/> is a structural <see cref="AccessPath"/> (a field chain + fixed-table accessors + the D10
/// transitional subscript strings), rendered by <c>CodeGen.PlaceRenderer</c>. A fixed-table member is a plain lvalue,
/// so its read and write render the same path (a subscripted OCCURS DYNAMIC element is the sibling
/// <see cref="DynTablePlace"/>, whose accessor is direction-specific).
/// </summary>
public sealed record MemberPlace(AccessPath Path, DataItem MemberItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => MemberItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => MemberItem;
}

/// <summary>
/// A subscripted OCCURS DYNAMIC element (ISO/IEC 1989:2023 §8.5.1.9.2/.9.3; data-model D9). Unlike a fixed table
/// (whose <c>CobolTable.At</c> <c>ref T</c> serves BOTH directions), a dynamic table has direction-specific accessors:
/// <c>RefSending(occ)</c> on a read (an out-of-range occurrence is benign scratch) and <c>RefReceiving(occ)</c> on a
/// write (an occurrence past the current capacity GROWS the table). The <see cref="Path"/>'s trailing
/// <see cref="DynTableSegment"/> renders that polarity at emit time (SENDING for a read, RECEIVING for a write) — one
/// structural path replaces the former two precomputed strings. Rendered by <c>CodeGen.PlaceRenderer</c>. (Kept a
/// distinct subtype rather than folded into <see cref="MemberPlace"/> to bound the Step 11 blast radius —
/// <c>UsageCollectionPass</c>/<c>MoveEmitter</c> discriminate it by type.)
/// </summary>
public sealed record DynTablePlace(AccessPath Path, DataItem ElementItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => ElementItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ElementItem;

    /// <summary>A subscripted reference to the dynamic-capacity table entry itself denotes ONE OCCURRENCE, whose
    /// capability is the element shape's (<see cref="DataItem.ElementImageCapable"/>) — the dynamic axis is the
    /// table's, not the occurrence's (kb/Work PB189; §8.5.1.12.1). A member path beneath the element asks its
    /// own item as usual.</summary>
    public override bool ImageCapable => ElementItem.IsDynamicTable ? ElementItem.ElementImageCapable : ElementItem.IsImageCapable;

    /// <summary>The <see cref="ImageCapable"/> law applied to the one-way transfer image (kb/Work PB244).</summary>
    public override bool TransferImageCapable =>
        ElementItem.IsDynamicTable ? ElementItem.ElementTransferImageCapable : ElementItem.TransferImageCapable;
}

/// <summary>
/// A Tier-B REDEFINES view (COBOLNET_DESIGN §4.2): a typed <c>(offset, width)</c> character window over the class's
/// ONE <see cref="string"/> backing field. Reading yields the window's character image (a substring); writing splices
/// a new image back into the backing, preserving its full width — so a write through any view is visible through every
/// other view of the class (one stored backing, ISO §13.18.44). The window carries the VIEW's <see cref="DataItem"/>,
/// so its category/scale/profile drive interpretation: a numeric-DISPLAY view is flagged
/// <see cref="DataItem.StoreAsImage"/>, so the numeric pipeline decodes/encodes the window exactly as for a
/// whole-group numeric leaf. <paramref name="Backing"/> is the structural path to the class's stored backing field;
/// <paramref name="OffsetExpr"/> is the 0-based window offset — the D10 transitional string (a constant, or the
/// <c>classOffset + Σ (idx − 1) × stride</c> arithmetic for a view inside an OCCURS, ISO §13.18.44). Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RedefViewPlace(AccessPath Backing, string OffsetExpr, int Width, DataItem ViewItem) : Place
{
    /// <summary>⛔ THE WINDOW'S CODING — how the member's VALUE CARRIER relates to the bytes of the class's one
    /// backing. Null is the identity coding (an alphanumeric / edited / byte-form-numeric member, whose carrier
    /// IS its bytes); <see cref="BitWindow"/> re-expresses the window in BIT positions (kb/Work PB203);
    /// <see cref="NationalWindow"/> keeps the BYTE window and transcodes it UTF-16BE (kb/Work PB231 —
    /// RESIDUE-11).
    /// <para>⛔ ONE PAYLOAD, NOT ONE PER CODING, and not a second <see cref="Place"/> subtype. Every consumer
    /// that discriminates <c>is RedefViewPlace</c> is asking "is this member's read already the value string?",
    /// and for every coding the answer is still yes — the string is the member's boolean carrier or its national
    /// characters instead of its bytes. Only <c>PlaceRenderer</c>'s Read/Write pair reads this, so a NEW coding
    /// is one record here plus one arm there, and the byte-window channel cannot grow a coding that some surface
    /// silently ignores. The WIDTH stays the member's <see cref="DataItem.ByteWidth"/> in every coding, because
    /// the backing is byte-addressed (§13.18.44.4 GR1's "area sufficient to contain the number of bits
    /// required").</para></summary>
    public WindowCoding? Coding { get; init; }

    /// <summary>The bit coding, or null — the discriminated read of <see cref="Coding"/>.</summary>
    public BitWindow? Bit => Coding as BitWindow;

    /// <summary>⛔ TRUE WHEN THIS VIEW'S <c>Read()</c> IS A CHARACTER STRING — the ONE test for the assumption
    /// the wrapping consumers make about a Tier-B view ("its read is already the value image, use it
    /// verbatim"). It holds for the identity coding, for <see cref="BitWindow"/> (the boolean carrier) and for
    /// <see cref="NationalWindow"/> (the transcoded national characters), and it does NOT hold for
    /// <see cref="SlotWindow"/>, whose read is a managed pointer/object reference (kb/Work PB231). Stated once,
    /// here, so a fourth coding cannot silently inherit the wrong half of the assumption.</summary>
    public bool ReadsCharacterImage => Coding is not SlotWindow;   // a DynSlotWindow / VarGroupWindow reads a string (kb/Work PB1026)

    /// <summary>⛔ THE ONE Tier-B window builder (kb/Work PB203). Three sites compose this window — the resolver
    /// (<c>ReferenceResolver.PlaceForItem</c>), the INITIALIZE receiver cursor and the MOVE CORRESPONDING leaf
    /// cursor — and before this factory each carried its own copy of the offset law, which is exactly how the BIT
    /// unit came to be missing from all three at once.
    /// <para>The law: a member's window starts at <paramref name="byteOffsetExpr"/> characters into the class
    /// backing, or — when the member is a bit item, whose positions §13.18.44.4 GR1 and §8.5.1.6.3 both count in
    /// BITS — at <c>8 × (the caller's RUNTIME byte displacement) + <see cref="DataItem.ClassBitOffset"/> +
    /// Σ (index − 1) × the per-occurrence bit extent</c>. The displacement is passed separately from the byte
    /// expression because the item's own in-class position is already carried in bits by
    /// <see cref="DataItem.ClassBitOffset"/> — re-deriving it from the byte expression would round a sub-byte
    /// member down to its containing byte.</para></summary>
    /// <param name="runtimeByteDisplacement">The part of <paramref name="byteOffsetExpr"/> that is NOT the item's
    /// own static in-class offset — a BASED class's pointer displacement, or an enclosing cursor's hoisted
    /// offset. Null/empty/"0" when the window is at a compile-time-known position.</param>
    /// <param name="occursBitTerms">The <c> + (index − 1) * bits</c> terms for each OCCURS level crossed WITHIN
    /// the class, already in bit units (<see cref="BitLayout.WidthBits"/> per level). Empty when none.</param>
    /// <param name="cell">The structural path to the class's backing <c>StorageCell</c> — supplied only by the
    /// three CELL-BACKED surfaces (BASED, EXTERNAL, ADDRESS OF; <c>RedefinesClass.BackingCellCsName</c>) and
    /// null for a REDEFINES class, whose backing is a plain stored string field. It is what a
    /// <see cref="SlotWindow"/> member needs (kb/Work PB231); see <see cref="SlotWindow.CarriedBySlot"/> for
    /// why a REDEFINES class can never hold one.</param>
    public static RedefViewPlace For(AccessPath backing, DataItem item, string byteOffsetExpr,
                                     string? runtimeByteDisplacement = null, string occursBitTerms = "",
                                     AccessPath? cell = null)
    {
        // ⛔ THE WINDOW IS THE MEMBER'S STORAGE EXTENT, NOT ITS CHARACTER-POSITION COUNT (kb/Work PB231).
        // §13.18.44.4 GR1 states the association over "an area sufficient to contain the number of BITS
        // required by the data item referenced by the subject of the entry" and §14.9.3.4 GR3 the allocated
        // area as "the number of BYTES required to hold an item as described by data-name-1" — both storage
        // units. ByteWidth IS ImageWidth for every leaf kind but NATIONAL, whose §13.18.60.4 GR8 size this
        // implementation pins at two bytes per position (D-N1), so this read is byte-identical everywhere
        // else and is the whole of RESIDUE-11's geometry here.
        var window = new RedefViewPlace(backing, byteOffsetExpr, item.ByteWidth, item);
        // ⛔ A POINTER-CLASS MEMBER'S VALUE IS NOT IN THE BYTES (kb/Work PB231 — the pointer third): it is the
        // managed slot of the SAME area at the SAME byte offset, so its bytes stay reserved (§14.9.3.4 GR3's
        // byte quantity, and every following member's offset, are unaffected) and its value rides
        // StorageCell.SlotAt. `cell` is null exactly for a REDEFINES class, which cannot hold such a member on
        // conforming source — see SlotWindow.CarriedBySlot — and its class is Rejected before any place is
        // built for it. Falling through to the plain byte window would alias the member onto its own reserved
        // placeholder characters, silently; this backstop makes that a compile-time internal error instead.
        if (SlotWindow.CarriedBySlot(item))
            return cell is not null
                ? window with { Coding = new SlotWindow(cell) }
                : throw new System.InvalidOperationException(
                    $"'{item.CobolName ?? item.CsName}' is of a pointer class and its storage class has no "
                    + "backing StorageCell — a managed slot has nowhere to live (ISO §14.9.3.4 GR9; kb/Work "
                    + "PB231). Such a class must be Rejected at classification, never given a byte window.");
        // ⛔ A DYNAMIC-LENGTH MEMBER'S CONTENT IS NOT IN THE BYTES EITHER (kb/Work PB1026): it rides the cell's
        // dynamic slot (§8.5.1.10.3's "located elsewhere"), and a group over such members reads and writes as its
        // contiguous image (§8.5.1.11.2). The same backstop as the pointer arm: only a cell has slots, and the
        // REDEFINES classifier refuses a dynamic-length member before any place is built for its class.
        if (DynSlotWindow.CarriedBySlot(item) || VarGroupWindow.Applies(item))
            return cell is null
                ? throw new System.InvalidOperationException(
                    $"'{item.CobolName ?? item.CsName}' is or contains a dynamic-length item and its storage class "
                    + "has no backing StorageCell — the content has nowhere to live (ISO §8.5.1.10.3; kb/Work "
                    + "PB1026). Such a class must be Rejected at classification, never given a byte window.")
                : window with
                {
                    Coding = DynSlotWindow.CarriedBySlot(item)
                        ? new DynSlotWindow(cell, item.ClassDynOrdinal)
                        : VarGroupWindow.Of(item, cell),
                };
        if (NationalWindow.WindowPositionsOf(item) is { } positions)
            return window with { Coding = new NationalWindow(positions) };
        if (!BitLayout.IsBitItem(item)) return window;
        string at = runtimeByteDisplacement is null or "" or "0"
            ? $"{item.ClassBitOffset}"
            : $"{BitLayout.BitsPerCharacter} * ({runtimeByteDisplacement}) + {item.ClassBitOffset}";
        return window with
        {
            Coding = new BitWindow(at + occursBitTerms, BitLayout.WidthBits(item))
            {
                ClassRelativeExpr = $"{item.ClassBitOffset}{occursBitTerms}",
            },
        };
    }

    /// <inheritdoc/>
    public override PicInfo? Pic => ViewItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => ViewItem;
}

/// <summary>The CODING of a <see cref="RedefViewPlace"/> window over the class's one byte backing — the relation
/// between the member's VALUE CARRIER and the bytes it occupies. The absence of a coding (a null
/// <see cref="RedefViewPlace.Coding"/>) is the identity: an alphanumeric, edited or byte-form-numeric member's
/// carrier IS its bytes. Every coding keeps the window's byte geometry (<see cref="RedefViewPlace.OffsetExpr"/>
/// and <see cref="RedefViewPlace.Width"/>, in bytes); a coding that also re-expresses the geometry in a finer
/// unit carries it itself, as <see cref="BitWindow"/> does.</summary>
public abstract record WindowCoding;

/// <summary>A <see cref="RedefViewPlace"/>'s BIT window: the 0-based ABSOLUTE bit offset within the class's one
/// byte backing (the D10 transitional expression string) and the member's boolean-position count
/// (§13.18.29.4 GR1b's <c>m</c> for a bit group, the PICTURE 1(n) length for a bit leaf).</summary>
public sealed record BitWindow(string OffsetExpr, int Bits) : WindowCoding
{
    /// <summary>The CLASS-RELATIVE part of <see cref="OffsetExpr"/> — the member's static in-class bit offset plus
    /// each in-class OCCURS level's <c>(index − 1) × stride</c> term, WITHOUT the runtime byte displacement a
    /// BASED class adds (kb/Work PB240). §14.9.4.3 SR6/SR8 ask whether a BY REFERENCE bit operand "is aligned on
    /// a byte boundary", and that displacement is a whole number of BYTES by construction (a pointer addresses
    /// characters), so this is the part that can move the answer — and, being all-literal exactly when every
    /// subscript is, the part whose static provability the rules' second clause demands.</summary>
    public string ClassRelativeExpr { get; init; } = OffsetExpr;
}

/// <summary>A <see cref="RedefViewPlace"/>'s NATIONAL window (kb/Work PB231 — RESIDUE-11): the member occupies
/// <see cref="RedefViewPlace.Width"/> bytes of the backing, two per national character position (ISO
/// §13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two), holding the UTF-16BE serialization
/// <c>CobolBits.NatBytes</c> already defines for every other national storage channel. <paramref name="Positions"/>
/// is the character-position count the member's VALUE carrier holds — <c>Width / 2</c>, carried explicitly so the
/// renderer never re-derives it.</summary>
public sealed record NationalWindow(int Positions) : WindowCoding
{
    /// <summary>⛔ THE ONE test for "does this member ride the backing as national bytes?", and the position
    /// count when it does. It is exactly <b>USAGE NATIONAL</b> — §13.18.60.4 GR8 is written about the USAGE, not
    /// about a category ("The implicit or explicit USAGE NATIONAL clause specifies that a national coded
    /// character set shall be used to represent a data item in the storage of the computer"), so every one of
    /// §13.18.60.3 SR12's five picture shapes rides here on one test: category national and national-edited
    /// (whose usage SR13a/SR20 force to NATIONAL), and the national-form boolean, numeric and numeric-edited
    /// (kb/Work PB646). The count is the item's own CHARACTER-IMAGE width, which §13.18.40.4 GR1 makes a count
    /// of NATIONAL character positions under this usage — digits plus a SIGN SEPARATE position for a numeric
    /// item, the mask width for an edited one, the PICTURE length otherwise.
    /// <para>⚠ It used to name two shapes and read two different widths for them (<c>p.Length</c> for the
    /// category, <c>ElementaryImageWidth</c> for the numeric), which is a list where a rule belongs: the two
    /// widths are EQUAL for every category-national item — <c>ElementaryImageWidth</c>'s default arm IS
    /// <c>pic.Length</c> — so collapsing them changes no answer and makes the next SR12 shape automatic.</para>
    /// <para>A national GROUP is not one of them: §13.18.29.4 GR2b makes it as-if <c>PICTURE N(m)</c> but its
    /// LAYOUT stays its children's (D20), and each national leaf inside it gets its own window — the group's OWN
    /// window is a separate question, answered by <see cref="WindowPositionsOf"/>.</para></summary>
    public static int? PositionsOf(DataItem item) =>
        item is { IsElementary: true, Pic.Usage: Usage.National } ? item.ElementaryImageWidth : null;

    /// <summary>⛔ THE POSITION COUNT OF A MEMBER'S OWN WINDOW (kb/Work PB1653) — <see cref="PositionsOf"/> for an
    /// elementary national item, and for a NATIONAL GROUP its as-if <c>PICTURE N(m)</c> length: §13.18.29.4 GR2 b)
    /// "a national group is treated as though it were an elementary data item of usage national and class and
    /// category national described with PICTURE N(m), where m is the length of the group". The window of a national
    /// group (<see cref="RedefViewPlace.For"/>) is therefore a NATIONAL window exactly as a bit group's is a bit
    /// window: its VALUE is its m national positions, over the 2m bytes <see cref="RedefViewPlace.Width"/> says —
    /// never ten characters read from a ten-byte window. <see cref="PositionsOf"/> stays the ELEMENTARY test the
    /// storage-geometry and carriage gates ask (a group's layout is its children's, D20).</summary>
    public static int? WindowPositionsOf(DataItem item) =>
        PositionsOf(item) ?? (item is { IsAsIfElementary: true, GroupUsage: GroupUsage.National } ? item.AsIfPic!.Length : null);
}

/// <summary>A window coding whose value lives (wholly or partly) on the class's backing <c>StorageCell</c> rather
/// than in its byte image — so it carries a SECOND structural path, the cell (<see cref="Cell"/>), beside
/// <see cref="RedefViewPlace.Backing"/>. ⛔ ONE BASE, so every consumer that re-anchors a place (the containment
/// prefix, <c>ProgramEmitter.PrefixPlace</c>) re-anchors EVERY cell-carrying coding with one expression — a coding
/// added later cannot be forgotten there (kb/Work PB1026 added two).</summary>
public abstract record CellWindowCoding(AccessPath Cell) : WindowCoding;

/// <summary>A <see cref="RedefViewPlace"/>'s MANAGED-SLOT window (kb/Work PB231 — the pointer third): the member
/// is of a pointer class, whose value is a managed reference and therefore has no byte image at all. Its bytes in
/// the class backing are RESERVED placeholder positions — <see cref="RedefViewPlace.Width"/> of them, the item's
/// <see cref="DataItem.ByteWidth"/> — so §14.9.3.4 GR3's "the amount of storage to be allocated is the number of
/// bytes required to hold an item as described by data-name-1" and every following member's offset are exactly
/// what a byte-addressed area says; the VALUE rides <c>StorageCell.SlotAt</c> at that same byte offset, through
/// <c>CobolPtr.SlotRead</c>/<c>SlotWrite</c>.
/// <para><paramref name="Cell"/> is the structural path to the class's backing <c>StorageCell</c> — a SECOND path
/// beside <see cref="RedefViewPlace.Backing"/> because the slots and the byte image are two halves of ONE storage
/// area and only the cell holds both. It is available exactly on the three CELL-BACKED surfaces (BASED, EXTERNAL,
/// ADDRESS OF).</para>
/// <para>⛔ GR9 IS SATISFIED BY CONSTRUCTION, NOT BY A SEEDING LOOP: an unwritten slot reads null, so "data items
/// of class object or class pointer in the allocated storage are initialized to null" holds for every allocation
/// path that exists or will exist, and the read renders the item's own <c>PicInfo.DefaultInitializer</c> as the
/// null state — the SAME expression an ordinary declared pointer field is seeded from.</para></summary>
public sealed record SlotWindow(AccessPath Cell) : CellWindowCoding(Cell)
{
    /// <summary>⛔ THE ONE test for "does this member ride the area's managed slots rather than its bytes?" (the
    /// <see cref="NationalWindow.PositionsOf"/> model — the gate and the geometry must not be able to disagree
    /// about the population). It is the three pointer/object PICTURE categories the model gives a managed
    /// carrier: data-pointer, program-pointer and object reference. USAGE FUNCTION-POINTER and USAGE MESSAGE-TAG
    /// are NOT here because they gain no <c>PicInfo</c> at all (the staged §13.18.60 USAGE band, COBOLNET0899);
    /// they reach <c>DataBinder.ByteWindowResidueOf</c>'s "no bound representation" arm instead, and the day
    /// their stage lifts this predicate is where they join.
    /// <para>⚠ A REDEFINES class can never contain such a member on conforming source, which is why
    /// <see cref="RedefViewPlace.For"/> may treat a missing cell as an internal error: §13.18.44.3 SR12 bars the
    /// REDEFINES subject from being "of class object, message-tag, or pointer or a strongly-typed group item",
    /// SR14 bars data-name-2 from the same list plus "an item subordinate to a strongly-typed group item", and
    /// §13.18.60.3 SR14 admits a pointer USAGE "only for an elementary data item at level 1 or an elementary
    /// data item subordinate to a type declaration that includes the STRONG phrase" — so a pointer member of a
    /// REDEFINES-CLAUSE class is barred at the entry by the first two rules or at the declaration by the third. A
    /// file's IMPLICITLY shared record area (§13.18.33.4 GR3 — a level-1 pointer record beside another record of
    /// the same FD) is legal source, and there the pointer record is an OUT-OF-LINE record that never joins the
    /// class (FileModel.IsOutOfLineRecord — determination D-FRA, kb/Work PB981).</para></summary>
    public static bool CarriedBySlot(DataItem item) =>
        item.IsElementary && item.Pic is
            { Category: PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer
                  or PicCategory.ObjectReference };
}

/// <summary>A <see cref="RedefViewPlace"/>'s DYNAMIC-LENGTH window (kb/Work PB1026): the member is a dynamic-length
/// elementary item of a CELL-BACKED class. ISO §8.5.1.10.3 — "Dynamic-length elementary items may be physically
/// located in memory within the record they are subordinate to, or they may be located elsewhere in the computer's
/// memory" — and this implementation locates it in the cell's dynamic slot numbered <paramref name="Ordinal"/> (its
/// place among the class's dynamic-length items, in storage order), so every description sharing the cell
/// (§13.18.22.4 GR1 / GR4 b)) shares its content. It occupies ZERO bytes of the backing (its
/// <see cref="DataItem.ByteWidth"/>), which is §8.5.1.12.3's own accounting of the fixed run. Its read IS its
/// content string, so <see cref="RedefViewPlace.ReadsCharacterImage"/> holds.</summary>
public sealed record DynSlotWindow(AccessPath Cell, int Ordinal) : CellWindowCoding(Cell)
{
    /// <summary>⛔ THE ONE test for "does this member ride a dynamic-length slot?" — an elementary dynamic-length
    /// item. The cell forcer's gate (<c>DataBinder.ForceStringCanonical</c>), the REDEFINES classifier's carrier
    /// question and the geometry (<see cref="RedefViewPlace.For"/>) all ask it.</summary>
    public static bool CarriedBySlot(DataItem item) => item is { IsElementary: true, IsDynamicLength: true };
}

/// <summary>A <see cref="RedefViewPlace"/>'s VARIABLE-LENGTH GROUP window (kb/Work PB1026): a group of a CELL-BACKED
/// class with dynamic-length items under it. Its fixed run is <see cref="RedefViewPlace.Width"/> bytes of the
/// backing from the window's offset; its dynamic-length items are the cell's dynamic slots <paramref name="DynBase"/>
/// onward, sitting at fixed-run positions <paramref name="DynFixedAt"/> (relative to the group) with maximum sizes
/// <paramref name="DynMax"/>. ISO §8.5.1.11.2 — "a variable-length data item behaves in all respects as though it
/// were in fact contiguous with its neighbors whenever a procedural operation is applied to a group containing it"
/// — is why its read is the CONTIGUOUS image (<c>StorageCell.ContiguousAt</c>), exactly what a declared group's
/// generated <c>CurrentImage()</c> composes.</summary>
public sealed record VarGroupWindow(AccessPath Cell, int DynBase, IReadOnlyList<int> DynFixedAt,
                                    IReadOnlyList<int> DynMax) : CellWindowCoding(Cell)
{
    /// <summary>The coding for <paramref name="group"/> when it has dynamic-length items under it, else null.
    /// The layout is read off the class walk (<see cref="DataItem.ClassOffset"/> /
    /// <see cref="DataItem.ClassDynOrdinal"/>), never recomputed, so the group and its members cannot disagree
    /// about where a component sits.</summary>
    public static VarGroupWindow? Of(DataItem group, AccessPath cell)
    {
        if (!group.IsGroup) return null;
        var dyn = DataItem.DescendantsOf(group).Where(DynSlotWindow.CarriedBySlot).ToList();
        return dyn.Count == 0 ? null
            : new VarGroupWindow(cell, dyn[0].ClassDynOrdinal,
                dyn.Select(d => d.ClassOffset - group.ClassOffset).ToList(), dyn.Select(d => d.DynMaxSize).ToList());
    }

    /// <summary>True when <paramref name="group"/> would take this coding.</summary>
    public static bool Applies(DataItem group) =>
        group.IsGroup && DataItem.DescendantsOf(group).Any(DynSlotWindow.CarriedBySlot);
}

/// <summary>
/// A level-66 RENAMES place — the THROUGH form ONLY: ONE alphanumeric view composed over the spanned record
/// leaves. ⛔ <b>The composed CARRIER is one string; the DATA ITEM it stands for is a GROUP item</b>, and the two
/// must not be confused: ISO §13.18.45.4 GR2 — "When the THROUGH phrase is specified, data-name-1 defines an
/// alphanumeric GROUP item that includes all elementary items starting with data-name-2 …" — so every rule that
/// turns on elementary-vs-group (§14.9.25.4 GR4's MOVE classification above all, through
/// <c>MoveClassifier.IsGroupPlace</c>) must read it as a group. Calling the carrier "an elementary alphanumeric
/// item" here is what propagated the opposite claim into three call sites (kb/Work PB430). The WITHOUT-THROUGH
/// alias never reaches this record at all: §13.18.45.4 GR1 gives it data-name-2's own attributes, and
/// <c>ReferenceResolver</c> forwards it to that item's place.
/// Reading concatenates the leaves' character images (each leaf field invariantly holds exactly its image
/// width); writing stores the value at the span's width and distributes the slices back into the leaves left to
/// right — so a write through the alias is visible through every renamed item and vice versa (no second storage,
/// SR/GR — RENAMES adds no data item). Rendered by <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RenamesPlace(IReadOnlyList<Place> Leaves, DataItem AliasItem, IReadOnlyList<int> Widths) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => AliasItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => AliasItem;
}

/// <summary>
/// The <see cref="Place"/> of a GROUP operand whose subtree contains an occurs-depending table (ISO/IEC 1989:2023
/// §13.18.38 Format 2). It decorates the plain member place — read/write render as the struct lvalue unchanged (the
/// inherited inner access), so every consumer that does not know about ODO behaves exactly as before — and the GR8
/// operand seams (rendered by <c>CodeGen.PlaceRenderer</c> over this record's structure) consult the decoration:
/// <list type="bullet">
///   <item><b>Sending</b> (the sending side of BOTH GR8 quadrants): "only that part of the table area that is
///     specified by the value of [data-name-1] at the start of the operation will be used" — the group image
///     truncated to the current extent. SR22 (the subject may be followed within its record only by entries
///     subordinate to it) guarantees the table is the TRAILING storage, so the current extent is a character PREFIX
///     of the maximum image; a zero count with no preceding fixed part is the zero-length item of §8.5.4 item 1.</item>
///   <item><b>Receiving, data-name-1 outside the group</b> (GR8a): the same current extent — character positions
///     past it are NOT modified; the stored prefix is spliced over the live image.</item>
///   <item><b>Receiving, data-name-1 inside the group</b> (GR8b): "the maximum length of the group will be used"
///     — <see cref="DependingInside"/> lets each receiver keep the plain full-width <c>FromImage</c> store.</item>
/// </list>
/// The legacy engine proved exactly this direction split over the NIST-85 corpus (NC247A; its
/// <c>LocationResolver.ResolveWholeItem(receiving)</c>); the greenfield twin computes the extent at the operand
/// site — no runtime table state (COBOLNET_DESIGN §3.6 / §14.4 — ONE image facility, the GR8 slice is a view over
/// it). The legacy's LINKAGE max-length shortcut is deliberately NOT ported: GR8 applies in any section.
/// <para>⛔ <b>THE EXTENT IS IN POSITIONS, AND A POSITION IS NOT ALWAYS A CHARACTER</b> (kb/Work PB173).
/// <see cref="PositionsPerCharacter"/> is 8 for a subtree holding a USAGE BIT leaf and 1 otherwise, so
/// <see cref="FixedUnits"/>/<see cref="ElemUnits"/> are bit positions for the first and character positions for the
/// second — <c>fixed + occ × elem</c> is an identity only in a unit where each occurrence starts where the previous
/// one ended, which §8.5.1.6.3's shared-byte runs break in characters. The BIT channel (<c>AsBits</c>/
/// <c>FromBits</c>, §13.18.29.4 GR1b) uses the positions directly; the CHARACTER channel (<c>AsImage</c>/
/// <c>FromImage</c>) takes the ceiling, which is also where §8.5.1.6.3 rule 4's trailing filler lives.</para>
/// </summary>
public sealed record OdoGroupPlace(
    Place Inner, Place Depending, int FixedUnits, int ElemUnits, int MinOccurs, int MaxOccurs,
    bool DependingInside, int PositionsPerCharacter = 1)
    : PlaceDecorator(Inner);

/// <summary>
/// A <c>table(… ALL …)</c> intrinsic-function argument (ISO §15.3 — "When the definition of a function permits an
/// argument to be repeated a variable number of times, a table may be referenced by specifying the data-name … followed
/// immediately by subscripting where one or more of the subscripts is the word ALL"; kb/Work PB62): ONE operand that
/// stands for EVERY element the ALL subscripts range over — "the effect is as if each table element associated with
/// that subscript position were specified", left to right, the rightmost ALL varying fastest.
/// <para>The <see cref="Element"/> is the element's own place with each ALL level's subscript written as the index
/// variable <c>{IndexVar}[k]</c> (k = the ALL level's ordinal, outermost first) and every fixed subscript already
/// rendered; <see cref="Counts"/> gives each ALL level's RANGE, outermost first — a fixed OCCURS count, an OCCURS
/// DEPENDING table's data-name-1 value ("the range of values is determined by the object of the OCCURS DEPENDING ON
/// clause") or a dynamic-capacity table's current capacity ("from 1 to the current capacity of the table"). The two
/// runtime ranges are why this is a PLACE the backend ENUMERATES (<c>CobolTable.AllArgs</c>) rather than a
/// bind-time expansion into N operands: the count is a runtime value, and a nested dynamic table's capacity even
/// depends on the outer index (its <see cref="AllCount.Capacity"/> place carries the outer index variable).</para>
/// <para>A decorator over the element so every STATIC classifier (class, category, usage, width, the §15.3 argument
/// screen, MAX/MIN's type resolution) sees the element's item exactly as it would a single subscripted reference;
/// only the intrinsic argument-list renderers expand it, and a single-value read (<c>PlaceRenderer.Read</c>) is an
/// internal error by construction — a table(ALL) is never a value.</para>
/// </summary>
public sealed record TableAllPlace(Place Element, string IndexVar, IReadOnlyList<AllCount> Counts) : PlaceDecorator(Element)
{
    /// <summary>The number of elements when EVERY ALL level has a fixed range — the product of the OCCURS counts;
    /// null when any level's range is a runtime value (an OCCURS DEPENDING or dynamic-capacity table).</summary>
    public long? StaticCount
    {
        get
        {
            long n = 1;
            foreach (var c in Counts)
            {
                if (c is not AllCount.Fixed f) return null;
                n *= f.Occurs;
            }
            return n;
        }
    }
}

/// <summary>The RANGE of one ALL subscript level of a <see cref="TableAllPlace"/> (ISO §15.3) — structural, so the
/// backend renders it: a fixed OCCURS count, an OCCURS DEPENDING table's current count, or a dynamic-capacity table's
/// current capacity.</summary>
public abstract record AllCount
{
    /// <summary>A fixed table: <c>OCCURS integer-2</c> occurrences.</summary>
    public sealed record Fixed(int Occurs) : AllCount;

    /// <summary>An OCCURS DEPENDING table: data-name-1's current value, which §13.18.38.4 GR7 requires to lie in
    /// [integer-1, integer-2] (EC-BOUND-ODO outside — the same clamp the sending image applies).</summary>
    public sealed record Odo(Place Depending, int MinOccurs, int MaxOccurs) : AllCount;

    /// <summary>A dynamic-capacity table: its current capacity — the register view over the table (a nested table's
    /// path carries the OUTER index variables, so the capacity is the right occurrence's).</summary>
    public sealed record Capacity(CapacityRegisterPlace Register) : AllCount;
}

/// <summary>
/// A NUMERIC-DISPLAY item viewed as its CHARACTER IMAGE for reference modification (ISO §8.4.3.3.4 GR6 — the unique
/// result is an elementary alphanumeric item over the operand's standard data format): reading formats the stored
/// value's display image; writing decodes the spliced image back into the typed field (sign-aware both ways via
/// the FormatDisplay/StoreDisplay pair). Rendered by <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record NumericImagePlace(Place Inner) : PlaceDecorator(Inner);

/// <summary>
/// A GROUP item viewed as its CHARACTER IMAGE for reference modification (ISO §8.4.3.3.3 SR1 — "an alphanumeric
/// group item" / "a group item that is neither a strongly-typed group nor a variable-length group"; §8.4.3.3.4 GR6 —
/// the unique data item is an elementary alphanumeric item over the group's positions): reading is the generated
/// <c>AsImage()</c> (an occurs-depending group with data-name-1 outside sends its CURRENT-count part, §13.18.38 GR8);
/// writing distributes the spliced image back through the ONE group-image store (<c>PlaceRenderer.WriteGroupImage</c>
/// — <c>FromImage</c>, or the GR8a current-extent splice). A variable-length group or a group with a pointer/object-class leaf has no image (every NUMERIC leaf kind joined the image across kb/Work PB164 + R40)
/// (<see cref="DataItem.IsImageCapable"/>) and stays the loud Tier-C island. The <see cref="NumericImagePlace"/>
/// shape for groups — kb/Work PB70: before it, the resolver's category gate returned null for every group, which
/// was a run-time NotImplemented for a sending ref-mod and a SILENT no-op for a receiving one. Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record GroupImagePlace(Place Inner) : PlaceDecorator(Inner);

/// <summary>
/// A BIT GROUP viewed as its UNPACKED BOOLEAN-POSITION string, for reference modification (kb/Work PB173).
/// <para><b>The derivation, in three links.</b> §13.18.29.4 GR1b: "a bit group is treated as though it were an
/// elementary data item of usage bit and class and category boolean described with PICTURE 1(m), where m is the
/// bit length of the group". §8.4.3.3.3 SR1's last sentence: "For reference modification, bit group items and
/// national group items are treated as elementary data items" — so a bit group is admitted as identifier-1 AS AN
/// ELEMENTARY ITEM, not through the group bullet. §8.4.3.3.4 GR5a: "If the usage of identifier-1 is bit,
/// positions used in evaluation are BIT POSITIONS; otherwise, positions used in evaluation are character
/// positions." Chained: <c>G(1:3)</c> denotes bit positions 1..3 of the m-bit string, and by GR6 the resulting
/// unique item keeps usage bit and category boolean.</para>
/// <para>⛔ WHY THIS IS A SECOND TYPE AND NOT A FLAG ON <see cref="GroupImagePlace"/>. The two carry DIFFERENT
/// UNITS — bit positions versus character positions — and that split is GR5a's own two-branch sentence, which is
/// exactly the case where two types are right rather than the two-mechanism anti-pattern. Sharing one type let
/// the classifier and the substrate disagree: <see cref="RefModPlace.Category"/> already read the bit group's
/// as-if PICTURE (Boolean, length in BITS) while <c>Read</c> delivered <c>AsImage()</c>'s PACKED bytes
/// (ceil(m/8) characters), so <c>MOVE B"11" TO G(1:2)</c> spliced two characters at BYTE offsets of the packed
/// image and <c>FromImage</c> redistributed the corruption — a silent wrong answer. Do not "simplify" the split
/// back.</para>
/// <para>⛔ A NATIONAL GROUP TAKES <see cref="NatImagePlace"/>, THE SAME SHAPE. This paragraph used to say the
/// opposite — that a national group "deliberately keeps <c>GroupImagePlace</c>" because a national leaf's
/// <c>AsImage()</c> contribution "ALREADY IS its national-position string" — and that premise DIED with kb/Work
/// PB327: <c>AsImage()</c> is the group's BYTE image, and a national position occupies two of those bytes
/// (§13.18.60.4 GR8 / D-N1), which is what admits a national leaf to a file record at all. The bit case and the
/// national case are now one shape read twice: the as-if elementary item's own alphabet gets its own place over
/// the generated carrier face (<c>AsBits()</c> / <c>AsNat()</c>), and <see cref="GroupImagePlace"/> keeps the
/// bytes.</para>
/// <para>Rendered by <c>CodeGen.PlaceRenderer</c> through the generated <c>AsBits()</c> / <c>FromBits(string)</c>
/// pair, which <c>GroupImageCodec.EmitBitMethods</c> already emits for every bit group — no new runtime code.
/// Because <see cref="RefModPlace"/> sits OVER this place, its start/length then index the boolean string
/// directly and GR5a is satisfied structurally.</para>
/// </summary>
public sealed record BitImagePlace(Place Inner) : PlaceDecorator(Inner);

/// <summary>
/// A NATIONAL GROUP viewed as its NATIONAL-CHARACTER string, for reference modification and for every operand
/// context (kb/Work PB327 — the national twin of <see cref="BitImagePlace"/>, and for the same reason).
/// <para><b>The derivation.</b> §13.18.29.4 GR2b: "a national group is treated as though it were an elementary
/// data item of usage national and class and category national described with PICTURE N(m), where m is the length
/// of the group". §8.4.3.3.3 SR1's last sentence: "For reference modification, bit group items and national group
/// items are treated as elementary data items". §8.4.3.3.4 GR5a: positions are BIT positions only when the usage
/// is bit, "otherwise, positions used in evaluation are character positions" — and a national item's character
/// positions are its NATIONAL positions, one UTF-16 code unit each (D-N1), not the two BYTES each occupies.</para>
/// <para>⛔ WHY IT EXISTS AT ALL, given that a national group once rode <see cref="GroupImagePlace"/>: because
/// <c>AsImage()</c> is the group's STORAGE image and a national position takes two bytes of it (§13.18.60.4 GR8;
/// D-N1 pins two, UTF-16BE). While national leaves were barred from every byte-addressed surface the two
/// coincided, which is exactly how the asymmetry looked derived rather than accidental. It is the SAME
/// two-alphabet split <see cref="BitImagePlace"/> carries — <c>AsImage()</c> speaks bytes, <c>AsNat()</c> speaks
/// national positions — so a consumer reaching for the image reader on a national group compares UTF-16BE bytes
/// against national literals.</para>
/// <para>Rendered by <c>CodeGen.PlaceRenderer</c> through the generated <c>AsNat()</c> / <c>FromNat(string)</c>
/// pair (<c>GroupImageCodec.EmitNatMethods</c>, emitted for every national group). Because
/// <see cref="RefModPlace"/> sits OVER this place, its start/length index the national string directly and
/// GR5a is satisfied structurally.</para>
/// </summary>
public sealed record NatImagePlace(Place Inner) : PlaceDecorator(Inner);

/// <summary>
/// The CAPACITY register of an OCCURS DYNAMIC table (ISO/IEC 1989:2023 §13.18.38 GR15 / §8.5.1.9.1; data-model D9):
/// a VIEW over the table's current capacity, NOT its own storage. Reading renders <c>{table-path}.Capacity</c> — the
/// runtime <see cref="CobolNet.Runtime.CobolDynTable{T}.Capacity"/> (a native <c>long</c>, an unsigned integer per
/// SR31), so <see cref="RegisterItem"/> carries a native-binary <see cref="PicInfo"/> (scale 0) and the numeric
/// pipeline reads it as a scale-0 integer with no profile. The register is set ONLY by SET Format 14 (which emits
/// <c>SetCapacity</c>/<c>CapacityUpBy</c>/<c>CapacityDownBy</c> directly); an ordinary store receiver is rejected
/// COBOLNET1523 at bind time (SR30–32), so <c>PlaceRenderer.Write</c> of this place is an internal-error backstop.
/// </summary>
public sealed record CapacityRegisterPlace(AccessPath Table, DataItem RegisterItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>
/// A REPORT SECTION <b>sum counter</b> (ISO/IEC 1989:2023 §13.18.54.4 GR1/GR5/GR12; kb/Work PB840 × PB882): the
/// conceptual numeric data item an entry containing a SUM clause establishes. It is RWCS engine state, never
/// program storage, so this is a VIEW — reading renders <c>__RPT_{n}.SumValue({id})</c> and writing renders
/// <c>__RPT_{n}.SetSumValue({id}, …)</c>, the ONE place GR12's "It is permissible for procedure division
/// statements to alter the content of sum counters" lands. <paramref name="CounterId"/> is the ENTRY's ordinal
/// (GR1 — the identity is the entry, never GR5's data-name, which two entries may legally share);
/// <paramref name="RegisterItem"/> carries the GR1 profile (<see cref="PicInfo.SumCounterItem"/>) so the numeric
/// pipeline reads and writes it at the counter's own scale. Backend-neutral, like
/// <see cref="CapacityRegisterPlace"/>: no C# text lives here.
/// </summary>
public sealed record ReportSumCounterPlace(int ReportIndex, int CounterId, DataItem RegisterItem, int Depth = 0) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>
/// A report's <b>PAGE-COUNTER</b> (ISO/IEC 1989:2023 §8.4.3.15; kb/Work PB429) — the temporary unsigned integer
/// data item §8.4.3.15.4 GR1 maintains for each report, as a receiving-capable place.
/// <para><b>Why a place and not a MOVE arm.</b> §8.4.3.15.3 SR1 does not name a statement: "In the procedure
/// division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an integer data item may
/// appear", and SR3 then subtracts LINE-COUNTER — and only LINE-COUNTER — from the receiving side. A verb-shaped
/// permission would have to be re-granted in MOVE, every arithmetic resultant, INITIALIZE, INSPECT TALLYING and
/// every context added later; a PLACE reaches all of them through the one receiving chokepoint
/// (<c>ExpressionBinder.ResolveReceiving</c>), which is also where SR3's prohibition is screened, so the two
/// halves of the one rule sit side by side. §13.18.37.4 GR6's "(unless procedurally altered)" is the standard
/// confirming that a program assigning page numbers is the intended use.</para>
/// <para>A VIEW over the RWCS engine's counter, never program storage — the <see cref="CapacityRegisterPlace"/>
/// and <see cref="ReportSumCounterPlace"/> pattern: reading renders <c>__RPT_{n}.PageCounter</c> and writing
/// <c>__RPT_{n}.SetPageCounter(…)</c>, produced by <c>CodeGen.PlaceRenderer</c> so no C# text lives here.
/// <paramref name="RegisterItem"/> carries GR1's profile (<see cref="PicInfo.ReportCounterItem"/> — unsigned,
/// scale 0), which is what makes an out-of-range or negative sender behave as it would for any other unsigned
/// integer receiver.</para>
/// </summary>
public sealed record ReportPageCounterPlace(int ReportIndex, DataItem RegisterItem, int Depth = 0) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>The member of the X3.23-1985 <c>DEBUG-ITEM</c> register a <see cref="DebugRegisterPlace"/> refers to
/// (the whole group, or one elementary member). A STRUCTURAL selector — the backend
/// (<c>CodeGen.PlaceRenderer</c>) maps it to the C# read expression, so no C# text lives in the bound tree
/// (the G4 backend-neutrality invariant; the <see cref="CapacityRegisterPlace"/> precedent).</summary>
public enum DebugRegisterMember { Item, Line, Name, Sub1, Sub2, Sub3, Contents }

/// <summary>
/// The X3.23-1985 <c>DEBUG-ITEM</c> special register (and its members DEBUG-LINE / DEBUG-NAME / DEBUG-SUB-1/2/3 /
/// DEBUG-CONTENTS) — the '85 debug module, deleted 2002 and absent ISO 2023, so modeled only at <c>--std 85</c>
/// (VCR Table 7 row 7.17). It is IMPLICITLY described (no DATA DIVISION entry) and referenced only inside debugging
/// declaratives, so it is a VIEW, never its own storage: reading is the read-only program-instance
/// <c>__dbgItem</c> member selected by <paramref name="Member"/> (<see cref="CobolNet.Runtime.DebugItem"/>), which
/// the injected debug trigger populates — the C# text is produced by <c>CodeGen.PlaceRenderer</c>, NOT stored here
/// (backend-neutral, like <see cref="CapacityRegisterPlace"/>). A COBOL program never assigns to a DEBUG-* register
/// (the runtime sets it), so <c>PlaceRenderer.Write</c> of this place is an internal-error backstop.
/// <see cref="RegisterItem"/> carries the member's alphanumeric <see cref="PicInfo"/> (its fixed width) so
/// MOVE / DISPLAY interpret it as an X-item.
/// </summary>
public sealed record DebugRegisterPlace(DataItem RegisterItem, DebugRegisterMember Member) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>
/// The predefined object reference <c>EXCEPTION-OBJECT</c> (ISO §8.4.3.6; kb/Work PB922) — the run unit's
/// current exception object.
/// <para><b>Why it is a place at all.</b> §8.4.3.6.3 SR2: "EXCEPTION-OBJECT is implicitly described as class
/// object and category object reference, as an external data item, and as a universal object reference." It is a
/// DATA REFERENCE the standard itself declares, and NO data description entry declares it — so a compiler whose
/// resolver does not know it answers "'EXCEPTION-OBJECT' is not defined" about a name that is defined, and every
/// legal sending use (<c>IF EXCEPTION-OBJECT = NULL</c>, an INVOKE receiver, a function argument) is refused as
/// a typo. Resolved HERE, in the ONE resolver, every caller inherits the answer — which is why this sits beside
/// <see cref="CapacityRegisterPlace"/> and <see cref="DebugRegisterPlace"/> rather than in a verb.</para>
/// <para>A VIEW, never storage: §8.4.3.6.4 GR2 gives the run unit ONE instance of it, and GR1 makes its value
/// "the current exception object … set to null" when none is associated — which is the runtime's
/// <c>ExceptionState.ExceptionObject</c>. The C# text is produced by <c>CodeGen.PlaceRenderer</c>, never stored
/// here (backend-neutral, the <see cref="CapacityRegisterPlace"/> precedent).</para>
/// <para>⛔ READ-ONLY BY RULE. §8.4.3.6.3 SR1 — "EXCEPTION-OBJECT shall not be specified as a receiving operand"
/// — is screened at the ONE receiving chokepoint (<c>ExpressionBinder.ResolveReceiving</c>) and at SET's own
/// receiver list, so <c>PlaceRenderer.Write</c> of this place is an internal-error backstop, exactly as it is
/// for <see cref="DebugRegisterPlace"/>.</para>
/// <see cref="RegisterItem"/> carries SR2's implicit description — a UNIVERSAL object reference
/// (<c>ObjectRefDescriptor.Universal</c>) — so every class/category screen reads it like any other
/// object-reference item.
/// </summary>
public sealed record ExceptionObjectPlace(DataItem RegisterItem) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => RegisterItem.Pic;

    /// <inheritdoc/>
    public override DataItem Item => RegisterItem;
}

/// <summary>
/// One source reference-modification <c>(start : [length])</c>, reduced to its RENDERED index expressions — the
/// ISO §8.4.3.3.2 general format read off the source, independent of WHAT is being modified. Produced by the ONE
/// reader (<c>ReferenceResolver.ReadRefMod</c>, which accepts both source carriers: the DEFAULT-mode parsed
/// <c>refModPart</c> and the SUBSCRIPT-mode captured token group) and consumed by the two things a ref-mod can
/// attach to:
/// <list type="bullet">
///   <item>a storage <b>place</b> — <see cref="RefModPlace"/>, readable AND writable (the splice);</item>
///   <item>a <b>value</b> with no place — the result of a function-identifier (ISO §8.4.3.3.3 SR2), carried on
///         <c>BoundIntrinsicCall.RefMod</c>. §8.4.3.2.3 SR1 makes a function-identifier a non-receiving operand,
///         so the value form is read-only by construction and needs no splice counterpart.</item>
/// </list>
/// <paramref name="Start"/>/<paramref name="Length"/> are rendered index strings — the D10 TRANSITIONAL carrier,
/// deliberately the SAME one <see cref="RefModPlace"/> uses so PHASE 15 migrates both to <c>BoundExpr</c> in one
/// move rather than leaving a second, differently-shaped ref-mod behind.
/// </summary>
/// <param name="Start">The rendered leftmost-position expression (§8.4.3.3.4 item 5b).</param>
/// <param name="Length">The rendered length expression, or <see langword="null"/> for the omitted
/// "to the end" form (§8.4.3.3.4 item 5c).</param>
/// <param name="AllowZeroLength">The REF-MOD-ZERO-LENGTH directive (ISO §7.3.23) is ON at this ref-mod's source
/// line, so a zero-length result is allowed instead of raising EC-BOUND-REF-MOD.</param>
public readonly record struct RefModSpec(string Start, string? Length, bool AllowZeroLength);

/// <summary>
/// A reference-modified place <c>inner(start:length)</c> (COBOLNET_DESIGN §3.3 / §7.2): reading is a substring
/// (<c>CobolString.RefMod</c>); writing splices the new slice back into the inner field (<c>CobolString.SpliceInto</c>),
/// preserving the inner's width. <paramref name="Length"/> is <see langword="null"/> for the "to the end" form.
/// <paramref name="Start"/>/<paramref name="Length"/> stay the rendered index string (the D10 TRANSITIONAL carrier —
/// they become <c>BoundExpr</c> when PHASE 15 removes the SUBSCRIPT lexer mode). Rendered by
/// <c>CodeGen.PlaceRenderer</c>.
/// </summary>
public sealed record RefModPlace(Place Inner, string Start, string? Length) : PlaceDecorator(Inner)
{
    /// <summary>The REF-MOD-ZERO-LENGTH directive (ISO §7.3.23) is ON at this ref-mod's source line — a zero-length
    /// result is ALLOWED (no EC-BOUND-REF-MOD raise, §8.4.3.3.4 item 5c). The directive's GR default is OFF, so this
    /// is <see langword="false"/> for every ref-mod outside a <c>&gt;&gt;REF-MOD-ZERO-LENGTH ON</c> region — an
    /// init-only property (not a positional member) so existing deconstructions/constructions stay untouched.</summary>
    public bool AllowZeroLength { get; init; }

    /// <summary>The slice's length when it is known at COMPILE time, in identifier-1's own positions
    /// (§8.4.3.3.4 GR5a — bit positions for usage bit, character positions otherwise); <see langword="null"/>
    /// when it is a RUN-TIME quantity. Known exactly when the length is an integer literal, or when it is omitted
    /// ("to the end", §8.4.3.3.4 GR5c) with an integer-literal leftmost position over an identifier-1 of static
    /// <paramref name="innerLength"/>. ⛔ A null is never "the whole item": a static reader that substitutes the
    /// inner item's full length for an unknown slice over-states it (kb/Work PB589).</summary>
    /// <param name="innerLength">identifier-1's length in the same positions, or null when it is not static.</param>
    public int? StaticLength(int? innerLength) =>
        Length is { } len ? (int.TryParse(len, out int n) ? n : null)
        : innerLength is { } size && int.TryParse(Start, out int left) ? size - left + 1 : null;

    /// <summary>⛔ NOTHING A DATA DESCRIPTION ENTRY DECLARES — ISO §8.4.3.3.4 GR5: "Reference modification creates
    /// a unique data item that is a subset of the data item referenced by identifier-1." The slice IS a data item,
    /// and it is NOT <see cref="PlaceDecorator.Item"/> (which stays identifier-1, because GR6 takes the slice's
    /// class/category/usage from it and the storage is identifier-1's). So every rule of the form "shall be the
    /// data item …" — §14.9.41.3 SR5's RELATIVE KEY identity, SR6's record key, the arithmetic resultant screen —
    /// must answer NO here, and every "is this a whole item" test is one reading of this (kb/Work PB602).</summary>
    public override DataItem? DenotedItem => null;

    /// <summary>
    /// The CATEGORY of the unique data item reference modification creates — <b>ISO §8.4.3.3.4 GR6, verbatim</b>:
    /// "The unique data item has the same class, category, and usage as that defined for identifier-1, except
    /// that: a) the category alphanumeric-edited is considered class and category alphanumeric, b) the category
    /// national-edited is considered class and category national, c) the categories numeric and numeric-edited
    /// are considered class and category national if the usage is national; otherwise they are considered class
    /// and category alphanumeric."
    /// <para>
    /// ⛔ <b>THE ONE PLACE THIS RULE IS WRITTEN (fix-queue PB20).</b> It was previously written three times and
    /// none of the three was right: <c>IntrinsicArgumentRules.ClassOfPlace</c> returned class ALPHANUMERIC
    /// unconditionally, <c>ExpressionBinder</c> said the same in prose, and <c>MoveBinder</c> carried a partial
    /// map that got national and boolean right but still flattened national-edited and national-usage numeric.
    /// The base case is the whole point: <b>GR6 PRESERVES the category</b>, and only the three lettered
    /// exceptions rewrite it — so a ref-modified BOOLEAN item stays boolean and a ref-modified NATIONAL item
    /// stays national.
    /// </para>
    /// <para>
    /// ⚠ <b>All three copies justified themselves with "ISO 8.4.2.4", A CLAUSE THAT DOES NOT EXIST</b>
    /// (<c>cite.py --check 8.4.2.4</c> → "there is no clause 8.4.2.4 in the transcription"; §8.4.2 has only
    /// .1/.2/.3, and reference modification is §8.4.3.3). It was inherited into 21 sites across 14 files — the
    /// failure mode CLAUDE.md rule 1 names, at scale. A fabricated citation is how a wrong rule survives review:
    /// every reader saw a § and stopped.
    /// </para>
    /// <para>
    /// NOTE this compiler's <see cref="PicCategory.Alphanumeric"/> covers ISO's alphanumeric AND alphabetic
    /// categories (the alphabetic-ness rides on <c>PicInfo.IsAlphabetic</c>) — and ⛔ ALPHABETIC-NESS DOES SURVIVE
    /// REFERENCE MODIFICATION (re-corrected 2026-08-18, kb/Work PB73, reversing PB72's 2026-08-09 erasure): GR2's
    /// "operated upon for purposes of reference modification as if it were redefined as … alphanumeric" governs the
    /// OPERATION — the positions — while GR6 gives the RESULT "the same class, category, and usage as that DEFINED
    /// for identifier-1" with an EXHAUSTIVE exception list (alphanumeric-edited → alphanumeric, national-edited →
    /// national, numeric / numeric-edited → alphanumeric or national) that names neither alphabetic nor boolean;
    /// the COBOL-85 lineage of the two rules (85 listed "numeric, numeric edited, alphanumeric edited or alphabetic"
    /// in the operated-as-alphanumeric rule and only the first three in the class/category rule) shows they were
    /// always separate. So an alphabetic slice is Table 16's ALPHABETIC row/column and a display-form boolean slice
    /// is BOOLEAN (GR1 gives a boolean item boolean positions in either form; GR5a makes them character positions
    /// unless the usage is bit) — `MOVE A-ITEM(1:2) TO a-boolean` and `MOVE B4(1:1) TO PIC 9` are Table 16 "No"
    /// cells, exactly as their unsliced twins are. Consumers that need the finer Table-16 row build through
    /// <c>Table16Operand.Of(Place)</c>, whose alphabetic rider reads the inner PICTURE. No ref-mod result is ever
    /// category NUMERIC — GR6c rewrites numeric away — which is why the §8.8.1.1 arithmetic-operand bar on a
    /// ref-modified operand is correct as it stands. GnuCOBOL's "any slice is alphanumeric" is a leniency
    /// `--permissive` keeps (a warning) — never the strict axis.
    /// </para></summary>
    /// <remarks>
    /// ⚠ <b>GR6 a AND b HAVE NO ARM HERE, AND THAT IS A PROPERTY OF THE MODEL, NOT OF THE RULE.</b>
    /// <see cref="PicCategory"/> has no <c>AlphanumericEdited</c> member — an alphanumeric-edited item already
    /// carries <see cref="PicCategory.Alphanumeric"/> with an <c>EditMask</c> — so GR6a is satisfied by the base
    /// case rather than skipped. GR6b is the SAME shape and now for the same reason (kb/Work PB492): a
    /// national-edited item carries <see cref="PicCategory.National"/> with an <c>EditMask</c>, so the base arm
    /// preserves category NATIONAL and the EDIT MASK is the thing that must not survive — it does not, because
    /// <c>Table16Operand.Of(Place)</c> builds a ref-mod view with <c>IsEdited</c> false, which is exactly what
    /// GR6b's "considered class and category national" says. **If either category ever gains its own
    /// <see cref="PicCategory"/> member, both arms must appear here**, which is why they are named rather than
    /// silently absent.
    /// </remarks>
    /// <summary>The category of THIS view's unique data item — <see cref="CategoryOf"/> over the inner item's
    /// PICTURE, or ALPHANUMERIC for a ref-modified GROUP (§8.4.3.3.3 SR1 "an alphanumeric group item"; a bit group
    /// or national group would be boolean / national by SR1's last sentence once GROUP-USAGE is modelled — kb/Work
    /// PB79). ⛔ THE ONE READER for consumers that hold a <see cref="RefModPlace"/> (kb/Work PB70): six of them
    /// each re-spelled "Pic is {} ip ? CategoryOf(ip) : Alphanumeric", and one returned null for a group.</summary>
    public PicCategory Category => Inner.Item.OperandPic is { } ip ? CategoryOf(ip) : PicCategory.Alphanumeric;   // a bit / national group is "treated as elementary" (§8.4.3.3.3 SR1 last sentence; D20)

    public static PicCategory CategoryOf(PicInfo inner) => inner.Category switch
    {
        // GR6 c — numeric and numeric-edited become national under a national usage, alphanumeric otherwise.
        PicCategory.Numeric or PicCategory.NumericEdited =>
            inner.Usage == Usage.National ? PicCategory.National : PicCategory.Alphanumeric,
        // GR6 base — the category is PRESERVED. This is the arm the three old copies did not have: it is what
        // keeps a ref-modified BOOLEAN boolean (bit-form AND display-form — kb/Work PB73, adjudicated 2026-08-18:
        // GR6's exception list is exhaustive and names neither boolean nor alphabetic; GR2's alphanumeric
        // redefinition governs the OPERATION, GR1 gives a boolean item boolean positions in either form) and a
        // ref-modified NATIONAL national. Alphabetic-ness likewise survives — it rides Table16Operand.Of(Place)'s
        // rider, since PicCategory has no Alphabetic member.
        _ => inner.Category,
    };

    /// <summary>⛔ <b>THE ONE READER for "the number of positions in the data item referenced by identifier-1"</b>
    /// (ISO §8.4.3.3.4 GR5 b) and c), which bound leftmost-position and length by it) — the COUNT whose UNIT GR5 a)
    /// fixes: "If the usage of identifier-1 is bit, positions used in evaluation are bit positions; otherwise,
    /// positions used in evaluation are character positions." It is the maximum extent any slice of
    /// <paramref name="inner"/> can have, and therefore the capacity §14.9.25.4 GR1's intermediate result item
    /// needs (<c>SendingValueTemp.OfPlace</c>).
    /// <para>⛔ NOT <see cref="DataItem.ImageWidth"/>, which is the item's OCCUPANCY in characters and answers a
    /// different question for exactly the two shapes this reader exists for (kb/Work PB886): a <c>USAGE BIT</c>
    /// item of <c>PIC 1(8)</c> occupies ONE character (§8.5.1.6.3 packs eight boolean positions into a byte) while
    /// GR5 a) counts EIGHT positions, and a DYNAMIC LENGTH item occupies NONE statically (§8.5.1.10 — its current
    /// length is a run-time value) while §8.5.1.10.4 makes a reference-modified one "a fixed-length data item whose
    /// length is the dynamic-length elementary item's current length", bounded by the §13.18.19.4 GR2 LIMIT. Both
    /// measured as a wrong answer: <c>MOVE BE(1:4) TO B1 B2</c> gave <c>1000</c> and <c>MOVE DL(2:5) TO D1 D2</c>
    /// gave <c>B</c>, because a one-character capacity truncated the intermediate.</para>
    /// <para>The BIT and NATIONAL position counts are read through <see cref="DataItem.OperandPic"/> — THE ONE
    /// operand-category/length reader — so §13.18.29.4 GR1b/GR2b's as-if PICTURE answers for a bit group and a
    /// national group, which §8.4.3.3.3 SR1's last sentence ("bit group items and national group items are treated
    /// as elementary data items") requires. A variable-length group cannot be reference-modified at all (SR1's last
    /// bullet), so no arm is owed one.</para></summary>
    public static int PositionCount(DataItem inner) =>
        // §8.5.1.10.4 + §13.18.19.4 GR2 — the current length is a run-time value; the LIMIT is its ceiling.
        inner.IsDynamicLength ? Math.Max(0, inner.DynMaxSize)
        // GR5 a) — bit positions under usage bit; a national item's character positions are its NATIONAL
        // positions (D-N1: one UTF-16 code unit each), never the two bytes each occupies.
        : inner.OperandPic is { Usage: Usage.Bit or Usage.National } p ? p.Length
        // GR5 a)'s "otherwise" — character positions, which for every remaining shape (an alphanumeric or numeric
        // elementary item under GR2's as-if alphanumeric redefinition, including its GR4 SIGN IS SEPARATE
        // position, and an alphanumeric group) IS the item's character occupancy.
        : inner.ImageWidth;

    /// <summary>This view's <see cref="PositionCount"/> — the positions of the item reference modification indexes
    /// (ISO §8.4.3.3.4 GR5).</summary>
    public int InnerPositions => PositionCount(Inner.Item);
}
