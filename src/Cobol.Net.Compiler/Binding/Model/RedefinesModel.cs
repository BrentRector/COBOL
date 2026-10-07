// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>One storage part of a RENAMES THRU span (kb/Work PB96): a leaf of the record (a REDEFINES view is as
/// good a reader of the storage it overlays as the entry it redefines — NC252A's RDF8-5 THRU RDF8-6 lives inside a
/// double redefinition of an OCCURS 36 table), <paramref name="Occurrence"/> = null for the WHOLE leaf (every
/// occurrence of a table leaf, in order) or the 1-based occurrence the part lies in, and <paramref name="StartByte"/>
/// (1-based) / <paramref name="LengthBytes"/> the range of that occurrence's STORAGE BYTES the alias covers — the
/// whole cell (the ordinary case) or a partial slice when a span boundary lands inside it.
/// <para>⛔ A PART IS KEPT IN STORAGE BYTES, NEVER IN ITS LEAF'S CHARACTER POSITIONS (kb/Work PB2466, PB1902).
/// §13.18.45.4 GR2 makes data-name-1 "an alphanumeric group item" over the record's storage, so the window is tiled
/// in bytes, and a REDEFINES view of a different layout can put a part boundary — or leave an interior byte no other
/// leaf covers — on the odd byte of a national character (D-N1: two bytes per national position). That byte is no
/// range of the national item's positions, but it IS a range of its storage, which is what the alias renames: the
/// resolver addresses every part as a byte slice of its cell's storage image (<c>ReferenceResolver.PlaceForRenames</c>,
/// <see cref="NationalBytesPlace"/>), so every part the tiling can choose is addressable. Keeping the part in
/// positions made a half-character part unrepresentable, and legal source drew COBOLNET1655 "do not tile".</para>
/// <para><paramref name="Outer"/> is the 1-based occurrence of each OCCURS GROUP the leaf sits under, outermost first
/// (kb/Work PB986): §13.18.45.3 SR3 bars an OCCURS only on the three NAMED operands, so a table may lie INSIDE the
/// range, and a leaf of such a table lives once per occurrence of its group — S4 of <c>ST OCCURS 2</c> is
/// <c>S4 OF ST(1)</c> and <c>S4 OF ST(2)</c>, two parts, not one. Empty for a leaf under no table.</para></summary>
public sealed record RenamesSpanPart(DataItem Leaf, int? Occurrence, int StartByte, int LengthBytes, IReadOnlyList<int> Outer)
{
    /// <summary>The subscript of each OCCURS level on the leaf's path, outermost first — the table groups' occurrences
    /// (<see cref="Outer"/>) and then <paramref name="own"/> for the leaf's own OCCURS when it has one: the index list
    /// <c>ReferenceResolver.PlaceForItem</c> takes for one cell of the leaf.</summary>
    public IReadOnlyList<string> SubscriptsFor(int? own) =>
        [.. Outer.Select(o => o.ToString()), .. own is { } k ? new[] { k.ToString() } : Array.Empty<string>()];

    /// <summary>The whole leaf — every occurrence, every byte (the composed accessor's fast path).</summary>
    public bool IsWhole => Occurrence is null;

    /// <summary>A part that covers only some bytes of its occurrence (renders as a byte slice of the cell's storage
    /// image).</summary>
    public bool IsPartial => Occurrence is not null && (StartByte != 1 || LengthBytes != Leaf.ByteWidth);

    /// <summary>The part's extent in its leaf's CHARACTER positions when it covers whole positions — the carrier
    /// geometry a <c>GroupAtom</c> records beside the bytes (a national position is two bytes, D-N1) — and its bytes
    /// otherwise: a slice that splits a national character covers no whole position of the leaf, and the alias's
    /// image holds it as the storage bytes it is.</summary>
    public int Positions
    {
        get
        {
            int perPosition = Leaf.ImageWidth > 0 ? Leaf.ByteWidth / Leaf.ImageWidth : 1;
            return (StartByte - 1) % perPosition == 0 && LengthBytes % perPosition == 0
                ? LengthBytes / perPosition
                : LengthBytes;
        }
    }
}

