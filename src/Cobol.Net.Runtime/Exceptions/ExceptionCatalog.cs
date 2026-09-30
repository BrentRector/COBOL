// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>The fatality category of an exception condition (ISO/IEC 1989:2023 §14.6.13.1.6 Table 13, the
/// "Cat" column): nonfatal, fatal, or implementor-defined. A level-1/level-2 name has no category of its own
/// (only level-3 names carry exception status indicators — §14.6.13.1.1).</summary>
public enum EcFatality
{
    /// <summary>Nonfatal (Table 13 "NF") — handled per §14.6.13.1.4 (continue when unhandled).</summary>
    Nonfatal,
    /// <summary>Fatal — handled per §14.6.13.1.3 (abnormal run-unit termination when unhandled).</summary>
    Fatal,
    /// <summary>Implementor-defined ("Imp") — this implementation treats every raised EC-*-IMP as fatal
    /// (loud-failure doctrine, COBOLNET_DESIGN §1.4; the implementor latitude of §14.6.13.1.1).</summary>
    Imp,
    /// <summary>A level-1/level-2 hierarchy name — enables/selects only, never raised itself (§14.6.13.1.1:
    /// "Only the lowest level exception-names are associated with exception status indicators").</summary>
    Hierarchy,
}

/// <summary>One exception-name row (ISO §14.6.13.1.6 Table 13): the canonical name, its level (1/2/3), its
/// level-2 parent (level-3 rows only), its fatality, and the ISO edition that introduced it (the binder's D8-style
/// edition gate; 2002 = the EC model's introduction, 2023 = the VCR row-40/61 additions).</summary>
public readonly record struct EcInfo(string Name, int Level, string? Level2Parent, EcFatality Fatality, int IntroducedIn)
{
    /// <summary>Is a raise of this condition processed as FATAL (ISO §14.6.13.1.3) rather than nonfatal
    /// (§14.6.13.1.4)? Table 13's "F" rows, plus the "Imp" rows this implementation defines as fatal
    /// (<see cref="EcFatality.Imp"/>). The ONE spelling of that test — every emitter and binder that needs a
    /// condition's disposition asks here rather than re-deriving it from <see cref="Fatality"/>.</summary>
    public bool IsFatal => Fatality is EcFatality.Fatal or EcFatality.Imp;
}

/// <summary>
/// The exception-name catalog — the ONE machine form of ISO/IEC 1989:2023 §14.6.13.1.6 Table 13 plus the
/// three-level hierarchy of §14.6.13.1.1: level-1 <c>EC-ALL</c>; the level-2 family names; level-3 = level-2 +
/// suffix (only level-3 names carry exception status indicators). The catalog is NAME-keyed (not an enum): the
/// open-ended <c>EC-USER-*</c> family (§14.6.13.1.1 — always nonfatal, user-defined by mention) makes the NAME the
/// canonical identity of an EC; a closed enum would need a parallel name channel — two representations for one
/// concept (the singular-pattern rule). The implementor-defined <c>EC-IMP-*suffix</c> family is CLOSED: §14.6.13.1.1
/// leaves "the implementor [to define] the action to be taken, the fatality, and when any of these exceptions are
/// raised", and this implementation defines none (docs/CONFORMANCE.md DOC-A.1-99), so no EC-IMP-suffix name exists
/// to resolve (kb/Work PB1531). Compile-time consumers
/// (TURN expansion, RAISE/USE-F3 validation, edition gating) and the runtime (last-exception state, the generated
/// <c>__EcDispatch</c> selector's level-2 matching) share THIS table.
/// </summary>
public static class ExceptionCatalog
{
    /// <summary>The level-1 name (§14.6.13.1.1).</summary>
    public const string EcAll = "EC-ALL";

    /// <summary>ISO §13.18.34.4 GR6 b) 2's exception-name — <i>"the EC-I-O-LINAGE exception condition is set to
    /// exist"</i> (Table 13: Fatal). ⛔ It is the ONE EC-I-O level-3 name a rule NAMES OUTRIGHT rather than
    /// leaving to §9.1.13.1's status→EC correspondence, which has no entry for it, so the connector that detects
    /// the violation has to say the NAME (<c>FileConnector.IoConditionName</c>) and not only the status. Spelled
    /// once, here, because the raise site (<c>SequentialConnector</c>) and the mask
    /// (<see cref="IoMaskNames"/>) must agree on it exactly.</summary>
    public const string IoLinage = "EC-I-O-LINAGE";

