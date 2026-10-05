// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// One SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> clause (ISO/IEC 1989:2023
/// §12.3.7.2; DESIGN-locale-facility seam S1, increment T1): the locale-name (a user-defined word, §8.3.2.2) and the
/// EXTERNAL IDENTIFICATION it references — and nothing more. §8.1.5: "When a locale is specified, the associated
/// ordering is determined at runtime", and §12.3.7.4 GR5 leaves the allowable external-locale-names and literal
/// content to the implementor (DETERMINATION L1: a locale tag, POSIX spellings normalized by
/// <see cref="LocaleIdentification.Normalize"/>) — so the compiler never resolves the locale: the symbol carries the
/// normalized <see cref="Tag"/> the generated code hands the runtime, and availability (EC-LOCALE-MISSING) is decided
/// at the point of use. Scope is the declaring source unit and every directly or indirectly contained one
/// (§12.3.7.4 GR1), through the SPECIAL-NAMES configuration inheritance.
/// </summary>
/// <param name="Name">The locale-name as declared (case-insensitive; stored as written).</param>
/// <param name="External">The external identification as written — the external-locale-name word or the DECODED literal-4.</param>
/// <param name="FromLiteral">True when the identification came from literal-4 (the <c>"fr_FR.UTF-8"</c> branch), false for an external-locale-name word.</param>
public sealed record LocaleSymbol(string Name, string External, bool FromLiteral)
{
    /// <summary>The L1-normalized locale tag (<c>fr_FR.UTF-8</c> → <c>fr-FR</c>; <c>de@phonebook</c> → <c>de-u-co-phonebook</c>;
    /// <c>INVARIANT</c> → "") — what the generated code carries and the runtime resolves at use.</summary>
    public string Tag { get; } = LocaleIdentification.Normalize(External);

    /// <summary>Do two symbols carry "the same external identification" (ISO §8.5.3.1 "Additionally", §9.3.8.2.3 rule 3
    /// "Additionally", §14.8.2.3.2 "Additionally" a), §14.8.3.3 "Additionally" 1))? The identification AS WRITTEN,
    /// through the ONE rule <see cref="LocaleIdentification.SameExternalIdentification"/>, which the run-time
    /// universal-dispatch relation asks too (kb/Work PB480). The locale-NAME never enters: two locale-names bound to the
    /// same identification match.</summary>
    public bool SameExternalIdentificationAs(LocaleSymbol other) =>
        LocaleIdentification.SameExternalIdentification(External, FromLiteral, other.External, other.FromLiteral);

    public override string ToString() => $"LOCALE {Name} IS {(FromLiteral ? $"\"{External}\"" : External)} → {(Tag.Length == 0 ? "root" : Tag)}";
}

/// <summary>One locale-phrase of the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.6.2 —
/// <c>locale-name-n | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT</c>; §12.3.6.4 GR5; kb/Work PB64 T5): which locale's LC_CTYPE
/// classifies the class's characters. <see cref="Kind"/> <see cref="LocalePhraseKind.Named"/> carries the SPECIAL-NAMES
/// symbol (§12.3.6.3 SR3); the other kinds resolve at the module's activation (GR8; §14.6.6 r2).</summary>
public sealed record LocalePhrase(CobolNet.Runtime.Globalization.LocalePhraseKind Kind, LocaleSymbol? Symbol)
{
    /// <summary>The named locale's L1 tag, or null.</summary>
    public string? Tag => Symbol?.Tag;

    public override string ToString() => Kind switch
    {
        CobolNet.Runtime.Globalization.LocalePhraseKind.Named => $"locale {Symbol!.Name} ({Symbol.Tag})",
        CobolNet.Runtime.Globalization.LocalePhraseKind.Current => "LOCALE",
        CobolNet.Runtime.Globalization.LocalePhraseKind.SystemDefault => "SYSTEM-DEFAULT",
        CobolNet.Runtime.Globalization.LocalePhraseKind.UserDefault => "USER-DEFAULT",
        _ => "(coded character set)",
    };
}

