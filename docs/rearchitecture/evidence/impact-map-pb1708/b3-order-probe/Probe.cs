// PB1708 pivot evidence b3 — the ordering primitives xunit v2 offers, measured on this repository's versions.
//
// 24 classes (= 24 collections) x 4 facts, each fact sleeping 300 ms and appending "<ms since the first start>
// <collection>.<test>" to $ORDER_PROBE_LOG. PlanCollectionOrderer / PlanCaseOrderer read $ORDER_PROBE_PLAN (one test
// name per line, highest priority first): a collection ranks by its best-ranked test, cases by their own rank, and
// everything unplanned keeps xunit's default order AFTER the planned ones. Both orderers THROW if their output is not
// a permutation of their input. With $ORDER_PROBE_FAIL set, C17.T2 fails.
//
// The GATE LEG half (the review's request, 2026-09-28): GateFramework is a GateTestFramework-shaped framework whose
// executor DROPS every discovered case not in the requested leg. $ORDER_PROBE_LEG names the leg (1 or 2) and
// $ORDER_PROBE_LEGPLAN a file of leg-1 display names; a case whose display name is listed is leg 1, every other case
// leg 2. With $ORDER_PROBE_THROW set the framework's constructor throws, and with $ORDER_PROBE_BAD = throw | errorcases
// the executor throws or turns every case into an ExecutionErrorTestCase (the "partial gate environment" arms). Dup.Long
// is one theory whose two rows share a truncated display name; Skipped.S is a [Fact(Skip)]. run.ps1 drives it all.
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestCollectionOrderer("OrderProbe.PlanCollectionOrderer", "OrderProbe")]
[assembly: TestCaseOrderer("OrderProbe.PlanCaseOrderer", "OrderProbe")]
[assembly: TestFramework("OrderProbe.GateFramework", "OrderProbe")]

namespace OrderProbe;

