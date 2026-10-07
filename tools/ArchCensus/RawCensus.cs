// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Serialization;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// The host's whole output: compiler FACTS about the solution, written once as JSON for
/// <c>scripts/arch/census.py</c>, which owns every POLICY over them (the population check, the god-class threshold,
/// the dead-artifact queries, the committed record and its findings). Nothing here is committed as-is.
/// Paths are repository-relative with forward slashes; type ids are full metadata names
/// (<c>Namespace.Outer+Nested`1</c>), so they match what the built assembly's metadata calls them.
/// </summary>
internal sealed record RawCensus(
    string Solution,
    IReadOnlyList<RawProject> Projects,
    IReadOnlyList<RawType> Types,
    IReadOnlyList<RawEdge> NamespaceEdges,
    IReadOnlyList<RawCloneFamily> Clones,
    int CloneMinTokens,
    IReadOnlyList<RawMember> Members,
    IReadOnlyDictionary<string, string> ReachRules,
    IReadOnlyList<RawScaffold> TestScaffolds,
    IReadOnlyList<RawLiteral> DriftLiterals,
    RawWalkStats Walk);

/// <summary>One solution project. <c>Role</c> is the scope the caller assigned (census or reader); the two type
/// lists are the population check's halves, read by two independent readers (Roslyn's symbols and the built
/// assembly's metadata). A reader project carries neither.</summary>
internal sealed record RawProject(
    string Name,
    string Path,
    string Role,
    string AssemblyName,
    string? DefaultNamespace,
    IReadOnlyList<string> ProjectReferences,
    int Documents,
    int GeneratedDocuments,
    IReadOnlyList<string>? SourceTypes,
    IReadOnlyList<string>? CompiledTypes);

/// <summary>One partial declaration of a type: its file, its line span, and the namespace its folder implies.</summary>
internal sealed record RawPartial(string File, int Start, int End, string? FolderNamespace, bool Generated);

/// <summary>One named type of a census project, every partial included.</summary>
internal sealed record RawType(
    string Id,
    string Project,
    string Kind,
    string Namespace,
    bool Generated,
    IReadOnlyList<RawPartial> Partials,
    int Lines,
    int Members,
    int Fields,
    int NestedTypes,
    int FanIn,
    int FanInTests,
    int FanOut,
    IReadOnlyDictionary<string, int> FanOutNamespaces,
    IReadOnlyList<string> MethodNames,
    string? Reach);

/// <summary>A namespace-to-namespace dependency: <c>Pairs</c> distinct (from-type, to-type) pairs.</summary>
internal sealed record RawEdge(string From, string To, int Pairs);

/// <summary>A type-2 clone family: bodies whose token-kind streams are identical once identifiers and literals
/// are canonicalized.</summary>
internal sealed record RawCloneFamily(int Tokens, IReadOnlyList<RawCloneInstance> Instances);

internal sealed record RawCloneInstance(string File, int Start, int End, string Member);

/// <summary>A census member that production code does not reference (outside its own body). <c>Reach</c> says
/// why: <c>unreferenced</c>, <c>test-only</c>, or the name of the stated exclusion rule that keeps it out of the
/// unreachable count (<see cref="Reachability"/>). <c>Family</c> is the dispatch root it belongs to.</summary>
internal sealed record RawMember(
    string Id,
    string Type,
    string Project,
    string Kind,
    string File,
    int Line,
    int Lines,
    string Reach,
    string? Family);

/// <summary>A test-project type with no test method and no reference from outside itself.</summary>
internal sealed record RawScaffold(string Id, IReadOnlyList<string> Projects, string File, int Line, int Lines);

/// <summary>A string a drift test names: a literal, or the path a <c>TestRepo</c> call with literal arguments
/// builds. <c>Kind</c> says how the test uses it: <c>path</c> — a repository path built through <c>TestRepo</c>;
/// <c>list</c> — an entry of a static field's collection (an exemption or allow list, which names real things);
/// <c>other</c> — anything else, typically a fixture the test writes itself, which names nothing in the tree.</summary>
internal sealed record RawLiteral(string File, int Line, string Text, string Kind);

/// <summary>How the reference walk went: what it visited, and how often the independent confirmation
/// (<c>SymbolFinder</c>) found a reference the walk had missed — the walk's measured blind spot.</summary>
internal sealed record RawWalkStats(
    int Documents,
    int ReferencesRecorded,
    int Candidates,
    IReadOnlyList<string> WalkMisses,
    double Seconds);

/// <summary>The scope the caller assigns: every solution project is exactly one of census, reader or excluded,
/// and the host refuses a solution with a project the scope does not name (a skipped project is never silent).
/// <c>EmittedSurface</c> names the assemblies generated programs call (their members are reached by emitted text).</summary>
internal sealed record CensusScope(
    IReadOnlyList<string> Census,
    IReadOnlyList<string> Readers,
    IReadOnlyList<string> Excluded,
    IReadOnlyList<string> EmittedSurface);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RawCensus))]
[JsonSerializable(typeof(CensusScope))]
internal sealed partial class CensusJson : JsonSerializerContext;
