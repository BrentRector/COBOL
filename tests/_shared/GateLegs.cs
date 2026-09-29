// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// THE IN-ASSEMBLY LEG FILTER of the ordered gate (kb/Work PB1719; docs/rearchitecture/DESIGN-test-build-ci.md
// section 3.14.3). tests/Directory.Build.props links this file into every test project under tests/ and names
// GateTestFramework in each one's [assembly: Xunit.TestFramework] (except in the impact-recording build, whose
// ImpactTestFramework derives from GateTestFramework, so an assembly always names exactly one framework).
#nullable enable
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CobolNet.Gate;

/// <summary>
/// xunit's own framework, with an executor that runs ONE LEG of the gate's plan when, and only when, the gate driver
/// hands it a complete and self-consistent handshake (<see cref="GateHandshake"/>).
/// </summary>
/// <remarks>
/// <para>With no handshake it is xunit's framework exactly: every case, in xunit's order. That is the battery, CI,
/// the lander's single leg and an IDE.</para>
/// <para>⛔ WHY THE REFUSAL IS AN EXECUTION ERROR AND NOT A THROW (evidence <c>b3-order-probe</c>): xunit catches a
/// framework constructor that throws and silently falls back to its default framework, so every case runs and the
/// run is green. An executor that throws is a "Catastrophic failure" with no verdict line and no trx result. An
/// executor that reports every case as xunit's <see cref="ExecutionErrorTestCase"/> gives a failed result per case,
/// a <c>Failed!</c> verdict line and exit 1, so a partial or stale handshake can never pass as a whole run.</para>
/// </remarks>
public class GateTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    protected sealed override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new GateExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink, WrapMessageBus);

    /// <summary>The one extension point: a derived framework may wrap the message bus. The impact recorder
    /// (<c>tools/impact/ImpactTestFramework.cs</c>) opens and closes its hit contexts on the bus's messages.</summary>
    protected virtual IMessageBus WrapMessageBus(IMessageBus bus) => bus;
}

/// <summary>The executor: resolves the handshake once per run and hands the runner either every case (none),
/// the leg's cases in plan order (a consistent handshake) or every case as an execution error (anything else).</summary>
internal sealed class GateExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
    IMessageSink diagnosticMessageSink, Func<IMessageBus, IMessageBus> wrapMessageBus)
    : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
{
    // xunit's executor contract is `async void` (XunitTestFrameworkExecutor.RunTestCases); the runner reports the
    // run's end through the execution sink, never through a returned task.
    protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        var cases = testCases.ToList();
        GateSelection selection = Select(cases);
        using var runner = new GateAssemblyRunner(TestAssembly, selection.Cases, DiagnosticMessageSink,
            executionMessageSink, executionOptions, wrapMessageBus, selection.Plan);
        await runner.RunAsync();
    }

    /// <summary>What this run executes. Any failure while deciding — the handshake, the plan, the identity
    /// record, or a defect in this code — refuses the run: every case becomes an execution error naming the cause,
    /// so the one outcome that cannot happen is a silent whole or partial run.</summary>
    private GateSelection Select(List<IXunitTestCase> cases)
    {
        GateHandshake handshake;
        try
        {
            handshake = GateHandshake.Read(Environment.GetEnvironmentVariable,
                GatePlan.AssemblyKey(TestAssembly.Assembly.Name));
        }
        catch (Exception ex)
        {
            return Refuse(cases, $"the gate handshake could not be read: {ex}");
        }

        switch (handshake)
        {
            case GateHandshake.None:
                return new GateSelection(cases, null);
            case GateHandshake.Refused refused:
                return Refuse(cases, refused.Cause);
            case GateHandshake.Leg leg:
                try
                {
                    var mine = leg.Plan.CasesOfLeg(cases, leg.Number);
                    GateIdentityRecord.Write(leg, TestAssemblyFile(), cases.Count, mine);
                    return new GateSelection(mine, leg.Plan);
                }
                catch (Exception ex)
                {
                    return Refuse(cases, $"leg {leg.Number} could not start: {ex}");
                }
            default:
                return Refuse(cases, $"unknown handshake state {handshake.GetType().Name}");
        }
    }

    private Assembly TestAssemblyFile() =>
        TestAssembly.Assembly is IReflectionAssemblyInfo reflected
            ? reflected.Assembly
            : throw new InvalidOperationException("the test assembly is not a reflection assembly");

    private GateSelection Refuse(List<IXunitTestCase> cases, string cause) =>
        new(cases.Select(c => (IXunitTestCase)new ExecutionErrorTestCase(DiagnosticMessageSink,
                TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, c.TestMethod,
                $"{GateHandshake.RefusalPrefix}{cause} (case {c.DisplayName})")).ToList(),
            null);
}

