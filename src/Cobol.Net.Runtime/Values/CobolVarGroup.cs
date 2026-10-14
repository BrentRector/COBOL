// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ACTIVATION-BOUNDARY CARRIER OF A VARIABLE-LENGTH GROUP (ISO §8.5.1.12; kb/Work PB204).
/// <para>A fixed-length group crosses a CALL / INVOKE boundary as ONE string — its record image — because
/// §14.2.3 GR8 makes the formal "occupy the same storage area as the argument" and that storage has a fixed
/// window. A VARIABLE-LENGTH group has no such window, so a flat string cannot be inverted: the receiver
/// cannot tell where a dynamic member's content ends and the next fixed member begins. This carrier is the
/// §8.5.1.12 model made into a wire form, and nothing more:</para>
/// <list type="bullet">
/// <item><see cref="Fixed"/> — the group's image with every variable-length component contributing NOTHING.
/// §8.5.1.12.3 says "all dynamic-length elementary items are considered to be of zero length" and makes a
/// matched dynamic-capacity table "the length of a single element", which is exactly the byte accounting under
/// which compatible groups have the SAME relative positions. So both sides lay this string out identically as
/// far as their fixed material agrees, and any tail difference is the §14.8.2.2 rule-1 size latitude, absorbed
/// by the ordinary width window.</item>
/// <item><see cref="Dynamic"/> — each variable-length component's CURRENT content, in declaration order:
/// a dynamic-length elementary item's characters, a dynamic-capacity table's occurrences concatenated at its
/// current capacity. §8.5.1.12.2's positional correspondence puts the two sides' components in the same order,
/// one for one, which is why an ordinal array is a faithful carrier and not an encoding trick. The receiving
/// table recovers its capacity by dividing by its own element width — legitimate because §8.5.1.12.3 makes
/// corresponding tables match only when "the byte length of their elements is equal".</item>
/// </list>
/// <para>NESTING is flattened by the emitted composer, because §8.5.1.12 is stated over relative byte positions
/// and is blind to the declaration tree: a nested variable-length group contributes its own fixed run and its
/// own dynamic components inline, and <see cref="Slice"/> hands it back exactly that window on the way in.</para>
/// <para>⛔ ONE SHAPE IS NOT FLATTENED — A DYNAMIC-CAPACITY TABLE WHOSE ELEMENTS ARE THEMSELVES VARIABLE-LENGTH GROUPS
/// (kb/Work PB2496). Its occurrence count is a run-time quantity and each occurrence holds components of its own, so
/// splicing the occurrences' components into this list would make the ordinal of every LATER component depend on the
/// table's current capacity, and the positional correspondence the ordinal list rests on would be lost. The table is
/// ONE component, and its occurrences' own carriers ride beside it in <see cref="Elements"/>: §14.6.9.2 moves it
/// "Correspondingly numbered elements … according to the rules of the MOVE statement" (each occurrence a group MOVE
/// of its own) and §14.6.9.3 compares it element by element, so the element carriers are exactly what both
/// operations take.</para>
/// <para>⛔ NOT a general-purpose serializer, and never persisted: it exists only between the argument
/// evaluation and the formal's copy-in (and back at the copy-out), the same lifetime the string image has.</para>
/// </summary>
/// <param name="Fixed">The group's image with the variable-length components collapsed to zero width.</param>
/// <param name="Dynamic">Each variable-length component's current content, in declaration order. A NESTED component
/// (one with <see cref="Elements"/>) has no character content of its own and holds the empty string.</param>
/// <param name="Elements">Null when no component is nested; otherwise one entry per component of
/// <paramref name="Dynamic"/>: for a dynamic-capacity table of variable-length group elements, its occurrences' own
/// carriers in occurrence order up to its current capacity, and null for every other component.</param>
/// <param name="Created">Null unless the carrier describes STORAGE; otherwise one entry per component of
/// <paramref name="Dynamic"/>: for a dynamic-capacity table with the INITIALIZED phrase, the element a statement creates in
/// it at a 1-based occurrence number (its <c>CobolDynTable</c>'s creation seed, §8.5.1.9.5's INITIALIZED default) as a
/// carrier in the table's ELEMENT shape, and null for every other component (kb/Work PB2690) — a table without the phrase
/// included, whose created content §8.5.1.9.5 leaves undefined (<c>CobolDynTable.CreatedAs</c>). The storage builders state
/// it (the emitted <c>AsVarImage</c>, <c>StorageCell.VarGroupAt</c>) and the structural rebuilds of the same storage keep
/// it (<see cref="Slice"/>, <see cref="Concat"/>, <see cref="SplitTail"/>, <see cref="JoinTail"/>); a conversion's result
/// (<see cref="Reshape"/>, <see cref="Overlay"/>) is a value and states none. The builders state it only for a BY
/// REFERENCE crossing (their <c>byReference</c> argument), never for a MOVE's sender or a BY CONTENT copy, so the carrier
/// itself says which it is: <see cref="Reshape"/> shows a storage table's occurrences past its capacity as they would be
/// created, and <see cref="Overlay"/> creates the ones the callee stored into from it.</param>
public sealed record CobolVarGroup(string Fixed, string[] Dynamic, CobolVarGroup[]?[]? Elements = null,
    Func<int, CobolVarGroup>?[]? Created = null)
{
    /// <summary>The empty carrier — an absent / OMITTED argument's value (ISO §14.9.4.4 GR11 hands out a
    /// carrier whose accessors raise; this is the shape those accessors return when checking is off).</summary>
    public static readonly CobolVarGroup Empty = new("", []);

    /// <summary>Component <paramref name="i"/>, or the zero-length string when the sending side carried fewer
    /// components than this side declares. A SHORTER sender is the §14.8.2.2 rule-1 direction the standard
    /// permits (the formal may be described with fewer bytes than the argument, and the reverse is diagnosed at
    /// bind), so a missing component is a zero-length value, never an index fault.</summary>
    public string Dyn(int i) => (uint)i < (uint)Dynamic.Length ? Dynamic[i] : "";

    /// <summary>Whether component <paramref name="i"/> was CARRIED at all — distinct from its being carried
    /// EMPTY, and the distinction is normative (kb/Work PB393). ISO §14.9.25.4 GR9b space-fills the receiving
    /// group's excess part, and its step 2 sends a dynamic-capacity table there to §14.6.9.4, where "the current
    /// capacity of the dynamic table is unaffected, and each element of the dynamic table is space-filled" —
    /// whereas a table whose SENDER carried a zero-capacity table is recreated at capacity zero by §14.6.9.2's
    /// "recreates or overwrites the receiving table with a copy of the sending table". Both arrive as a
    /// zero-length component string; only this tells them apart.</summary>
    public bool HasDyn(int i) => (uint)i < (uint)Dynamic.Length;

    /// <summary>Component <paramref name="i"/>'s occurrence carriers when it is a NESTED component — a carried
    /// dynamic-capacity table of variable-length group elements (kb/Work PB2496) — else null.</summary>
    public CobolVarGroup[]? ElementsAt(int i) => Elements is { } e && (uint)i < (uint)e.Length ? e[i] : null;

    /// <summary><see cref="Elements"/> as one entry per component, null for every plain one — what an enclosing group
    /// splices in place when it flattens this carrier into its own (the emitted <c>AsVarImage</c>).</summary>
    public CobolVarGroup[]?[] ElementList => Elements ?? new CobolVarGroup[]?[Dynamic.Length];

    /// <summary>Component <paramref name="i"/>'s element creation (<see cref="Created"/>) when it is a dynamic-capacity
    /// table of storage, else null.</summary>
    public Func<int, CobolVarGroup>? CreatedAt(int i) => Created is { } c && (uint)i < (uint)c.Length ? c[i] : null;

    /// <summary><see cref="Created"/> as one entry per component, null for every other one — what an enclosing group
    /// splices in place beside <see cref="ElementList"/> when it flattens this carrier into its own (the emitted
    /// <c>AsVarImage</c>).</summary>
    public Func<int, CobolVarGroup>?[] CreatedList => Created ?? new Func<int, CobolVarGroup>?[Dynamic.Length];

    /// <summary>⛔ THE OCCURRENCE CARRIERS A NESTED RECEIVER STORES (kb/Work PB2496): component <paramref name="i"/>'s
    /// <see cref="ElementsAt"/>, or the empty list when the component was carried EMPTY (a sender whose table had no
    /// occurrence, §14.6.9.2's recreation at capacity zero). The caller asks <see cref="HasDyn"/> first: an absent
    /// component is §14.6.9.4's space fill. Every carrier of a nested table is built with its occurrence carriers
    /// (the emitted composer, <see cref="Reshape"/>, <see cref="Overlay"/>): a fixed-length group meets one through the
    /// pair's two shapes (<see cref="Reshape"/> over its image, kb/Work PB2690), so a nested component that holds
    /// characters but no occurrence is a carrier built wrongly, never a legal pair.</summary>
    /// <exception cref="InvalidOperationException">The component holds characters and no occurrence carriers.</exception>
    public CobolVarGroup[] ElementCarriersAt(int i) =>
        ElementsAt(i) ?? (Dyn(i).Length == 0 ? [] : throw new InvalidOperationException(
            $"internal error: component {i} of a variable-length group carrier is a table of variable-length elements "
            + "carried as characters, with no occurrence carriers (ISO §14.6.9.2 over §8.5.1.12.3)"));

    /// <summary>The window a NESTED variable-length group occupies inside this carrier:
    /// <paramref name="fixedWidth"/> character positions of <see cref="Fixed"/> starting at
    /// <paramref name="fixedAt"/> (space-padded when the sender's fixed run was shorter — the same store rule
    /// every image distribution uses), and <paramref name="dynCount"/> components starting at
    /// <paramref name="dynAt"/>.</summary>
    public CobolVarGroup Slice(int fixedAt, int fixedWidth, int dynAt, int dynCount)
    {
        string f = CobolString.Store(
            fixedAt >= Fixed.Length ? "" : Fixed[fixedAt..Math.Min(Fixed.Length, fixedAt + fixedWidth)],
            fixedWidth);
        // ⛔ The window carries only what was ACTUALLY carried, never dynCount padded with empties: a nested
        // group must be able to answer <see cref="HasDyn"/> the same way its parent can, or GR9b step 2's
        // §14.6.9.4 space fill degrades into §14.6.9.2's recreate-at-zero one level down (kb/Work PB393).
        var d = new string[Math.Clamp(Dynamic.Length - dynAt, 0, dynCount)];
        for (int k = 0; k < d.Length; k++) d[k] = Dyn(dynAt + k);
        CobolVarGroup[]?[]? e = null;
        if (Elements is not null)
        {
            e = new CobolVarGroup[]?[d.Length];
            for (int k = 0; k < e.Length; k++) e[k] = ElementsAt(dynAt + k);
        }
        Func<int, CobolVarGroup>?[]? c = null;
        if (Created is not null)
        {
            c = new Func<int, CobolVarGroup>?[d.Length];
            for (int k = 0; k < c.Length; k++) c[k] = CreatedAt(dynAt + k);
        }
        return new CobolVarGroup(f, d, e, c);
    }

    /// <summary>The carrier of a fixed-OCCURS table of variable-length group elements (kb/Work PB244): each
    /// occurrence's fixed run and components, in occurrence order — the same flattening a nested scalar group
    /// gets, repeated. §8.5.1.12.2 puts each occurrence's dynamic-length items at their own relative byte
    /// positions, so the multiplicity is the table's compile-time <c>OCCURS</c> count.</summary>
    public static CobolVarGroup Concat(CobolVarGroup[] occurrences)
    {
        var fixedRun = new System.Text.StringBuilder();
        var dyn = new List<string>();
        var elements = new List<CobolVarGroup[]?>();
        var created = new List<Func<int, CobolVarGroup>?>();
        bool nested = false, creates = false;
        foreach (var o in occurrences)
        {
            fixedRun.Append(o.Fixed);
            dyn.AddRange(o.Dynamic);
            for (int k = 0; k < o.Dynamic.Length; k++)
            {
                elements.Add(o.ElementsAt(k));
                created.Add(o.CreatedAt(k));
            }
            nested |= o.Elements is not null;
            creates |= o.Created is not null;
        }
        return new CobolVarGroup(fixedRun.ToString(), [.. dyn], nested ? [.. elements] : null, creates ? [.. created] : null);
    }

    /// <summary>This carrier with the group's TRAILING fixed material cut off the fixed run and appended as one
    /// more variable-length component (kb/Work PB244). The group's OCCURS DEPENDING table is the trailing storage
    /// of its record (§13.18.38.3 SR22) and rides the fixed run of the activation-boundary carrier, where
    /// §14.8.2.2 gives it the maximum length; in a RECORD's contiguous layout it is the last variable-length
    /// component instead, because the record's own length — not a constant — says how many occurrences it holds.
    /// <paramref name="tailAt"/> is where the table starts in the fixed run.</summary>
    public CobolVarGroup SplitTail(int tailAt) =>
        new(CobolString.Store(Fixed, tailAt), [.. Dynamic, Fixed.Length > tailAt ? Fixed[tailAt..] : ""],
            Elements is null ? null : [.. Elements, null], Created is null ? null : [.. Created, null]);

    /// <summary>The inverse of <see cref="SplitTail"/>: the LAST component rejoins the fixed run at
    /// <paramref name="tailAt"/>.</summary>
    public CobolVarGroup JoinTail(int tailAt) =>
        Dynamic.Length == 0 ? this
        : new(CobolString.Store(Fixed, tailAt) + Dynamic[^1], Dynamic[..^1], Elements?[..^1], Created?[..^1]);

    // ── The §8.5.1.12 LAYOUT of a group (kb/Work PB965) ───────────────────────────────────────────────────────────
    //
    // A group's layout is a FLAT sequence of (kind, chars, elementChars) triples in CHARACTER positions, left to
    // right, that GroupCompatibility.Layout derives from the group's §8.5.1.12 atoms (REDEFINES subtrees dropped, scalar
    // subordinate groups flattened, a table kept whole, consecutive fixed material merged). It answers the STORAGE
    // question (GroupCompatibility.LaysOver: can this description be laid over that area) and words a message; the
    // correspondence of a PAIR is the atoms' (GroupCompatibility.Walk, Reshape, Overlay), which carry every table's
    // element layout.

    /// <summary>Layout atom kind: fixed material (bytes that are neither a table nor dynamic).</summary>
    public const int LayoutFixed = 0;
    /// <summary>Layout atom kind: a table with a fixed number of occurrences; chars = its whole width.</summary>
    public const int LayoutTable = 1;
    /// <summary>Layout atom kind: an OCCURS DEPENDING table; chars = its maximum width (§13.18.38.3 SR22 makes
    /// it the last atom, so its current extent is "the rest of the image").</summary>
    public const int LayoutOdoTable = 2;
    /// <summary>Layout atom kind: a dynamic-capacity table (§8.5.1.9); chars = ONE element's width.</summary>
    public const int LayoutDynamicTable = 3;
    /// <summary>Layout atom kind: a dynamic-length elementary item (§8.5.1.10); chars = 0.</summary>
    public const int LayoutDynamicLength = 4;

    // ── A FIXED-LENGTH group meeting a variable-length group's carrier (kb/Work PB2690) ───────────────────────────
    //
    // ISO §8.5.1.12.1 admits the pair ("either both operands may be variable-length groups or only one of the operands
    // may be a variable-length group"), and §8.5.1.12.3 sentence 3 says how: a table corresponding to the other group's
    // dynamic-capacity table "is treated as though it were a dynamic-capacity table whose capacity is either its fixed
    // number of occurrences or the value of the DEPENDING operand, as applicable" (§14.6.9.1 states the same conversion for
    // the operation). So a fixed-length group is its record image with no components, in its OWN shape (its §8.5.1.12
    // atoms; a group with no table states none and is one fixed run of its width, GroupCompatibility.FixedRun), and it is
    // paired with the variable-length group by the same two-shape conversions a MOVE and a comparison use: Reshape and
    // Overlay. The activation boundaries (CALL, INVOKE, RETURNING) call the four members below, so there is ONE
    // correspondence for a fixed-length group and it carries the element layout of every table (a fixed table opposite a
    // dynamic-capacity table of variable-length elements is paired element by element, §14.6.9.2).

    /// <summary>A FIXED-length group's record image as a carrier in its own shape: no components, the whole image its
    /// fixed run.</summary>
    public static CobolVarGroup OfImage(string image) => new(image, []);

    /// <summary>The carrier of a FIXED-length group's record image <paramref name="image"/> (of shape
    /// <paramref name="imageShape"/>) in the VARIABLE-length group shape <paramref name="to"/> — the fixed group's table
    /// crossing at its fixed (or DEPENDING) occurrence count (§8.5.1.12.3 sentence 3), a table of fixed-length elements
    /// cut at its element width, each occurrence a carrier of its own.</summary>
    public static CobolVarGroup FromImage(string image, GroupAtom[] imageShape, GroupAtom[] to) =>
        Reshape(OfImage(image), imageShape, to);

    /// <summary>A variable-length carrier <paramref name="v"/> (of shape <paramref name="from"/>) as the record image of
    /// <paramref name="width"/> characters of the FIXED-length group of shape <paramref name="imageShape"/>: each
    /// dynamic-capacity table fitted to the fixed table it corresponds to, as §14.6.9.2 fits a dynamic sender into a
    /// non-dynamic receiver (superfluous elements are not moved, missing ones are space filled), and the fixed material
    /// space-filled or truncated at the right (§14.9.25.4 GR9b).</summary>
    public static string ToImage(CobolVarGroup v, GroupAtom[] from, GroupAtom[] imageShape, int width) =>
        CobolString.Store(Reshape(v, from, imageShape).Fixed, width);

    /// <summary>⛔ A FIXED-LENGTH VIEW'S STORE BACK INTO A VARIABLE-LENGTH GROUP'S STORAGE — the BY REFERENCE write-back
    /// of a fixed-length group FORMAL whose argument is the variable-length group <paramref name="current"/> (of shape
    /// <paramref name="currentShape"/>). §14.2.3 GR8: "the activated runtime element operates as if the formal parameter
    /// occupies the same storage area as the argument", so the store reaches only the storage the formal overlays
    /// (<see cref="Overlay"/>): each of its tables is written OVER the argument's current occurrences, and the argument's
    /// material past the formal's last character survives. A formal whose table has a FIXED occurrence count states no
    /// capacity of its own, so a store reaching the argument's dynamic-capacity table past its capacity creates the
    /// occurrence (§8.5.1.9.3, <see cref="OverlaidTable"/>) and one that does not leaves the capacity as it was. The image
    /// stored back is the one <see cref="ToImage"/> showed of the argument's storage, with the callee's stores in it;
    /// <paramref name="received"/> is what the callee referenced as a receiving item in it (<see cref="Overlay"/>).</summary>
    public static CobolVarGroup OverlayImage(CobolVarGroup current, GroupAtom[] currentShape, string image,
        GroupAtom[] imageShape, ReceivingReferences? received) =>
        Overlay(current, OfImage(image), currentShape, imageShape, received);

    /// <summary>The inverse crossing: a variable-length formal's carrier <paramref name="view"/> (of shape
    /// <paramref name="viewShape"/>) stored back over the storage of a FIXED-length group argument whose record image is
    /// <paramref name="current"/> (of shape <paramref name="imageShape"/>) — §14.2.3 GR8 through <see cref="Overlay"/>.
    /// The image keeps its width, so the fixed group cannot grow a table past its fixed extent (the callee's missing
    /// occurrences are space filled, §14.6.9.2), and the argument's material the formal does not describe survives.</summary>
    public static string OverlaidImage(string current, GroupAtom[] imageShape, CobolVarGroup view, GroupAtom[] viewShape) =>
        CobolString.Store(Overlay(OfImage(current), view, imageShape, viewShape, received: null).Fixed, current.Length);

    /// <summary>⛔ ONE VARIABLE-LENGTH GROUP'S CARRIER IN THE SHAPE OF ANOTHER, COMPATIBLE ONE (kb/Work PB480). Two
    /// variable-length groups are compatible when their variable-length items correspond and match (ISO §8.5.1.12.1),
    /// which leaves them free to differ in everything else: the shape and, after the last variable-length item, the
    /// length of their fixed material, and — §8.5.1.12.3 sentence 3 — a dynamic-capacity table in one where the other
    /// has a table of a FIXED number of occurrences ("treated as though it were a dynamic-capacity table whose capacity is
    /// either its fixed number of occurrences or the value of the DEPENDING operand"). So the carrier of
    /// <paramref name="from"/>'s layout is not, in general, the carrier of <paramref name="to"/>'s: this rebuilds it,
    /// segment by segment of the pair's correspondence (<see cref="GroupCompatibility.Walk"/>); a FIXED-length group is a
    /// carrier of its record image in its own shape (<see cref="OfImage"/>), so it is either side of the pair:
    /// <list type="bullet">
    ///   <item>the fixed material between two corresponding items lands at the same relative position, fitted to the
    ///     receiving shape's material there (the last run is §14.8.2.2 rule 1's prefix: truncated or space-filled);</item>
    ///   <item>a corresponding pair of components (dynamic-length items, dynamic-capacity tables) carries the content
    ///     whole;</item>
    ///   <item>a dynamic-capacity table opposite a fixed table: the fixed table's occurrences become the component, and a
    ///     component becomes the fixed table fitted to its width — §14.6.9.2's rule for a non-dynamic receiving table
    ///     (superfluous elements not moved, missing ones space filled), as <see cref="ToImage"/> applies it;</item>
    ///   <item>a dynamic-capacity table beyond the shorter group's last character corresponds to "a space-filled
    ///     fixed-length table" (§8.5.1.12.2): it gets no component (<see cref="HasDyn"/> false).</item>
    /// <item>a corresponding pair of tables at least one of which holds VARIABLE-LENGTH ELEMENTS (kb/Work PB2496):
    ///   each occurrence is reshaped from the one element's shape into the other's, §14.6.9.2's "Correspondingly
    ///   numbered elements are moved according to the rules of the MOVE statement"; a fixed table's occurrences are
    ///   its characters cut at its element width, each a fixed-length element's carrier.</item>
    /// <item>⛔ a carrier of STORAGE (one stating <see cref="Created"/>: a BY REFERENCE argument, which only the activation
    ///   boundary builds so, kb/Work PB2690) is not a MOVE's sender but the storage a formal VIEWS (§14.2.3 GR8: the formal
    ///   "occupies the same storage area as the argument"), so a fixed table opposite its INITIALIZED dynamic-capacity table
    ///   shows each occurrence past the capacity as the table would hold it once a store there created it (§8.5.1.9.3) —
    ///   §8.5.1.9.5's "INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT" — where a MOVE's remaining elements are "space
    ///   filled" (§14.6.9.2 2)). The §8.5.1.9.5 NOTE licenses the view ("functionally identical to one where the intermediate
    ///   occurrences are physically allocated and, if specified, initialized"), and <see cref="Overlay"/> recognises a store
    ///   by a difference from that same image (determination D-VGB). A table without the phrase states no creation (its
    ///   created content is "undefined") and shows the space fill, which no store of a numeric value matches.</item>
    /// </list>
    /// A pair the walk does not find compatible never reaches here — the bind-time screen or the run-time relation
    /// (<see cref="ActivationRelations.ParameterViolation"/>) refused it first — so one is an internal fault, raised
    /// loudly.</summary>
    public static CobolVarGroup Reshape(CobolVarGroup v, GroupAtom[] from, GroupAtom[] to)
    {
        if (GroupCompatibility.SameShape(from, to)) return v;
        var pairs = Correspondence(from, to);
        var src = GeometryOf(from);
        var dst = GeometryOf(to);
        var fixedRun = new System.Text.StringBuilder(v.Fixed.Length);
        var dyn = new List<string>();
        var elements = new List<CobolVarGroup[]?>();
        int prevA = 0, prevB = 0;
        for (int k = 0; k <= pairs.Count; k++)
        {
            (int pa, int pb) = k < pairs.Count ? pairs[k] : (from.Length, to.Length);
            fixedRun.Append(CobolString.Store(src.FixedSlice(v, prevA, pa), dst.FixedChars(prevB, pb)));
            if (k == pairs.Count) break;
            GroupAtom a = from[pa], b = to[pb];
            // A view of STORAGE shows a fixed table's occurrences past the dynamic table's capacity as it would create them.
            var create = a.IsComponent && !b.IsComponent ? v.CreatedAt(src.Ordinal[pa]) : null;
            if (IsNested(a) || IsNested(b))
            {
                // The element-by-element move (§14.6.9.2): every occurrence the sender has, in the receiver's element shape.
                var moved = src.Occurrences(v, pa) is { } occ ? Reshaped(occ, ElementShape(a), ElementShape(b)) : null;
                if (!b.IsComponent)
                {
                    var run = new System.Text.StringBuilder(moved is null ? "" : FixedImages(moved), b.Chars);
                    if (create is not null && moved is not null && b.ElementChars > 0)
                        for (int n = moved.Length + 1; run.Length + b.ElementChars <= b.Chars; n++)
                            run.Append(Reshape(create(n), ElementShape(a), ElementShape(b)).Fixed);
                    fixedRun.Append(CobolString.Store(run.ToString(), b.Chars));
                }
                else if (moved is not null)
                {
                    dyn.Add(IsNested(b) ? "" : FixedImages(moved));
                    elements.Add(IsNested(b) ? moved : null);
                }
            }
            else if (b.IsComponent)
            {
                // An absent component (the sender carried fewer) stays absent — absent ones are a suffix.
                if (src.Content(v, pa) is { } content)
                {
                    dyn.Add(content);
                    elements.Add(null);
                }
            }
            else
            {
                string? carried = src.Content(v, pa);
                string content = carried ?? "";
                if (create is not null && carried is not null && a.ElementChars > 0)
                    content += CreatedImages(create, content.Length / a.ElementChars, b.Chars - content.Length, a.ElementChars);
                fixedRun.Append(CobolString.Store(content, b.Chars));
            }
            (prevA, prevB) = (pa + 1, pb + 1);
        }
        return new CobolVarGroup(fixedRun.ToString(), [.. dyn], AnyNested(elements));
    }

    /// <summary>Each of <paramref name="occurrences"/> rebuilt from the element shape <paramref name="from"/> in
    /// <paramref name="to"/> — §14.6.9.2's element-by-element move (or, for occurrences of storage, the view of each).</summary>
    private static CobolVarGroup[] Reshaped(CobolVarGroup[] occurrences, GroupAtom[] from, GroupAtom[] to)
    {
        var outp = new CobolVarGroup[occurrences.Length];
        for (int i = 0; i < outp.Length; i++) outp[i] = Reshape(occurrences[i], from, to);
        return outp;
    }

    /// <summary>The image of the occurrences a dynamic-capacity table of fixed-length elements would create after its
    /// <paramref name="have"/> current ones, as many whole <paramref name="elementChars"/>-wide elements as fit in
    /// <paramref name="room"/> positions (<see cref="Reshape"/>'s view of storage).</summary>
    private static string CreatedImages(Func<int, CobolVarGroup> create, int have, int room, int elementChars)
    {
        if (elementChars <= 0 || room < elementChars) return "";
        var sb = new System.Text.StringBuilder(room);
        for (int n = have + 1; sb.Length + elementChars <= room; n++) sb.Append(CreatedImage(create, n, elementChars));
        return sb.ToString();
    }

    /// <summary>Occurrence <paramref name="n"/> as the table creates it, at its element's width — the space fill when
    /// the carrier states no creation (a fixed-length element's carrier has only its fixed run).</summary>
    private static string CreatedImage(Func<int, CobolVarGroup>? create, int n, int elementChars) =>
        CobolString.Store(create is null ? "" : create(n).Fixed, elementChars);

    /// <summary>⛔ A VIEW'S STORE BACK INTO THE STORAGE IT VIEWS — the BY REFERENCE write-back of
    /// <see cref="Reshape"/> (ISO §14.2.3 GR8: "the activated runtime element operates as if the formal parameter occupies
    /// the same storage area as the argument"). <paramref name="current"/> is the argument's carrier, of
    /// <paramref name="currentShape"/>; <paramref name="view"/> the formal's, of <paramref name="viewShape"/>. A store
    /// through the formal reaches only the argument storage it overlays:
    /// <list type="bullet">
    ///   <item>each run of fixed material the formal covers replaces the argument's leading characters there, and the
    ///     argument's material past it survives (the §14.8.2.2 rule 1 prefix);</item>
    ///   <item>a corresponding pair of components replaces the argument's component whole — both are variable, so the
    ///     formal's length or capacity IS the argument's;</item>
    ///   <item>a FIXED table of the formal opposite the argument's dynamic-capacity table is written OVER the argument's
    ///     current occurrences and creates the ones past its capacity that the callee stored into
    ///     (<see cref="OverlaidTable"/>, §8.5.1.9.3), each the element the table creates (<see cref="Created"/>,
    ///     §8.5.1.9.5) with the callee's stores in it, and a component of the formal opposite the argument's fixed table is
    ///     fitted to it — it cannot grow;</item>
    ///   <item>a pair of tables at least one of which holds VARIABLE-LENGTH ELEMENTS (kb/Work PB2496) takes the same rules
    ///     one level down: each occurrence of the view overlays the argument's occurrence of the same number, because the
    ///     formal's element occupies that element's storage; an occurrence the argument lacked (one a FIXED table of the
    ///     formal creates, or one a variable table of the formal brings) is the created element with the view's element
    ///     stored over it (<see cref="OverlaidOccurrences"/>);</item>
    ///   <item>every component of the argument the formal does not reach (beyond its last character) survives.</item>
    /// </list>
    /// <paramref name="received"/> records the positions of <paramref name="view"/>'s STORAGE (its fixed run with each
    /// dynamic-capacity table's one-element reservation in place, <see cref="Geometry.StorageAt"/>) the activated element
    /// referenced as a receiving item, and, per occurrence of each of the view's dynamic-capacity tables, that occurrence's
    /// own (<see cref="ReceivingReferences"/>, kept by the fresh area the view was copied into), or is null when nothing
    /// recorded them: a store into an occurrence past the capacity creates it whatever image it stored (§8.5.1.9.3;
    /// <see cref="OverlaidTable"/>), at every level.</summary>
    public static CobolVarGroup Overlay(CobolVarGroup current, CobolVarGroup view, GroupAtom[] currentShape,
        GroupAtom[] viewShape, ReceivingReferences? received)
    {
        if (GroupCompatibility.SameShape(currentShape, viewShape)) return view;
        var pairs = Correspondence(currentShape, viewShape);
        var arg = GeometryOf(currentShape);
        var formal = GeometryOf(viewShape);
        var fixedRun = new System.Text.StringBuilder(current.Fixed.Length);
        var dyn = (string[])current.Dynamic.Clone();
        var elements = new CobolVarGroup[]?[dyn.Length];
        for (int k = 0; k < dyn.Length; k++) elements[k] = current.ElementsAt(k);
        int prevA = 0, prevB = 0;
        for (int k = 0; k <= pairs.Count; k++)
        {
            (int pa, int pb) = k < pairs.Count ? pairs[k] : (currentShape.Length, viewShape.Length);
            fixedRun.Append(Overlaid(arg.FixedSlice(current, prevA, pa), formal.FixedSlice(view, prevB, pb)));
            if (k == pairs.Count) break;
            GroupAtom a = currentShape[pa], b = viewShape[pb];
            int ord = arg.Ordinal[pa];
            if (IsNested(a) || IsNested(b))
            {
                var seen = formal.Occurrences(view, pb);
                if (!a.IsComponent)
                    fixedRun.Append(CobolString.Store(
                        seen is null ? arg.Content(current, pa) ?? "" : FixedImages(Reshaped(seen, ElementShape(b), ElementShape(a))),
                        a.Chars));
                else if (ord < dyn.Length && seen is not null)
                {
                    var now = OverlaidOccurrences(arg.Occurrences(current, pa) ?? [], seen, resize: b.IsComponent,
                        ElementShape(a), ElementShape(b), current.CreatedAt(ord),
                        b.IsComponent ? TableReferences.OfDynamic(received, formal.Ordinal[pb])
                                      : TableReferences.OfFixed(received?.From(formal.StorageAt(pb)), b.ElementChars));
                    dyn[ord] = IsNested(a) ? "" : FixedImages(now);
                    elements[ord] = IsNested(a) ? now : null;
                }
            }
            else
            {
                string? now = formal.Content(view, pb);
                if (!a.IsComponent)
                    fixedRun.Append(CobolString.Store(now ?? arg.Content(current, pa) ?? "", a.Chars));
                else if (ord < dyn.Length && now is not null)
                    dyn[ord] = b.IsComponent ? now
                        : OverlaidTable(dyn[ord], now, a.ElementChars, current.CreatedAt(ord), received?.From(formal.StorageAt(pb)));
            }
            (prevA, prevB) = (pa + 1, pb + 1);
        }
        return new CobolVarGroup(fixedRun.ToString(), dyn, AnyNested(elements));
    }

    /// <summary>The occurrences of a table the view <paramref name="seen"/> stores over (<paramref name="was"/>, in the
    /// argument's element shape <paramref name="wasShape"/>): each of the view's occurrences overlays the argument's of
    /// the same number. <paramref name="resize"/> — the view's table is variable too — makes its capacity the argument's;
    /// otherwise the view is a FIXED table written over the argument's current occurrences, creating those past its
    /// capacity that it stored into (<see cref="GrownCount"/>, <see cref="OverlaidTable"/>'s determination). Either way an
    /// occurrence the argument lacked is CREATED in the argument's table (§8.5.1.9.3: "a new element is automatically
    /// created") and the view's element is stored over it, exactly as an occurrence the argument had is overlaid: the
    /// element the table creates (<paramref name="create"/>, §8.5.1.9.5's INITIALIZED default), or, without the phrase,
    /// <see cref="SpaceFilled"/>. So a table nested in the created element grows only as far as the callee referenced it,
    /// never to the formal's fixed count (kb/Work PB2690 branch review round 1 of wave 1078 A, the created-occurrence arm
    /// of finding R1-1). <paramref name="table"/> is the view's table's receiving-reference record, per occurrence.</summary>
    private static CobolVarGroup[] OverlaidOccurrences(CobolVarGroup[] was, CobolVarGroup[] seen, bool resize,
        GroupAtom[] wasShape, GroupAtom[] seenShape, Func<int, CobolVarGroup>? create, TableReferences table)
    {
        var now = new CobolVarGroup[resize ? seen.Length
            : GrownCount(was.Length, seen, wasShape, seenShape, create, table)];
        for (int i = 0; i < now.Length; i++)
            now[i] = i >= seen.Length ? was[i]
                : Overlay(i < was.Length ? was[i] : create?.Invoke(i + 1) ?? SpaceFilled(wasShape), seen[i], wasShape,
                    seenShape, table.Occurrence(i));
        return now;
    }

    /// <summary>The element a dynamic-capacity table WITHOUT the INITIALIZED phrase creates, in its element shape
    /// <paramref name="shape"/>, as the base a view's element is stored over (<see cref="OverlaidOccurrences"/>):
    /// §8.5.1.9.5 leaves its content "undefined", and the determination D-VGB shows it as the space fill
    /// (<see cref="Reshape"/>), so its fixed material is spaces and each component is carried EMPTY. A table nested in it
    /// then holds exactly the occurrences the view's store creates, and the store into storage raises it to its FROM
    /// minimum (<c>CobolDynTable.FromCurrentImage</c>), the capacity the created element's table starts at (§8.5.1.9.1).</summary>
    private static CobolVarGroup SpaceFilled(GroupAtom[] shape)
    {
        var geometry = GeometryOf(shape);
        int components = 0;
        foreach (var atom in shape)
            if (atom.IsComponent) components++;
        var dyn = new string[components];
        Array.Fill(dyn, "");
        return new CobolVarGroup(new string(' ', geometry.FixedChars(0, shape.Length)), dyn);
    }

    /// <summary>⛔ ONE TABLE OF THE FORMAL'S RECEIVING REFERENCES, OCCURRENCE BY OCCURRENCE (kb/Work PB2690;
    /// <see cref="ReceivingReferences"/>). A FIXED table's occurrences are positions of the formal's storage, each
    /// <c>ElementChars</c> wide from the table's own record (<see cref="OfFixed"/>); a DYNAMIC-CAPACITY table's are scopes of
    /// their own (an element cell, whose positions start again at zero), read by the table's component ordinal
    /// <c>Component</c> in the formal and the occurrence number (<see cref="OfDynamic"/>; <see cref="ReceivingReferences.Element"/>).
    /// Null <c>Record</c>: nothing recorded the references, and the store is recognised by its effect.</summary>
    private readonly record struct TableReferences(ReceivingReferences? Record, int Component, int ElementChars)
    {
        public static TableReferences OfFixed(ReceivingReferences? record, int elementChars) => new(record, -1, elementChars);

        public static TableReferences OfDynamic(ReceivingReferences? record, int component) => new(record, component, 0);

        /// <summary>The record of occurrence <paramref name="index"/> (zero-based).</summary>
        public ReceivingReferences? Occurrence(int index) =>
            Record is null ? null
            : Component >= 0 ? Record.Element(Component, index + 1)
            : Record.From(index * ElementChars);

        /// <summary>True when the activated element referenced occurrence <paramref name="index"/> (zero-based) of a FIXED
        /// table as a receiving item.</summary>
        public bool Referenced(int index) => Occurrence(index)?.Within(0, ElementChars) == true;
    }

    /// <summary><paramref name="was"/> with its leading characters replaced by <paramref name="now"/>'s, its length
    /// kept — a store through a view of the leading positions.</summary>
    private static string Overlaid(string was, string now) =>
        now.Length >= was.Length ? now[..was.Length] : now + was[now.Length..];

    /// <summary>⛔ A FIXED TABLE OF THE FORMAL STORED OVER THE ARGUMENT'S DYNAMIC-CAPACITY TABLE (kb/Work PB2690):
    /// <paramref name="now"/> is the formal's table (a whole number of <paramref name="elementChars"/>-wide occurrences),
    /// <paramref name="was"/> the argument's current occurrences. Its leading occurrences overlay the argument's
    /// (<see cref="Overlaid"/>); an occurrence PAST the argument's capacity that the callee stored into is created —
    /// §14.2.3 GR8 makes the formal occupy the argument's storage, so the store is a receiving reference to the dynamic
    /// table with a subscript that "exceeds the current capacity of the table", and §8.5.1.9.3 then creates "a new
    /// element" and raises the capacity "to the value given by the subscript" (intermediate occurrences too).
    /// <para>THE STORE IS THE CALLEE'S RECEIVING REFERENCE (kb/Work PB2690): the formal's fresh area records every position a
    /// store through it reached (<paramref name="received"/>, positions of the formal's table; <see cref="ReceivingReferences"/>),
    /// so the capacity becomes the highest occurrence the callee referenced as a receiving item, whatever image it stored
    /// there — the INITIALIZED default or spaces included.</para>
    /// <para>⚠ DETERMINATION D-VGB (docs/CONFORMANCE.md §3; the rest is §8.5.1.12.3 sentence 3, which makes the pair the same
    /// length only "for purposes of determining compatibility"): the view showed each occurrence the argument lacks as the
    /// table creates it (<see cref="Reshape"/>: its <paramref name="create"/> image, §8.5.1.9.5's INITIALIZED default, when
    /// the table writes the phrase; otherwise the space fill, since §8.5.1.9.5 leaves that content "undefined"), so the
    /// created occurrences of an INITIALIZED table keep the default in every item the callee did not store, and the
    /// intermediate ones are the default whole, as §8.5.1.9.5 requires. A callee that only READS the occurrences past the
    /// capacity leaves it alone. A store that reached the area by a path that records no reference — a further activation's
    /// boundary copy of the whole group back into it (<c>CallEmitter.BoundaryCopyWrite</c>), which would otherwise make a
    /// store into one occurrence a reference to all of them — is still recognised by its EFFECT: an occurrence whose image
    /// differs from the one the view showed was stored into.</para></summary>
    private static string OverlaidTable(string was, string now, int elementChars, Func<int, CobolVarGroup>? create,
        ReceivingReferences? received)
    {
        if (elementChars <= 0 || now.Length <= was.Length) return Overlaid(was, now);
        int kept = was.Length;
        for (int end = now.Length / elementChars * elementChars; end > kept; end -= elementChars)
            if (received?.Within(end - elementChars, end) == true
                || !now.AsSpan(end - elementChars, elementChars).SequenceEqual(CreatedImage(create, end / elementChars, elementChars)))
            {
                kept = end;
                break;
            }
        return kept == was.Length ? Overlaid(was, now) : now[..kept];
    }

    /// <summary>The occurrence count a FIXED table of the formal leaves the argument's dynamic-capacity table of
    /// variable-length elements with (<see cref="OverlaidTable"/>'s rule one level up): the argument's
    /// <paramref name="current"/> count, or the highest occurrence of <paramref name="seen"/> past it that the callee
    /// referenced as a receiving item (<paramref name="table"/>, the formal's fixed table's record) or that differs from what the view showed there — the created element (<paramref name="create"/>) seen in
    /// the formal's element shape <paramref name="seenShape"/> (<see cref="Reshape"/>), or, when the carrier states no
    /// creation (a table without INITIALIZED), the space fill: a fixed-length element's carrier with a blank fixed run and
    /// no components.</summary>
    private static int GrownCount(int current, CobolVarGroup[] seen, GroupAtom[] wasShape, GroupAtom[] seenShape,
        Func<int, CobolVarGroup>? create, TableReferences table)
    {
        for (int end = seen.Length; end > current; end--)
        {
            if (table.Referenced(end - 1)) return end;
            var s = seen[end - 1];
            bool shown = create is null
                ? s.Dynamic.Length == 0 && !s.Fixed.AsSpan().ContainsAnyExcept(' ')
                : SameContent(s, Reshape(create(end), wasShape, seenShape));
            if (!shown) return end;
        }
        return current;
    }

    /// <summary>Whether two carriers of one shape hold the same content: the same fixed run, the same components and
    /// the same occurrences of every nested one.</summary>
    private static bool SameContent(CobolVarGroup x, CobolVarGroup y)
    {
        if (x.Fixed != y.Fixed || !x.Dynamic.AsSpan().SequenceEqual(y.Dynamic)) return false;
        for (int k = 0; k < x.Dynamic.Length; k++)
        {
            CobolVarGroup[]? ex = x.ElementsAt(k), ey = y.ElementsAt(k);
            if (ex is null != ey is null) return false;
            if (ex is null) continue;
            if (ex.Length != ey!.Length) return false;
            for (int i = 0; i < ex.Length; i++)
                if (!SameContent(ex[i], ey[i])) return false;
        }
        return true;
    }

    /// <summary>The pair plans already worked out, keyed by the two atom arrays' REFERENCES (weakly: a plan lives as long as
    /// both arrays do). The compiler states each distinct array once, as a static field (<c>GroupAtomTable</c>, kb/Work
    /// PB2690), so a statement or a boundary that runs again meets the same two references and skips the §8.5.1.12 walk.</summary>
    private static readonly ConditionalWeakTable<GroupAtom[], ConditionalWeakTable<GroupAtom[], List<(int First, int Second)>>>
        s_pairPlans = new();

    /// <summary>The per-array geometry already worked out, keyed by the array's reference (see <see cref="s_pairPlans"/>).</summary>
    private static readonly ConditionalWeakTable<GroupAtom[], Geometry> s_geometries = new();

    private static Geometry GeometryOf(GroupAtom[] atoms) => s_geometries.GetValue(atoms, static a => new Geometry(a));

    /// <summary>The corresponding pairs of two compatible layouts, left to right — the segment boundaries of
    /// <see cref="Reshape"/>, <see cref="Overlay"/> and <see cref="Compare"/>. Worked out once per pair of arrays
    /// (<see cref="s_pairPlans"/>); a pair that is not compatible is never cached, it is the internal fault below.</summary>
    private static List<(int First, int Second)> Correspondence(GroupAtom[] a, GroupAtom[] b)
    {
        var plans = s_pairPlans.GetValue(a, static _ => new());
        if (plans.TryGetValue(b, out var known)) return known;
        var pairs = new List<(int, int)>();
        if (GroupCompatibility.Walk(a, b, pairs) is { } why)
            throw new InvalidOperationException(
                $"internal error: a variable-length group carrier was taken across an incompatible pair ({why.Kind}) — "
                + "the §8.5.1.12 relation should have refused it");
        plans.TryAdd(b, pairs);
        return pairs;
    }

    /// <summary>⛔ THE ONE TEST FOR A NESTED COMPONENT (kb/Work PB2496): a dynamic-capacity table whose element is
    /// itself a variable-length group — its element atoms hold a component. The emitted composer carries exactly these
    /// tables in <see cref="Elements"/> (<c>GroupImageCodec</c>'s nested arm, gated on the same declared shape).</summary>
    private static bool IsNested(GroupAtom t) =>
        t.Kind is GroupAtomKind.DynamicTable && t.Element is { } e && GroupCompatibility.HasComponent(e);

    /// <summary>A table atom's element shape: its element's atoms, or one run of fixed material for an elementary element.</summary>
    private static GroupAtom[] ElementShape(GroupAtom t) => t.Element ?? GroupCompatibility.FixedRun(t.ElementChars);

    /// <summary>The fixed runs of occurrence carriers concatenated — a table of FIXED-length elements' characters
    /// (a carrier in a fixed element shape has no components).</summary>
    private static string FixedImages(CobolVarGroup[] occurrences) =>
        string.Concat(Array.ConvertAll(occurrences, static e => e.Fixed));

    /// <summary>The <see cref="Elements"/> of a rebuilt carrier: null when no component is nested.</summary>
    private static CobolVarGroup[]?[]? AnyNested(IReadOnlyList<CobolVarGroup[]?> elements)
    {
        foreach (var e in elements)
            if (e is not null) return [.. elements];
        return null;
    }

    /// <summary>Where each atom of a layout lies in its carrier: a component's ordinal in <see cref="Dynamic"/>, every
    /// other atom's character offset in <see cref="Fixed"/> (components contribute nothing to the fixed run).</summary>
    private sealed class Geometry
    {
        private readonly GroupAtom[] _atoms;
        private readonly int[] _fixedAt;
        public readonly int[] Ordinal;

        public Geometry(GroupAtom[] atoms)
        {
            _atoms = atoms;
            _fixedAt = new int[atoms.Length + 1];
            Ordinal = new int[atoms.Length];
            int at = 0, ord = 0;
            for (int i = 0; i < atoms.Length; i++)
            {
                _fixedAt[i] = at;
                Ordinal[i] = atoms[i].IsComponent ? ord++ : -1;
                if (!atoms[i].IsComponent) at += atoms[i].Chars;
            }
            _fixedAt[atoms.Length] = at;
        }

        /// <summary>The fixed-run characters of atoms [<paramref name="from"/>, <paramref name="to"/>) — contiguous,
        /// because the components among them contribute nothing to the fixed run.</summary>
        public string FixedSlice(CobolVarGroup v, int from, int to)
        {
            int start = Math.Min(_fixedAt[from], v.Fixed.Length);
            return v.Fixed[start..Math.Min(_fixedAt[to], v.Fixed.Length)];
        }

        /// <summary>The character count of atoms [<paramref name="from"/>, <paramref name="to"/>) in the fixed run.</summary>
        public int FixedChars(int from, int to) => _fixedAt[to] - _fixedAt[from];

        /// <summary>Where atom <paramref name="i"/> starts in the group's STORAGE — its fixed run with each dynamic-capacity
        /// table's one-element reservation in place (§8.5.1.12.3: such a table counts "the length of a single element";
        /// <c>StorageCell.RunOf</c> cuts the reservations out of a cell's image): the positions a receiving reference is
        /// recorded in (<see cref="ReceivingReferences"/>).</summary>
        public int StorageAt(int i)
        {
            int at = 0;
            for (int j = 0; j < i; j++) at += _atoms[j].Chars;
            return at;
        }

        /// <summary>Atom <paramref name="i"/>'s content: a component's carried string (null when the carrier did not
        /// carry it), a fixed table's characters of the fixed run. A NESTED component has no characters; read through a
        /// shape that does not say so (<see cref="Positional"/>), its empty string would compare or move as if the table
        /// were empty — loud instead.</summary>
        public string? Content(CobolVarGroup v, int i) =>
            _atoms[i].IsComponent
                ? !v.HasDyn(Ordinal[i]) ? null
                  : v.ElementsAt(Ordinal[i]) is not null && !IsNested(_atoms[i])
                      ? NotImplemented.Value<string>("a table of variable-length elements inside a variable-length group "
                          + "whose layout cannot be stated (a USAGE BIT leaf; ISO §14.6.9.3; kb/Work PB2496)")
                  : v.Dynamic[Ordinal[i]]
                : FixedSlice(v, i, i + 1);

        /// <summary>Table atom <paramref name="i"/>'s occurrences, each a carrier in the shape of the table's element
        /// (kb/Work PB2496): a nested component's carried occurrence carriers; otherwise the table's characters
        /// (<see cref="Content"/>) cut at one element's width, each the carrier of a fixed-length element. Null when the
        /// sender did not carry the component.</summary>
        public CobolVarGroup[]? Occurrences(CobolVarGroup v, int i)
        {
            if (_atoms[i].IsComponent && IsNested(_atoms[i]))
                return v.HasDyn(Ordinal[i]) ? v.ElementCarriersAt(Ordinal[i]) : null;
            return Content(v, i) is { } chars
                ? Array.ConvertAll(CobolVarGroup.Occurrences(chars, _atoms[i].ElementChars), static x => new CobolVarGroup(x, []))
                : null;
        }

        /// <summary>Atoms [<paramref name="from"/>, end) of <paramref name="v"/> as the characters they occupy in the
        /// group's contiguous image (§8.5.1.11.2): fixed material, each component's content, each nested table's
        /// occurrences at their own extents — what the end of a §8.8.4.2.17 comparison compares with the other group's
        /// end, and with spaces where that one has ended.</summary>
        public string Contiguous(CobolVarGroup v, int from)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = from; i < _atoms.Length; i++)
            {
                if (!_atoms[i].IsComponent) sb.Append(FixedSlice(v, i, i + 1));
                else if (IsNested(_atoms[i]))
                {
                    var shape = GeometryOf(ElementShape(_atoms[i]));
                    foreach (var e in Occurrences(v, i) ?? []) sb.Append(shape.Contiguous(e, 0));
                }
                else sb.Append(Content(v, i));
            }
            return sb.ToString();
        }
    }

    /// <summary>⛔ A FILE RECORD'S CONTIGUOUS IMAGE, DECOMPOSED — the READ / RETURN half of a variable-length
    /// record (docs/CONFORMANCE.md §3 determination D-FRA; kb/Work PB981, PB1053). A WRITE sends the record "as
    /// though it were in fact contiguous with its neighbors" (ISO §8.5.1.11.2) — the group's <c>CurrentImage()</c>
    /// — so the characters alone carry no marker of where a dynamic member ends. Two sources can say where:
    /// <list type="bullet">
    /// <item><paramref name="recorded"/> — the record's own EXTENT TABLE's lengths (<see cref="RecordExtents"/>,
    /// D-FRA (v)), which the caller passes only after <see cref="RecordExtents.Describes"/> accepted it for THIS
    /// record and layout: component k takes exactly the characters it had when the record was sent, so the split is
    /// the exact inverse of the composer for every layout, however many components it has;</item>
    /// <item>otherwise the ONE TAKE STEP (<see cref="ContiguousTake"/>): walking the components left to right, each
    /// takes as many whole units (one character of a dynamic-length item, one element of a dynamic-capacity table)
    /// as the record holds beyond the FIXED material still to come, up to its maximum — exact for a record with ONE
    /// variable-length member, wherever it sits; with several, the EARLIER component takes the excess (the
    /// determination's reading for a record that carries no extent table).</item>
    /// </list>
    /// The fixed material then lands at its own positions.
    /// <para><paramref name="fixedForm"/> marks the record as the FIXED FORM a file of fixed-length records holds
    /// (<see cref="CobolContiguousLayout.ToFixedForm"/>, D-FRA (vi)): every member filled its field to its maximum,
    /// so the take step finds each at that width, and each then drops the space padding.</para>
    /// <para><paramref name="fixedAt"/>[k] is component k's offset in the FIXED run (the §8.5.1.12.3
    /// zero-length accounting <see cref="Fixed"/> uses), <paramref name="unit"/>[k] its unit width in
    /// characters, <paramref name="maxUnits"/>[k] its maximum size in units (§8.5.1.10.1's maximum size / the
    /// table's maximum capacity). A record SHORTER than the fixed run leaves every component empty and the
    /// fixed run short — <c>FromVarImage</c> space-fills it, as §14.9.30.4 GR15 fills a short line. A recorded
    /// component longer than the receiving item's maximum is carried whole here and truncated on the right by
    /// the item's own receiving store (§8.5.1.10.4 — "If the maximum length is reached, the value is truncated on
    /// the right as necessary").</para></summary>
    public static CobolVarGroup FromContiguous(string record, int fixedTotal, IReadOnlyList<int> fixedAt,
        IReadOnlyList<int> unit, IReadOnlyList<long> maxUnits, IReadOnlyList<int>? recorded = null,
        bool fixedForm = false, IReadOnlyList<CobolDynStructure?>? structure = null)
    {
        var dyn = new string[fixedAt.Count];
        var fixedRun = new System.Text.StringBuilder(fixedTotal);
        long excess = Math.Max(0, record.Length - fixedTotal);
        int pos = 0, fpos = 0;
        for (int k = 0; k < dyn.Length; k++)
        {
            int lead = fixedAt[k] - fpos;
            fixedRun.Append(Slice(record, pos, lead));
            pos += lead;
            fpos = fixedAt[k];
            int take = ComponentTake(k, record, pos, ref excess, recorded, fixedForm, unit[k], maxUnits[k], structure, out string? data);
            dyn[k] = data ?? Slice(record, pos, take);
            pos += take;
            // THE FIXED FORM (D-FRA (vi); CobolContiguousLayout.ToFixedForm): the member filled its whole field, and
            // a space in a fixed-size field is padding, never data. ISO's own LINE SEQUENTIAL record does the same
            // (§14.9.51.4 GR21 — spaces to the right of the rightmost non-space are not transferred). Every component
            // of a FILE record is a dynamic-length ELEMENTARY item — a dynamic-capacity table "may be defined in any
            // place, other than the file section" (§8.5.1.9.1 3), COBOLNET1526 — so the trim never meets a table whose
            // blank elements are data; `fixedForm` is passed for a file record and nothing else. A STRUCTURED
            // component (§12.3.7.4 GR18/GR19) is never trimmed: its length field or delimiter says where its data ends,
            // so a trailing space is data, not padding (`data` is non-null for it).
            if (fixedForm && data is null) dyn[k] = dyn[k].TrimEnd(' ');
        }
        fixedRun.Append(Slice(record, pos, fixedTotal - fpos));
        return new CobolVarGroup(fixedRun.ToString(), dyn);
    }

    /// <summary>⛔ THE ONE STEP OF THE RECORD WALK (kb/Work PB981, PB1025, PB1094): how many characters component
    /// <paramref name="k"/> occupies in <paramref name="record"/> at <paramref name="pos"/>, and — for a component
    /// laid out by a DYNAMIC LENGTH STRUCTURE (<paramref name="structure"/>[k], §12.3.7.4 GR18/GR19) — its DATA in
    /// <paramref name="data"/> (null for a plain component, whose data IS the characters). Three sources, in order:
    /// the record's own extent table (<paramref name="recorded"/>); for a structured component in the fixed form of a
    /// fixed-length file, its whole maximum extent; for one with no table, the structure itself
    /// (<see cref="CobolDynStructure.TakeAt"/>); and for a plain component with no table the take step
    /// (<see cref="ContiguousTake"/>), which leaves the later STRUCTURED components their minimum (their length field
    /// and delimiter) so the earlier plain one cannot swallow them. <see cref="FromContiguous"/> and
    /// <see cref="CobolContiguousLayout.Position"/> both walk with it, so a key located after a dynamic member lands
    /// exactly where the decomposition puts that member's end. <paramref name="excess"/> is the record's length beyond
    /// its fixed run, less what earlier components took; this charges it.</summary>
    internal static int ComponentTake(int k, string record, int pos, ref long excess, IReadOnlyList<int>? recorded,
        bool fixedForm, int unit, long maxUnits, IReadOnlyList<CobolDynStructure?>? structure, out string? data)
    {
        data = null;
        var st = structure?[k];
        int take;
        if (recorded is not null)
        {
            take = recorded[k];
            if (st is not null) data = st.ContentOf(Slice(record, pos, take));
        }
        else if (st is not null)
        {
            if (fixedForm)
            {
                take = (int)Math.Min(int.MaxValue, st.Overhead + (unit <= 0 ? 0 : maxUnits * unit));
                data = st.ContentOf(Slice(record, pos, take));
                take = Math.Min(take, Math.Max(0, record.Length - pos));
            }
            else take = st.TakeAt(record, pos, out data);
            excess -= take;
        }
        else
        {
            long later = 0;
            if (structure is not null)
                for (int j = k + 1; j < structure.Count; j++) later += structure[j]?.Overhead ?? 0;
            long available = Math.Max(0, excess - later);
            take = ContiguousTake(ref available, unit, maxUnits);
            excess -= take;
        }
        return take;
    }

    /// <summary>⛔ THE ONE TAKE STEP of a contiguous record image (kb/Work PB981, PB1025): how many characters the
    /// next variable-length component takes — whole units of <paramref name="unit"/> characters, as many as the
    /// remaining <paramref name="excess"/> (the record's length beyond its FIXED run, less what earlier components
    /// took) holds, up to <paramref name="maxUnits"/> — charged to <paramref name="excess"/>.
    /// <see cref="ComponentTake"/> asks it for a plain component.</summary>
    internal static int ContiguousTake(ref long excess, int unit, long maxUnits)
    {
        long units = unit <= 0 ? 0 : Math.Min(excess / unit, maxUnits);
        int take = (int)(units * unit);
        excess -= take;
        return take;
    }

    private static string Slice(string s, int at, int length) =>
        at >= s.Length || length <= 0 ? "" : s.Substring(at, Math.Min(length, s.Length - at));

    /// <summary>⛔ ISO §8.8.4.2.17 — THE COMPARISON OF TWO COMPATIBLE GROUPS, ONE OR BOTH OF WHICH IS A
    /// VARIABLE-LENGTH GROUP (kb/Work PB1467, PB2496): it "proceeds from left to right as described under 8.8.4.2.7,
    /// Comparison of alphanumeric operands except that — when corresponding tables are encountered, they are
    /// compared as described in 14.6.9.3, Comparing two tables — when corresponding dynamic-length elementary items
    /// are encountered, the length is determined as described in 8.5.1.10.4 … After comparison of corresponding
    /// tables or dynamic-length elementary items, comparison continues with the next data item in each of the
    /// compatible groups."
    /// <para>Each operand is its carrier in its OWN shape (<paramref name="aShape"/>, <paramref name="bShape"/>; a
    /// fixed-length group is its record image with no components), and the pair's correspondence
    /// (<see cref="GroupCompatibility.Walk"/>, the same walk that decided the pair compatible) gives the corresponding
    /// items. The material between two of them lies at the same relative positions in both groups, so it is compared
    /// stretch by stretch; each corresponding pair is then compared as one §8.8.4.2.7 comparison, the shorter side
    /// extended with spaces — exactly both "except" clauses for a dynamic-length item (§8.5.1.10.4, "treated as a
    /// fixed-length data item whose length is the dynamic-length elementary item's current length") and for two tables
    /// of elementary or fixed-length elements, whose element-by-element comparison "until … the last element of the
    /// table with the smallest current capacity has been compared", then "each successive remaining element of the
    /// larger table with spaces" (§14.6.9.3), is the comparison of their concatenated occurrences. A table of
    /// VARIABLE-LENGTH elements is compared element by element in fact, each pair of elements by this same rule and
    /// each remaining element against spaces. After the last pair the two groups' remaining material is compared as it
    /// lies in their contiguous images — a dynamic-capacity table beyond the shorter group's end against spaces, the
    /// "space-filled fixed-length table" §8.5.1.12.2 makes correspond to it.</para>
    /// <para><paramref name="collation"/> is the alphanumeric program collating sequence when it is not native
    /// (§8.8.4.2.7), null otherwise.</para></summary>
    /// <returns>&lt;0, 0 or &gt;0 as <paramref name="a"/> is less than, equal to or greater than <paramref name="b"/>.</returns>
    public static int Compare(CobolVarGroup a, GroupAtom[] aShape, CobolVarGroup b, GroupAtom[] bShape,
        CobolCollation? collation = null)
    {
        var pairs = Correspondence(aShape, bShape);
        var ga = GeometryOf(aShape);
        var gb = GeometryOf(bShape);
        int prevA = 0, prevB = 0;
        foreach (var (pa, pb) in pairs)
        {
            int c = CompareRun(ga.FixedSlice(a, prevA, pa), gb.FixedSlice(b, prevB, pb), collation);
            if (c != 0) return c;
            c = IsNested(aShape[pa]) || IsNested(bShape[pb])
                ? CompareElements(ga.Occurrences(a, pa) ?? [], ElementShape(aShape[pa]),
                                  gb.Occurrences(b, pb) ?? [], ElementShape(bShape[pb]), collation)
                : CompareRun(ga.Content(a, pa) ?? "", gb.Content(b, pb) ?? "", collation);
            if (c != 0) return c;
            (prevA, prevB) = (pa + 1, pb + 1);
        }
        return CompareRun(ga.Contiguous(a, prevA), gb.Contiguous(b, prevB), collation);
    }

    /// <summary>§14.6.9.3 over two tables of which at least one holds VARIABLE-LENGTH elements: correspondingly
    /// numbered elements compared as two compatible groups (<see cref="Compare"/>), then each remaining element of the
    /// larger table against spaces — an element compared with the empty carrier, every part of which is spaces.</summary>
    private static int CompareElements(CobolVarGroup[] a, GroupAtom[] aShape, CobolVarGroup[] b, GroupAtom[] bShape,
        CobolCollation? collation)
    {
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            int c = i >= b.Length ? Compare(a[i], aShape, Empty, aShape, collation)
                : i >= a.Length ? Compare(Empty, bShape, b[i], bShape, collation)
                : Compare(a[i], aShape, b[i], bShape, collation);
            if (c != 0) return c;
        }
        return 0;
    }

    private static int CompareRun(string a, string b, CobolCollation? collation) =>
        collation is null ? CobolString.Compare(a, b) : CobolString.Compare(a, b, collation);

    /// <summary>Split a dynamic-capacity table's carried content into its occurrences at
    /// <paramref name="elementWidth"/> character positions each — the read half of the concatenation the
    /// composer emits. A trailing partial occurrence is padded, so a sender whose capacity ended mid-element
    /// (only reachable through the rule-1 size latitude) still yields well-formed elements.</summary>
    public static string[] Occurrences(string content, int elementWidth)
    {
        if (elementWidth <= 0 || content.Length == 0) return [];
        int n = (content.Length + elementWidth - 1) / elementWidth;
        var parts = new string[n];
        for (int k = 0; k < n; k++)
        {
            int at = k * elementWidth;
            parts[k] = CobolString.Store(content[at..Math.Min(content.Length, at + elementWidth)], elementWidth);
        }
        return parts;
    }
}
