// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>⛔ ONE REGISTRY OF NAMES A CONFIGURATION SECTION DECLARES, WHOSE EVERY RESOLUTION IS A §8.3.2.2 USE
/// (kb/Work PB3153). The types of user-defined word ISO §8.4.6.1 lists as "<i>declared within a configuration
/// section</i>" (alphabet-name, class-name, a SPECIAL-NAMES condition-name, dynamic-length-structure-name,
/// locale-name, mnemonic-name, ordering-name, symbolic-character) "<i>may be referenced only by statements and
/// entries either in that source element that contains a configuration section or in any source element contained
/// within that source element</i>", so a contained program, and a method of a class, resolves names that no
/// declaration of its own announced (§12.3.4 GR1 hands them down; <see cref="DataBinder.InheritConfiguration"/>).
/// §8.3.2.2 says "<i>Within a source element, a given user-defined word may be used as only one type of
/// user-defined word</i>": the element that writes <c>IF X IS DIGS</c> over an inherited class DIGS uses DIGS as a
/// class-name, and a paragraph DIGS of its own is then a second type in that element (refused COBOLNET2692 like the
/// flat program that declares both). The declaration funnel never saw such a use, because the name is declared in
/// ANOTHER element.
/// <para>So a lookup here is one of two named operations, and nothing else reads the entries: <see cref="TryUse"/>
/// resolves a reference the binder COMMITS to (the word stands in this type's position) and records it in the
/// referring element's census, through the one recorder the table is built with
/// (<c>DataBinder.RecordConfigurationNameUse</c>); <see cref="Declares"/> and <see cref="TryPeek"/> read without
/// recording, for what is no use of the word: a declaration's own duplicate check, a disambiguation predicate whose
/// caller resolves afterwards, an emitter's read. There is no <c>TryGetValue</c>, so a new consumer cannot resolve a
/// reference without choosing.</para></summary>
internal sealed class ConfigurationNames<T>(UserWordKind kind,
    Action<Antlr4.Runtime.RuleContext?, string, UserWordKind> recordUse)
{
    private readonly Dictionary<string, T> _entries = new(CobolNames.Comparer);

    public int Count => _entries.Count;

    /// <summary>The declared names, in declaration order (a diagnostic's "declared:" list).</summary>
    public IEnumerable<string> Names => _entries.Keys;

    /// <summary>Declare <paramref name="name"/>; false (the first declaration kept) when it is already declared.</summary>
    public bool TryAdd(string name, T value) => _entries.TryAdd(name, value);

    /// <summary>Declare <paramref name="name"/>, replacing an INHERITED entry of the same name.</summary>
    public void Set(string name, T value) => _entries[name] = value;

    /// <summary>Resolve a REFERENCE to <paramref name="name"/> in this type's position, and record it as a use of the
    /// word as this table's type in the census of the source element <paramref name="site"/> lies in (§8.3.2.2).
    /// False, recording nothing, when no entry is named so.</summary>
    public bool TryUse(Antlr4.Runtime.RuleContext? site, string name, [MaybeNullWhen(false)] out T value)
    {
        if (!_entries.TryGetValue(name, out value)) return false;
        recordUse(site, name, kind);
        return true;
    }

    /// <summary>Is <paramref name="name"/> declared? Records nothing: the question a disambiguation or a declaration
    /// asks, never a reference's resolution (that is <see cref="TryUse"/>).</summary>
    public bool Declares(string name) => _entries.ContainsKey(name);

    /// <summary>The entry named <paramref name="name"/>, read WITHOUT recording a use: for a declaration's own
    /// duplicate check and for a predicate whose caller resolves the reference afterwards through
    /// <see cref="TryUse"/>.</summary>
    public bool TryPeek(string name, [MaybeNullWhen(false)] out T value) => _entries.TryGetValue(name, out value);

    /// <summary>Take every entry of a CONTAINING element's table this one does not declare (§12.3.4 GR1; the
    /// containee's own declaration, where one is possible, wins).</summary>
    public void InheritFrom(ConfigurationNames<T> container)
    {
        foreach (var (k, v) in container._entries) _entries.TryAdd(k, v);
    }
}
