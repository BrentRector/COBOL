// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Common;

/// <summary>
/// The ONE COBOL string-literal codec (ISO/IEC 1989:2023 §8.3.3.1 — the quotation-mark (<c>"…"</c>) and
/// apostrophe (<c>'…'</c>) delimiter forms are EQUAL-STANDING; the delimiters are not part of the value, and a
/// doubled OPENING delimiter inside is one embedded delimiter). National / boolean literals (<c>N"…"</c>/<c>B"…"</c>
/// and their apostrophe forms, ISO §8.3.3.5/§8.3.3.4) carry the prefix letter as part of the token. This single
/// codec (rearchitecture PHASE 05, Step 1) replaces the three per-layer <c>DecodeCobolString</c>/<c>DecodeString</c>
/// twins AND — the confirmed silent-miscompile fix — the hard-coded <c>'"'</c>-only guards that used to gate whether
/// to decode, so an apostrophe-delimited <c>VALUE 'x'</c> no longer falls through to raw text (DESIGN-data-model §2.8).
/// </summary>
/// <summary>The class of a quoted literal (ISO §8.3.3.2 / §8.3.3.4 / §8.3.3.5) — what <see cref="CobolLiteral.ClassOf"/>
/// answers from the prefix. Front-end-side (no PICTURE model here); the binder maps it onto its data category.</summary>
public enum LiteralClass { Alphanumeric, National, Boolean }

/// <summary>Which of a literal's own §8.3.3 syntax rules <see cref="CobolLiteral.SyntaxViolation"/> found violated: the
/// prefixed formats' content repertoire, the hexadecimal grouping, or the length.</summary>
public enum LiteralRule { Repertoire, HexGrouping, Length }

public static class CobolLiteral
{
    /// <summary>The prefix letters a quoted literal may carry: <c>N</c> national (§8.3.3.5), <c>B</c> boolean
    /// (§8.3.3.4), <c>X</c> hexadecimal-format alphanumeric (§8.3.3.2).</summary>
    /// <remarks>
    /// ⛔ ONE LIST, read by both <see cref="IsStringLiteral"/> and <see cref="Decode"/>. It was written down in
    /// both of them, and BOTH copies omitted <c>X</c> — so a hexadecimal literal was neither recognised as a
    /// literal nor decoded as one, and every caller that pairs those two methods (all of them) mishandled it in
    /// two different ways at once. Adding a prefix is now an edit to this string.
    /// </remarks>
    private const string PrefixLetters = "NnBbXx";

    /// <summary>The prefix letter (upper-cased, <c>'\0'</c> when none) and the quoted body of a literal, or
    /// <see langword="null"/> when <paramref name="raw"/> is not a quoted literal at all.</summary>
    /// <summary>The prefix as WRITTEN, upper-cased — <c>""</c>, <c>"N"</c>, <c>"B"</c>, <c>"X"</c>, <c>"NX"</c>
    /// or <c>"BX"</c> — and the quoted body; null when <paramref name="raw"/> is not a quoted literal.</summary>
    private static (string Prefix, string Body)? SplitLiteral(string raw)
    {
        string body = raw;
        string prefix = "";
        // ⛔ THE TWO-LETTER PREFIXES ARE TESTED FIRST, AND THE ORDER IS THE WHOLE CORRECTNESS ARGUMENT
        // (fix-queue R03). `NX"…"` is §8.3.3.5.2 Format 2 (hexadecimal-national) and `BX"…"` is §8.3.3.4.2
        // Format 2 (hexadecimal-boolean). Both open with a letter that is ALSO a single-letter prefix, so a
        // single-letter test running first sees `N` followed by `X` rather than a quote, concludes "not a
        // literal", and hands back the raw source text — which is precisely how the token used to degrade.
        if (body.Length >= 4 && body[0] is 'N' or 'n' or 'B' or 'b' && body[1] is 'X' or 'x'
            && body[2] is '"' or '\'')
        {
            prefix = body[..2].ToUpperInvariant();
            body = body[2..];
        }
        else if (body.Length >= 3 && PrefixLetters.Contains(body[0]) && body[1] is '"' or '\'')
        {
            prefix = body[..1].ToUpperInvariant();
            body = body[1..];
        }
        // ⛔ ONE literal, WELL-FORMED (kb/Work PB71): the former first/last-character test answered "a literal" for
        // `"A"&"B"` (a concatenation's source text) and Decode then produced `A"&"B`. Inside the delimiters an
        // embedded delimiter shall be doubled (§8.3.3.2.3 r3), so an undoubled one before the end is not this literal.
        if (body.Length < 2 || body[0] is not ('"' or '\'') || body[^1] != body[0]) return null;
        char d = body[0];
        for (int i = 1; i < body.Length - 1; i++)
            if (body[i] == d) { if (i + 1 < body.Length - 1 && body[i + 1] == d) i++; else return null; }
        return (prefix, body);
    }