/// <summary>The cases a run executes, and the plan that orders them (null: xunit's order).</summary>
internal sealed record GateSelection(IReadOnlyList<IXunitTestCase> Cases, GateAssemblyPlan? Plan);

/// <summary>xunit's assembly runner with the bus wrapped and, under a plan, the assembly-level orderers.</summary>
internal sealed class GateAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
    ITestFrameworkExecutionOptions executionOptions, Func<IMessageBus, IMessageBus> wrapMessageBus,
    GateAssemblyPlan? plan)
    : XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
{
    protected override IMessageBus CreateMessageBus() => wrapMessageBus(base.CreateMessageBus());

    /// <summary>xunit reads assembly-level <c>[TestCaseOrderer]</c>/<c>[TestCollectionOrderer]</c> attributes in
    /// <see cref="XunitTestAssemblyRunner.AfterTestAssemblyStartingAsync"/>; the plan's orderers are installed
    /// after it, and only under a plan, so a run without a handshake keeps xunit's order exactly.</summary>
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync();
        if (plan is not null)
        {
            TestCollectionOrderer = new GateCollectionOrderer(plan.CollectionRanks(TestCases));
            TestCaseOrderer = new GateCaseOrderer(plan);
        }
    }
}

/// <summary>THE NAME KEY — the display name with the partition suffix <c>_P&lt;k&gt;</c> removed from its CLASS
/// segment. It mirrors <c>scripts/gate_plan.py#name_key</c> exactly (GateLegDriftTests runs both over one list):
/// split at the FIRST <c>(</c>; in the part before it, split on <c>.</c>; when there are at least three segments
/// and the second-to-last matches <c>(.+)_P\d+</c> in full, replace it with the capture; rejoin, and append the
/// <c>(</c> and everything after it unchanged. A theory's arguments are never touched.</summary>
internal static partial class GateNameKey
{
    [GeneratedRegex(@"\A(?<family>.+)_P\d+\z")]
    private static partial Regex PartitionClass();

    public static string Of(string displayName)
    {
        int paren = displayName.IndexOf('(');
        string head = paren < 0 ? displayName : displayName[..paren];
        string tail = paren < 0 ? "" : displayName[paren..];
        string[] parts = head.Split('.');
        if (parts.Length >= 3 && PartitionClass().Match(parts[^2]) is { Success: true } m)
        {
            parts[^2] = m.Groups["family"].Value;
        }

        return string.Join('.', parts) + tail;
    }
}

/// <summary>One assembly's part of the plan file (<c>scripts/gate_plan.py</c>, schema 1): its leg-1 and leg-2 keys
/// in RANK order (a key's rank is its position, leg 1's list first).</summary>
internal sealed class GatePlan
{
    /// <summary>The plan file's schema this reader understands.</summary>
    public const int Schema = 1;

    /// <summary>The gated test projects are <c>Cobol.Net.Tests.&lt;key&gt;</c>; the plan, the impact map and the
    /// gate name each by its key (<c>Conformance</c>, <c>Unit</c>, <c>Characterization</c>).</summary>
    private const string TestAssemblyPrefix = "Cobol.Net.Tests.";

    public static string AssemblyKey(string assemblyName)
    {
        string simple = new AssemblyName(assemblyName).Name
                        ?? throw new ArgumentException($"'{assemblyName}' names no assembly", nameof(assemblyName));
        return simple.StartsWith(TestAssemblyPrefix, StringComparison.Ordinal)
            ? simple[TestAssemblyPrefix.Length..]
            : simple;
    }

