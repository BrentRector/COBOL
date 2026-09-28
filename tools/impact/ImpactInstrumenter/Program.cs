// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace CobolNet.Impact.Instrumenter;

/// <summary>
/// The impact recorder's IL rewriter (kb/Work PB1683; docs/rearchitecture/DESIGN-test-build-ci.md §3.13).
///
/// <para>Given the bin directories of a RECORDING build (one whose assemblies carry the internal
/// <c>CobolNet.Impact.ImpactProbe</c> — tools/impact/ImpactRecording.targets), it rewrites every covered assembly
/// ONCE and copies the result over every copy of it in those directories:</para>
/// <list type="bullet">
/// <item>every method with sequence points calls <c>ImpactProbe.Hit(id)</c> on entry, where <c>id</c> is that
/// METHOD's entry in the probe table: its source file(s) and the line span its sequence points cover there. A
/// method-level map is what makes the derived gate selective — measured on trains 65–69, a FILE-level map selected
/// the whole Conformance assembly for 20 of 22 branches, because every golden executes the binder, the emitter and
/// the preprocessor files, while it executes only the few methods of them its constructs need;</item>
/// <item>a method with NO sequence points (an auto-property accessor, a record's synthesized member) is charged to
/// a TYPE-LEVEL entry carrying the type's files and no line span, so a change to its declaration falls back to the
/// file level instead of reading as free;</item>
/// <item>the table's <c>implies</c> relation names, for each entry, the static constructors of its type and every
/// enclosing type: a static field initializer runs once, in whichever test touches the type first, and every later
/// test depends on what it built, so a change to one selects every test that touched the type;</item>
/// <item>every <c>Process.Start</c> is redirected to the probe's twin, which hands the child a hits file and queues
/// it on the starting test's context — how a compiled COBOL program's runtime work reaches the test that ran it.</item>
/// </list>
/// <para>Ids are assigned in a deterministic order (assemblies by name, then metadata order), so one build always
/// yields one table.</para>
/// </summary>
internal static class Program
{
    private const string ProbeType = "CobolNet.Impact.ImpactProbe";
    private const string ImpactNamespace = "CobolNet.Impact";

