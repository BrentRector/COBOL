// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>The locale categories of ISO/IEC 1989:2023 §8.2.1 / §14.9.39 (SET LOCALE): LC_ALL names every category.</summary>
public enum LocaleCategory
{
    All = 0,
    Collate = 1,
    Ctype = 2,
    Messages = 3,
    Monetary = 4,
    Numeric = 5,
    Time = 6,
}

/// <summary>A SET of locale categories — the operand shape of SET LOCALE format 11 (ISO §14.9.39.2: the LC_ brace
/// carries choice indicators, so "one or more of the alternatives … each at most once … in any order" per §5.2.6.4;
/// DESIGN-locale-facility §4.3 — a scalar category would reject legal source). LC_ALL is every category
/// (§8.2.1's table: WiseOwl COBOL's locale exposes exactly these six).</summary>
[Flags]
public enum LocaleCategorySet
{
    None = 0,
    Collate = 1 << 0,
    Ctype = 1 << 1,
    Messages = 1 << 2,
    Monetary = 1 << 3,
    Numeric = 1 << 4,
    Time = 1 << 5,
    All = Collate | Ctype | Messages | Monetary | Numeric | Time,
}

/// <summary>
/// A LOCALE as COBOL sees it: a value per category (ISO §8.2.1 — "a locale specifies a set of cultural elements"
/// grouped into categories). A locale identified by ONE external identification has the same tag in every slot
/// (<see cref="Of"/>); a SAVED locale (SET format 12, §14.9.39.4 GR26) is the per-category snapshot of the state it
/// was taken from, which may name different locales per category (after a SET of LC_TIME alone). Immutable.
/// Tags are BCP-47 / CLDR ("es-ES", "fr", ""); "" is the root.
/// </summary>
public sealed record LocaleValue(string Collate, string Ctype, string Messages, string Monetary, string Numeric, string Time)
{
    /// <summary>The locale identified by one tag — every category the same.</summary>
    public static LocaleValue Of(string tag)
    {
        tag ??= "";
        return new(tag, tag, tag, tag, tag, tag);
    }

    /// <summary>The root locale in every category.</summary>
    public static LocaleValue Root { get; } = Of("");

    /// <summary>The tag of one category (LC_ALL → the LC_COLLATE tag).</summary>
    public string this[LocaleCategory category] => category switch
    {
        LocaleCategory.Ctype => Ctype,
        LocaleCategory.Messages => Messages,
        LocaleCategory.Monetary => Monetary,
        LocaleCategory.Numeric => Numeric,
        LocaleCategory.Time => Time,
        _ => Collate,
    };

    /// <summary>This value with the categories in <paramref name="categories"/> taken from <paramref name="from"/>
    /// (§14.6.6 r3: "the current locale remains unchanged for categories that are not switched").</summary>
    public LocaleValue With(LocaleCategorySet categories, LocaleValue from) => new(
        categories.HasFlag(LocaleCategorySet.Collate) ? from.Collate : Collate,
        categories.HasFlag(LocaleCategorySet.Ctype) ? from.Ctype : Ctype,
        categories.HasFlag(LocaleCategorySet.Messages) ? from.Messages : Messages,
        categories.HasFlag(LocaleCategorySet.Monetary) ? from.Monetary : Monetary,
        categories.HasFlag(LocaleCategorySet.Numeric) ? from.Numeric : Numeric,
        categories.HasFlag(LocaleCategorySet.Time) ? from.Time : Time);

    /// <summary>True when every category names the same locale.</summary>
    public bool IsUniform => Collate == Ctype && Collate == Messages && Collate == Monetary && Collate == Numeric && Collate == Time;

    public override string ToString() => IsUniform ? (Collate.Length == 0 ? "root" : Collate)
        : $"collate={Collate} ctype={Ctype} messages={Messages} monetary={Monetary} numeric={Numeric} time={Time}";
}