    /// <summary>Read one assembly's part of a plan file's bytes. Throws <see cref="GatePlanException"/> naming the
    /// defect when the bytes are not a schema-1 plan that names <paramref name="assemblyKey"/>.</summary>
    public static GateAssemblyPlan Parse(ReadOnlySpan<byte> bytes, string assemblyKey)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(bytes.ToArray());
        }
        catch (JsonException ex)
        {
            throw new GatePlanException($"the plan is not JSON ({ex.Message})");
        }

        using (doc)
        {
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("schema", out JsonElement schema)
                || schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out int s) || s != Schema)
            {
                throw new GatePlanException($"the plan is not schema {Schema}");
            }

            string content = root.TryGetProperty("sha256", out JsonElement sha) && sha.ValueKind == JsonValueKind.String
                ? sha.GetString()!
                : throw new GatePlanException("the plan carries no sha256");
            if (!root.TryGetProperty("assemblies", out JsonElement assemblies)
                || assemblies.ValueKind != JsonValueKind.Object)
            {
                throw new GatePlanException("the plan carries no assemblies object");
            }

            if (!assemblies.TryGetProperty(assemblyKey, out JsonElement mine) || mine.ValueKind != JsonValueKind.Object)
            {
                throw new GatePlanException($"the plan names no assembly '{assemblyKey}'");
            }

            List<string> leg1 = Keys(mine, "leg1"), leg2 = Keys(mine, "leg2");
            // gate_plan.py gives every key exactly one leg; a key in both is a plan no leg function can honour.
            if (leg1.Intersect(leg2, StringComparer.Ordinal).FirstOrDefault() is { } both)
            {
                throw new GatePlanException($"the plan puts '{both}' in both legs");
            }

            return new GateAssemblyPlan(assemblyKey, content, leg1, leg2);
        }
    }

    private static List<string> Keys(JsonElement assembly, string leg)
    {
        if (!assembly.TryGetProperty(leg, out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            throw new GatePlanException($"the plan's assembly carries no {leg} list");
        }

        var keys = new List<string>(list.GetArrayLength());
        foreach (JsonElement k in list.EnumerateArray())
        {
            keys.Add(k.ValueKind == JsonValueKind.String
                ? k.GetString()!
                : throw new GatePlanException($"the plan's {leg} list holds a {k.ValueKind}, not a key"));
        }

        return keys;
    }
}

/// <summary>A plan file that does not parse as the plan the driver was meant to hand over.</summary>
internal sealed class GatePlanException(string message) : Exception(message);

/// <summary>THE LEG FUNCTION and the rank, for one assembly.</summary>
/// <remarks>
/// <see cref="LegOf"/> mirrors <c>scripts/gate_plan.py#leg_of</c>: 2 for a leg-2 key, 1 otherwise — so a case the
/// plan never heard of runs in leg 1, never nowhere. It is a function of the display name alone, so every case
/// sharing one (xunit truncates long theory arguments with <c>···</c>) lands in the same leg: total and disjoint by
/// construction.
/// </remarks>
internal sealed class GateAssemblyPlan
{
    private readonly Dictionary<string, int> _rank = new(StringComparer.Ordinal);
    private readonly HashSet<string> _leg2 = new(StringComparer.Ordinal);

    public GateAssemblyPlan(string assemblyKey, string contentSha256, IReadOnlyList<string> leg1,
        IReadOnlyList<string> leg2)
    {
        AssemblyKey = assemblyKey;
        ContentSha256 = contentSha256;
        foreach (string k in leg1.Concat(leg2))
        {
            _rank.TryAdd(k, _rank.Count);
        }

        _leg2.UnionWith(leg2);
    }

    public string AssemblyKey { get; }

    /// <summary>The plan's own identity (its <c>sha256</c> field: the digest of its canonical content).</summary>
    public string ContentSha256 { get; }

    public int LegOf(string displayName) => _leg2.Contains(GateNameKey.Of(displayName)) ? 2 : 1;

    /// <summary>The case's position in the plan; a case the plan never heard of ranks after every planned one.</summary>
    public int RankOf(string displayName) =>
        _rank.TryGetValue(GateNameKey.Of(displayName), out int r) ? r : int.MaxValue;

    /// <summary>The leg's cases in rank order (stable: equal ranks keep discovery order). The order matters beyond
    /// the orderers: xunit runs a collection's classes in the order their first case arrives.</summary>
    public List<IXunitTestCase> CasesOfLeg(IEnumerable<IXunitTestCase> cases, int leg) =>
        cases.Where(c => LegOf(c.DisplayName) == leg).OrderBy(c => RankOf(c.DisplayName)).ToList();

