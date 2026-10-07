// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CobolNet.Tools.ArchCensus;

/// <summary>The census and reader projects of one solution, loaded and compiled, with every document classified.</summary>
internal sealed class LoadedSolution : IDisposable
{
    public const string CensusRole = "census";
    public const string ReaderRole = "reader";
    public const string ExcludedRole = "excluded";

    // The workspace outlives the load: SymbolFinder's confirmation queries run against its solution afterwards.
    private readonly MSBuildWorkspace workspace;

    private LoadedSolution(MSBuildWorkspace workspace, RepoPaths paths, Solution solution,
        IReadOnlyList<LoadedProject> projects)
    {
        this.workspace = workspace;
        Paths = paths;
        Solution = solution;
        Projects = projects;
    }

    public RepoPaths Paths { get; }

    public Solution Solution { get; }

    public IReadOnlyList<LoadedProject> Projects { get; }

    public IEnumerable<LoadedProject> Census => Projects.Where(p => p.IsCensus);

    public void Dispose() => workspace.Dispose();

    /// <summary>
    /// Opens the solution and refuses, rather than measures, a partial one: a workspace FAILURE (a project that did
    /// not load), a solution project the scope does not name, a scope name the solution lacks, and a compilation
    /// with errors (whose unbound names would read as missing references) each end the run.
    /// </summary>
    public static async Task<LoadedSolution> OpenAsync(string slnPath, CensusScope scope, TextWriter log)
    {
        var paths = new RepoPaths(Path.GetDirectoryName(Path.GetFullPath(slnPath))!);
        var workspace = MSBuildWorkspace.Create();
        try
        {
            return await LoadAsync(workspace, paths, slnPath, scope, log);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static async Task<LoadedSolution> LoadAsync(MSBuildWorkspace workspace, RepoPaths paths, string slnPath,
        CensusScope scope, TextWriter log)
    {
        var failures = new List<string>();
        workspace.RegisterWorkspaceFailedHandler(e =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                failures.Add(e.Diagnostic.Message);
            }
            else
            {
                log.WriteLine($"[workspace warning] {e.Diagnostic.Message}");
            }
        });
        Solution solution = await workspace.OpenSolutionAsync(slnPath);
        if (failures.Count > 0)
        {
            throw new CensusRefusal("the workspace failed to load the solution:\n  " + string.Join("\n  ", failures));
        }

        Dictionary<string, string> roles = Partition(scope, solution);
        var projects = new List<LoadedProject>();
        foreach (Project project in solution.Projects.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            string role = roles[project.Name];
            if (role == ExcludedRole)
            {
                continue;
            }

            Compilation compilation = await project.GetCompilationAsync()
                ?? throw new CensusRefusal($"{project.Name}: the workspace produced no compilation");
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Take(5).ToList();
            if (errors.Count > 0)
            {
                throw new CensusRefusal($"{project.Name} does not compile in the workspace (build the solution first):\n  "
                                        + string.Join("\n  ", errors));
            }

            string projectDir = Path.GetDirectoryName(project.FilePath!)!;
            var documents = new List<LoadedDocument>();
            foreach (Document document in project.Documents)
            {
                SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
                string fromProject = Path.GetRelativePath(projectDir, document.FilePath!);
                documents.Add(new LoadedDocument(tree, paths.Relative(document.FilePath!), fromProject,
                    GeneratedCode.IsGenerated(fromProject, tree)));
            }

            foreach (SourceGeneratedDocument document in await project.GetSourceGeneratedDocumentsAsync())
            {
                SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
                documents.Add(new LoadedDocument(tree, $"(source-generated)/{project.Name}/{document.HintName}",
                    document.HintName, Generated: true));
            }

            projects.Add(new LoadedProject(project, role, compilation, projectDir, documents));
            log.WriteLine($"loaded {project.Name} ({role}): {documents.Count} documents");
        }

        return new LoadedSolution(workspace, paths, solution, projects);
    }

    /// <summary>Every solution project in exactly one role, or a refusal naming the ones that are not.</summary>
    private static Dictionary<string, string> Partition(CensusScope scope, Solution solution)
    {
        var named = scope.Census.Select(n => (Name: n, Role: CensusRole))
            .Concat(scope.Readers.Select(n => (Name: n, Role: ReaderRole)))
            .Concat(scope.Excluded.Select(n => (Name: n, Role: ExcludedRole)))
            .ToList();
        var twice = named.GroupBy(x => x.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        var inSolution = solution.Projects.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var unnamed = inSolution.Where(n => named.All(x => x.Name != n)).Order(StringComparer.Ordinal).ToList();
        var absent = named.Select(x => x.Name).Where(n => !inSolution.Contains(n)).Order(StringComparer.Ordinal).ToList();
        if (twice.Count > 0 || unnamed.Count > 0 || absent.Count > 0)
        {
            throw new CensusRefusal(
                $"the scope does not partition the solution's projects: unnamed [{string.Join(", ", unnamed)}], " +
                $"absent from the solution [{string.Join(", ", absent)}], named twice [{string.Join(", ", twice)}]");
        }

        return named.ToDictionary(x => x.Name, x => x.Role, StringComparer.Ordinal);
    }
}

/// <summary>Repository-relative paths, forward-slashed on every OS, so a record reads the same from Windows and Linux.</summary>
internal sealed class RepoPaths(string root)
{
    public string Root { get; } = root;

    public string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');
}

/// <summary>One loaded project: its scope role, its compilation, and its documents (source-generated included).</summary>
internal sealed class LoadedProject(Project project, string role, Compilation compilation, string directory,
    IReadOnlyList<LoadedDocument> documents)
{
    public Project Project { get; } = project;

    public string Name => Project.Name;

    public string Role { get; } = role;

    public Compilation Compilation { get; } = compilation;

    public string Directory { get; } = directory;

    public IReadOnlyList<LoadedDocument> Documents { get; } = documents;

    public bool IsCensus => Role == LoadedSolution.CensusRole;
}

/// <summary>One document: the compilation's own tree, its repository-relative path, and whether a tool wrote it.</summary>
internal sealed record LoadedDocument(SyntaxTree Tree, string RepoPath, string ProjectPath, bool Generated);

/// <summary>A refusal to measure: the census exits non-zero with the reason instead of writing a partial record.</summary>
internal sealed class CensusRefusal(string message) : Exception(message);