/// <summary>
/// The external identification of a locale (ISO §12.3.7.4 GR5 — "locale-name-1 references a locale identified by
/// external-locale-name-1 or the value of literal-4. The implementor specifies the allowable external-locale-names
/// and the allowable content of literal-4"; §8.3.2.3.7 — "An external-locale-name identifies a locale that
/// specifies a set of cultural elements. This locale is provided in the operating environment").
/// <para><b>⚖ DETERMINATION L1 (DESIGN-locale-facility §4.1):</b> the identification is a locale TAG — a BCP-47 tag
/// (<c>fr-FR</c>, <c>de-CH</c>, <c>ja-JP</c>, <c>zh-Hant</c>), which since PB105 is every CLDR locale, or the
/// invariant/root locale spelled <c>INVARIANT</c> / <c>ROOT</c> / <c>C</c> / <c>POSIX</c>. A POSIX spelling is
/// NORMALIZED before lookup: <c>ll_CC[.codeset][@modifier]</c> → <c>ll-CC</c>; the <c>.codeset</c> suffix is
/// ignored (the repertoire is UTF-16 — D-N1 — so a codeset cannot change it); an <c>@modifier</c> that is a CLDR
/// collation type (<c>@phonebook</c>, <c>@pinyin</c>, <c>@stroke</c>, <c>@trad</c> …) becomes the BCP-47 extension
/// <c>-u-co-&lt;type&gt;</c>, any other modifier makes the locale unavailable (EC-LOCALE-MISSING at use).
/// <c>fr_FR</c>, <c>fr_FR.UTF-8</c> and <c>fr-FR</c> therefore SELECT the same locale — which matters because
/// <c>fr_FR</c> is a legal COBOL word (§8.3.2.1 admits the underscore) and appears in the external-locale-name
/// branch while <c>"fr_FR.UTF-8"</c> appears in the literal branch. ⚠ Selecting the same locale is not having
/// "the same external identification": §8.5.3.1 and the §9.3.8.2.3 / §14.8.2.3.2 / §14.8.3.3 PICTURE-clause
/// rules define that identification as "the external-locale-name or literal value" itself, so those compare the
/// identification as WRITTEN (the compiler's <c>LocaleSymbol.SameExternalIdentificationAs</c>, kb/Work PB1166) and
/// never this normalization.
/// Availability is a RUN-TIME property (§8.1.5 — the ordering "is determined at runtime"): the compiler never
/// resolves a tag; <see cref="IsAvailable"/> is the one rule, <see cref="CollationEngine.IsKnownLocale"/>.</para>
/// </summary>
public static class LocaleIdentification
{
    /// <summary>Do two LOCALE phrases carry "the same external identification, where the external identification is
    /// the external-locale-name or literal value associated with a locale-name in the LOCALE clause" (ISO §8.5.3.1
    /// "Additionally", §9.3.6 match rule 3 e) 3. b., §9.3.8.2.3 rule 3 "Additionally", §14.8.2.3.2 "Additionally" a),
    /// §14.8.3.3 "Additionally" 1))? The identification AS WRITTEN — never the locale <see cref="Normalize"/> resolves it
    /// to (kb/Work PB1166): <c>"en-US"</c> and <c>"en_US.UTF-8"</c> select one locale but are two literal values. A
    /// literal value compares character for character; an external-locale-name is a COBOL word, and COBOL basic letters
    /// outside a literal are case-insensitive (§8.1.3.2 GR3 a)), so any pair involving one compares without regard to
    /// case. The ONE copy of the rule: the compiler's <c>LocaleSymbol</c> and the run-time universal-dispatch relation
    /// (<see cref="ActivationRelations"/>, kb/Work PB480) both ask it.</summary>
    public static bool SameExternalIdentification(string a, bool aFromLiteral, string b, bool bFromLiteral) =>
        aFromLiteral && bFromLiteral ? string.Equals(a, b, StringComparison.Ordinal) : CobolNames.Same(a, b);

    /// <summary>Normalize an external identification to its locale tag (L1). Never throws; an unusable spelling
    /// comes back as given (lower-cased) and is simply not available.</summary>
    public static string Normalize(string? external)
    {
        string s = (external ?? "").Trim();
        if (s.Length == 0) return "";
        if (s.Equals("INVARIANT", StringComparison.OrdinalIgnoreCase) || s.Equals("ROOT", StringComparison.OrdinalIgnoreCase)
            || s.Equals("C", StringComparison.OrdinalIgnoreCase) || s.Equals("POSIX", StringComparison.OrdinalIgnoreCase)
            || s.Equals("und", StringComparison.OrdinalIgnoreCase))
            return "";
        string? modifier = null;
        int at = s.IndexOf('@');
        if (at >= 0) { modifier = s[(at + 1)..].Trim(); s = s[..at]; }
        int dot = s.IndexOf('.');
        if (dot >= 0) s = s[..dot];                     // the codeset suffix: ignored (UTF-16 repertoire)
        s = s.Replace('_', '-');
        if (modifier is { Length: > 0 })
        {
            // A CLDR collation type modifier (bcp47/collation.xml's `co` key — name or alias) → the -u-co- extension,
            // APPENDED to any -u- keys the tag already carries ("de-u-kf-upper@phonebook" → "de-u-kf-upper-co-phonebook").
            // Any other modifier stays in the tag as written — no locale of that name exists, so the locale is
            // unavailable (EC-LOCALE-MISSING at use), never silently the plain locale.
            if (!Collation.Cldr.CldrLocaleLoader.IsCollationType(modifier)) return s + "@" + modifier;
            string type = Collation.Cldr.CldrCollation.CanonicalType(modifier);
            s = s.Contains("-u-", StringComparison.OrdinalIgnoreCase) ? s + "-co-" + type : s + "-u-co-" + type;
        }
        return s;
    }

