// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>The census of ONE source element behind ISO §8.3.2.2's "<i>Within a source element, a given user-defined
/// word may be used as only one type of user-defined word</i>": the type each word was first recorded as, and the
/// words already reported as two types. A program's binder is one source element and owns one census; a class
/// binder also hosts its METHODS, each a source element of its own (§10.5, "A source element is a source unit
/// excluding any contained source units"), so each method carries its own census on
/// <see cref="OoMethodDataScope.Words"/> (kb/Work PB2921: one shared census refused a METHOD-ID spelled like its
/// CLASS-ID, and a condition-name in one method spelled like a data-name in another, both COBOLNET2692 on legal
/// source). Each element's census is the one <see cref="Compiler.Oo.OoClassTable.CensusOf"/> keeps for it. Recorded
/// by <see cref="DataBinder.DeclareUserWord"/> for every declaration, and by
/// <see cref="Compiler.Oo.OoClassTable.RecordReferenceUse"/> for every reference to an object-class-name,
/// interface-name or parameter-name, including one another element declares (a method's class, a contained
/// program's container; kb/Work PB3132). Keyed case-insensitively, like every COBOL word.</summary>
internal sealed class UserWordCensus
{
    private readonly Dictionary<string, UserWordKind> _firstType = new(CobolNames.Comparer);
    private readonly HashSet<string> _conflictReported = new(CobolNames.Comparer);

    /// <summary>Record one declaration or one use of <paramref name="word"/> as <paramref name="kind"/>. §8.3.2.2 says
    /// "used": a word an element references as a type another element declares (the class-name a method INVOKEs, a
    /// class a contained program's container's REPOSITORY names) is used in the referring element as that type, so a
    /// METHOD-ID or a paragraph-name spelled like that class-name is two types in the referring source element
    /// (kb/Work PB3132), while the same word in an element that never references the class-name is legal (§8.4.6.1;
    /// kb/Work PB2921). Returns the type the word was FIRST recorded as when this record is a §8.3.2.2 conflict not
    /// yet reported; null for a first record, a pair <see cref="UserWordKinds.MayBeOneWord"/> admits (the same type
    /// twice included: a TYPE expansion re-registers a clone of its template's words, and a class-name is referenced
    /// as often as it is written), or a word already reported, so a third record does not repeat the refusal.</summary>
    public UserWordKind? Record(string word, UserWordKind kind)
    {
        if (_firstType.TryAdd(word, kind)) return null;
        var first = _firstType[word];
        return UserWordKinds.MayBeOneWord(first, kind) || !_conflictReported.Add(word) ? null : first;
    }

    /// <summary>THE one wording of a §8.3.2.2 conflict (COBOLNET2692), for the declaration funnel and the method
    /// reference record alike: <paramref name="first"/> is the type the word was first recorded as.</summary>
    public static string ConflictMessage(string word, UserWordKind first, UserWordKind second) =>
        $"'{word}' names two types of user-defined word in this source element ({first.Spelling()} and "
        + $"{second.Spelling()}) — a given user-defined word may be used as only one type of user-defined word "
        + "(ISO §8.3.2.2); the "
        + "exceptions are a compilation-variable-name, a level-number that matches a paragraph-name or "
        + "section-name, and the constant-name / data-name / property-name / record-key-name / record-name group";

    /// <summary>True when <paramref name="word"/> was first recorded in this source element as
    /// <paramref name="kind"/>.</summary>
    public bool IsDeclaredAs(string word, UserWordKind kind) =>
        _firstType.TryGetValue(word, out var declared) && declared == kind;
}
