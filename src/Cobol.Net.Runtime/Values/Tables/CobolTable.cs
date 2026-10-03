// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
namespace CobolNet.Runtime;

/// <summary>
/// Table-element access (ISO §8.4.2.3 subscripting). Every emitted subscripted reference routes through
/// <see cref="At{T}"/> — the ONE occurrence-number→element mapping.
/// </summary>
public static class CobolTable
{
    /// <summary>
    /// The table element at a 1-based <paramref name="occurrence"/> number, as a writable reference.
    /// <para><b>Out-of-range</b> (ISO §8.4.2.3.4 GR2): with EC-BOUND-SUBSCRIPT checking ON the condition is raised
    /// (below); with it OFF — the COBOL-85 semantics, since COBOL-85 has no exception conditions — the reference
    /// continues benignly per the implementor-defined rule the NIST-85 golden requires: reads see a fresh
    /// default-valued element (spaces-equivalent for alphanumeric), writes are absorbed. The element is a per-type
    /// scratch slot, re-defaulted on every out-of-range access. Caveat: a GROUP element's scratch is a zeroed
    /// struct — its string members are null, so group-level use of an out-of-range element may still fail loudly.</para>
    /// </summary>
    public static ref T At<T>(T[] table, long occurrence)
    {
        // A NULL table is itself an out-of-range chain: a multi-dimension reference whose OUTER subscript was
        // out of range continues through the zeroed scratch struct, whose nested OCCURS arrays are null
        // (NC401M's 5-deep FAIL-path read) — every further level resolves benignly too.
        if (table is not null && occurrence >= 1 && occurrence <= table.Length) return ref table[(int)(occurrence - 1)];
        // §8.4.2.3.4 GR2 — "If the value of the subscript is not a positive integer or is less than one or is
        // greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is
        // set to exist." Raised BEFORE the scratch fallback so a CHECKING-ON reference reports the condition;
        // with checking off the helper returns and the scratch read below stands unchanged.
        ExceptionState.SubscriptError(
            $"subscript {occurrence} is outside 1..{(table?.Length ?? 0)} (ISO 8.4.2.3.4 GR2)");
        Scratch<T>.Slot = typeof(T) == typeof(string) ? (T)(object)string.Empty : default!;
        return ref Scratch<T>.Slot;
    }

    /// <summary>An element of an OCCURS DEPENDING table, referenced where EC-BOUND-ODO checking can be enabled:
    /// ISO §13.18.38.4 GR7 — "At the time the subject of entry is referenced or any data item subordinate or
    /// superordinate to the subject of entry is referenced, the value of the data item referenced by data-name-1 shall
    /// fall within the bounds from integer-1 through integer-2. If the value of the data item does not fall within the
    /// specified bounds, the EC-BOUND-ODO exception condition is set to exist." A reference to the subject or to an
    /// item subordinate to it passes through this level's subscript, so the test is made HERE, before the element is
    /// located (kb/Work PB1268 — only the superordinate group's extent, <see cref="OdoExtent"/>, used to test it).
    /// With checking off the condition is not raised and the element reference proceeds exactly as
    /// <see cref="At{T}(T[], long)"/>; the subscript's own range is still 1..integer-2 (§8.4.2.3.4 2): "the highest
    /// permissible occurrence number … of an occurs-depending table is the maximum number of occurrences").</summary>
    public static ref T At<T>(T[] table, long occurrence, long depending, int min, int max)
    {
        if (depending < min || depending > max)
            ExceptionState.OdoError(
                $"OCCURS DEPENDING value {depending} is outside {min}..{max} at a reference to the table (ISO 13.18.38.4 GR7)");
        return ref At(table, occurrence);
    }

    /// <summary>The out-of-range reference's scratch cell (checking off: the reference reads the empty value and a
    /// store into it is discarded). <b>One per THREAD</b> (kb/Work PB1069): it is returned by <c>ref</c>, so a
    /// process-wide cell let two run units on two threads that both took an out-of-range subscript write one
    /// location between the reset above and the caller's use. A run unit executes on one thread at a time and the
    /// cell is overwritten before every use, so a per-thread cell carries nothing between run units.</summary>
    private static class Scratch<T>
    {
        [ThreadStatic] private static Cell? s_cell;