    /// <summary>True when <paramref name="raw"/> is a quoted literal in EITHER ISO delimiter, optionally with an
    /// <c>N</c>/<c>B</c>/<c>X</c> prefix letter. This is the delimiter-agnostic replacement for the former
    /// <c>raw[0] == '"'</c> guards.</summary>
    /// <remarks>
    /// A HEXADECIMAL literal answers true, because §8.3.3.2 makes it one FORM of an alphanumeric literal rather
    /// than a separate kind of thing. Every caller pairs this with <see cref="Decode"/>, so the two agreeing is
    /// what makes <c>VALUE ALL X"41"</c> initialize to <c>AAAA</c> instead of to the characters <c>ALLX</c>.
    /// </remarks>
    public static bool IsStringLiteral(string raw) => SplitLiteral(raw) is not null;

    /// <summary>True when <paramref name="raw"/> is a ZERO-LENGTH LITERAL — ISO §3.178, "alphanumeric, boolean, or
    /// national literal that contains zero characters". The test is the standard's own structural one (§8.3.3.1:
    /// "If the opening and closing delimiters are contiguous, the length of the literal is zero"), asked of the
    /// quoted body AFTER the prefix letters, so every spelling the four formats admit — <c>""</c>, <c>''</c>,
    /// <c>N""</c>, <c>B""</c>, <c>X""</c>, <c>NX""</c>, <c>BX""</c> — answers true and nothing else does.
    /// <para>⛔ NOT <c>Decode(raw).Length == 0</c>: <see cref="Decode"/> yields the empty string for an ILL-FORMED
    /// hexadecimal literal too (<c>X"414"</c> — the §8.3.3.2.3 r6 grouping violation <see cref="HexGroupViolation"/>
    /// reports), so that spelling of the question would answer "zero-length" for a literal that is not one, and the
    /// several syntax rules that forbid a zero-length literal (§14.9.37.3 SR13, §12.3.7.3 SR11, …) would report the
    /// wrong rule.</para></summary>
    public static bool IsZeroLength(string raw) => SplitLiteral(raw) is { Body.Length: 2 };

    /// <summary>The CLASS of a quoted literal by its PREFIX — ISO §8.3.3.2 (<c>"…"</c> / <c>X"…"</c> alphanumeric),
    /// §8.3.3.5 (<c>N"…"</c> / <c>NX"…"</c> national), §8.3.3.4 (<c>B"…"</c> / <c>BX"…"</c> boolean) — or
    /// <see langword="null"/> when <paramref name="raw"/> is not a quoted literal. ⛔ THE ONE CLASSIFIER (kb/Work
    /// PB71): the VALUE-clause validator computed the class from <c>raw[0]</c>/<c>raw[1]</c> and so refused the
    /// Format-2 hexadecimal spellings (<c>NX"…"</c>, <c>BX"…"</c>) and every <c>ALL literal</c>; the ALL-figurative
    /// binder tested two token kinds of four. Both ask this now.</summary>
    public static LiteralClass? ClassOf(string raw) => SplitLiteral(raw) switch
    {
        null => null,
        ("N" or "NX", _) => LiteralClass.National,
        ("B" or "BX", _) => LiteralClass.Boolean,
        _ => LiteralClass.Alphanumeric,
    };

    /// <summary>True when <paramref name="raw"/> is written in a HEXADECIMAL FORMAT — <c>X"…"</c> (§8.3.3.2 Format 2),
    /// <c>NX"…"</c> (§8.3.3.5.2 Format 2) or <c>BX"…"</c> (§8.3.3.4.2 Format 2). ⛔ THE ONE hexadecimal-format question,
    /// asked of the parsed PREFIX (the classifier's own) and never of a second <c>text[0] is 'X'</c> test: the rules
    /// that treat a hexadecimal literal apart from its decoded characters (§12.3.7.3 SR19 and SR26 — a currency literal
    /// "in hexadecimal format") need the FORM, which the decoded characters cannot tell (kb/Work PB791).</summary>
    public static bool IsHexadecimalFormat(string raw) => SplitLiteral(raw) is { Prefix: "X" or "NX" or "BX" };