    /// <summary>ISO §14.9.51.4 GR27 a)'s two exception-names — <i>"If the end-of-page condition was caused by the
    /// action in General rule 26a, the EC-I-O-EOP-OVERFLOW exception condition is set to exist. If the end-of-page
    /// condition was caused by the action in General rule 26b, the EC-I-O-EOP exception condition is set to
    /// exist."</i> (Table 13: both Nonfatal.) Named outright by the rule, like <see cref="IoLinage"/>, and riding a
    /// SUCCESSFUL status ("the WRITE statement is successful"), so §9.1.13.1's correspondence could never reach
    /// them; spelled once here for the raise site (<c>SequentialConnector.PositionOnLogicalPage</c>), the mask
    /// (<see cref="IoMaskNames"/>) and the hook's END-OF-PAGE suppression (<see cref="IsEndOfPage"/>) — kb/Work
    /// PB854.</summary>
    public const string IoEop = "EC-I-O-EOP";

    /// <summary>See <see cref="IoEop"/>.</summary>
    public const string IoEopOverflow = "EC-I-O-EOP-OVERFLOW";

    /// <summary>Is <paramref name="ecName"/> one of GR27 a)'s end-of-page conditions — the conditions a WRITE's
    /// own END-OF-PAGE phrase takes precedence over (§14.9.51.4 GR27 b)-d))?</summary>
    public static bool IsEndOfPage(string? ecName) =>
        string.Equals(ecName, IoEop, StringComparison.OrdinalIgnoreCase)
        || string.Equals(ecName, IoEopOverflow, StringComparison.OrdinalIgnoreCase);

    // ── The level-2 family names (§14.6.13.1.1, 23 names + EC-CONTINUE) ───────────────────────────────────────
    // NOTE: the §14.6.13.1.1 prose lists 23 level-2 names and omits EC-CONTINUE, yet Table 13 carries the
    // EC-CONTINUE family (EC-CONTINUE-IMP / EC-CONTINUE-LESS-THAN-ZERO, the 2023 CONTINUE AFTER addition —
    // VERSION_CHANGE_REFERENCE row 40). The table governs (it names the conditions); EC-CONTINUE is treated as a
    // level-2 name introduced in 2023.
    private static readonly string[] Level2Names =
    [
        "EC-ARGUMENT", "EC-BOUND", "EC-CONTINUE", "EC-DATA", "EC-EXTERNAL", "EC-FLOW", "EC-FUNCTION", "EC-I-O",
        "EC-IMP", "EC-LOCALE", "EC-MCS", "EC-OO", "EC-ORDER", "EC-OVERFLOW", "EC-PROGRAM", "EC-RANGE",
        "EC-RAISING", "EC-REPORT", "EC-SCREEN", "EC-SIZE", "EC-SORT-MERGE", "EC-STORAGE", "EC-USER", "EC-VALIDATE",
    ];

    private static readonly Dictionary<string, EcInfo> Table = Build();