/// <summary>
/// A level-66 RENAMES descriptor (ISO/IEC 1989:2023 §13.18.45): a re-grouping alias over a contiguous run of sibling
/// items, <c>RENAMES data-name-2 [THRU data-name-3]</c>. RENAMES adds no storage — it is a COMPOSED view over the
/// existing fields (GR1 no-THRU = attribute inheritance; GR2 THRU = an alphanumeric group view over the spanned
/// leaves). The names are resolved to items, and the span flattened to its leaves, by the post-build pass.
/// </summary>
public sealed class RenamesInfo
{
    /// <summary>The <c>data-name-2</c> (FROM) operand text as written.</summary>
    public required string FromName { get; init; }

    /// <summary>The <c>data-name-3</c> (THRU) operand text, or <see langword="null"/> for the no-THRU form.</summary>
    public string? ThruName { get; init; }

    /// <summary>The IN/OF qualifiers written on data-name-2 — innermost first (kb/Work PB978). They are NOT
    /// redundant inside the owning record: two same-named items in one record are distinguished only by them
    /// (§8.4.2.2.3 SR1).</summary>
    public IReadOnlyList<string> FromQualifiers { get; init; } = [];

    /// <summary>The IN/OF qualifiers written on data-name-3 (empty for the no-THRU form).</summary>
    public IReadOnlyList<string> ThruQualifiers { get; init; } = [];

    /// <summary>The resolved FROM item (set post-build).</summary>
    public DataItem? From { get; set; }

    /// <summary>The resolved THRU item (set post-build; <see langword="null"/> for the no-THRU form).</summary>
    public DataItem? Thru { get; set; }

    /// <summary>The STORAGE parts the THRU span covers, in record storage order (kb/Work PB96): each part is one
    /// leaf of the record — a REDEFINES view's leaf as readily as the redefined entry's, since either reads the
    /// storage — and the 1-based range of its STORAGE BYTES inside the window (a whole leaf is (1, its byte width); a
    /// boundary that falls inside a leaf — a FROM / THRU that is a partial redefinition of it — makes a partial
    /// part). The tiling advances by what each part covers, so no byte is covered twice.</summary>
    public List<RenamesSpanPart> Span { get; } = [];

    /// <summary>The leaves the rename spans (the parts' leaves) — the strong-type check's view.</summary>
    public IEnumerable<DataItem> SpanLeaves => Span.Select(p => p.Leaf);

    /// <summary>True for a single no-THRU alias (Tier A forward, GR1); false for a THRU span (a Tier-B composition, GR2).</summary>
    public bool IsAlias => ThruName is null;

    /// <summary>⛔ THE DATA ITEMS A THROUGH ALIAS <i>INCLUDES</i>, in declaration order — §13.18.45.4 GR2 verbatim:
    /// <i>"When the THROUGH phrase is specified, data-name-1 defines an alphanumeric group item that includes all
    /// elementary items starting with data-name-2 (if data-name-2 is an elementary item) or the first elementary
    /// item in data-name-2 (if data-name-2 is a group item), and concluding with data-name-3 (if data-name-3 is an
    /// elementary item) or the last elementary item in data-name-3 (if data-name-3 is a group item)."</i>
    /// <para>This is the alias's MEMBERSHIP, which is a different question from its STORAGE (<see cref="Span"/>):
    /// the span is the record's storage bytes tiled once each by whichever leaves cover them, while GR2 names the ELEMENTARY ITEMS — every one of them between the two endpoints in the order
    /// the entries are written, a redefining entry's items included. The one reader today is §14.7.6's "a data
    /// item in D1" when D1 is an alias (kb/Work PB966); a rule asking which items an alias INCLUDES reads this,
    /// and a rule asking which characters it OCCUPIES reads <see cref="Span"/>.</para>
    /// <para>Empty for the no-THROUGH form (GR1 gives the alias data-name-2's own attributes — it is that item,
    /// and has no members of its own) and for an alias whose operands did not resolve.</para></summary>
    public IReadOnlyList<DataItem> IncludedElementaryItems => _included ??= ComputeIncluded();

