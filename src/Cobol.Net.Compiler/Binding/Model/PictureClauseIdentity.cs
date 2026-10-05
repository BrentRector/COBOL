// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ WHAT "THE SAME PICTURE CLAUSE" MEANS — the ONE identity every identical-description rule of the standard
/// compares (kb/Work PB1166). Five clauses demand that two elementary descriptions have "the same … PICTURE …
/// clauses" and qualify it with the same two exceptions: §8.5.3.1 (type equivalence), §9.3.6 3) (method matching),
/// §9.3.8.2.3 rules 3/6 (interface conformance, overrides and IMPLEMENTS), §14.8.2.3.2 rule 2 (BY REFERENCE
/// arguments) and §14.8.3.3 (returning items):
/// <list type="bullet">
///   <item>"Currency symbols match if and only if the corresponding currency strings are the same" — so the
///   identity carries the currency STRING (<see cref="CurrencyString"/>) and the character-string carries the
///   symbol canonicalized to <c>$</c>: <c>CURRENCY "USD" WITH PICTURE SYMBOL "U"</c> + <c>PIC U9.99</c> is the
///   same PICTURE clause as <c>"USD"</c> under the symbol <c>$</c>, and <c>"EUR"</c> under <c>$</c> is not.</item>
///   <item>"Period picture symbols match if and only if the DECIMAL-POINT IS COMMA clause is in effect for both
///   … or for neither of them. Comma picture symbols match if and only if …" — so the identity carries the
///   DECIMAL-POINT IS COMMA state of the source element that declared the item (<see cref="DecimalPointIsComma"/>),
///   but ONLY when the character-string has a period or comma symbol: a picture without one is the same clause
///   under either setting, and the rule speaks only of those two symbols.</item>
/// </list>
/// <para>Both facts are properties of the SOURCE ELEMENT, not of the character-string, which is why a raw
/// character-string compare missed them: a CLASS definition and a program in one compilation group each carry
/// their own SPECIAL-NAMES paragraph (§12.3.7.4 GR1 makes a CONTAINED unit inherit, so the difference is reachable
/// exactly across separately-defined units). The measured consequence before this type existed: a class returning
/// <c>PIC Z9,99</c> under DECIMAL-POINT IS COMMA into a non-DPC <c>PIC Z9,99</c> receiver compiled clean, and the
/// receiver read 1234 for the 12.34 the sender meant.</para>
/// <para>The <see cref="CharacterString"/> is the EXPANDED character-string (repetition factors unrolled, letters
/// uppercased — §8.1.3.2 GR3 a) makes a picture symbol case-insensitive), so <c>PIC X(3)</c> and <c>PIC XXX</c> are
/// one clause while <c>PIC A(5)</c> and <c>PIC X(5)</c> — equal in length and in storage — are two.
/// <see cref="Editing"/> is the resolved PICTURE EDITING phrase set (§13.18.40.2 Format 1), part of the clause.</para>
/// <para>Computed ONCE, by <c>PictureAnalyzer.Analyze</c> — the one place the character-string, the unit's
/// CURRENCY SIGN set and its DECIMAL-POINT IS COMMA state are all in hand — and carried on
/// <see cref="PicInfo.Clause"/>. Being a value-equality record it is part of <see cref="PicInfo"/>'s own record
/// equality, so the §8.5.3.1 profile compare (<c>StrongTypeModel</c>) and the activation / signature comparator
/// (<c>OoConformance.DescriptionMismatch</c>) read the same identity by construction.</para>
/// </summary>
/// <param name="CharacterString">The expanded, uppercased character-string with the currency symbol canonicalized
/// to <c>$</c>.</param>
/// <param name="CurrencyString">The currency string the picture's currency symbol stands for, or null when the
/// picture has no currency symbol.</param>
/// <param name="DecimalPointIsComma">Whether DECIMAL-POINT IS COMMA is in effect for the declaring source element,
/// or null when the picture has neither a period nor a comma symbol (the setting then cannot matter).</param>
/// <param name="Editing">The canonical text of the resolved PICTURE EDITING rules, or null when none.</param>
public sealed record PictureClauseIdentity(string CharacterString, string? CurrencyString, bool? DecimalPointIsComma,
    string? Editing)
{
    /// <summary>Builds the identity from what <c>PictureAnalyzer</c> holds after expansion.</summary>
    /// <param name="expanded">The expanded, uppercased character-string.</param>
    /// <param name="currencySymbol">The picture's currency symbol (the default <c>$</c> when it uses none).</param>
    /// <param name="currencyString">The string <paramref name="currencySymbol"/> stands for.</param>
    /// <param name="decimalPointIsComma">The declaring source element's DECIMAL-POINT IS COMMA state.</param>
    public static PictureClauseIdentity Of(string expanded, char currencySymbol, string currencyString,
        bool decimalPointIsComma)
    {
        bool hasCurrency = false, hasSeparator = false;
        var chars = expanded.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = char.ToUpperInvariant(chars[i]);
            if (c == currencySymbol) { hasCurrency = true; chars[i] = '$'; }
            else if (c is '.' or ',') hasSeparator = true;
        }
        return new PictureClauseIdentity(new string(chars), hasCurrency ? currencyString : null,
            hasSeparator ? decimalPointIsComma : null, Editing: null);
    }

    /// <summary>The canonical text of a resolved PICTURE EDITING rule set, or null when there is none.</summary>
    public static string? EditingText(IReadOnlyList<CobolEdit.EditRule>? rules) =>
        rules is null || rules.Count == 0 ? null
        : string.Join(";", rules.Select(r =>
            $"{r.Char1}:{r.Neg}:{r.Pos}:{(r.SimpleInsertion ? 'S' : '-')}{(r.Floating ? 'F' : '-')}"));

    /// <summary>Why <paramref name="formal"/> and <paramref name="arg"/> are NOT the same PICTURE clause, or null
    /// when they are. The message names the axis that differs, so a currency-string or DECIMAL-POINT IS COMMA
    /// difference is never reported as a character-string difference it is not.</summary>
    public static string? Mismatch(PictureClauseIdentity formal, PictureClauseIdentity arg)
    {
        if (!string.Equals(formal.CharacterString, arg.CharacterString, StringComparison.Ordinal))
            return $"PICTURE mismatch (formal '{formal.CharacterString}', argument '{arg.CharacterString}' — the "
                + "corresponding items shall have the same PICTURE clause)";
        if (!string.Equals(formal.CurrencyString, arg.CurrencyString, StringComparison.Ordinal))
            return $"currency string mismatch (the formal's currency symbol stands for \"{formal.CurrencyString}\", "
                + $"the argument's for \"{arg.CurrencyString}\" — currency symbols match if and only if the "
                + "corresponding currency strings are the same: ISO §14.8.2.3.2 rule 2 a), §14.8.3.3 exception 1), "
                + "§9.3.8.2.3 rule 3 a))";
        if (formal.DecimalPointIsComma != arg.DecimalPointIsComma)
            return "DECIMAL-POINT IS COMMA mismatch (it is in effect for the "
                + $"{(formal.DecimalPointIsComma == true ? "formal's" : "argument's")} source element and not for the "
                + $"{(formal.DecimalPointIsComma == true ? "argument's" : "formal's")} — period and comma picture "
                + "symbols match if and only if the clause is in effect for both or for neither: ISO §14.8.2.3.2 "
                + "rule 2 b), §14.8.3.3 exceptions 2)/3), §9.3.8.2.3 rule 3 b))";
        if (!string.Equals(formal.Editing, arg.Editing, StringComparison.Ordinal))
            return "PICTURE EDITING phrase mismatch (the corresponding items shall have the same PICTURE clause, and "
                + "its EDITING phrases are part of it — ISO §13.18.40.2)";
        return null;
    }

    /// <summary>The PICTURE compare for an ANY LENGTH formal (or sending returning item): §13.18.2.3 SR1 gives it
    /// a one-symbol picture (<c>X</c>, <c>N</c> or <c>1</c>) and §14.8.2.3.2 "Additionally" d) makes "its length
    /// … considered to match the length of the corresponding argument" — so the other side is the same PICTURE
    /// clause exactly when it is that symbol repeated, at whatever length. Null when it is.</summary>
    public static string? MismatchAtAnyLength(PictureClauseIdentity anyLength, PictureClauseIdentity other)
    {
        char symbol = anyLength.CharacterString.Length == 1 ? anyLength.CharacterString[0] : '\0';
        return symbol != '\0' && other.CharacterString.Length > 0 && other.CharacterString.All(c => c == symbol)
            ? null
            : $"PICTURE mismatch (the ANY LENGTH item is described '{anyLength.CharacterString}', the other "
              + $"'{other.CharacterString}' — only the length is considered to match: ISO §14.8.2.3.2 "
              + "\"Additionally\" d))";
    }

    /// <summary>The identity as one key, for the universal-dispatch activation description
    /// (<c>ActivationDescriptions.ElementaryClauses</c>) — equal keys ⇔ <see cref="Mismatch"/> is null. The character-string
    /// holds picture symbols only (never '|'), the currency string is length-prefixed and the free-text editing rules
    /// come last, so no two identities share a key.</summary>
    public string Key =>
        $"{CharacterString}|{CurrencyString?.Length ?? -1}:{CurrencyString}|"
        + $"{(DecimalPointIsComma is { } d ? (d ? "C" : "P") : "-")}|{Editing}";
}
