// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// Family F3 — character intrinsics (ISO §15). CHAR and ORD are PCS-relative (the program collating sequence,
/// §15.15 / §15.70): the parameterless overloads realize the NATIVE sequence (ordinal char codes — the greenfield
/// <c>CollatingModel</c> normalizes STANDARD-1/STANDARD-2/NATIVE to this identity); the
/// <see cref="AlphanumericCollation"/> overloads take the program's emitted <c>__COLLATE</c> object (PB59 —
/// positions + the §15.15.4 r2 representative array + NextFree; comparison paths read its raw
/// <c>.Positions</c>), passed ONLY when the binder flagged a non-identity PCS (hazard H5 — the field does not
/// exist otherwise).
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>The returned length of <c>CHAR</c> and <c>CHAR-NATIONAL</c> — ONE character position, fixed by
    /// the FUNCTION (§15.15.4 / §15.16.4 each return "the character in ordinal position argument-1"), never by
    /// the argument. That is what puts a rejected CHAR / CHAR-NATIONAL in docs/CONFORMANCE.md row
    /// <c>DOC-A.1-90</c>'s general "spaces" clause rather than in its zero-length class, and it is the length
    /// the four §15.15.3 / §15.16.3 guards hand to <see cref="Exceptions.ExceptionEngine.ArgumentErrorSpaces"/>
    /// (kb/Work PB470 — before it those guards spelled <c>" "</c> themselves).</summary>
    private const int CharPositions = 1;

    /// <summary>CHAR (§15.15.4): the character in ORDINAL position <paramref name="n"/> (1-based) of the
    /// alphanumeric program collating sequence — native sequence: position n is char code n−1 (IF105A asserts
    /// CHAR(37) = '$', ASCII 36). Out-of-range ordinal → EC-ARGUMENT default one-space result (§15.3).</summary>
    public static string Char(long n)
    {
        long c = n - 1;
        if (c is < 0 or > 0xFFFF)
        {
            // EC-ARGUMENT-FUNCTION raise point (the string-result twin of the long sites): the raise when
            // checking is on; otherwise docs/CONFORMANCE.md row DOC-A.1-90's GENERAL clause, ONE SPACE — CHAR's
            // returned length is fixed by the FUNCTION at one character position (§15.15.4), not by the rejected
            // argument, so the row's zero-length class does not reach here (kb/Work PB383, PB470).
            return Exceptions.ExceptionState.ArgumentErrorSpaces(
                $"CHAR argument {n} outside the collating sequence (§15.15.3 rule 1)", CharPositions);
        }
        return ((char)c).ToString();
    }

    /// <summary>CHAR under a non-identity PCS (§15.15.4): the character at ordinal position <paramref name="n"/>
    /// of the sequence — a shared (ALSO) position returns "the first character DEFINED for that character
    /// position" (rule 2 / §12.3.7.4 GR7 1.6: literal-1, in SOURCE order), an above-block position returns its
    /// native code unit (GR7 1.3), and the §15.15.3 r2 domain is the sequence's own
    /// <see cref="AlphanumericCollation.PositionCount"/> ("greater than zero and less than or equal to the
    /// number of positions").</summary>
    /// <remarks>⛔ THE ONE COLLATION READER (fix-queue PB59 / RV-15.15.4-1/-2, AR-15.15.3-2). The previous body
    /// carried the pre-PB3 native-ordinal fallback ORD lost (CHAR(255) under an ALSO collapse returned the EC
    /// default, CHAR(257) two positions low — CHAR and ORD were not inverses), scanned <c>weights[]</c> for the
    /// LOWEST-coded member of a shared position (CHAR(1) = 'A' where literal-1 'C' is owed), and bounded the
    /// domain by the 256-entry block (255 refused, 65,536 admitted — inverted on both ends).
    /// <see cref="AlphanumericCollation.CharAt"/> and <see cref="AlphanumericCollation.Weight"/> are exact
    /// inverses by construction, which is what makes ORD∘CHAR the identity the golden asserts.</remarks>
    public static string Char(long n, CobolCollation collation)
    {
        int c = collation.CharAt(n - 1);
        if (c < 0)
        {
            return Exceptions.ExceptionState.ArgumentErrorSpaces(     // row DOC-A.1-90's general clause — §15.15.4 fixes the length at 1
                $"CHAR argument {n} outside the collating sequence of "
                + $"{collation.PositionCount} positions (§15.15.3 rule 2)", CharPositions);
        }
        return ((char)c).ToString();
    }

    /// <summary>CHAR-NATIONAL (§15.16.4): the character in ORDINAL position <paramref name="n"/> (1-based) of
    /// the NATIVE national collating sequence — UTF-16 code-unit order (one char per national position, D-N1;
    /// position n is code unit n−1). This parameterless body serves every IDENTITY national program collating
    /// sequence (none declared, NATIVE, UCS-4 — the §8.5.1.4 codepoint≡code-unit equivalence); a non-native
    /// <c>ALPHABET … FOR NATIONAL</c> literal phrase routes to the <see cref="NationalCollation"/> overload.
    /// An out-of-range ordinal violates §15.16.3 rule 2 — EC-ARGUMENT-FUNCTION, with the §15.3 default
    /// one-space result when checking is off. The result is CLASS NATIONAL (§15.16.1) — the catalog row's
    /// National category carries that.</summary>
    public static string CharNational(long n)
    {
        long c = n - 1;
        if (c is < 0 or > 0xFFFF)
        {
            return Exceptions.ExceptionState.ArgumentErrorSpaces(     // row DOC-A.1-90's general clause — §15.16.4 fixes the length at 1
                $"CHAR-NATIONAL argument {n} outside the national collating sequence (§15.16.3 rule 2)", CharPositions);
        }
        return ((char)c).ToString();
    }

    /// <summary>CHAR-NATIONAL under a NON-native national program collating sequence (§15.16.4; the emitted
    /// <c>__COLLATE_NAT</c> table): the character at position n−1 — a shared (ALSO) position returns the FIRST
    /// character defined for it (rule 2, deterministic per implementor item 22).</summary>
    public static string CharNational(long n, CobolCollation national)
    {
        int c = national.CharAt(n - 1);
        if (c < 0)
        {
            return Exceptions.ExceptionState.ArgumentErrorSpaces(     // row DOC-A.1-90's general clause — §15.16.4 fixes the length at 1
                $"CHAR-NATIONAL argument {n} outside the national collating sequence (§15.16.3 rule 2)", CharPositions);
        }
        return ((char)c).ToString();
    }

    /// <summary>ORD (§15.70.4): the 1-based ordinal position of the argument's (single) character in the
    /// alphanumeric program collating sequence (r1) — native: char code + 1. The inverse of CHAR. A NATIONAL
    /// argument (§15.70.3 admits category national) reads the NATIONAL program collating sequence instead
    /// (r2) — this same parameterless body IS the r2 value for every IDENTITY national sequence (none declared,
    /// NATIVE, UCS-4 — D-N3 code-unit order); a non-native <c>ALPHABET … FOR NATIONAL</c> sequence routes to
    /// the <see cref="NationalCollation"/> overload below, and the binder never routes a national argument to
    /// the alphanumeric-weights overload (its 256-entry domain would alias national characters).</summary>
    public static long Ord(string s) => s.Length == 0
        ? Exceptions.ExceptionState.ArgumentError("ORD argument is empty (§15.70.3 — a one-character argument is required)")
        : s[0] + 1;

    /// <summary>ORD under a NON-identity collating sequence — the ONE <see cref="CobolCollation"/> carrier
    /// (§15.70.4 r1 for the alphanumeric program collating sequence, r2 for the national one; a LOCALE sequence
    /// answers through its materialized positions, DESIGN-locale-facility L7): the character's collating position
    /// + 1. ALSO members share one position; a character past a table's positioned block continues the sequence per
    /// §12.3.7.4 GR7 1.3 (the PB3 arithmetic, living in the ONE <see cref="AlphanumericCollation.Weight"/> reader
    /// beside its exact inverse <see cref="AlphanumericCollation.CharAt"/> — fix-queue PB59).</summary>
    public static long Ord(string s, CobolCollation collation) => s.Length == 0
        ? Exceptions.ExceptionState.ArgumentError("ORD argument is empty (§15.70.3 — a one-character argument is required)")
        : collation.Weight(s[0]) + 1L;

    /// <summary>UPPER-CASE (§15.97.4): every lowercase letter replaced by its uppercase correspondent; result
    /// length = argument length (the fixed-width field image in carries the width out).</summary>
    public static string UpperCase(string s) => s.ToUpperInvariant();

    /// <summary>LOWER-CASE (§15.57.4): every uppercase letter replaced by its lowercase correspondent.</summary>
    public static string LowerCase(string s) => s.ToLowerInvariant();

    /// <summary>REVERSE (§15.78.4): the argument's characters in reverse order; same length.</summary>
    public static string Reverse(string s)
    {
        char[] a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    /// <summary>LENGTH (§15.50.4) — the RUNTIME residue of the bind-time fold (deep-dive D7): the length in
    /// character positions of a value only the backend rendered (a nested string-function result, whose padded
    /// fixed-width image length IS its character-position count). Fixed items and literals fold at bind time and
    /// never reach here.</summary>
    public static long Length(string s) => s.Length;

    /// <summary>FUNCTION BYTE-LENGTH's runtime body (§15.14.4 — kb/Work PB61): the length of the argument's
    /// STORAGE image, which the emitter renders through the ONE byte channel (<c>OperandText.AsStorageImage</c> —
    /// char==byte; a national view is its UTF-16BE bytes) for the shapes the compile-time fold cannot size: a
    /// reference-modified view, an ANY LENGTH or DYNAMIC LENGTH item, an OCCURS DEPENDING group's current extent.
    /// FUNCTION LENGTH's §15.50.4 r6 (a dynamic-length item's current length in BYTES) rides the same body.</summary>
    public static long ByteLength(string storage) => storage.Length;

    /// <summary>BYTE-LENGTH (§15.14.4 r1) of a NATIONAL function result: the result's string image holds one UTF-16
    /// code unit per national position, and a national position is 2 bytes (D-N1) — kb/Work PB1398.</summary>
    public static long ByteLengthNational(string image) => 2L * image.Length;

    /// <summary>BYTE-LENGTH (§15.14.4 r1, r4) of a BOOLEAN function result: the result's string image holds one
    /// '0'/'1' character per boolean position, and a boolean function's implicit usage is BIT (§15.2 item 2), 8
    /// positions to the byte — "rounded to the next larger integer value" when they do not fill one (kb/Work PB1398).</summary>
    public static long ByteLengthBoolean(string image) => (image.Length + 7L) / 8;

    /// <summary>CONCAT (§15.18.4, 2023): the characters of all arguments in order — argument-1 followed by each
    /// argument-2 (rules 1 &amp; 4). Each argument arrives as its fixed-width display IMAGE (trailing padding
    /// included — §15.18.4 rule 1 "all of the characters"), so the result length is the sum of the argument
    /// widths.</summary>
    public static string Concat(params string[] parts) => string.Concat(parts);

    /// <summary>BASECONVERT (§15.12.4, 2023): the unsigned integer whose digits are <paramref name="value"/> in
    /// base <paramref name="fromBase"/>, re-expressed as a string of 0-9 / A-F digits in base
    /// <paramref name="toBase"/> (both bases 2..16 — §15.12.3). An out-of-range base, a digit invalid for the
    /// source base, or a DIGITLESS argument sets EC-ARGUMENT-FUNCTION and returns the §15.3 default (a
    /// zero-length result when checking is off).</summary>
    /// <remarks><para>⛔ ONLY the TRAILING fixed-width image pad is trimmed, and only the SPACE character
    /// (fix-queue PB59 / AR-15.12.3-2, AR-15.19.3-4's twin): a leading space in a left-justified alphanumeric
    /// item is CONTENT and reaches the digit screen — the old symmetric <c>Trim()</c> silently blessed
    /// <c>"  FF"</c> and turned an ALL-SPACES argument into a fabricated <c>"0"</c> (zero digits consumed, the
    /// accumulator's 0 indistinguishable from the digit 0 — the guard now counts DIGITS, not the value).</para>
    /// <para>⛔ LOWERCASE <c>a</c>–<c>f</c> ARE NOT DIGITS (derived 2026-08-09): §15.12.3 r2 sanctions "the
    /// basic-letters A to F", and §8.1.3.1 Table 1 defines the basic letters as the Latin CAPITAL letters;
    /// §8.1.3.2 GR3a's case-insensitivity governs the compilation group's TEXT, not runtime data. The previous
    /// lowercase arm was an unadjudicated leniency with no vendor precedent (the GPL corpus has no BASECONVERT
    /// case) — rejected loudly, never silently widened.</para>
    /// <para>The returned-value length is capped by the documented §15.4 maximum (8,191 character positions —
    /// CONFORMANCE.md item 93, the §8.8.3.2 SR2 concatenation precedent): past it, EC-ARGUMENT-FUNCTION, which
    /// also bounds §15.12.3 r3's input side (an input within its own 8,191-position item bound whose value
    /// needs more output digits than the cap takes the same raise).</para></remarks>
    public static string BaseConvert(string value, long fromBase, long toBase)
    {
        if (fromBase is < 2 or > 16 || toBase is < 2 or > 16)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                $"BASECONVERT base(s) {fromBase}/{toBase} out of the range 2..16 (§15.12.3 rule 1)");
        // §15.12.3 r1 — "with UNEQUAL values": the runtime twin of the compile-time equal-literals screen
        // (PB59 / AR-15.12.3-1 leg (b)); reachable only through data-item bases the binder cannot read.
        if (fromBase == toBase)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                $"BASECONVERT argument-2 and argument-3 shall have unequal values — both are {fromBase} (§15.12.3 rule 1)");
        System.Numerics.BigInteger acc = 0;
        int digitsSeen = 0;
        foreach (char ch in value.TrimEnd(' '))
        {
            int d = ch is >= '0' and <= '9' ? ch - '0'
                  : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (d < 0 || d >= fromBase)
                return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                    $"BASECONVERT: '{ch}' is not a base-{fromBase} digit (§15.12.3 rule 2 — the basic-letters A to F, §8.1.3.1 Table 1)");
            acc = acc * fromBase + d;
            digitsSeen++;
        }
        if (digitsSeen == 0)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                "BASECONVERT argument-1 contains no digits (§15.12.3 rule 2; §15.3)");
        if (acc == 0) return "0";
        const string digits = "0123456789ABCDEF";
        var sb = new System.Text.StringBuilder();
        for (; acc > 0; acc /= toBase) sb.Insert(0, digits[(int)(acc % toBase)]);
        if (sb.Length > 8191)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                $"BASECONVERT returned value of {sb.Length} digits exceeds the documented 8,191-position maximum (§15.4; §15.12.3 rule 3; CONFORMANCE.md row DOC-A.1-93)");
        return sb.ToString();
    }

    // ── CONVERT (§15.19, 2023) — data-representation conversion ─────────────────────────────────────────────────
    // Source codes: 0 = ANY, 1 = ANUM, 2 = HEX, 3 = NAT.  Dest codes: 1 = ANUM, 3 = NAT, 4 = BYTE.
    // Char-set model, TWO distinct implementor determinations (fix-queue PB59):
    //   (1) The CHARACTER correspondence (Annex A.1 item 33, CONFORMANCE.md §7): both repertoires are UTF-16, one
    //       code unit per character position (item 188's substrate), so the alphanumeric↔national correspondence
    //       is the TOTAL IDENTITY in both directions — see <see cref="Repertoire"/>. No substitution, no
    //       EC-DATA-CONVERSION, on any character↔character pathway.
    //   (2) The BYTE serialization (ANUM/ANY → BYTE/HEX) is a DISTINCT, implementor-defined 8-bit Latin-1 mapping
    //       (§8.1.2 NOTE 2 leaves the usage representation implementor-defined; CONFORMANCE.md item 209's one
    //       byte per DISPLAY character position): one byte per code unit, '?' + EC-DATA-CONVERSION for a code
    //       unit > 0xFF — the r1/r3-ANALOGOUS behavior extended to the serialization legs, a DOCUMENTED
    //       determination (item 209; RV-15.19.4-2): §15.19.4 r2's silence about an unrepresentable unit exists
    //       because the spec's model assumes a total serialization, and ours is partial above 0xFF.
    //       National is UTF-16BE, one char/position (D-N1; CobolBits.NatBytes is the ONE serializer).
    //       ⛔ Source ANY = the item's RAW STORAGE byte image, emitted by the compiler's storage channel
    //       (OperandText.AsStorageImage — §15.19.3 r7 "it is not necessary for the contents to be valid
    //       according to the usage"): char==byte, a sub-byte USAGE BIT source arrives PRE-PACKED high-order-
    //       first (CobolBits.Pack), never a display image (PB59 family 5b). Always paired with HEX (SR8).

    /// <summary>CONVERT (§15.19.4): re-express <paramref name="arg"/> (in source format <paramref name="src"/>) in
    /// the destination format (<paramref name="dst"/> + <paramref name="dstHex"/>). The character↔character
    /// pathways are the item-33 identity (no substitution, no EC); only the 8-bit BYTE serialization can
    /// substitute (see the char-set model above). An ANY source receives the raw storage byte image.</summary>
    public static string Convert(string arg, int src, int dst, bool dstHex)
    {
        const int ANUM = 1, HEX = 2, NAT = 3, BYTE = 4;

        // §15.19.3 r1: argument-1 shall not be of zero length — the RUNTIME screen (fix-queue PB59 /
        // AR-15.19.3-1). The bind-time COBOLNET1514 catches only the zero-length LITERAL (ISO 8.5.4's shape 8);
        // the other runtime zero-length shapes (a DYNAMIC LENGTH item at length 0, a zero-occurrence ODO group,
        // …) are only visible here. EC-ARGUMENT-FUNCTION + the §15.3 default, the Substitute shape.
        if (arg.Length == 0)
        {
            // Row DOC-A.1-90's ZERO-LENGTH class: §15.19.4 builds the returned value out of argument-1, so the
            // returned length derives from the very argument being rejected and nothing survives to size it.
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                "FUNCTION CONVERT argument-1 is of zero length (§15.19.3 rule 1)");
        }

        // Character translation (§15.19.4 r1/r3): both sides character, no HEX — the ONE item-33 correspondence
        // DISPLAY-OF/NATIONAL-OF share. Under the total identity the r1/r3 substitution sentences are vacuous
        // (NOTE 1/2 name those functions as "the same facility", and they agree).
        if (!dstHex && dst != BYTE && (src == ANUM || src == NAT))
            return Repertoire(arg);

        // Bit/byte pathway — reduce argument-1 to a byte string per the source format.
        byte[] bytes = src switch
        {
            // national → UTF-16BE through the ONE serializer (2 bytes/position, the r4 basis); every serialized
            // char is ≤ 0xFF by construction, so the shared reduction below can never substitute here.
            NAT => RawBytes(CobolBits.NatBytes(arg)),
            HEX => HexDigitsToBytes(arg),                            // a string of hex digits representing complete bytes (SR4)
            // ANUM = the characters under the 1-byte serialization; ANY = the RAW STORAGE byte image the
            // compiler emitted (char==byte — a wide char can only arrive through an alphanumeric CARRIER whose
            // storage IS its chars, where the documented item-209 substitution applies as for ANUM).
            _ => RawBytes(arg),
        };

        // Render the byte string in the destination format.
        if (dstHex)                                                  // r2 (ANUM HEX) / r4 (NAT HEX) — same digit code points (D-N4)
        {
            // §15.19.4 r2/r4 — trailing zero-BIT padding to a whole destination character. r2 (8 bits per
            // alphanumeric character): every reduction above is byte-aligned, and a sub-byte ANY source (USAGE
            // BIT) arrives PRE-PACKED with its trailing partial byte zero-filled high-order-first
            // (CobolBits.Pack), so the multiple-of-8 pad is already materialized — B"101" → 0xA0 → "A0".
            // r4 (16 bits per national character, D-N1): an ODD byte count takes one trailing zero byte —
            // CONVERT("A" ANUM NAT HEX) = "4100", B"101" ANY NAT HEX = "A000" (RV-15.19.4-4; NOTE 3 c's "E0"
            // is not derivable from B"101" under either rule — the NOTE is defective, the rule's arithmetic
            // decides).
            if (dst == NAT && (bytes.Length & 1) != 0)
                System.Array.Resize(ref bytes, bytes.Length + 1);
            return ToHex(bytes);
        }
        if (dst == NAT)                                              // HEX → NAT (r3): 2 bytes → one national char, pad a trailing odd byte
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
                sb.Append((char)((bytes[i] << 8) | (i + 1 < bytes.Length ? bytes[i + 1] : 0)));
            return sb.ToString();
        }
        // ANUM (r1) / BYTE (r5): one alphanumeric char per byte.
        var chars = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
        return new string(chars);
    }

    /// <summary>DISPLAY-OF (§15.26.4): each national character of argument-1 converted to its corresponding
    /// alphanumeric character (rule 1 — the item-33 TOTAL IDENTITY, see <see cref="Repertoire"/>). Under a total
    /// correspondence no national character lacks a correspondent, so the argument-2 substitution (rule 2) and
    /// the '?'+EC-DATA-CONVERSION arm (rule 3) are vacuous BY DECLARATION — <paramref name="sub"/> is accepted
    /// (bind screens still enforce §15.26.3 r2's class and one-position length) and never consumed. Result
    /// length = the argument's character count (rule 4 — one display position per national position).</summary>
    public static string DisplayOf(string arg, string? sub = null) => Repertoire(arg);

    /// <summary>NATIONAL-OF (§15.66.4): each alphanumeric character of argument-1 converted to its corresponding
    /// national character (rule 1 — the item-33 TOTAL IDENTITY, see <see cref="Repertoire"/>). Every character
    /// has a correspondent, so the argument-2 substitution character (rules 2/3) is never consumed — accepted
    /// for §15.66.2 conformance and §15.66.3 r2 screening, semantically inert BY DECLARATION.</summary>
    public static string NationalOf(string arg, string? sub = null) => Repertoire(arg);

    /// <summary>⛔ THE ONE alphanumeric↔national correspondence (Annex A.1 item 33 — CONFORMANCE.md §7 row 33;
    /// CONVERT §15.19.4 r1/r3, DISPLAY-OF §15.26.4 r1, NATIONAL-OF §15.66.4 r1, MOVE §14.9.25.4 GR6,
    /// §8.8.4.2.11 — one mechanism, never a second converter). Both repertoires are UTF-16, one code unit per
    /// character position (item 188's substrate), so the correspondence is the TOTAL IDENTITY in BOTH
    /// directions: every character corresponds to the same code unit, no character lacks a correspondent, the
    /// r2/r3 substitution machinery of §15.26.4/§15.66.4 is vacuous, and EC-DATA-CONVERSION is unreachable from
    /// any character pathway — a conforming determination (r1 grants the correspondence to the implementor),
    /// not a dead branch. The previous body cut NAT→ANUM at 0xFF ('?'+EC above it), contradicting item 188 at
    /// exactly one expression (fix-queue PB59; the triage measured DISPLAY-OF(N"Š") → '?', ORD 64).</summary>
    private static string Repertoire(string arg) => arg;

    /// <summary>The 8-bit BYTE-serialization substitution (the §8.1.2 NOTE 2 usage-representation determination —
    /// see the CONVERT char-set model above): a UTF-16 code unit above 0xFF has no one-byte image, so the
    /// ANUM/ANY→byte reduction substitutes '?' and sets EC-DATA-CONVERSION. ⚠ NOT the item-33 character
    /// correspondence (that is total, and raises nothing). This is the DOCUMENTED item-209 disposition
    /// (RV-15.19.4-2, decided 2026-08-09): §15.19.4 r2 carries no substitution sentence because the spec's model
    /// assumes the serialization is total; where ours is partial, the r1/r3-analogous substitution+EC is the
    /// smallest extension of the standard's own machinery — visible under checking, never silent.</summary>
    private static byte ByteSub()
    {
        Exceptions.ExceptionState.DataConversionError(
            "the code unit has no one-byte image in the 8-bit usage-DISPLAY serialization (§8.1.2; CONFORMANCE.md item 209)");
        return (byte)'?';
    }

    /// <summary>The shared char==byte reduction of the bit/byte pathway: one byte per code unit by the storage-byte
    /// law (<see cref="StorageByte"/> — the HIGH-VALUE character U+FFFF is the byte 0xFF, owner R52), with the
    /// documented item-209 substitution (<see cref="ByteSub"/>) for any other unit above 0xFF.</summary>
    private static byte[] RawBytes(string s)
    {
        var b = new byte[s.Length];
        for (int i = 0; i < s.Length; i++) b[i] = StorageByte.HasByte(s[i]) ? StorageByte.ToByte(s[i]) : ByteSub();
        return b;
    }

    private static byte[] HexDigitsToBytes(string s)
    {
        // ⛔ ONLY the TRAILING image pad, only SPACE (fix-queue PB59 / AR-15.19.3-4): a LEADING space is
        // content, not padding, and falls through HexVal to the non-digit raise below — the old symmetric
        // Trim() silently converted "  41" as if it were "41". ⛔ LOWERCASE a–f ARE NOT HEXADECIMAL DIGITS
        // IN DATA (corrected 2026-08-09 by RENDERING the §8.3.3.1 page rather than reasoning from the term):
        // "The hexadecimal digits are the basic digits '0' through '9' and the basic letters 'A' through
        // 'F'" — §8.1.3.1 Table 1 makes basic letters the CAPITALS, and §8.1.3.2 GR3a's case-folding covers
        // compilation-group TEXT (X"4a" in SOURCE is legal via CobolLiteral's decode), not runtime data.
        // A first draft of this comment defended the lowercase arm from the term's literal-format usage —
        // the rendered page refuted it, the same one-rule-two-carriers split BASECONVERT's r2 got.
        string t = s.TrimEnd(' ');
        // A malformed HEX source VIOLATES argument rule SR4 (§15.19.3) — that is a FATAL EC-ARGUMENT-FUNCTION
        // (the §15.3 argument-rule default), NOT the nonfatal EC-DATA-CONVERSION (which is only for an untranslatable
        // RESULT value, §15.19.4 r1/r3). ArgumentError throws when checking is on; else the implementor-defined
        // pad/zero result stands.
        // An all-space source has NO digits — §15.19.3 r4's "a valid string of hexadecimal digits" is violated
        // (NOT r1: the ITEM is not zero length — citing the zero-length rule here would be a mis-citation).
        if (t.Length == 0)
        {
            Exceptions.ExceptionState.ArgumentError("CONVERT: HEX source contains no hexadecimal digits (§15.19.3 rule 4)");
            return [];
        }
        if ((t.Length & 1) != 0)                                     // SR4 — complete bytes ⇒ an even digit count
        { Exceptions.ExceptionState.ArgumentError("CONVERT: HEX source is not a whole number of bytes (§15.19.3 SR4)"); t += "0"; }
        var b = new byte[t.Length / 2];
        for (int i = 0; i < b.Length; i++)
        {
            int hi = HexVal(t[2 * i]), lo = HexVal(t[2 * i + 1]);
            if (hi < 0 || lo < 0)
            { Exceptions.ExceptionState.ArgumentError("CONVERT: HEX source has a non-hex digit (§15.19.3 SR4)"); hi = hi < 0 ? 0 : hi; lo = lo < 0 ? 0 : lo; }
            b[i] = (byte)((hi << 4) | lo);
        }
        return b;
    }
    private static int HexVal(char c) => c is >= '0' and <= '9' ? c - '0'
        : c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;   // capitals only — §8.3.3.1's hexadecimal-digit definition (PB59)

    private static string ToHex(byte[] bytes)
    {
        const string D = "0123456789ABCDEF";
        var sb = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (byte x in bytes) { sb.Append(D[x >> 4]); sb.Append(D[x & 0xF]); }
        return sb.ToString();
    }

    /// <summary>SUBSTITUTE (§15.87.4): argument-1 (<paramref name="source"/>) with each occurrence of a
    /// <paramref name="froms"/> substring replaced by the parallel <paramref name="tos"/> string. Each pair's
    /// <paramref name="modes"/> flags select FIRST (bit 0, only the first occurrence — rule 3.a), LAST (bit 1,
    /// only the last — rule 3.b), or ALL (default), and ANYCASE (bit 2, case-folded matching per LOWER-CASE —
    /// rule 5). The scan is a single left-to-right pass: at each position the first pair (in listed order) whose
    /// argument-2 matches AND is eligible is substituted, then the scan resumes past the matched SOURCE substring
    /// (rules 3/4 — a substituted argument-3 is never re-scanned; occurrences count over argument-1, non-
    /// overlapping). A zero-length argument-1 or any zero-length argument-2 sets EC-ARGUMENT-FUNCTION and returns
    /// a zero-length value (rule 1). FIRST/LAST target the pair's first/last occurrence in the source.</summary>
    public static string Substitute(string source, string[] froms, string[] tos, int[] modes, LocaleFacts? classification = null)
    {
        if (source.Length == 0 || froms.Any(f => f.Length == 0))                 // §15.87.4 rule 1
        {
            // §15.87.4 r1 states the result itself — "the returned value is of zero length" — so this arm does
            // NOT rest on an implementor determination; it shares only the mechanism.
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                "SUBSTITUTE argument-1 or an argument-2 is of zero length (§15.87.4 rule 1)");
        }
        int k = froms.Length;
        // ANYCASE (rule 5) folds via LOWER-CASE without the LOCALE argument - the ONE fold every ANYCASE matcher
        // reads (CobolLocale.LowerCase: the module's CHARACTER CLASSIFICATION when it has one, §15.57.4 r3, else the
        // implementor's correspondence, r4; kb/Work PB2631). Length-preserving, so positions over the lowered images align.
        string srcLower = CobolLocale.LowerCase(source, classification);
        var fromsLower = new string[k];
        for (int p = 0; p < k; p++) fromsLower[p] = CobolLocale.LowerCase(froms[p], classification);
        static bool MatchAt(string s, string sLower, int i, string f, string fLower, bool anycase) =>
            i + f.Length <= s.Length
            && (anycase ? string.CompareOrdinal(sLower, i, fLower, 0, f.Length) == 0
                        : string.CompareOrdinal(s, i, f, 0, f.Length) == 0);
        // The single designated position for a FIRST/LAST pair (rule 3.a/3.b), computed over the SOURCE
        // occurrences (non-overlapping); −1 means "every occurrence" (the default) or "no occurrence".
        var target = new int[k];
        for (int p = 0; p < k; p++)
        {
            bool anycase = (modes[p] & 4) != 0;
            if ((modes[p] & 3) == 0) { target[p] = -1; continue; }               // ALL — no single target
            int first = -1, last = -1;
            for (int i = 0; i <= source.Length - froms[p].Length; )
                if (MatchAt(source, srcLower, i, froms[p], fromsLower[p], anycase))
                { if (first < 0) first = i; last = i; i += froms[p].Length; }
                else i++;
            target[p] = (modes[p] & 1) != 0 ? first : last;                      // FIRST or LAST
        }
        var sb = new System.Text.StringBuilder(source.Length);
        for (int i = 0; i < source.Length; )
        {
            int hit = -1;
            for (int p = 0; p < k; p++)
                if (MatchAt(source, srcLower, i, froms[p], fromsLower[p], (modes[p] & 4) != 0)
                    && (target[p] == -1 ? (modes[p] & 3) == 0 : target[p] == i)) // ALL, or the FIRST/LAST target
                { hit = p; break; }
            if (hit >= 0) { sb.Append(tos[hit]); i += froms[hit].Length; }        // rule 3 — resume past the source match
            else { sb.Append(source[i]); i++; }
        }
        return sb.ToString();
    }

    /// <summary>SUBSTITUTE with a table(ALL) among its argument-2 / argument-3 pairs (ISO §15.3 over §15.87.2's
    /// `{ argument-2 argument-3 } …` repetition — kb/Work PB81): the pairing is a RUN-TIME fact. Each
    /// <paramref name="parts"/> entry is a written operand (a singleton) or an enumerated table(ALL); the elements are
    /// paired in order — an even-position element is an argument-2, the next its argument-3 — and each pair's mode is
    /// the flag on the part whose FIRST element opens the pair (<paramref name="partFlags"/>, the keywords that preceded
    /// it in the source; the keywords precede argument-2). The §15.87.2 shapes the binder cannot decide statically are
    /// decided here and set EC-ARGUMENT-FUNCTION with a zero-length result: an odd element count (an argument-2 without
    /// its argument-3), a keyword flag on an argument-3 element, or FIRST together with LAST. The substitution itself is
    /// the ONE <see cref="Substitute"/> kernel.</summary>
    public static string SubstituteFlat(string source, string[][] parts, int[] partFlags, LocaleFacts? classification = null)
    {
        int n = 0;
        foreach (var p in parts) n += p.Length;
        if (n % 2 != 0)
        {
            // Row DOC-A.1-90's ZERO-LENGTH class for all three shape screens here: §15.87.4 r3 populates the
            // returned value by copying argument-1 THROUGH the argument-2 / argument-3 pairing, so it is the
            // pairing that determines the returned length — and the pairing is exactly what is rejected. (r1
            // states zero length outright, but only for a zero-length argument-1 / argument-2; these three rest
            // on the row.) The value is unchanged from before kb/Work PB470 — only its authority is now cited.
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                "SUBSTITUTE: the enumerated argument-2 / argument-3 list has an odd number of elements — every argument-2 needs its argument-3 (ISO §15.87.2 over §15.3)");
        }
        var froms = new string[n / 2];
        var tos = new string[n / 2];
        var modes = new int[n / 2];
        int at = 0;
        for (int p = 0; p < parts.Length; p++)
            for (int e = 0; e < parts[p].Length; e++, at++)
            {
                int flag = e == 0 ? partFlags[p] : 0;
                if ((at & 1) == 0)
                {
                    if ((flag & 3) == 3)
                        return Exceptions.ExceptionState.ArgumentErrorZeroLength(   // row DOC-A.1-90 zero-length class, as above
                            "SUBSTITUTE: FIRST and LAST are specified for one argument-2 / argument-3 pair (ISO §15.87.2)");
                    froms[at / 2] = parts[p][e];
                    modes[at / 2] = flag;
                }
                else
                {
                    if (flag != 0)
                        return Exceptions.ExceptionState.ArgumentErrorZeroLength(   // row DOC-A.1-90 zero-length class, as above
                            "SUBSTITUTE: an ANYCASE / FIRST / LAST keyword precedes an argument-3 element — the keywords precede argument-2 (ISO §15.87.2)");
                    tos[at / 2] = parts[p][e];
                }
            }
        return Substitute(source, froms, tos, modes, classification);
    }

    /// <summary>FIND-STRING (§15.37.4): the 1-based character position of argument-2 (<paramref name="needle"/>)
    /// within argument-1 (<paramref name="hay"/>). With <paramref name="last"/> the LAST occurrence is sought
    /// (rule 1); <paramref name="skip"/> is argument-3 — the number of matches to ignore before determining the
    /// position returned (rule 2, counted from the first occurrence, or from the last when <paramref name="last"/>);
    /// <paramref name="anycase"/> folds case per LOWER-CASE without a LOCALE argument (rule 4) - under the module's
    /// <paramref name="classification"/> when it has one (§15.57.4 r3). A zero-length argument-1 or
    /// argument-2 (rule 5), or no remaining match (rule 3), returns 0. An OCCURRENCE is any character position at
    /// which argument-2 matches a substring of argument-1 (§15.37.4 rule 1 — a plain substring match, OVERLAPPING
    /// occurrences included; §15.37 defines no consumption/advance and never references INSPECT's scanning).</summary>
    /// <remarks>⛔ <paramref name="skip"/> IS <c>Int128</c> BECAUSE ARGUMENT-3 IS TOTAL (kb/Work PB254).
    /// §15.37.3 r3 is the whole argument rule — "argument-3 shall be an integer data item or integer literal" —
    /// and places no constraint on the value, while §15.37.4 r2/r3 answer for every integer: ignore that many
    /// matches, and return 0 when none is left. So argument-3 must NOT cross <c>CobolIntrinsics.IntegerArg</c>,
    /// the §15.3 landing for the PARTIAL integer functions — that landing answered an argument past
    /// <c>long</c> with the checking-off default 0, i.e. "ignore no matches", and returned the FIRST match
    /// where rule 3 requires zero; under <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING ON</c> it terminated the
    /// run unit on conforming source. kb/Work R08 removed the same narrowing ONE LEVEL DOWN (an <c>(int)</c>
    /// cast inside this body) and left the <c>Int128</c>→<c>long</c> one at the intake.</remarks>
    public static long FindString(string hay, string needle, bool last, Int128 skip, bool anycase,
                                  LocaleFacts? classification = null)
    {
        if (hay.Length == 0 || needle.Length == 0) return 0;                     // §15.37.4 rule 5
        // rule 4 - the LOWER-CASE-without-LOCALE fold, ONE for every ANYCASE matcher (kb/Work PB2631): the module's
        // CHARACTER CLASSIFICATION when it has one (§15.57.4 r3), else the implementor's correspondence (r4).
        string h = anycase ? CobolLocale.LowerCase(hay, classification) : hay;
        string n = anycase ? CobolLocale.LowerCase(needle, classification) : needle;
        var positions = new List<int>();
        for (int i = 0; (i = h.IndexOf(n, i, StringComparison.Ordinal)) >= 0; i++)   // every match position (overlapping incl., rule 1)
            positions.Add(i + 1);                                                // 1-based character position
        if (positions.Count == 0) return 0;                                      // rule 3 — no match
        if (skip < 0) skip = 0;
        // kb/Work R08 + PB254: the comparison stays in the WIDEST domain the argument can arrive on — an
        // unchecked (int) narrowing let any argument-3 whose LOW 32 BITS fell in 0..Count-1 return a match
        // position where rule 2 leaves no matches and rule 3 requires zero (2^32 reproduced; 2^31 and 2^32-1
        // missed it by luck), and the Int128→long narrowing at the INTAKE did the same thing one level up.
        if (skip >= positions.Count) return 0;                                   // rule 2 exhausts the matches → 0 (rule 3)
        int s = (int)skip;                                                       // exact — 0 ≤ skip < positions.Count
        return positions[last ? positions.Count - 1 - s : s];                    // rule 1 (first/LAST) + rule 2 (ignore skip)
    }

    /// <summary>TRIM (§15.96.4): the argument with LEADING (<paramref name="mode"/> 1), TRAILING (2), or BOTH
    /// (0) characters that match the delete set removed. The delete set is each argument-2's single character
    /// (§15.96.3 rule 2); with no argument-2 it is a space (rule 3.a). An argument consisting only of delete-set
    /// characters (or of zero length) returns a zero-length string (rule 4).</summary>
    public static string Trim(string s, long mode, params string[] chars)
    {
        // ⛔ SEQUENTIAL folds, never a set union (§15.96.4 r5 — "each argument-2 is processed completely in the
        // order that they are specified"; its NOTE: TRIM(a b c) ≡ TRIM(TRIM(a b) c), and TRIM("aabbcc" "c" "b")
        // is "aa"). The former one-pass s.Trim(set) removed interleavings the nesting cannot reach:
        // TRIM("bcab" "c" "b") is "ca" per the rule — the inner fold strips nothing ('c' guards neither edge) —
        // where the union gave "a" (kb/Work PB117, the §15 close-out's find). The NOTE's own example agrees
        // under both readings, which is why a NOTE-derived golden would have passed over the defect.
        // ⛔ r3's ANTECEDENT IS SYNTACTIC — "When no argument-2 is specified" (§15.96.3 r3) — and an EMPTY
        // `chars` array IS exactly that syntactic fact, which is why it may be decided here: IntrinsicRenderer's
        // "Trim" arm emits no `chars` at all for the one-argument form, and the table(ALL) lane cannot deliver an
        // empty list (CobolTable.AllArgs raises EC-ARGUMENT-FUNCTION on an enumeration over no occurrences).
        // Nothing else reaches this branch. r3 a) and b) are ONE fold because the typed-native model stores a
        // national item as UTF-16 code units, one per national character position, so the national space IS
        // U+0020 and a separate constant would be a second mechanism (AR-15.96.3-3).
        // (kb/Work PB249: the old body decided r3 on an `any` flag computed AFTER every zero-length argument-2
        // had been filtered out, so r3's default was re-applied where its antecedent is FALSE —
        // `FUNCTION TRIM(X DYN-ITEM)` over an empty ANY LENGTH / DYNAMIC LENGTH argument-2 silently trimmed
        // SPACES, a value the source never asked for.)
        if (chars.Length == 0) return TrimOne(s, mode, ' ');
        string r = s;
        foreach (string c in chars)
        {
            // §15.96.3 r2 — argument-2 "shall be a single character". IntrinsicArgumentRules' ExactWidth(1)
            // predicate decides every argument-2 whose width is STATIC and fails OPEN on the rest (an ANY
            // LENGTH or DYNAMIC LENGTH item, a computed-length ref-mod, a table(ALL) element), so the VALUE
            // screen is owed here: a specified argument-2 that is not one character position is an incorrect
            // argument value, §15.3 rule 14 — "If the evaluation of an argument results in an incorrect value
            // for that argument … the EC-ARGUMENT-FUNCTION exception condition is set to exist". The
            // substituted result is the zero-length value (docs/CONFORMANCE.md DOC-A.1-90's first class:
            // TRIM's returned LENGTH is derived from the rejected argument, since argument-2 alone decides
            // which character positions are removed, so no length survives the rejection).
            if (c.Length != 1)
                return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                    $"FUNCTION TRIM argument-2 is {c.Length} character positions, not the single character "
                    + "ISO §15.96.3 rule 2 requires");
            r = TrimOne(r, mode, c[0]);
        }
        return r;

        static string TrimOne(string t, long mode, char c) => mode switch
        {
            1 => t.TrimStart(c),     // LEADING (rule 1)
            2 => t.TrimEnd(c),       // TRAILING (rule 2)
            _ => t.Trim(c),          // both (rule 3)
        };
    }

    // ── FUNCTION STANDARD-COMPARE (§15.85) — the cultural-ordering comparison ────────────────────────────────

    /// <summary>The per-(table, level) collators STANDARD-COMPARE has already configured. The engine caches its
    /// own root-table collators, but a TAILORED ordering table (a locale-tagged literal-9) is a distinct
    /// <c>CollationTable</c> instance per name, so the (table, level) pair is what this function repeats.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(CollationTable Table, int Level), Collator>
        s_orderingCollators = new();

    /// <summary>
    /// STANDARD-COMPARE (ISO §15.85.4): compare <paramref name="a"/> and <paramref name="b"/> "in accordance with
    /// the ordering table and ordering level being used" (r5) and return <c>"&lt;"</c>, <c>"="</c> or <c>"&gt;"</c>
    /// (r6), one character long (r7).
    /// </summary>
    /// <param name="orderingTable">The DECODED literal-9 of the ORDER TABLE clause the call's ordering-name-1
    /// resolves to, or <see langword="null"/> for §15.85.3 r5's default table 'ISO 14651_2020_TABLE1'.</param>
    /// <param name="level">Argument-4, the ordering level; <see langword="null"/> means it was not specified, which
    /// §15.85.4 r1 resolves to "the highest level defined in the ordering table" — four here (the ISO/IEC 14651-style
    /// four-level ordering: primary, secondary, tertiary, and the shifted variable weights). A specified value of
    /// zero or less violates §15.85.3 r6 (EC-ARGUMENT-FUNCTION); one past the table's highest is §15.85.4 r2's
    /// EC-ORDER-NOT-SUPPORTED.</param>
    /// <remarks>
    /// <para><b>r2 — the unavailable table or level.</b> "If the cultural ordering table is not available on the
    /// processor, or the specified ordering level is not available, or the level number specified by argument-4
    /// is not defined in the ordering table, the EC-ORDER-NOT-SUPPORTED exception condition is set to exist."
    /// Both legs route through the ONE raise site (<c>ExceptionState.OrderNotSupportedError</c>): fatal when
    /// checking is enabled, and otherwise the §14.6.13.1.3 #8 implementor choice — this implementation continues
    /// and answers <c>"="</c>, a defined deterministic value rather than an undefined one.</para>
    /// <para><b>r3 — the national conversion.</b> "If the arguments are of different classes, and one is
    /// national, the other argument is converted to class national for purposes of comparison." On the D-N1
    /// substrate an alphanumeric position and a national position are both ONE UTF-16 code unit and the two
    /// repertoires are the same 65,536 code units, so that conversion is the IDENTITY and both operands arrive
    /// here as <see cref="string"/> already in it. Nothing to do — recorded so its absence is not mistaken for
    /// an omission.</para>
    /// <para><b>r4 — the operands.</b> "For purposes of comparison, trailing spaces are truncated from the
    /// operands except that an operand consisting of all spaces is truncated to a single space" — the SAME rule
    /// §8.8.4.2.11 states for locale-based comparison, so it is the same one implementation
    /// (<c>LocaleCollation.TrimForLocale</c>), never a second copy. Note it is not a plain trim: <c>"    "</c>
    /// becomes <c>" "</c>, not <c>""</c>.</para>
    /// <para><b>The alternate handling is SHIFTED</b>, at every level: that is the ISO/IEC 14651 default the
    /// default ordering table names — variable elements (space, punctuation, symbols) ignored through level 3
    /// and weighted at level 4 — so <c>"a-b"</c> and <c>"ab"</c> compare equal at levels 1–3 and differ at 4.</para>
    /// </remarks>
    public static string StandardCompare(string? a, string? b, string? orderingTable, long? level)
    {
        // ⛔ §15.85.3 r6 is an ARGUMENT rule, decided on the VALUE of a specified argument-4 (kb/Work PB2631): a data
        // item holding 0 or a negative integer violates "shall be a positive nonzero integer" and sets
        // EC-ARGUMENT-FUNCTION - it is neither "unspecified" (the old 0 sentinel silently compared at level 4) nor a
        // level the table does not define (r2's EC-ORDER-NOT-SUPPORTED, which a NEGATIVE one raised instead). The
        // result is DOC-A.1-90's fixed one-character class: one space.
        if (level is <= 0)
            return Exceptions.ExceptionState.ArgumentErrorSpaces(
                $"FUNCTION STANDARD-COMPARE argument-4 {level} shall be a positive nonzero integer (§15.85.3 r6)", 1);
        // r1: an unspecified argument-4 is the highest level the ordering table defines. The derived table's
        // levels are UCA's four, so that is 4 — Quaternary under Shifted, which IS CollationEngine.Standard.
        long lvl = level ?? 4;
        CollationTable table;
        if (orderingTable is null)
            table = CollationTable.Root;                       // r5's default table 'ISO 14651_2020_TABLE1'
        else if (!CollationEngine.TryGetOrderingTable(orderingTable, out table))
            return OrderNotSupported($"the cultural ordering table '{orderingTable}' is not available on this "
                + "processor (ISO §15.85.4 r2; §12.3.7.4 GR17 leaves literal-9's allowable content to the "
                + "implementor)");
        if (lvl is < 1 or > 4)
            return OrderNotSupported($"ordering level {lvl} is not defined in the ordering table "
                + $"'{orderingTable ?? CollationEngine.DefaultOrderingTableName}', which defines levels 1..4 "
                + "(ISO §15.85.4 r2)");

        var collator = ReferenceEquals(table, CollationTable.Root)
            ? CollationEngine.StandardAtLevel((int)lvl)        // the engine already caches the root-table lane
            : s_orderingCollators.GetOrAdd((table, (int)lvl),
                static k => new Collator(k.Table, (CollationStrength)k.Level, AlternateHandling.Shifted));
        int c = collator.Compare(LocaleCollation.TrimForLocale(a), LocaleCollation.TrimForLocale(b));
        return c < 0 ? "<" : c > 0 ? ">" : "=";                // r6, one character (r7)

        static string OrderNotSupported(string detail)
        {
            Exceptions.ExceptionState.OrderNotSupportedError("FUNCTION STANDARD-COMPARE: " + detail);
            return "=";   // checking off — the §14.6.13.1.3 #8 implementor choice, documented in CONFORMANCE.md
        }
    }

    // ── FUNCTION BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN (§15.13 / §15.45) — boolean conversions ─────────────

    /// <summary>BOOLEAN-OF-INTEGER (ISO §15.13.4 r1): the boolean value whose bit configuration is the binary
    /// representation of <paramref name="value"/> — the rightmost boolean position is the low-order binary
    /// digit — zero-filled or TRUNCATED ON THE LEFT to exactly <paramref name="length"/> boolean positions.
    /// Left truncation is NORMAL, not an error: the result is <c>value mod 2^length</c> (Annex D.10's
    /// 544→low-6-bits worked example). §15.13.3: argument-2 shall be a positive nonzero integer (r2);
    /// argument-1 shall be positive (r1) — WiseOwl COBOL accepts 0 (all-zero bits; the r1-vs-r2
    /// "positive"/"positive nonzero" drafting contrast reads as arg-2-only excluding zero) and rejects a
    /// negative via EC-ARGUMENT-FUNCTION (§15.3). The documented WiseOwl COBOL maximum returned-value length
    /// (§15.4) is the §8.3.3.4.3 SR1 boolean-literal maximum, 8 191 positions. The '0'/'1' string is the
    /// D-B1 boolean substrate.</summary>
    /// <remarks>⛔ THE VALUE CARRIER IS Int128, AND THE BIT WALK COVERS IT (fix-queue PB65 / RV-15.13.4-1 D1).
    /// §15.13.4 r1's "binary representation of the value of argument-1" is a mathematical bit configuration
    /// (its own NOTE says so), and §15.13.3 r1 admits ANY positive integer — the previous <c>long</c> carrier
    /// silently zeroed every bit from 2⁶³ up (the intake bridge EC'd the value to the checking-off default 0,
    /// and the walk itself stopped at bit 62), so <c>BOOLEAN-OF-INTEGER(2⁶³, 70)</c> returned seventy zeros.
    /// Every legal fixed-point argument value rides the Int128 lane exactly (§13.18.40.3 SR14 caps a PICTURE
    /// at 31 digits &lt; 2¹²⁷); bit 127 is the sign bit and a negative value never reaches the walk, so
    /// <c>i &lt; 127</c> covers the whole carrier. This is INTEGER-OF-BOOLEAN's widening (below), mirrored
    /// onto the argument side.</remarks>
    public static string BooleanOfInteger(Int128 value, long length)
    {
        // ⛔ THREE DIFFERENT RULES, THREE ARMS — never one condition (kb/Work PB383). The three rejections below
        // have different OWNERS and, because §15.13.4 r1 makes the returned LENGTH argument-2 itself, two
        // different documented substituted results. A single `length < 1 || length > 8191` arm answering "0"
        // conflated the first two and got the answer wrong for both.

        // ── §15.13.3 rule 2 — an ARGUMENT rule: "Argument-2 shall be a positive nonzero integer."
        // The rejected argument IS the one the returned length derives from, so there is no length to return at
        // all: docs/CONFORMANCE.md row DOC-A.1-90's zero-length class (§15.3 rule 14), the same answer its
        // FORMATTED-DATE / FORMATTED-DATETIME / FORMATTED-TIME / BASECONVERT siblings give.
        if (length < 1)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                $"FUNCTION BOOLEAN-OF-INTEGER argument-2 {length} shall be a positive nonzero integer (§15.13.3 r2)");

        // ── §15.4 — a RETURNED-VALUE-LENGTH rule, NOT an argument rule. 8,192 *is* a positive nonzero integer,
        // so §15.13.3 r2 is satisfied; what is violated is "the maximum length specified by the implementor for
        // a returned value", which docs/CONFORMANCE.md row DOC-A.1-93 fixes at 8,191 character positions (the
        // §8.3.3.4.3 SR1 boolean-literal maximum). Row 93's documented result is likewise a zero-length value,
        // and this is the second of the two enforcement sites that row names — CobolIntrinsics.BaseConvert,
        // whose §15.4 guard is the first, has always answered it this way.
        if (length > 8191)
            return Exceptions.ExceptionState.ArgumentErrorZeroLength(
                $"FUNCTION BOOLEAN-OF-INTEGER would return {length} boolean positions, past the documented "
                + "8,191-position maximum returned-value length (§15.4; CONFORMANCE.md row DOC-A.1-93)");

        // ── §15.13.3 rule 1 — an ARGUMENT rule about argument-1. ⚠ THE SUBSTITUTE IS DELIBERATELY NOT ZERO
        // LENGTH: argument-2 is valid here, so the returned length is fully determined and row DOC-A.1-90's
        // general clause applies instead of its zero-length class — "the zero value of the type the function
        // returns", which for the §15.13.4 r1 boolean item of argument-2 positions is that many '0' positions.
        // This arm is the discriminator the two above are measured against; do not merge it with them.
        if (value < 0)
        {
            Exceptions.ExceptionState.ArgumentError(
                $"FUNCTION BOOLEAN-OF-INTEGER argument-1 {value} is negative (§15.13.3 r1)");
            return new string('0', (int)length);
        }
        var chars = new char[length];
        for (int i = 0; i < length; i++)   // the rightmost position is the low-order digit (§15.13.4 r1)
            chars[length - 1 - i] = i < 127 && ((value >> i) & 1) != 0 ? '1' : '0';
        return new string(chars);
    }

    /// <summary>INTEGER-OF-BOOLEAN (ISO §15.45.4 r1): the unsigned binary value of argument-1's bit
    /// configuration, most-significant bit first, over a temporary boolean item sized to argument-1 (r1a/r1b).
    /// The value rides the exact Int128 carrier; a configuration whose value exceeds the documented D1
    /// intermediate takes the size-error escape below — LOUD, never a silent cap (§15.45 itself sets no cap).
    /// A zero-length argument (a zero-length hex-boolean literal, §8.3.3.4.3) is value 0 —
    /// the natural reading of an empty configuration (no explicit rule; flagged in the P11 scout notes).</summary>
    public static Int128 IntegerOfBoolean(string boolean)
    {
        // ⛔ THE ACCUMULATOR IS THE EXACT Int128 CARRIER (fix-queue PB65): §15.45.4 r1b is "the unsigned
        // binary value represented by the same bit configuration" — a mathematical value, and the previous
        // signed-long accumulator inherited a 63-bit maximum from its carrier (a 64-one-bit item silently
        // returned the EC default under the OFF checking default). 126 bits ride Int128 exactly; past that
        // the value exceeds the D1 intermediate — the size-error condition at the escape boundary, LOUD,
        // exactly as the alignment escape behaves.
        Int128 v = 0;
        foreach (char c in boolean)
        {
            if (c is not ('0' or '1'))
                return Exceptions.ExceptionState.ArgumentError(
                    "FUNCTION INTEGER-OF-BOOLEAN argument-1 is not of class boolean (§15.45.3 r1)");
            if (v > Int128.MaxValue >> 1)
                throw new CobolSizeError("FUNCTION INTEGER-OF-BOOLEAN: the unsigned binary value exceeds "
                    + "the Int128 intermediate (the D1 escape boundary — EC-SIZE-OVERFLOW)", "EC-SIZE-OVERFLOW");
            v = (v << 1) | (uint)(c - '0');
        }
        return v;
    }
}