    private IReadOnlyList<DataItem>? _included;

    private IReadOnlyList<DataItem> ComputeIncluded()
    {
        if (From is not { } from || Thru is not { } thru) return [];
        DataItem root = from;
        while (root.Parent is { } up) root = up;
        DataItem first = from, last = thru;
        while (!first.IsElementary && first.Children.Count > 0) first = first.Children[0];
        while (!last.IsElementary && last.Children.Count > 0) last = last.Children[^1];
        var items = new List<DataItem>();
        bool inside = false, done = false;
        void Walk(DataItem n)
        {
            if (done) return;
            if (n.IsElementary)
            {
                if (ReferenceEquals(n, first)) inside = true;
                if (inside) items.Add(n);
                if (ReferenceEquals(n, last)) done = true;
                return;
            }
            foreach (var c in n.Children) Walk(c);
        }
        Walk(root);
        return done ? items : [];
    }
}

/// <summary>
/// WHICH CONSTRUCT makes an item share its storage area with <see cref="DataItem.RedefinesTarget"/> (kb/Work PB836).
/// Three different constructs produce ONE storage fact — the redefines class that the layout, the tiering and the
/// emitter all read — and the rules that screen them are NOT the same rules:
/// <list type="bullet">
///   <item><see cref="Clause"/> — a written REDEFINES clause (ISO §13.18.44). Its syntax rules (§13.18.44.3 SR5,
///     SR8, SR12, SR14, SR16, SR17) are rules about THE CLAUSE: its subject and its data-name-2 operand.</item>
///   <item><see cref="ImplicitFileRecord"/> — the second and later level-1 entries of one FD or SD, which
///     "represent implicit redefinitions of the same area" (§13.18.33.4 GR3). No clause was written, so no
///     REDEFINES-clause rule applies; §13.18.57.3 SR4 ("shall not be implicitly or explicitly redefined") does.</item>
///   <item><see cref="SameRecordArea"/> — the first record of a later file in a record-area SAME clause, which "is
///     equivalent to an implicit redefinition of the area with records aligned on the leftmost byte position"
///     (§12.4.6.4.4 GR2). Screened exactly as <see cref="ImplicitFileRecord"/>.</item>
/// </list>
/// The binder used to write the storage fact alone, so the file section's implicit redefinitions reached every
/// REDEFINES-clause screen and were refused under rules about source the program never wrote.
/// </summary>
public enum RedefinitionKind
{
    /// <summary>The item redefines nothing.</summary>
    None,
    /// <summary>A written REDEFINES clause (§13.18.44).</summary>
    Clause,
    /// <summary>A later level-1 record of one FD/SD (§13.18.33.4 GR3).</summary>
    ImplicitFileRecord,
    /// <summary>The record of a later file of one record-area SAME clause (§12.4.6.4.4 GR2).</summary>
    SameRecordArea,
}