    /// <summary>Is the locale available in this operating environment (§14.9.39.4 GR24 — "If the locale specified by
    /// locale-name-1 is not available, the EC-LOCALE-MISSING exception condition is set to exist")? The ONE rule
    /// (<see cref="CollationEngine.IsKnownLocale"/>): the root; a locale with CLDR collation data of its own; a
    /// site <c>.tailor</c>; a culture .NET recognizes. A <c>-u-</c> extension is allowed.</summary>
    public static bool IsAvailable(string? tag) => CollationEngine.IsKnownLocale(Normalize(tag));
}

/// <summary>
/// The run unit's LOCALE state (DESIGN-locale-facility §4.3; ISO §8.2.1 "the current locale", §14.6.6): the two
/// implementor defaults, the locale currently in effect per category, and the saved-locale handles of SET format 12.
/// Owned by <see cref="RunUnit"/> — one per run unit, established at activation, NEVER per activation frame:
/// §14.6.6 r9 — "Upon return of control from another COBOL runtime element, the locale in effect for each locale
/// category at the time of exit from the returning runtime element becomes the current locale for that category" —
/// so a callee's SET is not unwound (the callee restores, per the NOTE). Nothing else may hold current-locale state
/// (one mechanism; <c>LocaleManager</c> and the <c>IS LOCALE</c> collating sequence read THIS).
/// <para><b>⚖ DETERMINATION L2 (owner decision Q2, 2026-08-18):</b> the USER default locale is the environment variable
/// <c>COBOL_USER_LOCALE</c>, else the process's <see cref="CultureInfo.CurrentCulture"/>, else the root; the SYSTEM
/// default is <c>COBOL_SYSTEM_LOCALE</c>, else <see cref="CultureInfo.InstalledUICulture"/>, else the root. Both are
/// read ONCE, when the state is created (L3 — a foreign <c>CurrentCulture</c> switch is never observed; §8.2.1:
/// a default switched by a non-COBOL module "is not utilized by COBOL unless a SET statement is executed"). §8.2.1:
/// "The capability of setting the system default locale from COBOL is not provided" — <see cref="SystemDefault"/>
/// has no setter; the USER default is set by SET LOCALE USER-DEFAULT TO … (§14.9.39.4 GR22).</para>
/// <para><b>The state model</b> (§14.6.6): r1 — at activation the user default becomes current for every category
/// (a COPY: a later change of the user default does not move categories already current); r3 — a SET switches only
/// the named categories; §14.9.39.4 GR25 — each stays until another SET names it. Values are per-category
/// (<see cref="LocaleValue"/>) because a SAVED locale (GR26) snapshots a state whose categories may differ.</para>
/// <para><b>⚖ DETERMINATION L4 — the saved-locale pointer is a managed handle:</b> SET format 12 allocates a
/// <see cref="SavedLocalePointer"/> (an immutable snapshot, owned by THIS state, numbered monotonically) and stores it
/// in the data-pointer; format 11 through a pointer accepts only a live handle of this run unit's state — NULL, an
/// <c>ADDRESS OF</c> pointer or another run unit's handle is EC-LOCALE-INVALID-PTR and the statement is unsuccessful
/// (GR21). Invariant 1: no address into a byte image.</para>
/// </summary>
public sealed class LocaleState
{
    /// <summary>The environment variable naming the user default locale (Q2).</summary>
    public const string UserDefaultVariable = "COBOL_USER_LOCALE";

    /// <summary>The environment variable naming the system default locale (Q2).</summary>
    public const string SystemDefaultVariable = "COBOL_SYSTEM_LOCALE";