    private static int Main(string[] args)
    {
        string? repo = null, table = null;
        var bins = new List<string>();
        var names = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--repo": repo = Path.GetFullPath(args[++i]); break;
                case "--table": table = args[++i]; break;
                case "--assembly": names.Add(args[++i]); break;
                default: bins.Add(Path.GetFullPath(args[i])); break;
            }
        }

        if (repo is null || table is null || bins.Count == 0 || names.Count == 0)
        {
            Console.Error.WriteLine(
                "usage: ImpactInstrumenter --repo <root> --table <probes.json> --assembly <name>... <bin-dir>...");
            return 2;
        }

        var probes = new ProbeTable(repo);
        var report = new List<object>();
        foreach (string name in names.Order(StringComparer.Ordinal))
        {
            string[] copies = [.. bins.Select(b => Path.Combine(b, name + ".dll")).Where(File.Exists)];
            if (copies.Length == 0)
            {
                Console.Error.WriteLine($"⛔ {name}.dll is in none of the bin directories — nothing to instrument");
                return 1;
            }

            // ⛔ Every copy must be the SAME build: an id means one method only if one assembly carries it.
            string[] hashes = [.. copies.Select(c => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(c))))];
            if (hashes.Distinct().Count() != 1)
            {
                Console.Error.WriteLine($"⛔ the copies of {name}.dll differ — rebuild before instrumenting:\n  "
                                        + string.Join("\n  ", copies));
                return 1;
            }

            var (stats, error) = Instrument(copies[0], probes);
            if (error is not null)
            {
                Console.Error.WriteLine($"⛔ {name}: {error}");
                return 1;
            }

            foreach (string copy in copies.Skip(1))
            {
                File.Copy(copies[0], copy, overwrite: true);
                string pdb = Path.ChangeExtension(copies[0], ".pdb");
                if (File.Exists(pdb))
                {
                    File.Copy(pdb, Path.ChangeExtension(copy, ".pdb"), overwrite: true);
                }
            }

            report.Add(new { assembly = name, sha256 = hashes[0], copies = copies.Length, stats.Methods,
                stats.Charged, stats.ProcessStarts });
            Console.WriteLine($"  {name}: {stats.Methods} methods probed ({stats.Charged} charged to their type), "
                              + $"{stats.ProcessStarts} Process.Start redirected, {copies.Length} copies");
        }

        File.WriteAllText(table, JsonSerializer.Serialize(new
        {
            entries = probes.Entries,
            implies = probes.Implies,
            assemblies = report,
        }));
        Console.WriteLine($"probe table: {probes.Entries.Count} entries → {table}");
        return 0;
    }

    private sealed record Stats(int Methods, int Charged, int ProcessStarts);

    private static (Stats stats, string? error) Instrument(string path, ProbeTable probes)
    {
        string pdb = Path.ChangeExtension(path, ".pdb");
        bool symbols = File.Exists(pdb);
        using var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(path)!);
        using var module = ModuleDefinition.ReadModule(path, new ReaderParameters
        {
            ReadSymbols = symbols,
            InMemory = true,
            AssemblyResolver = resolver,
        });
        if (!symbols || !module.HasSymbols)
        {
            return (new Stats(0, 0, 0), "no portable PDB beside the assembly — a probe needs sequence points");
        }

        TypeDefinition? probe = module.GetType(ProbeType);
        if (probe is null)
        {
            return (new Stats(0, 0, 0),
                $"{ProbeType} is not compiled in — this is not a recording build (ImpactRecording.targets)");
        }

        MethodDefinition hit = probe.Methods.Single(m => m.Name == "Hit");
        var starts = new Dictionary<string, MethodDefinition>(StringComparer.Ordinal);
        foreach (var m in probe.Methods.Where(m => m.Name is "Start" or "StartInstance"))
        {
            starts[m.Name + "(" + string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) + ")"] = m;
        }

        // Pass 1: an entry per method (or per type, for the members with no sequence points).
        var types = module.GetTypes().Where(t => !InImpactNamespace(t)).ToList();
        var owners = Owners(types);
        var ids = new Dictionary<MethodDefinition, int>();
        var cctorIds = new Dictionary<TypeDefinition, int>();
        int charged = 0;
        foreach (TypeDefinition type in types)
        {
            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                var segments = Segments(method);
                int id;
                if (segments.Count > 0)
                {
                    id = probes.Method(method.FullName, Ranges(method, owners));
                }
                else
                {
                    var docs = TypeDocuments(type);
                    if (docs.Count == 0)
                    {
                        continue; // no source anywhere in the chain: generated scaffolding with no file to charge
                    }

                    id = probes.TypeLevel(type.FullName, docs);
                    charged++;
                }

                ids[method] = id;
                if (method.IsConstructor && method.IsStatic && segments.Count > 0)
                {
                    cctorIds[type] = id;
                }
            }
        }

        // Pass 2: the implies relation, the probes and the Process.Start redirections.
        int processStarts = 0;
        foreach (TypeDefinition type in types)
        {
            var chain = new List<int>();
            for (TypeDefinition? t = type; t is not null; t = t.DeclaringType)
            {
                if (cctorIds.TryGetValue(t, out int c))
                {
                    chain.Add(c);
                }
            }

            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                processStarts += RedirectProcessStarts(method, starts);
                if (!ids.TryGetValue(method, out int id))
                {
                    continue;
                }

                probes.AddImplies(id, chain.Where(c => c != id));
                MethodBody body = method.Body;
                body.SimplifyMacros();
                ILProcessor il = body.GetILProcessor();
                Instruction first = body.Instructions[0];
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, id));
                il.InsertBefore(first, il.Create(OpCodes.Call, hit));
                body.OptimizeMacros();
            }
        }

        module.Write(path, new WriterParameters { WriteSymbols = true });
        return (new Stats(ids.Count, charged, processStarts), null);
    }

    /// <summary>Redirect each <c>Process.Start</c> overload the probe twins; an overload it does not twin is left
    /// alone and reported, because its child would then be an orphan (recorded, but unattributed).</summary>
    private static int RedirectProcessStarts(MethodDefinition method, Dictionary<string, MethodDefinition> starts)
    {
        int n = 0;
        foreach (Instruction ins in method.Body.Instructions)
        {
            if ((ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt)
                || ins.Operand is not MethodReference mr
                || mr.DeclaringType.FullName != "System.Diagnostics.Process" || mr.Name != "Start")
            {
                continue;
            }

            string key = mr.HasThis
                ? "StartInstance(System.Diagnostics.Process)"
                : "Start(" + string.Join(",", mr.Parameters.Select(p => p.ParameterType.FullName)) + ")";
            if (!starts.TryGetValue(key, out var twin))
            {
                Console.Error.WriteLine($"  ⚠ {method.FullName}: Process.{key} has no probe twin — its child is an orphan");
                continue;
            }

            ins.OpCode = OpCodes.Call;
            ins.Operand = twin;
            n++;
        }

        return n;
    }

    private static bool InImpactNamespace(TypeDefinition t)
    {
        while (t.DeclaringType is not null)
        {
            t = t.DeclaringType;
        }

        return t.Namespace == ImpactNamespace || t.Namespace.StartsWith(ImpactNamespace + ".", StringComparison.Ordinal);
    }

    /// <summary>Per document, the first and last line the method's non-hidden sequence points cover.</summary>
    private static Dictionary<string, (int Min, int Max)> Segments(MethodDefinition method)
    {
        var spans = new Dictionary<string, (int Min, int Max)>(StringComparer.Ordinal);
        if (method.DebugInformation is { HasSequencePoints: true } info)
        {
            foreach (var sp in info.SequencePoints)
            {
                if (sp.IsHidden || sp.Document is null)
                {
                    continue;
                }

                string url = sp.Document.Url;
                int lo = sp.StartLine, hi = Math.Max(sp.EndLine, sp.StartLine);
                spans[url] = spans.TryGetValue(url, out var s) ? (Math.Min(s.Min, lo), Math.Max(s.Max, hi)) : (lo, hi);
            }
        }

        return spans;
    }

    /// <summary>Per document, every non-hidden sequence point's start line and the method that owns it, sorted.</summary>
    private static Dictionary<string, List<(int Line, MethodDefinition Owner)>> Owners(List<TypeDefinition> types)
    {
        var owners = new Dictionary<string, List<(int Line, MethodDefinition Owner)>>(StringComparer.Ordinal);
        foreach (var method in types.SelectMany(t => t.Methods).Where(m => m.HasBody))
        {
            if (method.DebugInformation is not { HasSequencePoints: true } info)
            {
                continue;
            }

            foreach (var sp in info.SequencePoints.Where(sp => !sp.IsHidden && sp.Document is not null))
            {
                if (!owners.TryGetValue(sp.Document.Url, out var list))
                {
                    owners[sp.Document.Url] = list = [];
                }

                list.Add((sp.StartLine, method));
            }
        }

        foreach (var list in owners.Values)
        {
            list.Sort((a, b) => a.Line.CompareTo(b.Line));
        }

        return owners;
    }

    /// <summary>
    /// The line RANGES a method owns: its sequence points, merged across the lines between two of them unless a
    /// point of ANOTHER method lies between. So the braces, <c>else</c> lines and comments inside a body belong to
    /// it, a lambda's body belongs to the lambda, and a constructor whose field initializers sit at the top of the
    /// class does not swallow every method declared between them and its body.
    /// </summary>
    private static List<(string File, int Lo, int Hi)> Ranges(MethodDefinition method,
        Dictionary<string, List<(int Line, MethodDefinition Owner)>> owners)
    {
        var ranges = new List<(string File, int Lo, int Hi)>();
        var byDoc = method.DebugInformation.SequencePoints
            .Where(sp => !sp.IsHidden && sp.Document is not null)
            .GroupBy(sp => sp.Document.Url, StringComparer.Ordinal);
        foreach (var doc in byDoc)
        {
            var points = doc.OrderBy(sp => sp.StartLine).ToList();
            var all = owners[doc.Key];
            int lo = points[0].StartLine, hi = Math.Max(points[0].EndLine, points[0].StartLine);
            foreach (var sp in points.Skip(1))
            {
                if (sp.StartLine > hi && ForeignBetween(all, hi, sp.StartLine, method))
                {
                    ranges.Add((doc.Key, lo, hi));
                    lo = sp.StartLine;
                }

                hi = Math.Max(hi, Math.Max(sp.EndLine, sp.StartLine));
            }

            ranges.Add((doc.Key, lo, hi));
        }

        return ranges;
    }

    /// <summary>Is there a sequence point of another method strictly between lines <paramref name="after"/> and
    /// <paramref name="before"/>?</summary>
    private static bool ForeignBetween(List<(int Line, MethodDefinition Owner)> all, int after, int before,
        MethodDefinition self)
    {
        int lo = 0, hi = all.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (all[mid].Line <= after) lo = mid + 1; else hi = mid;
        }

        for (int i = lo; i < all.Count && all[i].Line < before; i++)
        {
            if (!ReferenceEquals(all[i].Owner, self))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly Dictionary<TypeDefinition, HashSet<string>> s_typeDocs = [];

    /// <summary>Every document any method of <paramref name="type"/> (or, when it has none, of an enclosing type)
    /// maps to.</summary>
    private static HashSet<string> TypeDocuments(TypeDefinition type)
    {
        if (s_typeDocs.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        for (TypeDefinition? t = type; t is not null && set.Count == 0; t = t.DeclaringType)
        {
            foreach (var m in t.Methods.Where(m => m.HasBody))
            {
                set.UnionWith(Segments(m).Keys);
            }
        }

        return s_typeDocs[type] = set;
    }

    /// <summary>The probe table: one entry per probed method, one per type for its sequence-point-free members;
    /// paths are made repo-relative with '/'.</summary>
    private sealed class ProbeTable(string repo)
    {
        private readonly Dictionary<string, int> _typeLevel = new(StringComparer.Ordinal);
        private readonly string _root = repo.Replace('\\', '/').TrimEnd('/') + "/";

        /// <summary>Each entry: <c>n</c> the member, <c>s</c> its segments as [file, firstLine, lastLine] — lines
        /// 0 for a type-level entry, which stands for the whole of each file.</summary>
        public List<Entry> Entries { get; } = [];

        public Dictionary<int, List<int>> Implies { get; } = [];

        public int Method(string name, List<(string File, int Lo, int Hi)> ranges)
        {
            Entries.Add(new Entry(name, [.. ranges.Select(r => new object[] { Normalize(r.File), r.Lo, r.Hi })]));
            return Entries.Count - 1;
        }

        public int TypeLevel(string typeName, HashSet<string> documents)
        {
            if (!_typeLevel.TryGetValue(typeName, out int id))
            {
                Entries.Add(new Entry(typeName + " <type-level>", [.. documents.Select(Normalize).Distinct()
                    .Order(StringComparer.Ordinal).Select(f => new object[] { f, 0, 0 })]));
                id = _typeLevel[typeName] = Entries.Count - 1;
            }

            return id;
        }

        public void AddImplies(int id, IEnumerable<int> cctors)
        {
            foreach (int c in cctors)
            {
                if (!Implies.TryGetValue(id, out var list))
                {
                    Implies[id] = list = [];
                }

                if (!list.Contains(c))
                {
                    list.Add(c);
                }
            }
        }

        private string Normalize(string url)
        {
            string p = url.Replace('\\', '/');
            if (p.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            {
                return p[_root.Length..];
            }

            return p.StartsWith("/_/", StringComparison.Ordinal) ? p[3..] : p;
        }
    }

    internal sealed record Entry(
        [property: System.Text.Json.Serialization.JsonPropertyName("n")] string Name,
        [property: System.Text.Json.Serialization.JsonPropertyName("s")] object[][] Segments);
}