/// <summary>
/// The overlay tier of a redefines class (COBOLNET_DESIGN §4.2; priority cascade D &gt; C &gt; B &gt; A, lattice
/// A ⊑ B ⊑ C ⊑ D, join = max tier). Every member of a class shares one stored canonical backing; the tier decides
/// what that backing is and how each view reads/writes it.
/// </summary>
public enum RedefinesTier
{
    /// <summary>A — identical storage type (same PIC+USAGE, or numeric-over-numeric of the same digit count, or
    /// RENAMES no-THRU): one typed field; every other name is a pass-through over it (a numeric view carries its own
    /// scale/profile, so the shared unscaled value reinterprets for free).</summary>
    Alias,
    /// <summary>B — the shared area as ONE <see cref="string"/> backing of class-max BYTE width under the
    /// Latin-1 char==byte convention; each view a typed (offset, StorageWidth) window whose bytes are the
    /// leaf's pinned <see cref="CobolNet.Runtime.NumericByteForm"/> representation (zoned, radix-2, BCD, IEEE,
    /// the R40 index bytes — the Step D arm-1 dissolution admitted every numeric leaf kind; the former Tier C
    /// dissolved into this). The dominant real case.</summary>
    StringCanonical,
    /// <summary>D — spec-forbidden / unmodelable: a loud diagnostic, which is conformant since these are already
    /// illegal. The rejection set, one entry per screen, each with the rule it enforces:
    /// <list type="bullet">
    ///   <item>§13.18.44.3 SR12/SR14 — object / pointer / strongly-typed, per WRITTEN ENTRY (COBOLNET1697,
    ///     kb/Work PB179).</item>
    ///   <item>§13.18.44.3 SR17 — neither side a variable-length group per §8.5.1.12.1, nor a dynamic-length
    ///     elementary item (COBOLNET1698, kb/Work PB177 arm C).</item>
    ///   <item>§13.18.44.3 SR5 SENTENCE 1 — "The data description entry for data-name-2 shall not contain an
    ///     OCCURS clause", every format of it (COBOLNET1701).</item>
    ///   <item>§13.18.44.3 SR5 SENTENCE 4 — "Neither the original definition nor the redefinition shall include
    ///     an occurs-depending table" (COBOLNET0855).</item>
    ///   <item>The SUBJECT that IS a dynamic-capacity table — the one shape in this family that no syntax rule
    ///     literally names, decided by §13.18.44.4 GR1's storage association against §8.5.1.9.1 (COBOLNET1525).</item>
    /// </list>
    /// <para>⛔ THIS LIST HAS BEEN WRONG TWICE, IN OPPOSITE DIRECTIONS, AND BOTH ARE WHY IT IS NOW SPELLED PER
    /// SENTENCE. First it read "OCCURS DEPENDING ON / variable-length SR5/17" while the dynamic-LENGTH half of
    /// SR17 had NO screen anywhere in the tree — worse than unscreened, it was silently mis-modelled
    /// (<c>StorageFormPass</c> gave such a view its own disjoint storage). Then the repair narrowed "SR5" to
    /// "SR5's occurs-depending table (COBOLNET0855)" — accurate about sentence 4 and silently dropping sentence
    /// 1, whose population (a data-name-2 carrying a fixed OCCURS) was screened NOWHERE and compiled clean. A
    /// four-sentence rule cited by NUMBER hides which sentence is meant; a tier doc asserting coverage the code
    /// lacks is how a gap survives a reading, and the negative fixtures beside each code are what make these
    /// sentences checkable.</para></summary>
    Rejected,
}

/// <summary>
/// A storage-sharing equivalence class produced by the post-build classification pass: the items that overlay one
/// storage area (ISO §13.18.44 — a REDEFINES original + every entry that redefines it, directly or transitively).
/// Exactly ONE member is the stored canonical (<see cref="Canonical"/>); every other member is a computed view over
/// the single backing (COBOLNET_DESIGN §4.1 — never two stored fields per area).
/// </summary>
public sealed class RedefinesClass
{
    /// <summary>The non-redefining anchor (the original; SR7/SR11) — the one stored member.</summary>
    public required DataItem Canonical { get; init; }

    /// <summary>The class members (canonical + every redefiner), in source order.</summary>
    public List<DataItem> Members { get; } = [];

    /// <summary>The overlay tier (decides the backing kind + the view accessors). Written ONLY through
    /// <see cref="Classify"/> (P5.11d).</summary>
    public RedefinesTier Tier { get; private set; }

