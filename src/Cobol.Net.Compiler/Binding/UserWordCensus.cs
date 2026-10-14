// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>The census of ONE source element behind ISO §8.3.2.2's "<i>Within a source element, a given user-defined
/// word may be used as only one type of user-defined word</i>": the type each word was first declared as, and the
/// words already reported as two types. A program's binder is one source element and owns one census; a class
/// binder also hosts its METHODS, each a source element of its own (§10.5, "A source element is a source unit
/// excluding any contained source units"), so each method carries its own census on
/// <see cref="OoMethodDataScope.Words"/> (kb/Work PB2921: one shared census refused a METHOD-ID spelled like its
/// CLASS-ID, and a condition-name in one method spelled like a data-name in another, both COBOLNET2692 on legal
/// source). Asked only through <see cref="DataBinder.DeclareUserWord"/>. Keyed case-insensitively, like every COBOL
/// word.</summary>
internal sealed class UserWordCensus
{
    private readonly Dictionary<string, UserWordKind> _firstType = new(CobolNames.Comparer);
    private readonly HashSet<string> _conflictReported = new(CobolNames.Comparer);

    /// <summary>Record one declaration of <paramref name="word"/> as <paramref name="kind"/>. Returns the type the
    /// word was FIRST declared as when this declaration is a §8.3.2.2 conflict not yet reported; null for a first
    /// declaration, a pair <see cref="UserWordKinds.MayBeOneWord"/> admits (the same type twice included: a TYPE
    /// expansion re-registers a clone of its template's words), or a word already reported, so a third declaration
    /// does not repeat the refusal.</summary>
    public UserWordKind? Declare(string word, UserWordKind kind)
    {
        if (_firstType.TryAdd(word, kind)) return null;
        var first = _firstType[word];
        return UserWordKinds.MayBeOneWord(first, kind) || !_conflictReported.Add(word) ? null : first;
    }

    /// <summary>True when <paramref name="word"/> was first declared in this source element as
    /// <paramref name="kind"/>.</summary>
    public bool IsDeclaredAs(string word, UserWordKind kind) =>
        _firstType.TryGetValue(word, out var declared) && declared == kind;
}
