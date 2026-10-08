// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// The member index (kb/Work PB2118 Draft 8, J2; DESIGN-architecture-review §8.7 "the file-set partition"): from the
/// census's own semantic walk (<see cref="ReferenceIndex"/>), every symbol's NAME mapped to the authored documents
/// whose code uses it, every census type's name mapped to the authored documents declaring it, and every authored
/// member, used or not, mapped to the documents declaring it. A restructuring
/// note's file set is computed from it (<c>scripts/arch/member_index.py</c>), so the callers a wave must edit in the
/// same change (CLAUDE.md rule 4) are measured, never hand-listed.
/// <para>Names the code generator writes as C# TEXT (string literals and interpolated text: <c>catch (ProgramReturn)</c>)
/// bind no symbol, so for every type and member of the EMITTED SURFACE (the assemblies generated programs call — the
/// census's own rule, <see cref="Reachability"/>'s "emitted-text") the index also maps the name to the authored
/// documents whose text writes its identifier (<c>emitted</c>; kb/Work PB2118 Draft 9, the eighth refuter's K1).</para>
/// <para>A name is the documentation id without its kind prefix (and a conversion's return type), so each overload keeps
/// its own entry; <c>scripts/arch/member_index.py</c> answers a bare <c>Ns.Type.Member</c> with every overload.</para>
/// </summary>
internal static class MemberIndex
{
    public static void Write(string path, ReferenceIndex index, TypeInventory inventory, IReadOnlySet<string> emittedSurface)
    {
        var uses = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach ((string key, HashSet<string> users) in index.FilesUsing)
        {
            if (NameOf(key) is not { } name)
            {
                continue;
            }

            if (!uses.TryGetValue(name, out SortedSet<string>? set))
            {
                uses[name] = set = new SortedSet<string>(StringComparer.Ordinal);
            }

            set.UnionWith(users);
        }

        var types = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (CensusType type in inventory.Types.Where(t => !t.Generated))
        {
            if (NameOf(type.Key) is { } name)
            {
                types[name] = new SortedSet<string>(
                    type.Partials.Where(p => !p.Document.Generated).Select(p => p.Document.RepoPath), StringComparer.Ordinal);
            }
        }

        // Every member of every census type (a generated type's too) by name, used or not: a Delete note names members
        // no code uses, and a name the index does not know must mean a typo or a rename, never "unused".
        var declared = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (CensusType type in inventory.Types)
        {
            var fileOf = type.Partials
                .GroupBy(p => p.Document.Tree).ToDictionary(g => g.Key, g => g.First().Document.RepoPath);
            foreach (ISymbol member in TypeInventory.AuthoredMembers(type.Symbol))
            {
                if (NameOf(SymbolKeys.Key(member) ?? "") is not { } name)
                {
                    continue;
                }

                if (!declared.TryGetValue(name, out SortedSet<string>? set))
                {
                    declared[name] = set = new SortedSet<string>(StringComparer.Ordinal);
                }

                set.UnionWith(member.Locations.Where(l => l.SourceTree is not null && fileOf.ContainsKey(l.SourceTree))
                    .Select(l => fileOf[l.SourceTree!]));
            }
        }

        // The emitted surface's names written as text: a type by its simple name, a member by its own.
        var emitted = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (CensusType type in inventory.Types.Where(t => emittedSurface.Contains(t.Project.Compilation.AssemblyName ?? "")))
        {
            AddEmitted(emitted, NameOf(type.Key), type.Symbol.Name, index);
            foreach (ISymbol member in TypeInventory.AuthoredMembers(type.Symbol))
            {
                AddEmitted(emitted, NameOf(SymbolKeys.Key(member) ?? ""), member.Name, index);
            }
        }

        var files = uses.Values.Concat(types.Values).Concat(declared.Values).Concat(emitted.Values).SelectMany(s => s).Distinct()
            .Order(StringComparer.Ordinal).ToList();
        var at = files.Select((f, i) => (f, i)).ToDictionary(x => x.f, x => x.i, StringComparer.Ordinal);
        using FileStream stream = File.Create(path);
        using var json = new Utf8JsonWriter(stream);
        json.WriteStartObject();
        json.WriteNumber("schema", 2);
        json.WriteStartArray("files");
        files.ForEach(json.WriteStringValue);
        json.WriteEndArray();
        WriteMap(json, "uses", uses, at);
        WriteMap(json, "types", types, at);
        WriteMap(json, "declared", declared, at);
        WriteMap(json, "emitted", emitted, at);
        json.WriteEndObject();
    }

    private static void AddEmitted(SortedDictionary<string, SortedSet<string>> emitted, string? name, string identifier,
        ReferenceIndex index)
    {
        if (name is null || !index.EmittedIn.TryGetValue(identifier, out HashSet<string>? writers))
        {
            return;
        }

        if (!emitted.TryGetValue(name, out SortedSet<string>? set))
        {
            emitted[name] = set = new SortedSet<string>(StringComparer.Ordinal);
        }

        set.UnionWith(writers);
    }

    private static void WriteMap(Utf8JsonWriter json, string property, SortedDictionary<string, SortedSet<string>> map,
        Dictionary<string, int> at)
    {
        json.WriteStartObject(property);
        foreach ((string name, SortedSet<string> paths) in map)
        {
            json.WriteStartArray(name);
            foreach (string f in paths)
            {
                json.WriteNumberValue(at[f]);
            }

            json.WriteEndArray();
        }

        json.WriteEndObject();
    }

    /// <summary><c>Assembly|M:Ns.Type.Member(System.Int32)</c> → <c>Ns.Type.Member</c>; null for a key with no id.</summary>
    private static string? NameOf(string key)
    {
        int bar = key.IndexOf('|', StringComparison.Ordinal);
        string id = bar < 0 ? key : key[(bar + 1)..];
        if (id.Length < 3 || id[1] != ':')
        {
            return null;
        }

        string name = id[2..];
        int end = name.IndexOf('~', StringComparison.Ordinal);   // a conversion operator's return type
        return end < 0 ? name : name[..end];
    }
}
