// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// COBOL character-position value semantics over .NET <see cref="string"/> — the ONE fixed-width store/compare
/// substrate for the string-stored categories: alphanumeric (PIC X / A), national (PIC N — one .NET UTF-16
/// <see cref="char"/> per national position, the documented D-N1 implementor choice), and boolean (PIC 1 — one
/// '0'/'1' character per boolean position, the §13.18.40.4 GR14 alphanumeric-representation license, D-B1).
/// The category difference is carried entirely by <paramref name="pad"/>: alphanumeric/national fill with
/// space (§14.6.8.4/§14.6.8.5 — the national space is U+0020 under the Latin-1 identity), boolean with
/// boolean zero '0' (§14.6.8.6).
/// </summary>
public static class CobolString
{
    /// <summary>
    /// Store <paramref name="value"/> into a character receiver of <paramref name="width"/> positions,
    /// applying COBOL MOVE rules (ISO/IEC 1989:2023 §14.9.25 / §14.6.8): left-justified by default — pad on the
    /// right with <paramref name="pad"/>, truncate on the right when too long; right-justified
    /// (<c>JUSTIFIED RIGHT</c>, §13.18.32 GR1/GR2) — pad/truncate on the left.
    /// </summary>
    public static string Store(string? value, int width, bool justifiedRight = false, char pad = ' ')
    {
        value ??= "";
        if (width <= 0) return "";
        if (value.Length == width) return value;

        if (justifiedRight)
            return value.Length > width ? value[^width..] : value.PadLeft(width, pad);
        return value.Length > width ? value[..width] : value.PadRight(width, pad);
    }

    /// <summary>The character image <paramref name="s"/> repeated <paramref name="n"/> times — used to seed a
    /// Tier-B REDEFINES backing over a fixed-OCCURS entry, where every occurrence takes the VALUE (ISO §13.18.63
    /// GR9). n ≤ 1 returns the image unchanged.</summary>
    public static string Repeat(string s, int n) => n <= 1 ? s : string.Concat(Enumerable.Repeat(s, n));

    /// <summary>
    /// ⛔ THE ONE §8.3.3.6.4 GR2 SIZING of a figurative constant against an associated operand (kb/Work PB297).
    /// The rule, verbatim: "the string of characters is repeated character by character until the size of the
    /// resultant string is greater than or equal to the number of character positions in the associated data
    /// item, literal, or intermediate result. This resultant string is then truncated from the right until the
    /// number of character positions remaining is equal either to 1 or to the number of character positions in
    /// the associated data item, literal, or intermediate result, whichever is greater."
    /// <para><paramref name="seed"/> is the figurative's own string — one fill character for a plain figurative
    /// word, literal-1 for <c>ALL literal-1</c>. An EMPTY seed yields spaces (nothing to repeat). The compiler's
    /// COMPILE-TIME fold (<c>EmitText.RepeatToWidth</c>, used where the associated width is a static property of
    /// a VALUE clause or a fixed receiver) forwards HERE, so the constant-folded and the runtime-sized forms can
    /// never drift apart.</para>
    /// </summary>
    public static string FigToWidth(string seed, int width)
    {
        int w = Math.Max(width, 1);                    // GR2's "equal either to 1 or to … whichever is greater"
        if (seed.Length == 0) return new string(' ', w);
        if (seed.Length == w) return seed;             // already the associated operand's size — no allocation
        if (seed.Length == 1) return new string(seed[0], w);
        var sb = new System.Text.StringBuilder(w + seed.Length);
        while (sb.Length < w) sb.Append(seed);
        return sb.ToString()[..w];
    }

