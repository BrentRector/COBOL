// PB1708 pivot evidence b2 — does ONE process scale the continuity cell's work across threads?
//
//     dotnet run --project docs/rearchitecture/evidence/impact-map-pb1708/b2-scaling -- [programs] [threads,...]
//
// A continuity cell (VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition) is two CheckOnly compiles of a
// green NIST program at a later edition, permissive then strict. This runs exactly that work — the same
// CompilerDriver.Options EditionHarness.CompileNist builds — over the first N green programs of
// tests/nist/corpus.tsv, at each thread count, in ONE process, and reports per thread count: the wall, the speed-up
// over one thread, the process CPU consumed, the CPU utilisation (CPU ÷ (wall × threads)), the contended Monitor
// acquisitions (Monitor.LockContentionCount) and the GC pause time. Default build (Debug), like the gate.
// Every input is in the repository; the output depends on the host (record it beside the result).
using System.Diagnostics;
using CobolNet;

string repo = FindRepo(AppContext.BaseDirectory);
int n = args.Length > 0 ? int.Parse(args[0]) : 96;
int[] threadCounts = args.Length > 1 ? [.. args[1].Split(',').Select(int.Parse)] : [1, 2, 4, 8, 12, 16, 24];
string[] programs = [.. File.ReadLines(Path.Combine(repo, "tests", "nist", "corpus.tsv"))
    .Where(l => l.Length > 0 && l[0] != '#')
    .Select(l => l.Split('\t'))
    .Where(f => f.Length > 2 && f[2] == "green")
    .Select(f => f[0])
    .Take(n)];
string scratch = Path.Combine(Path.GetTempPath(), "pb1708-b2-" + Environment.ProcessId);
Directory.CreateDirectory(scratch);

void Cell(string name, int edition)
{
    foreach (bool permissive in new[] { true, false })
    {
        var r = CompilerDriver.Compile(new CompilerDriver.Options(
            Path.Combine(repo, "tests", "nist", "programs", name + ".cob"),
            Path.Combine(scratch, name + ".dll"), NistTestName: name, DialectLevel: edition,
            Permissive: permissive, CheckOnly: true));
        if (permissive && !r.Success)
            throw new InvalidOperationException($"{name}@{edition} permissive failed: {string.Join("; ", r.Errors)}");
    }
}

// Warm-up: JIT, static tables, the parser's shared DFA — every measured round below starts from the same state.
foreach (string p in programs) Cell(p, 2023);

var proc = Process.GetCurrentProcess();
Console.WriteLine($"{programs.Length} green NIST programs x 2 CheckOnly compiles (permissive + strict) at COBOL-2023;"
    + $" {Environment.ProcessorCount} logical processors; server GC {System.Runtime.GCSettings.IsServerGC}");
Console.WriteLine("threads  wall_s  speedup  cpu_s  cpu_util  lock_contentions  gc_pause_s  gc_count");
double baseWall = 0;
foreach (int k in threadCounts)
{
    proc.Refresh();
    TimeSpan cpu0 = proc.TotalProcessorTime;
    long lock0 = Monitor.LockContentionCount;
    TimeSpan pause0 = GC.GetTotalPauseDuration();
    int gc0 = GC.CollectionCount(0);
    var sw = Stopwatch.StartNew();
    Parallel.ForEach(programs, new ParallelOptions { MaxDegreeOfParallelism = k }, p => Cell(p, 2023));
    sw.Stop();
    proc.Refresh();
    double wall = sw.Elapsed.TotalSeconds;
    double cpu = (proc.TotalProcessorTime - cpu0).TotalSeconds;
    if (k == threadCounts[0]) baseWall = wall * k / threadCounts[0];
    Console.WriteLine($"{k,7}  {wall,6:F1}  {baseWall / wall,7:F2}  {cpu,5:F0}  {cpu / (wall * k),8:P0}  "
        + $"{Monitor.LockContentionCount - lock0,16}  {(GC.GetTotalPauseDuration() - pause0).TotalSeconds,10:F1}  "
        + $"{GC.CollectionCount(0) - gc0,8}");
}

try { Directory.Delete(scratch, recursive: true); } catch (IOException) { }

static string FindRepo(string dir)
{
    for (var d = new DirectoryInfo(dir); d is not null; d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "CobolSharp.sln"))) return d.FullName;
    throw new DirectoryNotFoundException("CobolSharp.sln not found above " + dir);
}