    private static Dictionary<string, EcInfo> Build()
    {
        var t = new Dictionary<string, EcInfo>(StringComparer.OrdinalIgnoreCase);
        t[EcAll] = new EcInfo(EcAll, 1, null, EcFatality.Hierarchy, 2002);
        foreach (string l2 in Level2Names)
            t[l2] = new EcInfo(l2, 2, null, EcFatality.Hierarchy, l2 is "EC-CONTINUE" or "EC-MCS" or "EC-EXTERNAL" ? 2023 : 2002);

        // Level-3 rows — ISO §14.6.13.1.6 Table 13 in table order. IntroducedIn: 2002 = the EC model's
        // introduction edition; 2023 = the names VERSION_CHANGE_REFERENCE rows 40/61 record as new in 2023
        // (EC-MCS-*, EC-FLOW-APPLY-COMMIT/-COMMIT/-ROLLBACK, EC-CONTINUE-*, EC-EXTERNAL-*, EC-I-O-WARNING,
        // EC-I-O-RECORD-CONTENT). 2014 = the dynamic-capacity-table EC names (§8.5.1.9 is a COBOL-2014 feature):
        // EC-BOUND-OVERFLOW. (EC-BOUND-SET, the explicit-SET twin, is raised by CobolDynTable.SetCapacity — kb/Work
        // PB460; its IntroducedIn is unaudited and left at the model's 2002.) The IntroducedIn is observably inert where the enabling construct is itself
        // edition-gated (a dyn table cannot exist below 2014 to overflow), but the metadata is kept correct.
        void L3(string name, EcFatality f, int introduced = 2002) =>
            t[name] = new EcInfo(name, 3, Level2OfName(name), f, introduced);

        L3("EC-ARGUMENT-FUNCTION", EcFatality.Fatal);
        L3("EC-ARGUMENT-IMP", EcFatality.Imp);
        L3("EC-BOUND-FUNC-RET-VALUE", EcFatality.Nonfatal);
        L3("EC-BOUND-IMP", EcFatality.Imp);
        L3("EC-BOUND-ODO", EcFatality.Fatal);
        L3("EC-BOUND-OVERFLOW", EcFatality.Nonfatal, 2014);   // dynamic-capacity tables — §8.5.1.9 (COBOL-2014)
        L3("EC-BOUND-PTR", EcFatality.Fatal);
        L3("EC-BOUND-REF-MOD", EcFatality.Fatal);
        L3("EC-BOUND-SET", EcFatality.Nonfatal);
        L3("EC-BOUND-SUBSCRIPT", EcFatality.Fatal);
        L3("EC-BOUND-TABLE-LIMIT", EcFatality.Fatal);
        L3("EC-CONTINUE-IMP", EcFatality.Imp, 2023);
        L3("EC-CONTINUE-LESS-THAN-ZERO", EcFatality.Nonfatal, 2023);
        L3("EC-DATA-CONVERSION", EcFatality.Nonfatal);
        L3("EC-DATA-IMP", EcFatality.Imp);
        L3("EC-DATA-INCOMPATIBLE", EcFatality.Fatal);
        L3("EC-DATA-NOT-FINITE", EcFatality.Fatal);
        L3("EC-DATA-OVERFLOW", EcFatality.Fatal);
        L3("EC-DATA-PTR-NULL", EcFatality.Fatal);
        L3("EC-EXTERNAL-DATA-MISMATCH", EcFatality.Fatal, 2023);
        L3("EC-EXTERNAL-FILE-MISMATCH", EcFatality.Fatal, 2023);
        L3("EC-EXTERNAL-FORMAT-CONFLICT", EcFatality.Fatal, 2023);
        L3("EC-EXTERNAL-IMP", EcFatality.Imp, 2023);
        L3("EC-FLOW-APPLY-COMMIT", EcFatality.Fatal, 2023);
        L3("EC-FLOW-COMMIT", EcFatality.Fatal, 2023);
        L3("EC-FLOW-GLOBAL-EXIT", EcFatality.Fatal);
        L3("EC-FLOW-GLOBAL-GOBACK", EcFatality.Fatal);
        L3("EC-FLOW-IMP", EcFatality.Imp);
        L3("EC-FLOW-RELEASE", EcFatality.Fatal);
        L3("EC-FLOW-REPORT", EcFatality.Fatal);
        L3("EC-FLOW-RETURN", EcFatality.Fatal);
        L3("EC-FLOW-ROLLBACK", EcFatality.Fatal, 2023);
        L3("EC-FLOW-SEARCH", EcFatality.Fatal);
        L3("EC-FLOW-USE", EcFatality.Fatal);
        L3("EC-FUNCTION-ARG-OMITTED", EcFatality.Fatal);
        L3("EC-FUNCTION-IMP", EcFatality.Imp);
        L3("EC-FUNCTION-NOT-FOUND", EcFatality.Fatal);
        L3("EC-FUNCTION-PTR-INVALID", EcFatality.Fatal);
        L3("EC-FUNCTION-PTR-NULL", EcFatality.Fatal);
        L3("EC-I-O-AT-END", EcFatality.Nonfatal);
        L3(IoEop, EcFatality.Nonfatal);
        L3(IoEopOverflow, EcFatality.Nonfatal);
        L3("EC-I-O-FILE-SHARING", EcFatality.Nonfatal);
        L3("EC-I-O-IMP", EcFatality.Imp);
        L3("EC-I-O-INVALID-KEY", EcFatality.Nonfatal);
        L3(IoLinage, EcFatality.Fatal);
        L3("EC-I-O-LOGIC-ERROR", EcFatality.Fatal);
        L3("EC-I-O-PERMANENT-ERROR", EcFatality.Fatal);
        L3("EC-I-O-RECORD-CONTENT", EcFatality.Fatal, 2023);
        L3("EC-I-O-RECORD-OPERATION", EcFatality.Nonfatal);
        L3("EC-I-O-WARNING", EcFatality.Nonfatal, 2023);
        L3("EC-LOCALE-IMP", EcFatality.Imp);
        L3("EC-LOCALE-INCOMPATIBLE", EcFatality.Fatal);
        L3("EC-LOCALE-INVALID", EcFatality.Fatal);
        L3("EC-LOCALE-INVALID-PTR", EcFatality.Fatal);
        L3("EC-LOCALE-MISSING", EcFatality.Fatal);
        L3("EC-LOCALE-SIZE", EcFatality.Fatal);
        L3("EC-MCS-ABNORMAL-TERMINATION", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-IMP", EcFatality.Imp, 2023);
        L3("EC-MCS-INVALID-TAG", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-MESSAGE-LENGTH", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-NO-REQUESTER", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-NO-SERVER", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-NORMAL-TERMINATION", EcFatality.Nonfatal, 2023);
        L3("EC-MCS-REQUESTOR-FAILED", EcFatality.Nonfatal, 2023);
        L3("EC-OO-ARG-OMITTED", EcFatality.Fatal);
        L3("EC-OO-CONFORMANCE", EcFatality.Fatal);
        L3("EC-OO-EXCEPTION", EcFatality.Fatal);
        L3("EC-OO-IMP", EcFatality.Imp);
        L3("EC-OO-METHOD", EcFatality.Fatal);
        L3("EC-OO-NULL", EcFatality.Fatal);
        L3("EC-OO-RESOURCE", EcFatality.Fatal);
        L3("EC-OO-UNIVERSAL", EcFatality.Fatal);
        L3("EC-ORDER-IMP", EcFatality.Imp);
        L3("EC-ORDER-NOT-SUPPORTED", EcFatality.Fatal);
        L3("EC-OVERFLOW-IMP", EcFatality.Imp);
        L3("EC-OVERFLOW-STRING", EcFatality.Nonfatal);
        L3("EC-OVERFLOW-UNSTRING", EcFatality.Nonfatal);
        L3("EC-PROGRAM-ARG-MISMATCH", EcFatality.Fatal);
        L3("EC-PROGRAM-ARG-OMITTED", EcFatality.Fatal);
        L3("EC-PROGRAM-CANCEL-ACTIVE", EcFatality.Fatal);
        L3("EC-PROGRAM-IMP", EcFatality.Imp);
        L3("EC-PROGRAM-NOT-FOUND", EcFatality.Fatal);
        L3("EC-PROGRAM-PTR-NULL", EcFatality.Fatal);
        L3("EC-PROGRAM-RECURSIVE-CALL", EcFatality.Fatal);
        L3("EC-PROGRAM-RESOURCES", EcFatality.Fatal);
        L3("EC-RAISING-IMP", EcFatality.Imp);
        L3("EC-RAISING-NOT-SPECIFIED", EcFatality.Fatal);
        L3("EC-RANGE-IMP", EcFatality.Imp);
        L3("EC-RANGE-INDEX", EcFatality.Fatal);
        L3("EC-RANGE-INSPECT-SIZE", EcFatality.Fatal);
        L3("EC-RANGE-INVALID", EcFatality.Nonfatal);
        L3("EC-RANGE-PERFORM-VARYING", EcFatality.Fatal);
        L3("EC-RANGE-PTR", EcFatality.Fatal);
        L3("EC-RANGE-SEARCH-INDEX", EcFatality.Nonfatal);
        L3("EC-RANGE-SEARCH-NO-MATCH", EcFatality.Nonfatal);
        L3("EC-REPORT-ACTIVE", EcFatality.Fatal);
        L3("EC-REPORT-COLUMN-OVERLAP", EcFatality.Nonfatal);
        L3("EC-REPORT-FILE-MODE", EcFatality.Fatal);
        L3("EC-REPORT-IMP", EcFatality.Imp);
        L3("EC-REPORT-INACTIVE", EcFatality.Fatal);
        L3("EC-REPORT-LINE-OVERLAP", EcFatality.Nonfatal);
        L3("EC-REPORT-NOT-TERMINATED", EcFatality.Nonfatal);
        L3("EC-REPORT-PAGE-LIMIT", EcFatality.Nonfatal);
        L3("EC-REPORT-PAGE-WIDTH", EcFatality.Nonfatal);
        L3("EC-REPORT-SUM-SIZE", EcFatality.Fatal);
        L3("EC-REPORT-VARYING", EcFatality.Fatal);
        L3("EC-SCREEN-FIELD-OVERLAP", EcFatality.Nonfatal);
        L3("EC-SCREEN-IMP", EcFatality.Imp);
        L3("EC-SCREEN-ITEM-TRUNCATED", EcFatality.Nonfatal);
        L3("EC-SCREEN-LINE-NUMBER", EcFatality.Nonfatal);
        L3("EC-SCREEN-STARTING-COLUMN", EcFatality.Nonfatal);
        L3("EC-SIZE-ADDRESS", EcFatality.Fatal);
        L3("EC-SIZE-EXPONENTIATION", EcFatality.Fatal);
        L3("EC-SIZE-IMP", EcFatality.Imp);
        L3("EC-SIZE-OVERFLOW", EcFatality.Fatal);
        L3("EC-SIZE-TRUNCATION", EcFatality.Fatal);
        L3("EC-SIZE-UNDERFLOW", EcFatality.Fatal);
        L3("EC-SIZE-ZERO-DIVIDE", EcFatality.Fatal);
        L3("EC-SORT-MERGE-ACTIVE", EcFatality.Fatal);
        L3("EC-SORT-MERGE-FILE-OPEN", EcFatality.Fatal);
        L3("EC-SORT-MERGE-IMP", EcFatality.Imp);
        L3("EC-SORT-MERGE-RELEASE", EcFatality.Fatal);
        L3("EC-SORT-MERGE-RETURN", EcFatality.Fatal);
        L3("EC-SORT-MERGE-SEQUENCE", EcFatality.Fatal);
        L3("EC-STORAGE-IMP", EcFatality.Imp);
        L3("EC-STORAGE-NOT-ALLOC", EcFatality.Nonfatal);
        L3("EC-STORAGE-NOT-AVAIL", EcFatality.Nonfatal);
        // The EC-VALIDATE family is OBSOLETE in 2023 (Table 13 NOTE; VERSION_CHANGE_REFERENCE row 125).
        L3("EC-VALIDATE-CONTENT", EcFatality.Nonfatal);
        L3("EC-VALIDATE-FORMAT", EcFatality.Nonfatal);
        L3("EC-VALIDATE-IMP", EcFatality.Imp);
        L3("EC-VALIDATE-RELATION", EcFatality.Nonfatal);
        L3("EC-VALIDATE-VARYING", EcFatality.Fatal);
        return t;
    }

    /// <summary>Look up a name's catalog row. Handles the Table 13 fixed names AND the one open family: an
    /// <c>EC-USER-suffix</c> (always nonfatal — §14.6.13.1.1: "The name is defined by specifying it anywhere that an
    /// exception-name may be specified") with a valid suffix (basic letters/digits/hyphen/underscore, not ending in
    /// hyphen or underscore) resolves to a synthesized level-3 row. ⛔ An <c>EC-IMP-suffix</c> does NOT: it is
    /// defined by the IMPLEMENTOR, this implementation defines none (DOC-A.1-99), so it names no exception condition
    /// and returns false like any other unknown name (kb/Work PB1531). Unknown names return false.</summary>
    public static bool TryGet(string name, out EcInfo info)
    {
        if (Table.TryGetValue(name, out info)) return true;
        string upper = name.ToUpperInvariant();
        if (upper.StartsWith("EC-USER-", StringComparison.Ordinal) && ValidOpenSuffix(upper["EC-USER-".Length..]))
        {
            info = new EcInfo(upper, 3, "EC-USER", EcFatality.Nonfatal, 2002);
            return true;
        }
        return false;
    }

    /// <summary>Does <paramref name="name"/> have the FORM of an implementor-defined level-3 name — <c>EC-IMP-</c> then
    /// a valid suffix (§14.6.13.1.1)? A statement about SPELLING only, never about existence: no such name is
    /// catalogued (<see cref="TryGet"/> refuses it), but the standard still RESERVES the family in compiler
    /// directives (§8.12 — "all of the exception-names specified in 14.6.13.1", which lists EC-IMP-<i>suffix</i>), so
    /// <c>CompilerDirectiveWords.IsReserved</c> asks this.</summary>
    public static bool IsImplementorSuffixForm(string name) =>
        name.StartsWith("EC-IMP-", StringComparison.OrdinalIgnoreCase) && ValidOpenSuffix(name["EC-IMP-".Length..]);

    /// <summary>The §14.6.13.1.1 open-suffix character rule: basic letters, basic digits, hyphen and underscore;
    /// the hyphen or underscore shall not be the last character.</summary>
    private static bool ValidOpenSuffix(string suffix) =>
        suffix.Length > 0
        && suffix.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
        && suffix[^1] is not ('-' or '_');

    /// <summary>The level-2 parent of a level-3 exception-name (longest-prefix match over the level-2 family
    /// names — so <c>EC-I-O-AT-END</c> → <c>EC-I-O</c>, not <c>EC-IMP</c>; <c>EC-SORT-MERGE-ACTIVE</c> →
    /// <c>EC-SORT-MERGE</c>). Null when the name is not under any family. Used by the generated
    /// <c>__EcDispatch</c> selector's level-2 tiers (ISO §14.9.49.4 GR3d/GR3f).</summary>
    public static string? Level2OfName(string level3Name)
    {
        string? best = null;
        foreach (string l2 in Level2Names)
            if (level3Name.StartsWith(l2 + "-", StringComparison.OrdinalIgnoreCase)
                && (best is null || l2.Length > best.Length))
                best = l2;
        return best;
    }

    /// <summary>True when <paramref name="level3Name"/> falls under <paramref name="level2Name"/>
    /// (§14.9.49.4 GR3d/f selection; case-insensitive, longest-family-prefix discipline).</summary>
    public static bool UnderLevel2(string level3Name, string level2Name) =>
        string.Equals(Level2OfName(level3Name), level2Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when <paramref name="name"/> begins with the COBOL characters <c>EC-I-O</c>
    /// (the §14.9.49.3 SR13 / §7.3.25.3 SR4 file-association test).</summary>
    public static bool IsIoName(string name) => name.StartsWith("EC-I-O", StringComparison.OrdinalIgnoreCase);

    /// <summary>Does a <c>&gt;&gt;TURN</c> directive naming <paramref name="directiveName"/> cover level-3
    /// <paramref name="level3"/> — the §7.3.25.4 hierarchy expansion, written ONCE. EC-ALL covers every name
    /// except EC-I-O-WARNING (GR2); a level-2 name covers its level-3 children except EC-I-O-WARNING (GR3);
    /// EC-I-O-WARNING toggles only when named explicitly (GR4).
    /// <para>It lives in the CATALOG rather than in the compiler's <c>TurnState</c> because two folds ask it:
    /// the compile-time one (a statement whose raised name is known at bind) and the run-time one
    /// (<see cref="EcCheckingProfile"/> — §14.9.18.4 GR1 b) asks the ACTIVATING element about a name a callee
    /// chooses at run time). Two copies of an expansion rule is how the two answers would come to disagree
    /// (kb/Work PB408).</para></summary>
    public static bool DirectiveCovers(string directiveName, string level3)
    {
        if (directiveName.Equals(level3, StringComparison.OrdinalIgnoreCase)) return true;
        if (level3.Equals("EC-I-O-WARNING", StringComparison.OrdinalIgnoreCase)) return false;   // explicit only (GR4)
        if (directiveName.Equals(EcAll, StringComparison.OrdinalIgnoreCase)) return true;        // GR2
        return TryGet(directiveName, out var info) && info.Level == 2
            && UnderLevel2(level3, directiveName);                                               // GR3
    }

    /// <summary>All catalogued LEVEL-3 names (the >>TURN GR2/GR3 expansion universe). The open EC-USER-*
    /// family is not enumerable — TURN matching treats EC-ALL / a level-2 event as covering them by the
    /// hierarchy predicate instead of by expansion (same observable behavior, ISO §7.3.25.4 GR2/GR3).</summary>
    public static IEnumerable<EcInfo> Level3Rows => Table.Values.Where(i => i.Level == 3);

    /// <summary>The EC-I-O level-3 exception-name for an I-O status value (ISO §9.1.13.1): first digit 1→AT-END,
    /// 2→INVALID-KEY, 3→PERMANENT-ERROR, 4→LOGIC-ERROR, 5→RECORD-OPERATION, 6→FILE-SHARING, 7→RECORD-CONTENT,
    /// 9→IMP; '0x' with x≠'0' → EC-I-O-WARNING; '00' (clean success) → null (no exception condition).
    /// <para>⛔ THIS IS THE DEFAULT CORRESPONDENCE, NOT THE ONLY ANSWER. It is the map §9.1.13.1 prints, and it
    /// covers every condition the standard reaches THROUGH a status. A rule that NAMES its condition instead —
    /// §13.18.34.4 GR6 b) 2's EC-I-O-LINAGE is the one such EC-I-O rule — wins over it for that operation
    /// (§14.6.13.1.1: <i>"Unless otherwise specified, if more than one exception is detected during the execution
    /// of a statement, the one that is set to exist is undefined"</i> — the specific rule is the "otherwise
    /// specified"). The connector carries the named one on <c>FileConnector.IoConditionName</c> and
    /// <c>CobolFile.IoConditionName</c> is the ONE place the two are combined; never call this directly from a
    /// generated hook.</para></summary>
    public static string? IoEcOfStatus(string status)
    {
        if (status.Length < 2) return null;
        return status[0] switch
        {
            '0' => status[1] == '0' ? null : "EC-I-O-WARNING",
            '1' => "EC-I-O-AT-END",
            '2' => "EC-I-O-INVALID-KEY",
            '3' => "EC-I-O-PERMANENT-ERROR",
            '4' => "EC-I-O-LOGIC-ERROR",
            '5' => "EC-I-O-RECORD-OPERATION",
            '6' => "EC-I-O-FILE-SHARING",
            '7' => "EC-I-O-RECORD-CONTENT",
            '9' => "EC-I-O-IMP",
            _ => null,
        };
    }

    /// <summary>True when the I-O status value indicates a FATAL EC-I-O exception condition (ISO §9.1.13.1:
    /// "any that begin with the digit 3, 4, or 7, and any that begin with the digit 9 that the implementor
    /// defines as fatal" — this implementation defines 9x as fatal, the loud-failure doctrine).</summary>
    public static bool IsFatalIoStatus(string status) =>
        status.Length > 0 && status[0] is '3' or '4' or '7' or '9';

    /// <summary>The EC-I-O level-3 names a file connector can set to exist, in THE canonical mask-bit order — the
    /// compiler's per-statement enable mask (bit i = name i) and the generated <c>__IoCheckEc</c> consult this
    /// one order; never re-derive it elsewhere (singular-pattern rule). ⛔ ORDER IS APPEND-ONLY: the mask is a
    /// compile-time integer baked into the generated call and decoded at run time, so a reordering would make a
    /// program's enabled names mean a different set.
    /// <para>The first nine are §9.1.13.1's status-derived names (first digit → name). <c>EC-I-O-LINAGE</c> is
    /// NOT one of them — no I-O status value corresponds to it; §13.18.34.4 GR6 b) 2 names it directly — and it
    /// is in the mask for the same reason the others are: a program enables checking for it by name, per file,
    /// through <c>&gt;&gt;TURN</c>. <c>EC-I-O-EOP</c> and <c>EC-I-O-EOP-OVERFLOW</c> (§14.9.51.4 GR27 a)) are the
    /// same shape and were ABSENT until kb/Work PB854 — catalogued and never raisable, so a program enabling them
    /// got a zero mask bit and its declarative never ran. <c>TurnStateTests</c> now asserts that EVERY level-3
    /// EC-I-O row of the catalog has a bit here, so a name cannot be catalogued without one again.</para></summary>
    public static readonly string[] IoMaskNames =
    [
        "EC-I-O-AT-END", "EC-I-O-INVALID-KEY", "EC-I-O-PERMANENT-ERROR", "EC-I-O-LOGIC-ERROR",
        "EC-I-O-RECORD-OPERATION", "EC-I-O-FILE-SHARING", "EC-I-O-RECORD-CONTENT", "EC-I-O-IMP", "EC-I-O-WARNING",
        IoLinage, IoEop, IoEopOverflow,
    ];

    /// <summary>The mask bit of a connector-raisable EC-I-O name (0 for a name outside the mask set).</summary>
    public static int IoBit(string ecName)
    {
        for (int i = 0; i < IoMaskNames.Length; i++)
            if (IoMaskNames[i].Equals(ecName, StringComparison.OrdinalIgnoreCase)) return 1 << i;
        return 0;
    }
}