    private LocaleValue _current;
    private long _nextHandle = 1;

    public LocaleState()
    {
        UserDefault = LocaleValue.Of(Determine(UserDefaultVariable, () => CultureInfo.CurrentCulture));
        SystemDefault = LocaleValue.Of(Determine(SystemDefaultVariable, () => CultureInfo.InstalledUICulture));
        _current = UserDefault;   // §14.6.6 r1 — the user default becomes the current locale for all categories
    }

    /// <summary>The user default locale (L2), fixed at run-unit activation unless a SET LOCALE USER-DEFAULT TO …
    /// changes it (§14.9.39.4 GR22).</summary>
    public LocaleValue UserDefault { get; private set; }

    /// <summary>The system default locale (L2), fixed at run-unit activation; not settable from COBOL (§8.2.1).</summary>
    public LocaleValue SystemDefault { get; }

    /// <summary>The locale currently in effect, per category.</summary>
    public LocaleValue CurrentLocale => _current;

    /// <summary>The locale currently in effect for <paramref name="category"/> (LC_ALL → the LC_COLLATE tag).</summary>
    public string Current(LocaleCategory category) => _current[category];

    // ── SET LOCALE format 11 (§14.9.39.4 GR22–GR25) ─────────────────────────────────────────────────────────────

    /// <summary><c>SET LOCALE categories TO locale-name</c> (GR23a with locale-name-1): the named locale becomes
    /// current for the categories. GR24 — an unavailable locale sets EC-LOCALE-MISSING (fatal when checking is on;
    /// the state is unchanged either way). <paramref name="external"/> is the SPECIAL-NAMES locale-name's external
    /// identification (any spelling L1 accepts).</summary>
    public void SetFromLocale(LocaleCategorySet categories, string external)
    {
        string tag = LocaleIdentification.Normalize(external);
        if (!LocaleIdentification.IsAvailable(tag))
        {
            ExceptionState.LocaleMissingError($"SET LOCALE: the locale '{external}' is not available in this operating environment (ISO §14.9.39.4 GR24)");
            return;
        }
        _current = _current.With(categories, LocaleValue.Of(tag));
    }

    /// <summary><c>SET LOCALE categories TO identifier</c> (GR23a with identifier-10 — a saved locale): the categories
    /// are taken from the saved snapshot. GR21 — a pointer that does not reference saved locale information of THIS
    /// run unit is EC-LOCALE-INVALID-PTR and the statement is unsuccessful.</summary>
    public void SetFromSaved(LocaleCategorySet categories, ManagedPointer? pointer)
    {
        if (!TryResolveSaved(pointer, "SET LOCALE", out var saved)) return;
        _current = _current.With(categories, saved.Value);
    }

    /// <summary><c>SET LOCALE categories TO USER-DEFAULT</c> (GR23b): the categories are taken from the user default.</summary>
    public void SetFromUserDefault(LocaleCategorySet categories) => _current = _current.With(categories, UserDefault);

    /// <summary><c>SET LOCALE categories TO SYSTEM-DEFAULT</c> (GR23c): the categories are taken from the system default.</summary>
    public void SetFromSystemDefault(LocaleCategorySet categories) => _current = _current.With(categories, SystemDefault);

    /// <summary><c>SET LOCALE USER-DEFAULT TO locale-name</c> (GR22): the user default becomes the named locale.
    /// GR24 applies (EC-LOCALE-MISSING; unchanged).</summary>
    public void SetUserDefaultFromLocale(string external)
    {
        string tag = LocaleIdentification.Normalize(external);
        if (!LocaleIdentification.IsAvailable(tag))
        {
            ExceptionState.LocaleMissingError($"SET LOCALE USER-DEFAULT: the locale '{external}' is not available in this operating environment (ISO §14.9.39.4 GR24)");
            return;
        }
        UserDefault = LocaleValue.Of(tag);
    }

    /// <summary><c>SET LOCALE USER-DEFAULT TO identifier</c> (GR22 with a saved locale): the user default becomes the
    /// saved snapshot. GR21 applies.</summary>
    public void SetUserDefaultFromSaved(ManagedPointer? pointer)
    {
        if (!TryResolveSaved(pointer, "SET LOCALE USER-DEFAULT", out var saved)) return;
        UserDefault = saved.Value;
    }

    // ── SET format 12 (§14.9.39.4 GR26 / GR27) ────────────────────────────────────────────────────────────────────