    /// <summary>A collection's rank is its best-ranked case's.</summary>
    public Dictionary<Guid, int> CollectionRanks(IEnumerable<ITestCase> cases)
    {
        var ranks = new Dictionary<Guid, int>();
        foreach (ITestCase c in cases)
        {
            Guid id = c.TestMethod.TestClass.TestCollection.UniqueID;
            int r = RankOf(c.DisplayName);
            ranks[id] = ranks.TryGetValue(id, out int have) ? Math.Min(have, r) : r;
        }

        return ranks;
    }
}

/// <summary>THE ENVIRONMENT HANDSHAKE — three variables, all written by the gate driver and scrubbed by every other
/// caller of <c>dotnet test</c> (section 3.14.3).</summary>
internal abstract record GateHandshake
{
    public const string PlanVariable = "COBOLNET_GATE_PLAN";
    public const string LegVariable = "COBOLNET_GATE_LEG";
    public const string DigestVariable = "COBOLNET_GATE_PLAN_SHA256";

    /// <summary>The first words of every refused case's message.</summary>
    public const string RefusalPrefix = "GATE ENVIRONMENT INCOMPLETE: ";

    /// <summary>No variable set: every case, xunit's order, exactly as without this framework.</summary>
    public sealed record None : GateHandshake;

    /// <summary>All three set and consistent: run leg <see cref="Number"/> of <see cref="Plan"/>.</summary>
    public sealed record Leg(int Number, string PlanPath, string Digest, GateAssemblyPlan Plan) : GateHandshake;

    /// <summary>Anything else: every case an execution error carrying <see cref="Cause"/>.</summary>
    public sealed record Refused(string Cause) : GateHandshake;

    /// <summary>Resolve the handshake from <paramref name="environment"/> for the assembly <paramref name="assemblyKey"/>.
    /// An empty value counts as unset (Windows cannot hold an empty environment variable at all).</summary>
    public static GateHandshake Read(Func<string, string?> environment, string assemblyKey)
    {
        string?[] values = [Value(environment, PlanVariable), Value(environment, LegVariable),
            Value(environment, DigestVariable)];
        string[] names = [PlanVariable, LegVariable, DigestVariable];
        int set = values.Count(v => v is not null);
        if (set == 0)
        {
            return new None();
        }

        if (set < names.Length)
        {
            string present = string.Join(" and ", names.Where((_, i) => values[i] is not null));
            string absent = string.Join(" and ", names.Where((_, i) => values[i] is null));
            return new Refused($"{present} {(set == 1 ? "is" : "are")} set but {absent} {(set == 2 ? "is" : "are")} not");
        }

        string path = values[0]!, legText = values[1]!, digest = values[2]!;
        if (legText is not ("1" or "2"))
        {
            return new Refused($"{LegVariable} is '{legText}', not 1 or 2");
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or NotSupportedException)
        {
            return new Refused($"{PlanVariable} '{path}' cannot be read ({ex.GetType().Name}: {ex.Message})");
        }

        string actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (!string.Equals(actual, digest, StringComparison.OrdinalIgnoreCase))
        {
            return new Refused($"{DigestVariable} is {digest} but the plan '{path}' hashes to {actual}");
        }

        try
        {
            return new Leg(legText == "1" ? 1 : 2, path, actual, GatePlan.Parse(bytes, assemblyKey));
        }
        catch (GatePlanException ex)
        {
            return new Refused($"{PlanVariable} '{path}': {ex.Message}");
        }
    }

    private static string? Value(Func<string, string?> environment, string name) =>
        environment(name) is { Length: > 0 } v ? v : null;
}