/// <summary>The CHARACTER CLASSIFICATION clause of a source unit (ISO §12.3.6.2; kb/Work PB64 T5): the alphanumeric
/// and national locale-phrases — either may be absent (§12.3.6.4 GR5 e/j: the coded character set's classification);
/// inherited by contained units (§12.3.6.4 GR1). The emitter resolves it at each activation of the module
/// (<c>CharacterClassification.Resolve</c> — GR8 / §14.6.6 r2).</summary>
public sealed record ClassificationSpec(LocalePhrase? Alphanumeric, LocalePhrase? National)
{
    public override string ToString() => $"CHARACTER CLASSIFICATION alphanumeric: {Alphanumeric?.ToString() ?? "-"}; national: {National?.ToString() ?? "-"}";
}

/// <summary>
/// The ONE "which locale" operand of every locale consumer (DESIGN-locale-facility seam S2 / §4.2): a NAMED locale
/// (a <see cref="LocaleSymbol"/> — the <c>IS LOCALE locale-name-2</c> alphabet, the LOCALE phrase of an intrinsic or
/// PICTURE) or the locale CURRENT at use (<see cref="Current"/> — the phrase without a name: §12.3.7.4 GR7e "otherwise
/// by the locale that is current at the time the collating sequence is used at runtime", and §14.6.6 r4–r8's
/// "if a locale-name is specified … otherwise the current locale", stated once).
/// </summary>
public readonly record struct LocaleRef(LocaleSymbol? Named)
{
    /// <summary>The locale current at each use.</summary>
    public static LocaleRef Current { get; } = new((LocaleSymbol?)null);

    /// <summary>True for the current-locale form.</summary>
    public bool IsCurrent => Named is null;

    /// <summary>The normalized tag of a named reference, or null for the current form.</summary>
    public string? Tag => Named?.Tag;

    /// <summary>The LOCALE-phrase half of the "same PICTURE clause" rules: "both specify the LOCALE phrase without
    /// a locale-name or both specify the LOCALE phrase with the same external identification" (ISO §8.5.3.1,
    /// §9.3.8.2.3 rule 3, §14.8.2.3.2, §14.8.3.3 — each clause's "Additionally" sentence) — <see cref="LocaleSymbol.SameExternalIdentificationAs"/>.</summary>
    public bool SameIdentificationAs(LocaleRef other) =>
        IsCurrent ? other.IsCurrent : !other.IsCurrent && Named!.SameExternalIdentificationAs(other.Named!);

    public override string ToString() => Named is null ? "LOCALE (current)" : $"LOCALE {Named.Name} ({Named.Tag})";
}

/// <summary>A PICTURE format 2 item's locale-edit identity (ISO §13.18.40.2; kb/Work PB64 T6): the LOCALE
/// phrase's named-else-current locale (§13.18.40.5 r11 — resolved at each edit/de-edit, never at bind), the SIZE
/// phrase's integer-1 (§13.18.40.4 GR17 — the item's ONLY size input), and the canonical expanded
/// character-string-1 (uppercased, repeats unrolled, the currency symbol canonicalized to <c>$</c> — the shape
/// <c>CobolLocaleEdit</c> parses: symbols <c>+ $ Z 9 .</c> only). Type equivalence for two such items is the SAME
/// SIZE phrase, the same character-string and the same locale identity — both current, or the same external
/// identification AS WRITTEN (§8.5.3.1 "Additionally"; <see cref="LocaleRef.SameIdentificationAs"/>). The record's
/// equality IS that rule (it is what the §8.5.3.1 profile compare reads through <see cref="PicInfo"/>'s own
/// equality), so it is written out: the synthesized member-wise equality would compare the locale-NAME too and
/// refuse two locale-names bound to one identification.</summary>
public sealed record LocaleEditSpec(LocaleRef Locale, int Size, string Picture)
{
    /// <inheritdoc/>
    public bool Equals(LocaleEditSpec? other) =>
        other is not null && Size == other.Size && string.Equals(Picture, other.Picture, StringComparison.Ordinal)
        && Locale.SameIdentificationAs(other.Locale);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Size, Picture, Locale.IsCurrent);
}
