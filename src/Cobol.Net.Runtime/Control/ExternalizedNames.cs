// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// THE formation and mapping rules of a name externalized to the operating environment — ISO §8.3.2.2 2): "The
/// implementor defines the formation and mapping rules of these names." Documented as DOC-A.1-68 in
/// <c>docs/CONFORMANCE.md</c> §7, and written ONCE here so that the side that REGISTERS a name (a PROGRAM-ID /
/// FUNCTION-ID / CLASS-ID / INTERFACE-ID / METHOD-ID / REPOSITORY / EXTERNAL <c>AS literal</c>, formed at bind
/// time) and the side that LOOKS ONE UP (a CALL / CANCEL / program- or function-address target, an INVOKE method
/// name, an EXTERNAL item's shared key, formed at run time) can never disagree again (kb/Work PB1539: the AS
/// literal was registered untrimmed while every CALL target was trimmed, so <c>PROGRAM-ID. S AS "trail  "</c>
/// could not be reached even by <c>CALL "trail  "</c>).
/// <list type="bullet">
/// <item><b>Formation</b> (<see cref="Form"/>): the name is the literal's or the identifier's content with its
/// LEADING AND TRAILING SPACES removed; spaces inside the name are kept. Only the space character is removed —
/// the national space of this implementation's UTF-16 repertoire is the same U+0020 — never other white space,
/// because a tab or a no-break space is a character of the name, not padding.</item>
/// <item><b>Mapping</b> (<see cref="Same"/>, <see cref="Comparer"/>): two externalized names are the same name
/// when they are the same COBOL word — equal under the ONE Annex C fold, <see cref="CobolNames"/> (§8.1.3.2 GR3 b)
/// and GR4 b); DOC-A.1-219, kb/Work PB1402) — so <c>CALL "sub-p"</c> reaches <c>PROGRAM-ID. SUB-P</c> and
/// <c>CALL "prüf"</c> reaches <c>PROGRAM-ID. PRÜF</c>, by the same equality the compiler's name tables use.
/// §8.3.2.2 2) then makes every source element that names the same externalized name refer to the same
/// instance.</item>
/// </list>
/// </summary>
public static class ExternalizedNames
{
    /// <summary>Form an externalized name from a written literal's content or an identifier's value: leading
    /// and trailing spaces removed (DOC-A.1-68). A null value forms the empty name, which names nothing.</summary>
    public static string Form(string? written) => (written ?? "").Trim(' ');

    /// <summary>True when <paramref name="written"/> is not already in its formed shape — the one question a
    /// bind-time screen asks before warning that an AS literal's spaces are not part of the name.</summary>
    public static bool HasFormationSpaces(string written) =>
        written.Length > 0 && (written[0] == ' ' || written[^1] == ' ');

    /// <summary>The mapping rule as an equality: the Annex C fold. Two nulls (two NULL addresses) are the same; a
    /// null and a name are not.</summary>
    public static bool Same(string? a, string? b) => CobolNames.Same(a, b);

    /// <summary>The mapping rule as a key comparer, for every table keyed by an externalized name.</summary>
    public static StringComparer Comparer => CobolNames.Comparer;
}
