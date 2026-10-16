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
/// equality, so the §8.5.3.1 profile compare (<c>StrongTypeModel</c>) and every activation and signature relation
/// (<c>ActivationRelations</c>, over the <see cref="Key"/> an activation description's clauses carry) read the same
/// identity by construction.</para>
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

    /// <summary>The identity as one key, for the universal-dispatch activation description
    /// (<c>ActivationDescriptions.ElementaryClauses</c>) — equal keys ⇔ the same PICTURE clause. The character-string
    /// holds picture symbols only (never '|'), the currency string is length-prefixed and the free-text editing rules
    /// come last, so no two identities share a key.</summary>
    public string Key =>
        $"{CharacterString}|{CurrencyString?.Length ?? -1}:{CurrencyString}|"
        + $"{(DecimalPointIsComma is { } d ? (d ? "C" : "P") : "-")}|{Editing}";

    /// <summary>The identity as a reader spells it, one phrase per fact (<c>ActivationDescription.ClausePhrases</c>,
    /// never compared — <see cref="Key"/> is): the PICTURE clause, a run of four or more of one symbol written with
    /// its repetition count (<c>9(6)</c>, <c>X(10)</c>); the currency string the source element's CURRENCY SIGN clause
    /// gives its currency symbol; DECIMAL-POINT IS COMMA when the clause is in effect for it; and the EDITING phrase.
    /// Equal keys have equal phrases, so a refusal never names a difference the identity does not hold; two EDITING
    /// phrases are named by presence only.</summary>
    public IEnumerable<string> Phrases()
    {
        yield return "PICTURE " + Spelled(CharacterString);
        if (CurrencyString is not null) yield return $"CURRENCY SIGN IS \"{CurrencyString}\"";
        if (DecimalPointIsComma is true) yield return "DECIMAL-POINT IS COMMA";
        if (Editing is not null) yield return "EDITING";
    }

    /// <summary>An expanded character-string as it would be written: each run of four or more of one symbol as
    /// <c>symbol(count)</c>, the character-string's repetition form, and every shorter run as its symbols.</summary>
    private static string Spelled(string expanded)
    {
        var text = new System.Text.StringBuilder(expanded.Length);
        for (int i = 0; i < expanded.Length;)
        {
            int run = 1;
            while (i + run < expanded.Length && expanded[i + run] == expanded[i]) run++;
            if (run >= 4) text.Append(expanded[i]).Append('(').Append(run).Append(')');
            else text.Append(expanded, i, run);
            i += run;
        }
        return text.ToString();
    }
}