/// <summary>THE IDENTITY RECORD each leg host writes into the gate's run directory (the plan's directory) before it
/// runs: the plan digest it read, its leg, the test assembly's MVID and SHA-256, the SHA-256 of every product
/// assembly beside it (the compiler under test), and the keys of the cases it received and runs. The driver's
/// population check compares the records of one gate's legs (section 3.14.4).</summary>
internal static class GateIdentityRecord
{
    public static string PathFor(GateHandshake.Leg leg) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(leg.PlanPath))!,
            $"leg-{leg.Number}-{leg.Plan.AssemblyKey}.json");

    public static void Write(GateHandshake.Leg leg, Assembly testAssembly, int received,
        IEnumerable<IXunitTestCase> runs)
    {
        string location = testAssembly.Location;
        string bin = Path.GetDirectoryName(location)!;
        var products = ProductAssemblies(bin, location).ToList();
        if (products.Count == 0)
        {
            throw new InvalidOperationException(
                $"no product assembly beside {location}: the identity record could not name the compiler under test");
        }

        string target = PathFor(leg);
        string temp = target + ".tmp";
        using (var stream = File.Create(temp))
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("schema", 1);
            json.WriteString("assembly", leg.Plan.AssemblyKey);
            json.WriteNumber("leg", leg.Number);
            json.WriteString("plan_sha256", leg.Digest);
            json.WriteString("plan_content_sha256", leg.Plan.ContentSha256);
            json.WriteStartObject("test_assembly");
            json.WriteString("file", Path.GetFileName(location));
            json.WriteString("mvid", testAssembly.ManifestModule.ModuleVersionId.ToString());
            json.WriteString("sha256", FileSha256(location));
            json.WriteEndObject();
            json.WriteStartObject("product_assemblies");
            foreach (string dll in products)
            {
                json.WriteString(Path.GetFileName(dll), FileSha256(dll));
            }

            json.WriteEndObject();
            json.WriteNumber("received", received);
            json.WriteStartArray("runs");
            foreach (IXunitTestCase c in runs)
            {
                json.WriteStringValue(GateNameKey.Of(c.DisplayName));
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        File.Move(temp, target, overwrite: true);
    }

    /// <summary>The compiler under test: the repository's own assemblies in the test assembly's directory
    /// (<c>Cobol.Net.*</c>, <c>CobolSharp.*</c> and the CLI's <c>cobol.dll</c>), the test assembly excluded.</summary>
    internal static IEnumerable<string> ProductAssemblies(string bin, string testAssemblyLocation) =>
        Directory.EnumerateFiles(bin, "*.dll")
            .Where(f =>
            {
                string name = Path.GetFileName(f);
                return !string.Equals(f, testAssemblyLocation, StringComparison.OrdinalIgnoreCase)
                       && (name.StartsWith("Cobol.Net.", StringComparison.Ordinal)
                           || name.StartsWith("CobolSharp.", StringComparison.Ordinal)
                           || name == "cobol.dll");
            })
            .Order(StringComparer.Ordinal);

    private static string FileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}

/// <summary>Orders collections by their best-ranked case; an unplanned collection keeps xunit's order after them.</summary>
internal sealed class GateCollectionOrderer(IReadOnlyDictionary<Guid, int> ranks) : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        GateOrder.Permutation(testCollections, cs => cs
            .OrderBy(c => ranks.TryGetValue(c.UniqueID, out int r) ? r : int.MaxValue));
}

/// <summary>Orders a class's cases by their rank; unplanned cases keep xunit's order after them.</summary>
internal sealed class GateCaseOrderer(GateAssemblyPlan plan) : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase =>
        GateOrder.Permutation(testCases, cs => cs.OrderBy(c => plan.RankOf(c.DisplayName)));
}

/// <summary>⛔ An orderer's output is CHECKED to be a permutation of its input. xunit trusts an orderer's result:
/// a case an orderer dropped would silently never run and one it repeated would run twice. The check throws instead,
/// and xunit then logs the orderer's exception and keeps its own (complete) order — ordering is best effort,
/// completeness is not.</summary>
internal static class GateOrder
{
    public static List<T> Permutation<T>(IEnumerable<T> input, Func<IReadOnlyList<T>, IEnumerable<T>> order)
    {
        var inList = input.ToList();
        var outList = order(inList).ToList();
        var count = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        foreach (T item in inList)
        {
            object key = item!;
            count[key] = count.GetValueOrDefault(key) + 1;
        }

        foreach (T item in outList)
        {
            object key = item!;
            if (!count.TryGetValue(key, out int n) || n == 0)
            {
                throw new InvalidOperationException(
                    $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out; "
                    + $"an item it returned was not in the input or was returned twice)");
            }

            count[key] = n - 1;
        }

        if (outList.Count != inList.Count)
        {
            throw new InvalidOperationException(
                $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out)");
        }

        return outList;
    }
}