        /// <summary>This thread's cell (created on the thread's first out-of-range reference of type T).</summary>
        public static ref T Slot => ref (s_cell ??= new Cell()).Value;

        private sealed class Cell { public T Value = default!; }
    }

    /// <summary>A NON-NUMERIC position operand's occurrence-number value — the two carriers only a
    /// <c>--permissive</c> position operand has: an index DATA item (an <c>IndexCell</c> is a <c>long</c>) and an
    /// alphanumeric / national / boolean item whose digit characters the documented COBOLNET0844 leniency decodes
    /// as an unsigned integer. A NUMERIC position operand never comes here: it reads through the profile arity
    /// below, which is where §14.6.13.2 rule 2 lives.</summary>
    public static long Occ(long value) => value;

    /// <inheritdoc cref="Occ(long)"/>
    /// <remarks>The digit decode of a non-numeric item's characters — <see cref="CobolNum.DigitMagnitude"/>, the
    /// extension COBOLNET0844's <c>--permissive</c> message promises (kb/Work PB170). Not a rule 2 read: the
    /// operand is not a numeric sending item, so there is no numeric class condition for its content to fail.</remarks>
    public static long Occ(string image) => CobolNum.Position(CobolNum.DigitMagnitude(image));

    /// <summary>⛔ THE NUMERIC POSITION READ — a NUMERIC data item used as a subscript, as the current count of an
    /// OCCURS DEPENDING table, or as any other integer the runtime positions by (a RECORD VARYING DEPENDING
    /// length, a report DEPENDING count), decoded through the item's OWN profile. One arity for every storage
    /// form: the bind-time subscript text names the field and C# overload resolution picks the carrier (native
    /// <c>long</c>, the character image the post-bind whole-group analysis may select, the <see cref="Int128"/>
    /// wide tier, or the unsigned <c>ulong</c> / <see cref="UInt128"/> BINARY-CAPACITY containers of §13.18.60.4
    /// GR12) at backend-compile time — kb/Work PB201's carrier set, which <c>PositionCarrierOverloadDriftTests</c>
    /// holds to the compiler's list.
    /// <para><b>The profile carries both rules the read owes.</b> (1) ISO §14.6.13.2 rule 2 — "When the content of
    /// a numeric sending item that is not described with a standard floating-point usage is referenced during the
    /// execution of a statement and the content of that sending operand would evaluate to false in a numeric class
    /// condition", EC-DATA-INCOMPATIBLE is set. A position operand is item identification, and rule 1 says so in
    /// as many words: the condition "is set to exist for a class condition and a VALIDATE statement when invalid
    /// data is detected during item identification" — so the class-condition exemption covers the ITEM a class
    /// test examines, never the subscript that locates it, and this read is checked in every context (kb/Work
    /// PB1117). The character-image arm is therefore <see cref="CobolNum.ParseImageSending"/>, THE checked
    /// fixed-point sending read; the native arms cannot hold invalid content and pay nothing. (2) §8.4.2.3.4 GR1b's
    /// integrality — the item's scale is <see cref="NumProfile.FractionDigits"/>, so a scaled item needs no second
    /// arity. Before PB1117 the image arm was the tolerant <see cref="CobolNum.DigitMagnitude"/> scan, which also
    /// DISCARDED the sign: a signed image holding −1 positioned occurrence 1.</para>
    /// <para>Narrowing SATURATES (<see cref="CobolNum.Position(Int128)"/>): an occurrence number past
    /// <c>long.MaxValue</c> must stay out of range, or §8.4.2.3.4 GR2's condition is lost to a wrap. With
    /// EC-BOUND-SUBSCRIPT checking OFF a fractional position truncates toward zero and the reference continues —
    /// the lenient posture <see cref="At{T}"/> takes for an out-of-range occurrence. The FLOAT carriers are
    /// deliberately absent: a float subscript routes to the D18 §15.4 temp, where GR1b is applied once, to the
    /// result.</para></summary>
    public static long Occ(long unscaled, in NumProfile item) => OccScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Occ(long, in NumProfile)"/>
    public static long Occ(Int128 unscaled, in NumProfile item) => OccScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Occ(long, in NumProfile)"/>
    public static long Occ(string image, in NumProfile item) => item.ImageExceedsInt128
        ? Occ(CobolNum.ParseImageU128Sending(image, item), item)
        : OccScaled(CobolNum.ParseImageSending(image, item), item.FractionDigits);

