// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// The architecture census's Roslyn host (kb/Work PB2115; DESIGN-architecture-review.md §3 R0). It loads a solution
/// through MSBuildWorkspace, measures it, and writes the facts as one JSON document for <c>scripts/arch/census.py</c>.
/// <para>Usage: <c>ArchCensus --solution &lt;sln&gt; --scope &lt;scope.json&gt; --out &lt;raw.json&gt;
/// [--min-tokens N]</c>, or <c>ArchCensus --solution &lt;sln&gt; --scope &lt;scope.json&gt; --member-index &lt;out.json&gt;</c>
/// for the member index alone (<see cref="MemberIndex"/>; the same load and walk, no reachability or clones). Exit 0
/// with the file written, 2 with a refusal on stderr (a partial solution is never measured).</para>
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // MSBuildLocator must register the SDK's MSBuild before any type that touches MSBuild is loaded, so the
        // workspace code lives in methods this one only calls after registration.
        MSBuildLocator.RegisterDefaults();
        try
        {
            Options options = Options.Parse(args);
            if (options.MemberIndex is { } indexPath)
            {
                await WriteMemberIndexAsync(options, indexPath);
                return 0;
            }

            RawCensus census = await MeasureAsync(options);
            await using FileStream output = File.Create(options.Out);
            await JsonSerializer.SerializeAsync(output, census, CensusJson.Default.RawCensus);
            return 0;
        }
        catch (CensusRefusal refusal)
        {
            await Console.Error.WriteLineAsync($"ArchCensus: REFUSED — {refusal.Message}");
            return 2;
        }
    }

    private static async Task WriteMemberIndexAsync(Options options, string indexPath)
    {
        CensusScope scope = JsonSerializer.Deserialize(await File.ReadAllTextAsync(options.Scope), CensusJson.Default.CensusScope)
                            ?? throw new CensusRefusal($"unreadable scope file {options.Scope}");
        using LoadedSolution solution = await LoadedSolution.OpenAsync(options.Solution, scope, Console.Out);
        var inventory = new TypeInventory(solution);
        ReferenceIndex index = Walk(solution, inventory, scope.EmittedSurface);

        MemberIndex.Write(indexPath, index, inventory, scope.EmittedSurface.ToHashSet(StringComparer.Ordinal));
        Console.WriteLine($"member index: walked {index.Documents} documents, {index.Recorded} uses -> {indexPath}");
    }

    /// <summary>The ONE semantic walk both modes share: every document of every census and reader project.</summary>
    private static ReferenceIndex Walk(LoadedSolution solution, TypeInventory inventory, IEnumerable<string> surface)
    {
        var index = new ReferenceIndex(inventory.Types.Select(t => t.Key), surface);
        bool userConversions = solution.Projects.SelectMany(p => p.Documents)
            .Any(d => d.Tree.GetRoot().DescendantNodes().Any(n => n.IsKind(SyntaxKind.ConversionOperatorDeclaration)));
        foreach (LoadedProject project in solution.Projects)
        {
            foreach (LoadedDocument document in project.Documents)
            {
                index.Walk(project, document, userConversions);
            }
        }

        return index;
    }

    private static async Task<RawCensus> MeasureAsync(Options options)
    {
        var clock = Stopwatch.StartNew();
        CensusScope scope = JsonSerializer.Deserialize(await File.ReadAllTextAsync(options.Scope), CensusJson.Default.CensusScope)
                            ?? throw new CensusRefusal($"unreadable scope file {options.Scope}");
        using LoadedSolution solution = await LoadedSolution.OpenAsync(options.Solution, scope, Console.Out);

        var inventory = new TypeInventory(solution);
        var surface = scope.EmittedSurface.ToHashSet(StringComparer.Ordinal);
        ReferenceIndex index = Walk(solution, inventory, surface);

        Console.WriteLine($"walked {index.Documents} documents, {index.Recorded} uses, {clock.Elapsed.TotalSeconds:F0}s");
        var reachability = new Reachability(solution, inventory, index, surface);
        IReadOnlyList<RawMember> members = await reachability.MembersAsync();
        IReadOnlyDictionary<string, string> typeReach = await reachability.TypesAsync();
        Console.WriteLine($"reachability: {members.Count} members judged, {reachability.WalkMisses.Count} walk misses, "
                          + $"{clock.Elapsed.TotalSeconds:F0}s");

        IReadOnlyList<RawCloneFamily> clones = CloneFamilies.Detect(
            solution.Census.SelectMany(p => p.Documents).Where(d => !d.Generated), options.MinTokens);

        return new RawCensus(
            solution.Paths.Relative(Path.GetFullPath(options.Solution)),
            [.. solution.Projects.Select(p => Project(p, solution, inventory))],
            [.. inventory.Types.Select(t => TypeFacts(t, index, inventory.Namespaces, typeReach)).OrderBy(t => t.Id, StringComparer.Ordinal)],
            NamespaceEdges(inventory, index),
            clones,
            options.MinTokens,
            members,
            Reachability.Rules,
            TestArtifacts.Scaffolds(solution, index),
            TestArtifacts.DriftLiterals(solution),
            new RawWalkStats(index.Documents, index.Recorded, reachability.Candidates,
                [.. reachability.WalkMisses.Order(StringComparer.Ordinal)], Math.Round(clock.Elapsed.TotalSeconds, 1)));
    }

    private static RawProject Project(LoadedProject project, LoadedSolution solution, TypeInventory inventory)
    {
        var references = project.Project.ProjectReferences
            .Select(r => solution.Solution.GetProject(r.ProjectId)?.Name)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();
        IReadOnlyList<string>? source = null;
        IReadOnlyList<string>? compiled = null;
        if (project.IsCensus)
        {
            source = [.. inventory.Types.Where(t => t.Project == project).Select(t => t.Id).Order(StringComparer.Ordinal)];
            compiled = CompiledPopulation.Read(project.Project.OutputFilePath
                                               ?? throw new CensusRefusal($"{project.Name} has no output path"));
        }

        return new RawProject(project.Name, solution.Paths.Relative(project.Project.FilePath!), project.Role,
            project.Compilation.AssemblyName ?? project.Name, project.Project.DefaultNamespace, references,
            project.Documents.Count, project.Documents.Count(d => d.Generated), source, compiled);
    }

    private static RawType TypeFacts(CensusType type, ReferenceIndex index, IReadOnlyDictionary<string, string> namespaces,
        IReadOnlyDictionary<string, string> typeReach)
    {
        INamedTypeSymbol symbol = type.Symbol;
        var partials = type.Partials
            .Select(p =>
            {
                FileLinePositionSpan span = p.Syntax.SyntaxTree.GetLineSpan(p.Syntax.Span);
                return new RawPartial(p.Document.RepoPath, span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1,
                    FolderNamespace(type.Project, p.Document), p.Document.Generated);
            })
            .OrderBy(p => p.File, StringComparer.Ordinal)
            .ThenBy(p => p.Start)
            .ToList();
        bool generated = type.Generated;
        int lines = partials.Where(p => generated || !p.Generated).Sum(p => p.End - p.Start + 1);
        var members = TypeInventory.AuthoredMembers(symbol).ToList();
        IReadOnlySet<string> dependsOn = index.DependenciesOf(type.Key);
        var fanOutNamespaces = dependsOn
            .GroupBy(k => namespaces[k], StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return new RawType(
            type.Id,
            type.Project.Name,
            TypeInventory.KindOf(symbol),
            symbol.ContainingNamespace?.ToDisplayString() ?? "",
            generated,
            partials,
            lines,
            members.Count,
            members.Count(m => m is IFieldSymbol),
            symbol.GetTypeMembers().Length,
            index.ProductionUsersOf(type.Key).Count,
            index.TestUsersOf(type.Key).Count,
            dependsOn.Count,
            fanOutNamespaces,
            [.. members.OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary)
                .Select(m => m.Name).Distinct().Order(StringComparer.Ordinal)],
            typeReach.TryGetValue(type.Key, out string? reach) ? reach : null);
    }

    /// <summary>The namespace a document's folder implies: the project's root namespace plus each folder below the
    /// project directory (MSBuild's own default-namespace rule). Null for a generated document.</summary>
    private static string? FolderNamespace(LoadedProject project, LoadedDocument document)
    {
        if (document.Generated)
        {
            return null;
        }

        var folders = document.ProjectPath.Split('/', '\\').SkipLast(1).ToList();
        string root = project.Project.DefaultNamespace ?? project.Name;
        return folders.Count == 0 ? root : $"{root}.{string.Join(".", folders)}";
    }

    private static IReadOnlyList<RawEdge> NamespaceEdges(TypeInventory inventory, ReferenceIndex index)
    {
        IReadOnlyDictionary<string, string> namespaces = inventory.Namespaces;
        var pairs = new Dictionary<(string From, string To), int>();
        foreach (CensusType from in inventory.Types.Where(t => !t.Generated))
        {
            foreach (string to in index.DependenciesOf(from.Key))
            {
                var edge = (namespaces[from.Key], namespaces[to]);
                if (edge.Item1 != edge.Item2)
                {
                    pairs[edge] = pairs.GetValueOrDefault(edge) + 1;
                }
            }
        }

        return [.. pairs.Select(p => new RawEdge(p.Key.From, p.Key.To, p.Value))
            .OrderBy(e => e.From, StringComparer.Ordinal).ThenBy(e => e.To, StringComparer.Ordinal)];
    }

    private sealed record Options(string Solution, string Scope, string Out, int MinTokens, string? MemberIndex)
    {
        public static Options Parse(string[] args)
        {
            string? solution = null, scope = null, output = null, memberIndex = null;
            int minTokens = 75;
            for (int i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new CensusRefusal($"{args[i]} needs a value");
                switch (args[i])
                {
                    case "--solution": solution = Next(); break;
                    case "--scope": scope = Next(); break;
                    case "--out": output = Next(); break;
                    case "--member-index": memberIndex = Next(); break;
                    case "--min-tokens": minTokens = int.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    default: throw new CensusRefusal($"unknown argument {args[i]}");
                }
            }

            return new Options(
                solution ?? throw new CensusRefusal("--solution is required"),
                scope ?? throw new CensusRefusal("--scope is required"),
                output ?? (memberIndex is null ? throw new CensusRefusal("--out or --member-index is required") : ""),
                minTokens,
                memberIndex);
        }
    }
}