public sealed class GateFramework : XunitTestFramework
{
    public GateFramework(IMessageSink messageSink) : base(messageSink)
    {
        if (Environment.GetEnvironmentVariable("ORDER_PROBE_THROW") is not null)
            throw new InvalidOperationException("GATE ENVIRONMENT INCOMPLETE: the probe's planted partial environment");
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new GateExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

public sealed class GateExecutor(AssemblyName assemblyName, ISourceInformationProvider sip, IMessageSink diag)
    : XunitTestFrameworkExecutor(assemblyName, sip, diag)
{
    protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
    {
        // The two ways an executor can refuse a bad environment: throw, or turn every case into xunit's own
        // ExecutionErrorTestCase (a case that reports one failure carrying the message).
        switch (Environment.GetEnvironmentVariable("ORDER_PROBE_BAD"))
        {
            case "throw":
                throw new InvalidOperationException("GATE ENVIRONMENT INCOMPLETE: thrown by the executor");
            case "errorcases":
                base.RunTestCases(testCases.Select(c => (IXunitTestCase)new ExecutionErrorTestCase(DiagnosticMessageSink,
                        TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, c.TestMethod,
                        "GATE ENVIRONMENT INCOMPLETE: " + c.DisplayName)).ToList(),
                    executionMessageSink, executionOptions);
                return;
        }

        string? leg = Environment.GetEnvironmentVariable("ORDER_PROBE_LEG");
        if (leg is null)
        {
            base.RunTestCases(testCases, executionMessageSink, executionOptions);
            return;
        }

        var legOne = File.ReadAllLines(Environment.GetEnvironmentVariable("ORDER_PROBE_LEGPLAN")!)
            .Where(l => l.Length > 0).ToHashSet(StringComparer.Ordinal);
        base.RunTestCases(testCases.Where(c => (legOne.Contains(c.DisplayName) ? "1" : "2") == leg).ToList(),
            executionMessageSink, executionOptions);
    }
}

public static class Plan
{
    static readonly Lazy<Dictionary<string, int>> Ranks = new(() =>
    {
        string? path = Environment.GetEnvironmentVariable("ORDER_PROBE_PLAN");
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        if (path is null || !File.Exists(path)) return d;
        int i = 0;
        foreach (string line in File.ReadLines(path)) if (line.Length > 0) d.TryAdd(line.Trim(), i++);
        return d;
    });

    public static int Rank(ITestCase c) =>
        Ranks.Value.TryGetValue(c.TestMethod.TestClass.Class.Name.Split('.').Last() + "." + c.TestMethod.Method.Name, out int r)
            ? r : int.MaxValue;

    // A default collection is one class ("Test collection for OrderProbe.C17"); it ranks by its best-ranked test.
    public static int ClassRank(string cls) =>
        Ranks.Value.Where(kv => kv.Key.StartsWith(cls + ".", StringComparison.Ordinal)).Select(kv => kv.Value)
            .DefaultIfEmpty(int.MaxValue).Min();

    public static IEnumerable<T> Permutation<T>(IEnumerable<T> input, Func<IEnumerable<T>, IEnumerable<T>> order)
    {
        var inList = input.ToList();
        var outList = order(inList).ToList();
        if (outList.Count != inList.Count || inList.Except(outList).Any())
            throw new InvalidOperationException($"orderer is not a permutation: {inList.Count} in, {outList.Count} out");
        return outList;
    }

    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static readonly object Gate = new();

    public static void Record(string what)
    {
        string? log = Environment.GetEnvironmentVariable("ORDER_PROBE_LOG");
        if (log is null) return;
        lock (Gate) File.AppendAllText(log, $"{Clock.ElapsedMilliseconds} {what}\n");
    }
}

public sealed class PlanCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        Plan.Permutation(testCollections, cs => cs.Select((c, i) => (c, i))
            .OrderBy(p => Plan.ClassRank(p.c.DisplayName.Split('.').Last()))
            .ThenBy(p => p.i).Select(p => p.c));
}

public sealed class PlanCaseOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
        Plan.Permutation(testCases, cs => cs.Select((c, i) => (c, i)).OrderBy(p => Plan.Rank(p.c)).ThenBy(p => p.i).Select(p => p.c));
}

public abstract class Facts
{
    void Run(string name)
    {
        string me = GetType().Name + "." + name;
        Plan.Record("start " + me);
        Thread.Sleep(300);
        if (me == "C17.T2" && Environment.GetEnvironmentVariable("ORDER_PROBE_FAIL") is not null)
            Assert.Fail("the seeded red");
    }

    [Fact] public void T0() => Run(nameof(T0));
    [Fact] public void T1() => Run(nameof(T1));
    [Fact] public void T2() => Run(nameof(T2));
    [Fact] public void T3() => Run(nameof(T3));
}

public sealed class Dup
{
    // Two rows whose argument differs only after xunit's 50-character argument truncation: one display name, twice.
    [Theory]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890-A")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890-B")]
    public void Long(string s) => Plan.Record("start Dup.Long " + s[^1]);
}

public sealed class Skipped
{
    [Fact(Skip = "the probe's skipped fact")]
    public void S() { }
}

public sealed class C00 : Facts; public sealed class C01 : Facts; public sealed class C02 : Facts; public sealed class C03 : Facts;
public sealed class C04 : Facts; public sealed class C05 : Facts; public sealed class C06 : Facts; public sealed class C07 : Facts;
public sealed class C08 : Facts; public sealed class C09 : Facts; public sealed class C10 : Facts; public sealed class C11 : Facts;
public sealed class C12 : Facts; public sealed class C13 : Facts; public sealed class C14 : Facts; public sealed class C15 : Facts;
public sealed class C16 : Facts; public sealed class C17 : Facts; public sealed class C18 : Facts; public sealed class C19 : Facts;
public sealed class C20 : Facts; public sealed class C21 : Facts; public sealed class C22 : Facts; public sealed class C23 : Facts;