    /// <inheritdoc cref="Occ(long, in NumProfile)"/>
    public static long Occ(ulong unscaled, in NumProfile item) => OccScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="Occ(long, in NumProfile)"/>
    public static long Occ(UInt128 unscaled, in NumProfile item) =>
        // A value past Int128's range is out of range for every table whatever its fraction: saturate.
        unscaled > (UInt128)Int128.MaxValue ? long.MaxValue : OccScaled((Int128)unscaled, item.FractionDigits);

    private static long OccScaled(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            NotAnInteger(CobolNum.PlainValue(unscaled, scale));
        return CobolNum.PositionOf(unscaled, scale);
    }

    private static void NotAnInteger(string shown) =>
        ExceptionState.SubscriptError($"subscript value {shown} is not an integer (ISO 8.4.2.3.4 GR1b)");

    /// <summary>A subscript that is an arithmetic EXPRESSION (or a floating-point item), as the occurrence number it
    /// denotes — the statement pre-operation's intake, asked on the carrier the expression evaluated on (kb/Work
    /// PB1890). §8.4.2.3.4 GR1b: "If the evaluation of arithmetic-expression-1 does not result in an integer, the
    /// EC-BOUND-SUBSCRIPT exception condition is set to exist" — so the test reads the EXACT intermediate, never a
    /// copy stored at a fixed fraction width, which made <c>T(IX + 0.0000000001)</c> an integer. The same lenient
    /// continue as <see cref="Occ(long, in NumProfile)"/> with checking off: the position truncates toward zero.
    /// Three carriers, three names (an integer literal converts to both <c>Int128</c> and <c>double</c>).</summary>
    public static long OccValue(Int128 unscaled, int scale) => OccScaled(unscaled, scale);