    /// <summary>The OMITTED reference-modification length sentinel — the <c>identifier(leftmost:)</c> "to the end"
    /// form (ISO §8.4.3.3.4: "If length is not specified … to the end"). A distinct sentinel (NOT −1) so that a
    /// SPECIFIED length that evaluates negative at runtime is DISTINGUISHABLE from the omitted form and can raise
    /// EC-BOUND-REF-MOD (review C14 — a −1 sentinel collided with a specified −1, making the §8.4.3.3.4 item 5c
    /// positive-nonzero violation structurally undetectable). Emitted by <c>PlaceRenderer.RmLen</c> for the
    /// length-omitted ref-mod and by the INVOKE §14.8.2.2 rule-1 prefix splice.</summary>
    public const int OmittedRefModLength = int.MinValue;

    /// <summary>A SPECIFIED reference-modifier length, floored one above <see cref="OmittedRefModLength"/> so a
    /// hugely negative specified length can never be the omitted sentinel and be read as "to the end" — it stays a
    /// negative length, which item 5c rejects (kb/Work PB1033). The floor keeps the exact text of the value it moves
    /// (<see cref="Occurrence.AtLeast"/>), so the violation still names the length the program holds (kb/Work PB2938).</summary>
    public static Occurrence SpecifiedRefModLength(Occurrence length) => length.AtLeast(OmittedRefModLength + 1L);

    /// <summary>⛔ THE ONE §8.4.3.3.4 item 5c range test, shared by the read (<see cref="RefMod"/>) and the write
    /// (<see cref="SpliceInto"/>) — they were two copies. "leftmost … shall be 1..size; a SPECIFIED length shall
    /// be a positive nonzero integer, with leftmost+length-1 &lt;= size." The last conjunct is written as
    /// <c>length &gt; size - leftmost + 1</c>: the natural <c>leftmost + length - 1 &gt; size</c> OVERFLOWS for a
    /// length near the top of its type (a saturated one included) and wraps to a negative sum that passes the test
    /// (kb/Work PB1033). <paramref name="leftmost"/> is already within 1..size when it is evaluated, so the rewritten
    /// form cannot overflow. The operands are the positions' <c>long</c> (<see cref="Occurrence.Value"/>, saturated, so
    /// out of range stays out of range): the test never narrows to <c>int</c>, because the narrowing is what used to
    /// leave the violation's detail naming 2147483647 for a position the program computed as 10^20 (kb/Work PB2938).</summary>
    private static bool RefModOutOfRange(long leftmost, long length, int size, bool omitted, bool allowZeroLength) =>
        leftmost < 1 || leftmost > size
        || (!omitted && length < 0) || (length == 0 && !allowZeroLength)
        || (length > 0 && length > size - leftmost + 1);

    /// <summary>⛔ THE ONE EC-BOUND-REF-MOD RAISE of the range-checked reference modification — the read
    /// (<see cref="RefMod"/>), the write (<see cref="SpliceInto"/>) and the address (<see cref="RefModStartOffset"/>) were
    /// three copies of this message. The detail names the positions the program holds (<see cref="Occurrence.ToString"/>:
    /// the exact value even past <c>long</c>, kb/Work PB2938), <paramref name="what"/> being the kind of area they fall
    /// outside ("item" or "receiver"). Returns normally only where checking is off AND the run unit survives — it does
    /// not: <see cref="ExceptionState.RefModViolation"/> never returns (kb/Work R60).</summary>
    private static void RefModViolation(Occurrence leftmost, Occurrence length, bool omitted, int size, string what) =>
        ExceptionState.RefModViolation(
            $"reference modification ({leftmost}:{(omitted ? "" : length.ToString())}) out of range for a "
            + $"{size}-position {what} (ISO §8.4.3.3.4 item 5c)");

