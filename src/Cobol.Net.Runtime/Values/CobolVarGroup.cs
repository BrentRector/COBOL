// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
/// <para>⛔ NOT a general-purpose serializer, and never persisted: it exists only between the argument
/// evaluation and the formal's copy-in (and back at the copy-out), the same lifetime the string image has.</para>
/// </summary>
/// <param name="Fixed">The group's image with the variable-length components collapsed to zero width.</param>
/// <param name="Dynamic">Each variable-length component's current content, in declaration order.</param>
public sealed record CobolVarGroup(string Fixed, string[] Dynamic)
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
        return new CobolVarGroup(f, d);
    }

    // ── The §8.5.1.12 LAYOUT of a group, and the correspondence between two of them (kb/Work PB965) ─────────────
    //
    // A group's layout is a FLAT sequence of (kind, chars, elementChars) triples in CHARACTER positions, left to
    // right, computed at compile time from the group's own §8.5.1.12 atoms (VariableLengthCompatibility.Layout —
    // REDEFINES subtrees dropped, scalar subordinate groups flattened, a table kept whole). It is the group's
    // DESCRIPTION as far as §8.5.1.12 needs one, and it travels with the group across an activation boundary
    // (CobolArg.Layout) exactly as a numeric item's NumProfile does, because the two sides of a CALL are compiled
    // apart and the correspondence is a fact about the PAIR.

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

    /// <summary>⛔ THE ONE §8.5.1.12.2 CORRESPONDENCE between a FIXED-length group and a VARIABLE-length group,
    /// answered as the character spans of the FIXED group's tables that correspond to the variable-length group's
    /// dynamic-capacity tables, in order — exactly the <paramref name="fixedLayout"/>-relative spans
    /// <see cref="FromFixedImage"/> and <see cref="ToFixedImage"/> take. Null when the pair is NOT compatible.
    /// <para>§8.5.1.12.2: "Two tables correspond if at least one of them is a dynamic-capacity table and they
    /// occupy the same relative byte positions within their groups." — so a fixed table corresponds only when a
    /// dynamic-capacity table STARTS where it starts; every other fixed table is plain material, and lifting it
    /// out as a component (what "every table of the fixed group" did before PB965) shifted every later component
    /// one place and moved the wrong table. §8.5.1.12.3 sentence 3 treats the fixed table "as though it were a
    /// dynamic-capacity table whose capacity is either its fixed number of occurrences or the value of the
    /// DEPENDING operand", and makes the dynamic one "the same length as the corresponding table", so the walk
    /// advances BOTH sides by the fixed table's width there. Corresponding tables match only "when the byte
    /// length of their elements is equal" (sentence 2).</para>
    /// <para>Every dynamic-LENGTH item of the variable-length group needs a corresponding dynamic-length item
    /// (§8.5.1.12.1 rule 3), which a fixed-length group has none of — so one fails the pair. A dynamic-capacity
    /// table whose position is BEYOND the fixed group's last character takes §8.5.1.12.2's last sentence: it "is
    /// treated as if it corresponds to a space-filled fixed-length table", so it gets NO component — the carrier
    /// then leaves it absent (<see cref="HasDyn"/> false), which is the §14.6.9.4 space fill at an unaffected
    /// capacity rather than an invented length.</para>
    /// <para>Used at COMPILE time by the §14.9.25.4 GR9 MOVE (both groups known) and at RUN time by the CALL
    /// boundary (each side compiled apart) — one walk, so the two cannot disagree about which table is
    /// which.</para></summary>
    public static int[]? CorrespondingSpans(int[]? fixedLayout, int[] varLayout)
    {
        // No stated layout: nothing is known about the fixed side's shape, not even its length.
        if (fixedLayout is null) return null;
        int nF = fixedLayout.Length / 3, nV = varLayout.Length / 3;
        var spans = new List<int>();
        int i = 0, j = 0, pf = 0, pv = 0;
        while (j < nV)
        {
            int vk = varLayout[3 * j], vc = varLayout[3 * j + 1], ve = varLayout[3 * j + 2];
            bool vDynamic = vk is LayoutDynamicTable or LayoutDynamicLength;
            // The fixed side lags: its atom lies wholly before the variable side's position — plain material.
            if (i < nF && pf < pv) { pf += fixedLayout![3 * i + 1]; i++; continue; }
            // No fixed atom STARTS at the variable side's position.
            if (pv < pf || i >= nF)
            {
                if (!vDynamic) { pv += vc; j++; continue; }
                // §8.5.1.12.2's last sentence — a dynamic-capacity table past the shorter group's end.
                if (vk == LayoutDynamicTable && i >= nF && pv >= pf) { j++; continue; }
                return null;
            }
            // Both sides stand at the same relative position.
            if (vk == LayoutDynamicLength) return null;
            int fk = fixedLayout![3 * i], fc = fixedLayout[3 * i + 1], fe = fixedLayout[3 * i + 2];
            if (vk == LayoutDynamicTable)
            {
                if (fk is not (LayoutTable or LayoutOdoTable) || fe != ve) return null;
                spans.Add(pf);
                spans.Add(fk == LayoutOdoTable ? -1 : fc);
                pf += fc; pv += fc; i++; j++;
                continue;
            }
            // Plain material on the variable side (bytes, or a table that is not dynamic): consume it; the
            // fixed side re-aligns on the next pass. Widths may differ freely — §8.5.1.12 constrains only the
            // POSITIONS of the variable-length items.
            pv += vc; j++;
        }
        return [.. spans];
    }

    /// <summary>The layout of a fixed-length group that states none — a group with no table crosses without one
    /// (<c>CobolArg.Layout</c> is null), and its only §8.5.1.12 fact is its length: one run of fixed material,
    /// which corresponds to a dynamic-capacity table only through §8.5.1.12.2's beyond-the-end sentence.</summary>
    public static int[] FixedRun(int chars) => [LayoutFixed, chars, 0];

    /// <summary>⛔ THE FIXED-LENGTH GROUP'S VIEW OF THIS CARRIER — the adapter that lets a FIXED group stand on
    /// the other side of an ISO §14.9.25.4 GR9 move (kb/Work PB393). §8.5.1.12.1 admits the pair explicitly
    /// ("either both operands may be variable-length groups or only one of the operands may be a variable-length
    /// group"), and §8.5.1.12.3 sentence 3 says how: a table corresponding to the other group's dynamic-capacity
    /// table "is treated as though it were a dynamic-capacity table whose capacity is either its fixed number of
    /// occurrences or the value of the DEPENDING operand, as applicable" — §14.6.9.1 states the same conversion
    /// for the operation itself. So a fixed group decomposes into EXACTLY this carrier: its record image with
    /// each corresponding table's character span lifted out as a component, in order.
    /// <para><paramref name="spans"/> is a FLAT (offset, width) pair list in character positions — the PAIR's
    /// correspondence, <see cref="CorrespondingSpans"/>, never "every table of the fixed group" (kb/Work PB965).
    /// A width of −1 means "to the end of the image" —
    /// the occurs-depending table, whose current extent is a run-time length and which §13.18.38.3 SR22 makes
    /// the trailing storage of its record, so "the rest" IS its current occurrences.</para></summary>
    public static CobolVarGroup FromFixedImage(string image, int[] spans)
    {
        var dyn = new string[spans.Length / 2];
        var fixedRun = new System.Text.StringBuilder(image.Length);
        int at = 0;
        for (int k = 0; k < dyn.Length; k++)
        {
            int off = spans[2 * k];
            int width = spans[2 * k + 1];
            int start = Math.Min(off, image.Length);
            int end = width < 0 ? image.Length : Math.Min(start + width, image.Length);
            fixedRun.Append(image[Math.Min(at, image.Length)..start]);
            dyn[k] = image[start..end];
            at = end;
        }
        if (at < image.Length) fixedRun.Append(image[at..]);
        return new CobolVarGroup(fixedRun.ToString(), dyn);
    }

    /// <summary>The inverse of <see cref="FromFixedImage"/>: rebuild a FIXED group's record image of
    /// <paramref name="totalWidth"/> character positions by re-inserting each component at its span. A component
    /// is fitted to its span's width — ISO §14.6.9.2's own rule for a non-dynamic receiving table ("if the
    /// sending table has a higher current capacity than the receiving table, superfluous elements are not moved";
    /// "if the sending table has a lower current capacity … all the remaining elements of the receiving table are
    /// space filled") — and the fixed run is fitted to what is left, which is §14.9.25.4 GR9b's excess rule for
    /// the fixed material (space fill when short, ignore when long).</summary>
    public static string ToFixedImage(CobolVarGroup v, int totalWidth, int[] spans)
    {
        var outp = new System.Text.StringBuilder(totalWidth);
        int fixedAt = 0;
        for (int k = 0; k < spans.Length / 2; k++)
        {
            int off = spans[2 * k];
            int width = spans[2 * k + 1] < 0 ? Math.Max(0, totalWidth - off) : spans[2 * k + 1];
            int take = Math.Max(0, off - outp.Length);
            outp.Append(CobolString.Store(
                fixedAt >= v.Fixed.Length ? "" : v.Fixed[fixedAt..Math.Min(v.Fixed.Length, fixedAt + take)], take));
            fixedAt += take;
            outp.Append(CobolString.Store(v.Dyn(k), width));
        }
        outp.Append(fixedAt >= v.Fixed.Length ? "" : v.Fixed[fixedAt..]);
        return CobolString.Store(outp.ToString(), totalWidth);
    }

    /// <summary>⛔ A FIXED-LENGTH VIEW'S STORE BACK INTO A VARIABLE-LENGTH GROUP'S STORAGE (kb/Work PB965) — the
    /// BY REFERENCE write-back of a fixed-length group FORMAL whose argument is a variable-length group. §14.2.3
    /// GR8: "the activated runtime element operates as if the formal parameter occupies the same storage area as
    /// the argument", so a store through the formal reaches only the argument storage the formal overlays and
    /// leaves the rest as it stands:
    /// <list type="bullet">
    ///   <item>each corresponding table (<paramref name="spans"/>, the pair's <see cref="CorrespondingSpans"/>)
    ///     is written OVER the argument's current occurrences, never re-sized. ⚠ DETERMINATION (§8.5.1.12 and
    ///     §14.2.3 are silent): a formal whose table has a FIXED occurrence count cannot change the argument
    ///     table's current capacity — that description has no capacity to state — so an occurrence the argument
    ///     does not have is not stored and one past the formal's count is untouched, the mirror of
    ///     <see cref="ToFixedImage"/>'s rule for the reverse pair;</item>
    ///   <item>the fixed material the formal covers is replaced and the argument's material past it survives —
    ///     the §14.8.2.2 rule 1 prefix, and every component past the formal's last character.</item>
    /// </list></summary>
    public static CobolVarGroup OverlayFixedImage(CobolVarGroup current, string image, int[] spans)
    {
        var view = FromFixedImage(image, spans);
        string fixedRun = view.Fixed.Length >= current.Fixed.Length
            ? view.Fixed[..current.Fixed.Length]
            : view.Fixed + current.Fixed[view.Fixed.Length..];
        var dyn = (string[])current.Dynamic.Clone();
        for (int k = 0; k < view.Dynamic.Length && k < dyn.Length; k++)
        {
            string was = dyn[k], now = view.Dynamic[k];
            dyn[k] = now.Length >= was.Length ? now[..was.Length] : now + was[now.Length..];
        }
        return new CobolVarGroup(fixedRun, dyn);
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
    /// VARIABLE-LENGTH GROUP (kb/Work PB1467): it "proceeds from left to right as described under 8.8.4.2.7,
    /// Comparison of alphanumeric operands except that — when corresponding tables are encountered, they are
    /// compared as described in 14.6.9.3, Comparing two tables — when corresponding dynamic-length elementary items
    /// are encountered, the length is determined as described in 8.5.1.10.4 … After comparison of corresponding
    /// tables or dynamic-length elementary items, comparison continues with the next data item in each of the
    /// compatible groups."
    /// <para>The two carriers already lay their operands out in that order: <see cref="Fixed"/> is each group's
    /// material with every variable-length component collapsed (the §8.5.1.12.3 accounting under which compatible
    /// groups have the same relative positions), and component k sits at <paramref name="fixedAt"/>[k] of it. So
    /// the walk compares each stretch of fixed material, then each pair of components, in order, and stops at the
    /// first inequality. Every step is one §8.8.4.2.7 comparison — the shorter side extended with spaces — which is
    /// exactly both "except" clauses: a dynamic-length item compares at its current length (§8.5.1.10.4 — "treated
    /// as a fixed-length data item whose length is the dynamic-length elementary item's current length"), and two
    /// tables of equal element width (§8.5.1.12.3's matching) compared element by element "until … the last element
    /// of the table with the smallest current capacity has been compared", after which "each successive remaining
    /// element of the larger table [is compared] with spaces" (§14.6.9.3), is the same as their concatenated
    /// occurrences compared with space padding. A component a side does not carry (a dynamic-capacity table beyond
    /// a shorter fixed group's end, §8.5.1.12.2) is the empty string — compared with spaces, the space-filled table
    /// that sentence substitutes.</para>
    /// <para><paramref name="collation"/> is the alphanumeric program collating sequence when it is not native
    /// (§8.8.4.2.7), null otherwise.</para></summary>
    /// <returns>&lt;0, 0 or &gt;0 as <paramref name="a"/> is less than, equal to or greater than <paramref name="b"/>.</returns>
    public static int Compare(CobolVarGroup a, CobolVarGroup b, IReadOnlyList<int> fixedAt, CobolCollation? collation = null)
    {
        int pos = 0;
        for (int k = 0; k < fixedAt.Count; k++)
        {
            int at = fixedAt[k];
            int c = CompareRun(Slice(a.Fixed, pos, at - pos), Slice(b.Fixed, pos, at - pos), collation);
            if (c != 0) return c;
            c = CompareRun(a.Dyn(k), b.Dyn(k), collation);
            if (c != 0) return c;
            pos = at;
        }
        int rest = Math.Max(a.Fixed.Length, b.Fixed.Length) - pos;
        return CompareRun(Slice(a.Fixed, pos, rest), Slice(b.Fixed, pos, rest), collation);
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