    /// <inheritdoc cref="OccValue(Int128, int)"/>
    public static long OccValueDec(CobolDec value)
    {
        if (value.HasFraction) NotAnInteger(value.ToDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return CobolNum.PositionOf(value);
    }

    /// <inheritdoc cref="OccValue(Int128, int)"/>
    public static long OccValueReal(double value)
    {
        if (CobolNum.HasFraction(value)) NotAnInteger(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return CobolNum.PositionOf(value);
    }

    /// <summary>The CURRENT character extent of an occurs-depending GROUP operand (ISO/IEC 1989:2023 §13.18.38
    /// GR8): the fixed prefix plus data-name-1's value clamped to [0, max] occurrences, times the per-occurrence
    /// width. A count outside integer-1..integer-2 makes the excess content undefined (GR7); the benign clamp is
    /// the COBOL-85 policy (no exception conditions) and the 2002+ default until EC-BOUND-ODO checking lands.</summary>
    /// <param name="min">integer-1 of the OCCURS DEPENDING clause — the LOWER bound §13.18.38.4 GR7 requires the
    /// control value to fall within. It was previously absent and the floor hardcoded to 0, so a below-minimum
    /// DEPENDING value silently clamped instead of raising.</param>
    public static int OdoExtent(long count, int min, int max, int fixedUnits, int elemUnits)
    {
        // §13.18.38.4 GR7 — the value "shall fall within the bounds from integer-1 through integer-2. If the
        // value of the data item does not fall within the specified bounds, the EC-BOUND-ODO exception condition
        // is set to exist." Both ends matter; checking off keeps the clamp, whose result GR7's closing sentence
        // makes undefined content and therefore a conforming implementor choice.
        if (count < min || count > max)
            ExceptionState.OdoError(
                $"OCCURS DEPENDING value {count} is outside {min}..{max} (ISO 13.18.38.4 GR7)");
        long c = count < 0 ? 0 : count > max ? max : count;
        return fixedUnits + (int)c * elemUnits;
    }

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand whose positions are counted in
    /// <paramref name="positionsPerChar"/> units (1 for a character subtree, 8 for one holding USAGE BIT leaves —
    /// ISO/IEC 1989:2023 §8.5.1.6.3, kb/Work PB173): <see cref="OdoExtent"/> rounded UP to whole characters. The
    /// ceiling is where §8.5.1.6.3's trailing-filler rule puts the group's own partial byte, and it is the same
    /// rounding §15.50.4 r9 requires of every other bit width. With <paramref name="positionsPerChar"/> = 1 it is
    /// the identity, so the character channel has ONE arm, never a units branch.</summary>
    public static int OdoExtentChars(long count, int min, int max, int fixedUnits, int elemUnits, int positionsPerChar)
    {
        int units = OdoExtent(count, min, max, fixedUnits, elemUnits);
        return positionsPerChar <= 1 ? units : (units + positionsPerChar - 1) / positionsPerChar;
    }

    // ── The table(ALL) intrinsic-argument enumeration (ISO §15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>
    /// The argument list a <c>table(… ALL …)</c> intrinsic argument stands for (ISO §15.3 — "When ALL is specified
    /// as a subscript, the effect is as if each table element associated with that subscript position were
    /// specified. The order … is from left to right, with the first … specification being the identifier with
    /// each subscript specified by the word ALL replaced by one, the next … the rightmost subscript specified by
    /// the word ALL incremented by one …"): a row-major enumeration over the ALL levels' RANGES, outermost first.
    /// Each level's range is read when the level is ENTERED, as a function of the outer indices — a fixed OCCURS
    /// count, an OCCURS DEPENDING table's current count, or a dynamic-capacity table's current capacity ("from 1 to
    /// the current capacity of the table"), which for a table nested inside another depends on the outer occurrence.
    /// <para>"The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of the
    /// reference to the function-identifier is undefined." WiseOwl COBOL defines the undefined case as
    /// EC-ARGUMENT-FUNCTION (set when checking is on) and terminates the reference with that name either way — a
    /// zero-argument list is never handed to a body whose result over nothing is itself undefined.</para>
    /// <para><paramref name="lead"/>, when given, renders the FIRST element enumerated in place of
    /// <paramref name="element"/>: the table(ALL) is then the function's leading argument, and its first implicit
    /// element is argument-1 (the enumeration order above), which a body such as PRESENT-VALUE screens against its
    /// argument-1 value domain on the element's EXACT carrier (kb/Work PB1000) before it joins the binary64 list.
    /// Which element is first is known only here — an inner ALL level may range over nothing for the first outer
    /// occurrence — so the choice is made by the walk, not by the caller's subscripts.</para>
    /// </summary>
    public static T[] AllArgs<T>(Func<long[], long>[] counts, Func<long[], T> element, Func<long[], T>? lead = null)
    {
        var idx = new long[counts.Length];
        var list = new List<T>();
        void Walk(int level)
        {
            if (level == counts.Length) { list.Add(list.Count == 0 && lead is not null ? lead(idx) : element(idx)); return; }
            long n = counts[level](idx);
            for (long i = 1; i <= n; i++) { idx[level] = i; Walk(level + 1); }
        }
        Walk(0);
        if (list.Count == 0)
        {
            const string why = "a table(ALL) intrinsic argument ranged over no occurrences — the ALL subscript shall result in at least one argument (ISO §15.3)";
            ExceptionState.ArgumentError(why);                          // EC-ARGUMENT-FUNCTION when checking is on (fatal, USE-dispatchable)
            throw new CobolFatalException("EC-ARGUMENT-FUNCTION", why);  // the undefined case is defined LOUD, checking or not
        }
        return list.ToArray();
    }

    /// <summary>The intrinsic argument list assembled from written operands and <see cref="AllArgs{T}"/> enumerations,
    /// in source order — the ONE array a <c>params T[]</c> body receives.</summary>
    public static T[] ArgConcat<T>(params T[][] parts)
    {
        int n = 0;
        foreach (var p in parts) n += p.Length;
        var all = new T[n];
        int at = 0;
        foreach (var p in parts) { Array.Copy(p, 0, all, at, p.Length); at += p.Length; }
        return all;
    }

    /// <summary>Bind an evaluated argument list to a name inside an EXPRESSION (the C# analogue of <c>let</c>): the
    /// intrinsic renderers use it when a body needs the SAME enumerated list twice — MEAN's sum and its count, a
    /// leading positional argument and the tail — so a runtime table(ALL) enumeration is evaluated exactly once.</summary>
    public static R With<T, R>(T value, Func<T, R> body) => body(value);

    /// <summary>
    /// ⛔ THE TABLE SORT (ISO §14.9.40 Format 2), run so that a COBOL EXCEPTION CONDITION RAISED BY THE KEY
    /// COMPARISON STILL REACHES THE STATEMENT GUARD.
    /// </summary>
    /// <remarks>
    /// <para>The sort is <c>OrderBy</c> with a <see cref="Comparison{T}"/> — stable, which is what §14.9.40.4
    /// GR19c/GR3c require of equal keys. But the framework's array sort CATCHES anything a comparer throws and
    /// re-throws it as <see cref="InvalidOperationException"/> "Failed to compare two elements in the array",
    /// on the assumption that a throwing comparer is an inconsistent one. A COBOL comparer is not: §14.9.40.4
    /// GR8 makes a key comparison follow the relation-condition rules, and §14.6.13.2 rule 2 makes REFERENCING a
    /// numeric key whose content is not valid set the fatal EC-DATA-INCOMPATIBLE. Wrapped, that fatal never
    /// matched the emitted statement guard's <c>catch (CobolFatalException)</c>, so a program with a USE
    /// declarative for the condition died with an unhandled .NET exception instead of running its declarative.
    /// MEASURED, not deduced (kb/Work PB230): a group MOVE plants "AB1" in a key, and the run unit aborted with
    /// <c>System.InvalidOperationException: Failed to compare two elements in the array</c>.</para>
    /// <para>So the wrapper is undone here, at the ONE place a COBOL comparer is handed to the framework, and the
    /// original is re-thrown with its stack intact (<see cref="ExceptionDispatchInfo"/>, never a bare
    /// <c>throw f</c>, which would reset it). Any future comparer raise — a float key's EC-DATA-NOT-FINITE, a
    /// locale collation condition — is covered by construction rather than by remembering to add an arm.</para>
    /// </remarks>
    /// <param name="occurrences">The table's CURRENT occurrences, and only those (§14.9.40.4 GR20 — "The number of
    /// occurrences of table elements referenced by data-name-2 is determined by the rules in the OCCURS clause";
    /// kb/Work PB1174): the first data-name-1 elements of an OCCURS DEPENDING table (§13.18.38.4 GR7), a
    /// dynamic-capacity table's current capacity (<see cref="CobolDynTable{T}.CurrentOccurrences"/>), the whole
    /// array of a fixed one. Sorting the physical array pulled the stale content past the current count into the
    /// table and pushed real elements out of it. The sorted occurrences are placed back into the same span (GR24).</param>
    /// <param name="compare">The statement's key comparer (GR2 significance, GR19 direction).</param>
    public static void SortInPlace<T>(Span<T> occurrences, Comparison<T> compare)
    {
        T[] sorted;
        try
        {
            sorted = [.. occurrences.ToArray().OrderBy(e => e, Comparer<T>.Create(compare))];
        }
        catch (InvalidOperationException ex) when (ex.InnerException is CobolFatalException fatal)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fatal).Throw();
            throw;   // unreachable — Throw() does not return; present because the compiler cannot know that
        }
        sorted.CopyTo(occurrences);
    }
}