    /// <summary>A NUMERIC data item as a reference-modifier leftmost-position or length, decoded through the
    /// item's own profile (ISO §8.4.3.3.4 rule 5)c); fix-queue PB41): "If the evaluation of leftmost-position or
    /// length results in a non-integer value, a zero value, or a value that references a position outside the
    /// area of identifier-1, the EC-BOUND-REF-MOD exception condition is set to exist." §8.4.3.3.3 SR4 makes both
    /// positions arithmetic expressions, so a scaled numeric item is legal there and its VALUE — not its unscaled
    /// storage — is the ordinal position; the scale is the profile's <see cref="NumProfile.FractionDigits"/>.
    /// <para>The exact twin of <c>CobolTable.Occ(…, in NumProfile)</c> for the subscript position, differing ONLY
    /// in which Table 13 condition it names; the shared de-scale/integrality arithmetic is
    /// <c>CobolNum.HasFraction</c>/<c>PositionOf</c>, and the character-image arm is the SAME §14.6.13.2 rule 2
    /// checked read (<see cref="CobolNum.ParseImageSending"/>): a position is item identification, which rule 1
    /// names as checked even inside a class condition (kb/Work PB1117 — the tolerant digit scan it replaced made
    /// EC-DATA-INCOMPATIBLE unreachable here and dropped a signed image's sign). Returns <c>long</c> because the
    /// rendered ref-mod positions are long-valued COBOL expressions, passed to <see cref="RefMod"/> as they are.
    /// A fractional position is the same violation as an out-of-range
    /// one and takes the same outcome (<see cref="ExceptionState.RefModViolation"/>: raised when checking is on, the
    /// run unit's termination when it is off — kb/Work R60).</para>
    /// <para>The result is an <see cref="Occurrence"/> — the saturated <c>long</c> the range test positions by, with the
    /// item's exact value riding along (kb/Work PB2938) — exactly as <c>CobolTable.Occ</c> returns one for a subscript.</para>
    /// <para>The carrier overloads mirror the subscript side's, and for the same reason: the item's storage form
    /// is decided after the bind-time expression text is produced.</para></summary>
    public static Occurrence RefModPosition(long unscaled, in NumProfile item) => RefModScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="RefModPosition(long, in NumProfile)"/>
    public static Occurrence RefModPosition(Int128 unscaled, in NumProfile item) => RefModScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="RefModPosition(long, in NumProfile)"/>
    public static Occurrence RefModPosition(string image, in NumProfile item) => item.ImageExceedsInt128
        ? RefModPosition(CobolNum.ParseImageU128Sending(image, item), item)
        : RefModScaled(CobolNum.ParseImageSending(image, item), item.FractionDigits);

    /// <inheritdoc cref="RefModPosition(long, in NumProfile)"/>
    public static Occurrence RefModPosition(ulong unscaled, in NumProfile item) => RefModScaled(unscaled, item.FractionDigits);

    /// <inheritdoc cref="RefModPosition(long, in NumProfile)"/>
    /// <remarks>Read as the UNSIGNED-WIDE value it is — never narrowed through <see cref="Int128"/> first, which a value
    /// at or above 2^127 does not fit — so §8.4.3.3.4 rule 5)c)'s integrality and the position both see the value itself
    /// (the twin of <c>CobolTable.Occ(UInt128, in NumProfile)</c>: a scale that divides it back inside <c>long</c>'s range
    /// makes it a valid position, and a fraction is a violation whatever the magnitude).</remarks>
    public static Occurrence RefModPosition(UInt128 unscaled, in NumProfile item)
    {
        if (CobolNum.HasFraction(unscaled, item.FractionDigits))
            NotAnInteger(CobolNum.PlainValue(unscaled, item.FractionDigits));
        return Occurrence.Of(unscaled, item.FractionDigits);
    }

    private static Occurrence RefModScaled(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            NotAnInteger(CobolNum.PlainValue(unscaled, scale));
        return Occurrence.Of(unscaled, scale);
    }

    private static void NotAnInteger(string shown) =>
        ExceptionState.RefModViolation(
            $"reference-modification position {shown} is not an integer (ISO §8.4.3.3.4 item 5c)");