    /// <summary><c>SET identifier TO LOCALE LC_ALL</c> (GR26 — "the current locale is saved and a reference to the
    /// saved locale is placed into the pointer data item") / <c>… TO LOCALE USER-DEFAULT</c> (GR27 — the user
    /// default is saved): a NEW handle over an immutable snapshot, owned by this state.</summary>
    public SavedLocalePointer Save(bool userDefault) =>
        new(this, _nextHandle++, userDefault ? UserDefault : _current);

    /// <summary>Is <paramref name="pointer"/> a live saved-locale handle of THIS run unit (GR21's "saved locale
    /// information")?</summary>
    public bool IsSavedLocale(ManagedPointer? pointer) => pointer is SavedLocalePointer sp && ReferenceEquals(sp.Owner, this);

    private bool TryResolveSaved(ManagedPointer? pointer, string statement, out SavedLocalePointer saved)
    {
        if (pointer is SavedLocalePointer sp && ReferenceEquals(sp.Owner, this))
        {
            saved = sp;
            return true;
        }
        saved = null!;
        string what = pointer is null || pointer.IsNull ? "NULL"
            : pointer is SavedLocalePointer ? "a saved locale of another run unit"
            : "a data address, not saved locale information";
        ExceptionState.LocaleInvalidPtrError($"{statement}: the pointer does not reference saved locale information of this run unit ({what}) — ISO §14.9.39.4 GR21");
        return false;
    }

    // ── host conveniences (LocaleManager; tests) — the same state, no second store ───────────────────────────

    /// <summary>Make <paramref name="localeTag"/> current for <paramref name="category"/> (LC_ALL: every category);
    /// null restores the user default for it. The host-side twin of SET LOCALE (no availability check — the host
    /// validated; <see cref="SetFromLocale"/> is the COBOL statement's path).</summary>
    public void Set(LocaleCategory category, string? localeTag)
    {
        var cats = ToSet(category);
        _current = _current.With(cats, localeTag is null ? UserDefault : LocaleValue.Of(LocaleIdentification.Normalize(localeTag)));
    }

    /// <summary>The category set one <see cref="LocaleCategory"/> names (LC_ALL → every category).</summary>
    public static LocaleCategorySet ToSet(LocaleCategory category) => category switch
    {
        LocaleCategory.Collate => LocaleCategorySet.Collate,
        LocaleCategory.Ctype => LocaleCategorySet.Ctype,
        LocaleCategory.Messages => LocaleCategorySet.Messages,
        LocaleCategory.Monetary => LocaleCategorySet.Monetary,
        LocaleCategory.Numeric => LocaleCategorySet.Numeric,
        LocaleCategory.Time => LocaleCategorySet.Time,
        _ => LocaleCategorySet.All,
    };

    /// <summary>The L2 determination: the variable when set (trimmed, L1-normalized; "" means root), else the
    /// culture's name.</summary>
    internal static string Determine(string variable, Func<CultureInfo> fallback)
    {
        string? env = Environment.GetEnvironmentVariable(variable);
        if (env is not null) return LocaleIdentification.Normalize(env);
        try { return fallback().Name; }
        catch (Exception) { return ""; }   // a host without culture data (invariant globalization): the root
    }
}

/// <summary>
/// A saved-locale HANDLE (ISO §14.9.39.4 GR26/GR27; DESIGN-locale-facility DETERMINATION L4): the value SET format
/// 12 places into a data-pointer — an immutable per-category snapshot owned by one run unit's
/// <see cref="LocaleState"/>, numbered monotonically (handles are never reused, so a stale or foreign one is
/// detectable rather than aliasing). A data-pointer holding one is neither NULL nor an address: dereferencing it as
/// a data address is EC-BOUND-PTR (<see cref="CobolPtr.Deref"/> — "does not address data storage"), and only SET
/// format 11 reads it (GR21).
/// </summary>
public sealed class SavedLocalePointer : ManagedPointer
{
    internal SavedLocalePointer(LocaleState owner, long handle, LocaleValue value)
    {
        Owner = owner;
        Handle = handle;
        Value = value;
    }

    /// <summary>The run unit's locale state the handle belongs to.</summary>
    public LocaleState Owner { get; }

    /// <summary>The handle number (per run unit, monotonic).</summary>
    public long Handle { get; }

    /// <summary>The saved locale — every category as it was when saved.</summary>
    public LocaleValue Value { get; }

    public override string ToString() => $"saved-locale #{Handle} ({Value})";
}