    /// <summary>If <paramref name="raw"/> is the figurative <c>ALL literal</c> form (a VALUE / level-88 operand
    /// text), the RAW literal after ALL — still prefixed, so <see cref="ClassOf"/> and <see cref="Decode"/> apply
    /// to it; otherwise <see langword="null"/>.</summary>
    public static string? AllLiteralRaw(string raw)
    {
        string t = raw.TrimStart();
        if (t.Length < 3 || !CobolNames.StartsWith(t, "ALL")) return null;
        string rest = t[3..].TrimStart();
        return IsStringLiteral(rest) ? rest : null;
    }

    /// <summary>Decode a <c>STRINGLIT</c> (or an <c>N</c>/<c>B</c>/<c>X</c>-prefixed national, boolean or
    /// HEXADECIMAL literal) to its character value; returns <paramref name="raw"/> unchanged when it is not a
    /// quoted literal. Unwraps either delimiter and collapses a doubled opening delimiter to one embedded
    /// delimiter (§8.3.3.1). Body ported verbatim from the retired <c>EmitText.DecodeCobolString</c> twin.</summary>
    /// <remarks>
    /// ⛔ THE <c>X</c> ARM IS THE FOURTH COPY OF A DISPATCH DA3 FOUND THREE OF, and it belongs here rather than
    /// at the call sites. This decoder handled the <c>N</c> and <c>B</c> prefixes and silently returned a
    /// hexadecimal literal AS ITS OWN SOURCE TEXT, so every caller that did not separately think to call
    /// <see cref="DecodeHex"/> got the characters <c>X"4142"</c> where the standard gives <c>AB</c>. The VALUE
    /// path was exactly such a caller: <c>01 B PIC X(2) VALUE X"4142"</c> initialized the item to <c>X"</c> — the
    /// literal's source text, truncated to the picture — with no diagnostic, while <c>MOVE X"4142" TO B</c>
    /// correctly stored <c>AB</c>. Silent data corruption, and the two paths disagreed with each other.
    /// <para>
    /// Adding a hex arm at <c>ValueInitializer</c> would have made it the fifth copy. §8.3.3.2 makes a
    /// hexadecimal literal one FORM of an alphanumeric literal, not a separate kind of thing, so the decoder that
    /// owns "literal text → characters" owns this too (<c>feedback_one_rule_one_place</c>).
    /// </para>
    /// <para>
    /// ⚠ Delegation is keyed on the parsed prefix from <see cref="SplitLiteral"/>, never on a leading <c>X</c>
    /// alone: <see cref="DecodeHex"/> returns the empty string for anything it does not recognise, so a bare
    /// prefix test would turn an ordinary unquoted word beginning with X — which this method contracts to return
    /// unchanged — into "".
    /// </para>
    /// </remarks>
    public static string Decode(string raw)
    {
        if (SplitLiteral(raw) is not { } lit) return raw;
        if (lit.Prefix == "X") return DecodeHex(raw);
        // §8.3.3.5.2 Format 2 — hexadecimal-national. §8.3.3.5.3 SR5 makes the digits-per-national-character an
        // IMPLEMENTOR choice, and D-N1 stores one UTF-16 code unit per national position, so that number is FOUR;
        // §8.3.3.5.4 GR4 then gives each national character the bit configuration of one such group.
        if (lit.Prefix == "NX") return DecodeHexGroups(lit.Body[1..^1], 4);
        // §8.3.3.4.2 Format 2 — hexadecimal-boolean. §8.3.3.4.4 GR5 spells the mapping out digit by digit
        // ("'0' is B\"0000\" … 'F' is B\"1111\""), i.e. each hexadecimal digit IS four boolean characters, and a
        // boolean item stores those as the '0'/'1' characters of the D-B1 bit-string world.
        if (lit.Prefix == "BX")
        {
            if (!IsInRepertoire(lit.Body[1..^1])) return "";   // §8.3.3.4.3 SR3 — reported by RepertoireViolation
            var sb = new System.Text.StringBuilder(lit.Body.Length * 4);
            foreach (char c in lit.Body[1..^1])
                sb.Append(System.Convert.ToString(System.Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0'));
            return sb.ToString();
        }
        char q = lit.Body[0];
        return lit.Body[1..^1].Replace(new string(q, 2), q.ToString());
    }

    /// <summary>
    /// ⭐ THE §8.3.3 HEXADECIMAL GROUPING RULE, for all three literal forms — a message naming the offending
    /// clause, or <see langword="null"/> when the literal is well formed or is not a hexadecimal literal at all.
    /// </summary>
    /// <remarks>
    /// Three formats state this rule and they do NOT state the same rule (fix-queue R03):
    /// <list type="bullet">
    /// <item><c>X"…"</c> — §8.3.3.2.3 <b>r6</b>: groups of the implementor's digits-per-alphanumeric-character,
    /// which is TWO (one byte per character).</item>
    /// <item><c>NX"…"</c> — §8.3.3.5.3 <b>r5</b>: the same sentence for national, and D-N1 stores one UTF-16 code
    /// unit per national position, so it is FOUR.</item>
    /// <item><c>BX"…"</c> — §8.3.3.4.3 <b>r3</b> says only "Hexadecimal-digit-1 shall be a hexadecimal digit".
    /// <b>There is NO grouping rule</b>, because §8.3.3.4.4 GR5 maps each digit individually to four boolean
    /// characters. Any digit count is well formed, so this returns null for BX always — writing a group check
    /// for it "for symmetry" would reject legal source.</item>
    /// </list>
    /// <para>
    /// ⛔ AN ILL-FORMED HEX LITERAL USED TO DECODE TO THE EMPTY STRING WITH NO DIAGNOSTIC — <c>X"414"</c> and
    /// <c>NX"041"</c> both made <c>FUNCTION LENGTH</c> answer 1, silently, on source the standard rejects. The
    /// decoders returned "" for a malformed shape and every caller took that as the value.
    /// </para>
    /// </remarks>
    public static string? HexGroupViolation(string raw)
    {
        if (SplitLiteral(raw) is not { } lit || RepertoireViolation(raw) is not null) return null;
        (int per, string clause) = lit.Prefix switch
        {
            "X" => (2, "§8.3.3.2.3 r6"),
            "NX" => (4, "§8.3.3.5.3 r5"),
            _ => (0, ""),      // BX has no grouping rule; every other prefix is not hexadecimal at all
        };
        if (per == 0) return null;
        int n = lit.Body.Length - 2;
        return n % per == 0 ? null
            : $"has {n} hexadecimal digit(s), which is not a whole number of {per}-digit groups — {clause} "
              + $"requires each hexadecimal character sequence to be {per} digits";
    }

    /// <summary>
    /// ⭐ A LITERAL'S OWN SYNTAX RULES, ASKED IN THEIR ONE ORDER — the first violated of <see cref="RepertoireViolation"/>,
    /// <see cref="HexGroupViolation"/> and <see cref="LengthViolation"/>, or <see langword="null"/>. Content outside the
    /// repertoire has no digit count to group, and a malformed hexadecimal literal has no value to measure, so the first
    /// violation is the one to report. Every consumer that meets a literal TOKEN asks this, never the three predicates:
    /// <c>LiteralScreenPass</c> for the unit's tree, and the compile-time evaluator for a compiler-directive operand,
    /// whose fragment is lexed apart from the unit and so is never on that tree (kb/Work PB1441).
    /// </summary>
    public static (LiteralRule Rule, string Message)? SyntaxViolation(string raw) =>
        RepertoireViolation(raw) is { } repertoire ? (LiteralRule.Repertoire, repertoire)
        : HexGroupViolation(raw) is { } grouping ? (LiteralRule.HexGrouping, grouping)
        : LengthViolation(raw) is { } length ? (LiteralRule.Length, length)
        : null;

    /// <summary>A literal as a diagnostic quotes it: whole when short, else its first and last few characters — an
    /// over-long literal is by definition too long to quote whole.</summary>
    public static string Abbreviated(string raw) => raw.Length <= 40 ? raw : raw[..24] + "…" + raw[^8..];

    /// <summary>
    /// ⭐ THE §8.3.3 CONTENT-REPERTOIRE RULE of the four prefixed formats whose content is not free text — a message
    /// naming the offending character and clause, or <see langword="null"/> when the content is within the format's
    /// repertoire or <paramref name="raw"/> is not such a literal (kb/Work PB1441, PB1394).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>X"…"</c> — §8.3.3.2.3 SR5: "Hex-character-sequence-1 shall be composed of hexadecimal digits".</item>
    /// <item><c>NX"…"</c> — §8.3.3.5.3 SR4: the same sentence for hexadecimal-national.</item>
    /// <item><c>BX"…"</c> — §8.3.3.4.3 SR3: "Hexadecimal-digit-1 shall be a hexadecimal digit".</item>
    /// <item><c>B"…"</c> — §8.3.3.4.3 SR2: "Boolean-character-1 shall be a boolean character, '0' or '1'".</item>
    /// </list>
    /// <para>⛔ THE LEXER NO LONGER DECIDES THIS, AND MUST NOT. Its bodies used to spell each repertoire, so a body
    /// outside it was not a literal at all and fell back to IDENTIFIER + Format 1 literal — a silent wrong answer
    /// beside a data item of the prefix's name. Every prefixed body is now any quoted content (CobolLexer.g4
    /// HEX_BODY), and this rule is asked of every literal token by <c>LiteralScreenPass</c>. It PRECEDES the
    /// grouping rule: a non-hexadecimal sequence has no digit count to group, so <see cref="HexGroupViolation"/>
    /// declines such a literal, and every decoder answers "" for it (<see cref="IsInRepertoire"/>).</para>
    /// </remarks>
    public static string? RepertoireViolation(string raw)
    {
        if (SplitLiteral(raw) is not { } lit) return null;
        (bool hex, string clause, string repertoire) = lit.Prefix switch
        {
            "X" => (true, "§8.3.3.2.3 SR5", "hexadecimal digits"),
            "NX" => (true, "§8.3.3.5.3 SR4", "hexadecimal digits"),
            "BX" => (true, "§8.3.3.4.3 SR3", "hexadecimal digits"),
            "B" => (false, "§8.3.3.4.3 SR2", "the boolean characters '0' and '1'"),
            _ => (false, "", ""),   // Format 1 alphanumeric and national: any character is content
        };
        if (clause.Length == 0) return null;
        string content = lit.Body[1..^1];
        int bad = FirstOutsideRepertoire(content, hex);
        return bad < 0 ? null
            : $"contains '{content[bad]}' — {clause} admits only {repertoire} between its delimiters";
    }

    /// <summary>The index of the first character of <paramref name="content"/> outside the hexadecimal digits
    /// (<paramref name="hex"/>; §3.98: 0–9 and A–F, "where the letters A-F are equivalent to the letters a-f") or the
    /// boolean characters '0'/'1'; -1 when none.</summary>
    private static int FirstOutsideRepertoire(string content, bool hex)
    {
        for (int i = 0; i < content.Length; i++)
            if (!(hex ? char.IsAsciiHexDigit(content[i]) : content[i] is '0' or '1')) return i;
        return -1;
    }

    /// <summary>True when every character of <paramref name="digits"/> is a hexadecimal digit — the precondition every
    /// hexadecimal decoder checks before converting, so a literal <see cref="RepertoireViolation"/> reports decodes to
    /// "" (the malformed-literal posture <see cref="HexGroupViolation"/> established) rather than throwing.</summary>
    private static bool IsInRepertoire(string digits) => FirstOutsideRepertoire(digits, hex: true) < 0;

    /// <summary>The largest number of character positions a literal of ANY of the three classes may hold, and the
    /// largest a concatenation expression may produce: ISO §8.3.3.2.3 SR1 (alphanumeric), §8.3.3.4.3 SR1
    /// (boolean) and §8.3.3.5.3 SR1 (national) each print "shall be less than or equal to 8,191 … character
    /// positions", and §8.8.3.2 SR2–SR4 print the same bound for the value a concatenation results in. Four rules,
    /// one number, one symbol.</summary>
    public const int MaxLiteralPositions = 8191;

    /// <summary><see cref="MaxLiteralPositions"/> as the standard prints it ("8,191"), culture-invariant so a
    /// diagnostic reads the same on every machine.</summary>
    public static string MaxLiteralPositionsText { get; } =
        MaxLiteralPositions.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// ⭐ THE §8.3.3 LITERAL LENGTH RULE, for all three classes and every format of each — a message naming the
    /// offending clause, or <see langword="null"/> when the literal is within bounds or is not a quoted literal.
    /// </summary>
    /// <remarks>
    /// The measure is the literal's VALUE in character positions of its class ("excluding the separators that
    /// delimit the literal" — and so excluding the prefix letters, and counting a doubled embedded delimiter as the
    /// one character it denotes): <c>X"4142"</c> is two alphanumeric positions, <c>NX"00410042"</c> two national
    /// ones, and <c>BX"F"</c> FOUR boolean ones, because §8.3.3.4.4 GR5 maps each hexadecimal digit to four boolean
    /// characters. That is exactly what <see cref="Decode"/> yields, so the rule asks the one codec.
    /// <para>A malformed hexadecimal literal (<see cref="HexGroupViolation"/>) decodes to nothing and is reported
    /// under that rule; its length is not separately judged.</para>
    /// </remarks>
    public static string? LengthViolation(string raw)
    {
        // Fast path for the overwhelmingly common short literal (this is asked of EVERY literal token): no format
        // decodes to more positions than four per source character — BX"…" is the widest, four boolean characters
        // per digit — so a token this short cannot exceed the cap, and no decode is allocated for it.
        if (raw.Length <= MaxLiteralPositions / 4) return null;
        if (ClassOf(raw) is not { } cls) return null;
        int positions = Decode(raw).Length;
        if (positions <= MaxLiteralPositions) return null;
        (string name, string clause) = cls switch
        {
            LiteralClass.National => ("national", "§8.3.3.5.3 SR1"),
            LiteralClass.Boolean => ("boolean", "§8.3.3.4.3 SR1"),
            _ => ("alphanumeric", "§8.3.3.2.3 SR1"),
        };
        return $"is {positions} {name} character positions long — {clause} limits a {name} literal to "
            + $"{MaxLiteralPositionsText}";
    }

    /// <summary>Decode <paramref name="digits"/> as groups of <paramref name="perChar"/> hexadecimal digits, one
    /// character per group — the §8.3.3.5.4 GR4 hexadecimal-national mapping, and the shape §8.3.3.2's
    /// alphanumeric hex form uses with <paramref name="perChar"/> = 2. A trailing partial group violates
    /// §8.3.3.5.3 SR5 (and a non-hexadecimal digit §8.3.3.5.3 SR4 / §8.3.3.2.3 SR5) and yields the empty string:
    /// the literal screen reports the violation (<see cref="HexGroupViolation"/>, <see cref="RepertoireViolation"/>),
    /// so a malformed literal has no value to produce.</summary>
    private static string DecodeHexGroups(string digits, int perChar)
    {
        if (digits.Length == 0) return "";                       // §8.3.3.5.4 GR4 — zero-length is legal
        if (digits.Length % perChar != 0 || !IsInRepertoire(digits)) return "";
        var chars = new char[digits.Length / perChar];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)Convert.ToInt32(digits.Substring(i * perChar, perChar), 16);
        return new string(chars);
    }

    /// <summary>Decode an <c>X"…"</c>/<c>X'…'</c> hexadecimal-format alphanumeric literal (ISO §8.3.3.2 —
    /// each pair of hexadecimal digits is one character) to its character value; an odd digit count or a non-hexadecimal
    /// digit (both reported by the literal screen) yields the empty string. The
    /// ONE hex decoder (P10 Step 14), reached through <see cref="Decode"/> by every literal consumer — the INVOKE
    /// method name (<c>OoBinder.OoMethodNameOf</c>, which once kept a private copy that read NX"…" as alphanumeric
    /// hex, kb/Work PB1136) included — and by the §8.8.3 concatenation fold.</summary>
    public static string DecodeHex(string raw)
        => SplitLiteral(raw) is { Prefix: "X" } lit ? DecodeHexGroups(lit.Body[1..^1], 2) : "";

    /// <summary>If <paramref name="raw"/> is the figurative <c>ALL "literal"</c> / <c>ALL 'literal'</c> form (a
    /// VALUE / level-88 operand text), the decoded literal; otherwise <see langword="null"/> (e.g. <c>ALL ZEROS</c>,
    /// a figurative word, is handled elsewhere). The decoded twin of <see cref="AllLiteralRaw"/> and NOT a second
    /// copy of its test (kb/Work PB461 — the ALL prefix was parted in seven places and no two of them the same
    /// way): tolerant of whether the front-end preserved the space between <c>ALL</c> and the literal, and
    /// delimiter-agnostic (the former <c>'"'</c>-only guard was the miscompile) because that ONE test is.</summary>
    public static string? AllLiteralText(string raw) => AllLiteralRaw(raw) is { } lit ? Decode(lit) : null;
}