    /// <summary>A reference-modifier leftmost-position or length that is an arithmetic EXPRESSION (or a
    /// floating-point item), as the position it denotes — the statement pre-operation's intake, the exact twin of
    /// <c>CobolTable.OccValue</c> differing only in the Table 13 condition it names (kb/Work PB1890). §8.4.3.3.4
    /// rule 5)c): a leftmost-position or length whose evaluation "results in a non-integer value" sets
    /// EC-BOUND-REF-MOD, so the test reads the EXACT intermediate on its own carrier.
    /// <para>The result is the EXACT integer the 38-digit position temporary then holds
    /// (<see cref="CobolNum.PositionOfWide(Int128, int)"/>), not a <c>long</c>: the position read
    /// (<see cref="RefModPosition(Int128, in NumProfile)"/>) narrows it, and only that narrowing saturates, so
    /// EC-BOUND-REF-MOD's detail names the value the expression computed (kb/Work PB2938; the twin of
    /// <c>CobolTable.OccValue</c>, kb/Work PB2695).</para></summary>
    public static Int128 RefModValue(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            NotAnInteger(CobolNum.PlainValue(unscaled, scale));
        return CobolNum.PositionOfWide(unscaled, scale);
    }

    /// <inheritdoc cref="RefModValue(Int128, int)"/>
    public static Int128 RefModValueDec(CobolDec value)
    {
        if (value.HasFraction) NotAnInteger(value.ToDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return CobolNum.PositionOfWide(value);
    }

    /// <inheritdoc cref="RefModValue(Int128, int)"/>
    public static Int128 RefModValueReal(double value)
    {
        if (CobolNum.HasFraction(value)) NotAnInteger(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return CobolNum.PositionOfWide(value);
    }

    /// <summary>
    /// Reference modification read (ISO §8.4.3.3): the substring of <paramref name="s"/> beginning at 1-based
    /// <paramref name="leftmost"/> for <paramref name="length"/> characters (<see cref="OmittedRefModLength"/> = the
    /// omitted "to the end" form). ⛔ A VIOLATION NEVER RETURNS A SLICE: an out-of-range leftmost/length or a
    /// zero-length result raises the fatal EC-BOUND-REF-MOD when checking is enabled (§14.6.13.1.1; §8.4.3.3.4,
    /// spec :7089) and TERMINATES the run unit when it is not — the owner's loud-abort rule for an outcome the
    /// standard names no answer for (kb/Work PB1707 part 2, R60), one rule for the read and the write
    /// (<see cref="SpliceInto"/>) through <see cref="RefModOutOfRange"/>. So past the range test the slice always
    /// lies wholly inside <paramref name="s"/>: there is nothing to clamp and nothing to pad.
    /// </summary>
    public static string RefMod(string? s, Occurrence leftmost, Occurrence length, bool allowZeroLength = false)
    {
        s ??= "";
        int size = s.Length;
        bool omitted = length.Value == OmittedRefModLength;
        // §8.4.3.3.4 (spec :7089), item 5c: leftmost shall be 1..size; a SPECIFIED length shall be a positive nonzero
        // integer (a negative specified length is a violation regardless of the directive — REF-MOD-ZERO-LENGTH,
        // §7.3.23, <paramref name="allowZeroLength"/>, relaxes ONLY the zero case, C14), with leftmost+length-1 <= size.
        // For the OMITTED (to-the-end) form only the leftmost is range-checked.
        if (RefModOutOfRange(leftmost, length, size, omitted, allowZeroLength))
            RefModViolation(leftmost, length, omitted, size, "item");
        int start = (int)(leftmost.Value - 1);
        int len = omitted ? size - start : (int)length.Value;   // in range: 0 <= len <= size - start
        return len == 0 ? "" : s.Substring(start, len);
    }

    /// <summary>The ZERO-BASED offset of a reference modifier's leftmost position, for an operand that needs the POSITION
    /// and not the slice — <c>ADDRESS OF identifier-1(leftmost:length)</c>, whose address is that of the unique data item's
    /// first position (ISO §8.4.3.11.4 GR1 over §8.4.3.3.4 GR5; kb/Work PB1407). ⛔ The range test is THE ONE
    /// <see cref="RefModOutOfRange"/> the read and the write spend, with the same outcome — the fatal EC-BOUND-REF-MOD
    /// when checking is enabled, the run unit's termination when it is not — so an address cannot be taken from a
    /// reference modifier that a read of the same reference would refuse. <paramref name="size"/> is identifier-1's
    /// positions, or <see cref="int.MaxValue"/> where the item's size is a run-time fact the bind cannot supply.</summary>
    public static int RefModStartOffset(Occurrence leftmost, Occurrence length, int size, bool allowZeroLength = false)
    {
        bool omitted = length.Value == OmittedRefModLength;
        if (RefModOutOfRange(leftmost, length, size, omitted, allowZeroLength))
            RefModViolation(leftmost, length, omitted, size, "item");
        return (int)(leftmost.Value - 1);
    }

    /// <summary>
    /// Reference modification write (ISO §8.4.3.3 / §14.9.24): return <paramref name="dst"/> with the
    /// <paramref name="length"/> characters at 1-based <paramref name="leftmost"/> replaced by
    /// <paramref name="slice"/> (left-justified, <paramref name="pad"/>-filled, truncated to the slice length).
    /// <paramref name="dst"/>'s overall length is preserved; only the targeted positions change (editing is not
    /// re-applied). A boolean receiver splices with boolean-zero fill (§14.6.8.6; §8.4.3.3 GR5a — a bit position
    /// IS a char index under D-B1). An out-of-range/zero-length ref-mod raises the fatal EC-BOUND-REF-MOD
    /// (§8.4.3.3.4) when checking is enabled and TERMINATES the run unit when it is not — the same
    /// <see cref="RefModOutOfRange"/> and the same outcome as the read (<see cref="RefMod"/>; kb/Work R60).
    /// <para><paramref name="repeat"/> makes <paramref name="slice"/> a FIGURATIVE SEED — one fill character, or
    /// the literal of <c>ALL literal-1</c> — repeated character by character over every position of the slice, the
    /// §8.3.3.6.4 GR2 sizing against the reference-modified item (§8.4.3.3.4 GR5 makes it a data item whose length
    /// is known only here, at run time — kb/Work PB1655). It is <see cref="FigToWidth"/>'s rule applied in place,
    /// so a figurative stored into a slice and one sized against a fixed receiver cannot answer differently.</para>
    /// </summary>
    public static string SpliceInto(string? dst, Occurrence leftmost, Occurrence length, string? slice, char pad = ' ',
        bool allowZeroLength = false, bool repeat = false)
    {
        dst ??= ""; slice ??= "";
        int size = dst.Length;
        bool omitted = length.Value == OmittedRefModLength;
        // §8.4.3.3.4 item 5c — a SPECIFIED length shall be positive nonzero; a zero-length receiving ref-mod is
        // allowed only under REF-MOD-ZERO-LENGTH (§7.3.23), a negative specified length never is (C14); an
        // out-of-range leftmost/length still raises regardless of the directive. The OMITTED form range-checks only
        // the leftmost.
        if (RefModOutOfRange(leftmost, length, size, omitted, allowZeroLength))
            RefModViolation(leftmost, length, omitted, size, "receiver");
        int start = (int)(leftmost.Value - 1);
        int len = omitted ? size - start : (int)length.Value;   // in range (RefModViolation never returns): 0 <= len <= size - start
        if (len == 0) return dst;
        var arr = dst.ToCharArray();
        if (repeat && slice.Length > 0)
            for (int i = 0; i < len; i++) arr[start + i] = slice[i % slice.Length];
        else
            for (int i = 0; i < len; i++) arr[start + i] = i < slice.Length ? slice[i] : pad;
        return new string(arr);
    }

    /// <summary>⛔ A WINDOW THE COMPILER CHOSE, NOT A REFERENCE MODIFICATION THE PROGRAM WROTE (kb/Work PB1707,
    /// R60). <see cref="RefMod"/> and <see cref="SpliceInto"/> are ISO §8.4.3.3's reference modification: a range
    /// violation there is EC-BOUND-REF-MOD and, with checking off, the end of the run unit. The runtime also slices
    /// character images at positions the COMPILER computes — a REDEFINES view over its class backing, an
    /// occurs-depending group's current extent, the prefix an INVOKE/CALL boundary splices back, the current record
    /// of a READ … INTO — and there a position past the end of the image is not a program error but the image being
    /// SHORTER than the extent asked for (a record area holds only what was read into it). This reads
    /// <paramref name="length"/> characters from 1-based <paramref name="leftmost"/>
    /// (<see cref="OmittedRefModLength"/> = to the end): it clamps to the image and space-extends the result to the
    /// requested length, raises nothing, and is never the lowering of source text. Two names for two jobs: a
    /// reference modification that cannot silently pad, and an image window that must.</summary>
    public static string Window(string? s, int leftmost, int length)
    {
        s ??= "";
        int start = Math.Max(leftmost - 1, 0);
        int avail = Math.Max(0, s.Length - start);
        int len = length == OmittedRefModLength || length < 0 ? avail : length;
        if (len <= 0) return "";
        string slice = start < s.Length ? s.Substring(start, Math.Min(len, avail)) : "";
        return slice.Length < len ? slice.PadRight(len) : slice;
    }

    /// <summary>The receiving twin of <see cref="Window"/>: <paramref name="dst"/> with the window's positions
    /// replaced by <paramref name="slice"/> (left-justified, <paramref name="pad"/>-filled, truncated to the window),
    /// its overall length preserved; a window beyond the image changes nothing. Raises nothing — see
    /// <see cref="Window"/> for why this is not <see cref="SpliceInto"/>.</summary>
    public static string WindowInto(string? dst, int leftmost, int length, string? slice, char pad = ' ')
    {
        dst ??= ""; slice ??= "";
        int start = leftmost - 1;
        if (start < 0 || start >= dst.Length) return dst;
        int len = length == OmittedRefModLength || length < 0 ? dst.Length - start : Math.Min(length, dst.Length - start);
        if (len <= 0) return dst;
        var arr = dst.ToCharArray();
        for (int i = 0; i < len; i++) arr[start + i] = i < slice.Length ? slice[i] : pad;
        return new string(arr);
    }

    /// <summary>
    /// Compare two character values under COBOL rules (ISO §8.8.4.2): the shorter operand is treated as if
    /// extended on the right with <paramref name="pad"/> — space for alphanumeric (§8.8.4.2.7) and national
    /// (§8.8.4.2.9/.10, ordinal = the D-N3 default national collating sequence), boolean zero '0' for boolean
    /// operands (§8.8.4.2.8 — value comparison, usage-independent under D-B1). Returns &lt;0, 0, or &gt;0 (ordinal).
    /// </summary>
    public static int Compare(string? left, string? right, char pad = ' ')
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            char a = i < left.Length ? left[i] : pad;
            char b = i < right.Length ? right[i] : pad;
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    /// <summary>
    /// Compare two character values under a NON-native collating sequence — the ONE <see cref="CobolCollation"/>
    /// carrier (kb/Work PB101): an <c>ALPHABET</c> literal-phrase table (alphanumeric or national) or a LOCALE-based
    /// sequence, each applying its own operand rule (the table arms space-extend, the locale arm truncates trailing
    /// spaces — §8.8.4.2.7/.9/.11). The compiler emits <c>__COLLATE</c> / <c>__COLLATE_NAT</c> here; the native
    /// sequence is the two-argument overload above. Returns &lt;0, 0, or &gt;0.
    /// </summary>
    public static int Compare(string? left, string? right, CobolCollation collation) => collation.Compare(left, right);

    /// <summary>
    /// A relation condition in which EXACTLY ONE operand is a figurative constant or <c>ALL literal-1</c>
    /// (kb/Work PB297). ISO §8.3.3.6.4 GR2 with its NOTE 1 — "A figurative constant is associated with a data
    /// item or literal when, for example, the figurative constant is moved to it, COMPARED WITH IT, or paired
    /// with it in a binary operation" — sizes the figurative to "the number of character positions in the
    /// ASSOCIATED data item", and §8.8.4.2.7 then compares the two.
    /// <para>⛔ THE ASSOCIATED OPERAND'S CHARACTER-POSITION COUNT IS ITS OWN RUNTIME LENGTH, WHICH IS WHY THIS
    /// SIZING IS A RUNTIME ONE. §8.4.3.3.4 GR5 makes a reference-modified operand "a unique data item" whose
    /// positions are the ref-mod's, not the base item's — and with a computed leftmost-position/length that
    /// count does not exist at compile time at all. The compiler previously tabulated the width per operand
    /// KIND, which silently answered every <c>X(1:1) = LOW-VALUE</c> over a wider X with FALSE (the pad in
    /// §8.8.4.2.7 rule 2 is a SPACE, so only SPACE-valued tests came out right).</para>
    /// <para><paramref name="figIsLeft"/> says which side carries the figurative SEED (one fill character, or
    /// literal-1); the other side is the associated operand, rendered once by the caller. Both figurative and
    /// both non-figurative relations use plain <see cref="Compare(string?,string?,char)"/> instead — with two
    /// figuratives there is no associated data item and §8.3.3.6.4 GR3 b/c gives each its OWN length.</para>
    /// </summary>
    public static int CompareFig(string? left, string? right, bool figIsLeft, char pad = ' ') =>
        figIsLeft ? Compare(FigToWidth(left ?? "", (right ?? "").Length), right, pad)
                  : Compare(left, FigToWidth(right ?? "", (left ?? "").Length), pad);

    /// <inheritdoc cref="CompareFig(string?,string?,bool,char)"/>
    /// <remarks>The NON-native collating-sequence overload (an <c>ALPHABET</c> literal phrase or a LOCALE-based
    /// sequence — the ONE <see cref="CobolCollation"/> carrier): GR2 materializes the figurative FIRST, exactly
    /// as the native overload does, and the sequence's own operand rule then applies to the sized pair.</remarks>
    public static int CompareFig(string? left, string? right, bool figIsLeft, CobolCollation collation) =>
        figIsLeft ? collation.Compare(FigToWidth(left ?? "", (right ?? "").Length), right)
                  : collation.Compare(left, FigToWidth(right ?? "", (left ?? "").Length));

    /// <summary>The ONE detail text for the §14.7.8 rule 2 raise — both THROUGH carriers (native here,
    /// non-native on <see cref="CobolCollation"/>) describe the same condition, so they do not describe it
    /// twice.</summary>
    internal const string InvertedRange =
        "the THROUGH range's starting value collates after its ending value — the range is empty (ISO §14.7.8 rule 2)";

    /// <summary>Membership of <paramref name="read"/> in the alphanumeric/national THROUGH range
    /// [<paramref name="lo"/>, <paramref name="hi"/>] under the effective collating sequence (ISO §14.7.8; a level-88
    /// VALUE THRU or an EVALUATE WHEN range). When <paramref name="lo"/> collates AFTER <paramref name="hi"/> (rule 2)
    /// the nonfatal EC-RANGE-INVALID is set and the range is treated as EMPTY (returns false); otherwise the inclusive
    /// bound test. The "empty range" behaviour was already emergent from the inclusive test — this adds only the EC.</summary>
    public static bool ThruMember(string? read, string? lo, string? hi, char pad = ' ')
    {
        if (Compare(lo, hi, pad) > 0) { ExceptionState.RangeInvalidError(InvertedRange); return false; }
        return Compare(read, lo, pad) >= 0 && Compare(read, hi, pad) <= 0;
    }

    /// <inheritdoc cref="ThruMember(string?,string?,string?,char)"/>
    public static bool ThruMember(string? read, string? lo, string? hi, CobolCollation collation) =>
        collation.ThruMember(read, lo, hi);

    /// <summary>The THROUGH membership of a range ONE OR BOTH OF WHOSE ENDS IS A FIGURATIVE SEED — the GR2-aware
    /// twin of <see cref="ThruMember(string?,string?,string?,char)"/>, standing to it exactly as
    /// <see cref="CompareFig(string?,string?,bool,char)"/> stands to <see cref="Compare(string?,string?,char)"/>.
    /// <para>ISO §8.3.3.6.4 GR2 sizes a figurative to "the associated data item", and in a range test that item is
    /// the one being tested for membership — <paramref name="read"/> — for BOTH ends, because both ends are
    /// compared against it (§14.9.13.4 GR4 a) 5.: "selection-subject &gt;= left-part AND selection-subject &lt;=
    /// right-part"). The sizing runs BEFORE the §14.7.8 rule 2 inversion test, so <c>LOW-VALUE THRU HIGH-VALUE</c>
    /// is weighed at the tested item's own width rather than one character against one character.</para>
    /// <para>⛔ IT LIVES HERE, NOT IN THE EMITTER (kb/Work PB401). The generated form would otherwise have to
    /// spell <c>FigToWidth(seed, (read).Length)</c> and evaluate <paramref name="read"/> a second time, and GR2's
    /// sizing rule would have a second author. Before it existed, an EVALUATE range with a figurative end took the
    /// carrier UNSIZED the moment EC checking or an <c>IN alphabet-name</c> phrase routed it there, and answered
    /// the opposite of the identical test written as a relation pair — measured on PIC X(2) LOW-VALUES against
    /// <c>LOW-VALUE THRU "AA"</c>: the relation said inside, the carrier said outside.</para>
    /// <para>⛔ A SEPARATE NAME, NOT AN OVERLOAD OF <see cref="ThruMember(string?,string?,string?,char)"/>, for the
    /// reason <see cref="CompareFig(string?,string?,bool,char)"/> is a separate name beside
    /// <see cref="Compare(string?,string?,char)"/>: the collating surface's overload set is COLLAPSED to exactly
    /// two entries per comparison name — a <c>char</c> pad (native) or the ONE <see cref="CobolCollation"/> — and
    /// <c>CobolCollationTests.Drift_TheCarrierIsTheOnlyCollatingParameterType</c> measures that. A figurative-aware
    /// twin is a different QUESTION, so it takes a different name and keeps its own two channels.</para></summary>
    /// <param name="loFig">Whether <paramref name="lo"/> is a figurative SEED (one fill character, or literal-1 of
    /// an <c>ALL literal-1</c>) rather than a value of its own.</param>
    /// <param name="hiFig">The same question for <paramref name="hi"/>.</param>
    public static bool ThruMemberFig(string? read, string? lo, string? hi, bool loFig, bool hiFig, char pad = ' ') =>
        ThruMember(read, SizedFig(lo, loFig, read), SizedFig(hi, hiFig, read), pad);

    /// <inheritdoc cref="ThruMemberFig(string?,string?,string?,bool,bool,char)"/>
    /// <remarks>The NON-native collating-sequence overload (an <c>ALPHABET</c> literal phrase or a LOCALE-based
    /// sequence): GR2 materializes the figurative FIRST, exactly as <see cref="CompareFig(string?,string?,bool,CobolCollation)"/>
    /// does, and §14.7.8 rule 2's sequence then orders the sized operands.</remarks>
    public static bool ThruMemberFig(string? read, string? lo, string? hi, bool loFig, bool hiFig,
        CobolCollation collation) =>
        collation.ThruMember(read, SizedFig(lo, loFig, read), SizedFig(hi, hiFig, read));

    /// <summary>ISO §8.3.3.6.4 GR2's materialization, applied only where the operand IS a figurative seed — the
    /// ONE place the range carrier's two overloads ask it.</summary>
    private static string? SizedFig(string? seed, bool isFig, string? associated) =>
        isFig ? FigToWidth(seed ?? "", (associated ?? "").Length) : seed;
}