    /// <summary>The class-max image width in BYTES (== character positions under the Latin-1 char==byte
    /// backing convention; a level-01 non-EXTERNAL original may be redefined larger, SR8).
    /// Written ONLY through <see cref="Classify"/> (P5.11d).</summary>
    public int Width { get; private set; }

    /// <summary>The C# name of the single stored backing field for a Tier-B (byte-window) class.</summary>
    public string BackingCsName => "_redef_" + Canonical.CsName;

    /// <summary>The C# name of the <c>StorageCell</c> BEHIND the backing, for the three CELL-BACKED surfaces —
    /// EXTERNAL (the run-unit <c>ExternalStore</c> cell), ADDRESS OF (the per-instance cell) and BASED (the
    /// pointer-deref bridge). The emitters define it and then define <see cref="BackingCsName"/> as
    /// <c>ref {cell}.Ref</c>, so the byte image and the cell's MANAGED SLOTS (kb/Work PB231 — the pointer third)
    /// are provably the same storage area rather than two expressions that happen to agree. It is
    /// <see cref="IsCellBacked"/> that says whether this name is emitted; a plain REDEFINES class's backing is a
    /// stored string field with no cell behind it.</summary>
    public string BackingCellCsName => "_scell_" + Canonical.CsName;

    /// <summary>True once one of the three cell surfaces has claimed this class, i.e.
    /// <see cref="BackingCellCsName"/> is emitted and a <c>SlotWindow</c> member may address it (kb/Work PB231).
    /// Set by <c>DataBinder.ForceStringCanonical</c>'s callers, which are the only sites that create a cell.</summary>
    public bool IsCellBacked { get; set; }

    /// <summary>For a BASED class (Phase-4b increment 2, ISO §13.18.5): the C# name of the implicit
    /// data-address pointer field (<c>__addr_X</c>). The backing "field" is then a deref bridge property
    /// (<c>ref CobolPtr.Deref(__addr_X, width).Ref</c>) and every view's window offset is displaced by the
    /// pointer's runtime offset (<c>CobolPtr.OffsetOf</c>) — the ONE place-construction site adds it. Null
    /// for ordinary (stored / external-cell / addressable-cell) classes.</summary>
    public string? BasedPointerField { get; set; }

    /// <summary>The loud-reject reason when <see cref="Tier"/> is <see cref="RedefinesTier.Rejected"/>, else null.
    /// Written ONLY through <see cref="Classify"/> (P5.11d).</summary>
    public string? RejectReason { get; private set; }

    /// <summary>THE one verdict-application site (P5.11d — single-source the tier verdict, DESIGN-data-model §2.3):
    /// exactly two callers exist. <c>DataBinder.ClassifyRedefinesClasses</c> applies the §13.18.44 REDEFINES
    /// verdict once per class (its <c>ComputeTier</c> reason table carries the ISO citations — the 2-byte
    /// national overlay per D-N1/D-N2 and the dynamic-table reject per §13.18.44 SR5; the float/COMP-5/BINARY-*/
    /// INDEX "Tier-C island" row is GONE, since every numeric usage now has a pinned §13.18.60.4 byte form and a
    /// mixed-usage class is an ordinary Tier-B byte-window class — kb/Work PB164, the Step D arm-1 dissolution).
    /// <c>DataBinder.ForceStringCanonical</c> — the ONE cell-backing
    /// forcer — may RE-classify an already-classified class: EXTERNAL/BASED/ADDRESS-OF re-basing deliberately
    /// overrides the stored-member verdict with the cell-backed Tier-B form (§13.18.22.4 GR5), or rejects when a
    /// leaf has no single-byte character image. No other writer exists; a new tier decision goes through one of
    /// those two, never a scattered assignment.</summary>
    internal void Classify(RedefinesTier tier, int width, string? rejectReason)
    {
        Tier = tier;
        Width = width;
        RejectReason = rejectReason;
    }
}
