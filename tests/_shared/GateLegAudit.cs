// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
using System.Reflection;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CobolNet.Gate;

/// <summary>
/// The per-assembly audits of the gate's leg contract (kb/Work PB1719; DESIGN-test-build-ci.md section 3.14.4 arms
/// (4) and (5)). Each gated assembly's <c>GateLegDriftTests</c> runs them over ITSELF, so the check travels with the
/// assembly it audits and needs no other assembly built.
/// </summary>
internal static class GateLegAudit
{
    /// <summary>Arm (4): the assembly names <see cref="GateTestFramework"/> as its xunit framework, by its own
    /// assembly name. xunit resolves the attribute by that name and, on any miss, SILENTLY runs its default
    /// framework — every case, no leg filter — so a wrong name is a gate that never filters.</summary>
    public static void AssertNamesTheGateFramework(Assembly assembly)
    {
        var frameworks = assembly.GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(TestFrameworkAttribute))
            .Select(a => string.Join(", ", a.ConstructorArguments.Select(x => x.Value)))
            .ToList();
        string expected = $"{typeof(GateTestFramework).FullName}, {assembly.GetName().Name}";
        Assert.True(frameworks.Count == 1 && frameworks[0] == expected,
            $"{assembly.GetName().Name} must carry exactly one assembly-level Xunit.TestFramework attribute naming "
            + $"({expected}) "
            + $"(tests/Directory.Build.props writes it for every test project under tests/); it carries "
            + $"[{string.Join("; ", frameworks)}]. Without it the gate's leg filter never runs and both legs run "
            + "the whole assembly.");
        Assert.True(assembly.GetType(typeof(GateTestFramework).FullName!) is not null,
            $"{assembly.GetName().Name} does not compile tests/_shared/GateLegs.cs");
    }

    /// <summary>The test host that runs <paramref name="assembly"/> is configured for SERVER garbage collection
    /// (kb/Work PB2526): <c>tests/Directory.Build.props</c> sets <c>ServerGarbageCollection</c>, which writes
    /// <c>System.GC.Server: true</c> into the assembly's runtimeconfig.json, and the host loads it as an AppContext
    /// property. Under the default workstation GC, 32 threads compiling COBOL spent 69 % of a probe's wall in GC
    /// pauses and stopped scaling past about 8 threads; Server GC took the whole Conformance leg from 318 s to 255 s.
    /// The CONFIGURED value is asserted, not <see cref="System.Runtime.GCSettings.IsServerGC"/>: the runtime runs
    /// workstation GC on a one-core machine whatever the configuration says, and that is not a regression.</summary>
    public static void AssertTestHostConfiguredForServerGc(Assembly assembly)
    {
        object? configured = AppContext.GetData("System.GC.Server");
        Assert.True(configured is true || (configured is string s && bool.TryParse(s, out bool b) && b),
            $"the test host running {assembly.GetName().Name} is not configured for Server GC (System.GC.Server is "
            + $"{configured ?? "absent"} in its runtimeconfig.json). tests/Directory.Build.props sets "
            + "<ServerGarbageCollection>true</ServerGarbageCollection> for every test project; a project that sets it "
            + "false, or a props change that drops it, puts every compiling thread of the gate's legs behind one "
            + "workstation-GC heap again (kb/Work PB2526).");
    }

    /// <summary>Arm (5): no discovered case of <paramref name="assembly"/> carries the repository root in its
    /// display name or in a string argument. A worktree path in a name makes the name differ between the worktree
    /// the impact map was recorded in and the one a gate runs in, so no plan could ever key on it (evidence
    /// <c>b8</c>: 404 Unit rows). Substituting a placeholder is not a fix — xunit truncates a long argument with
    /// <c>···</c> at a fixed length, so the cut moves with the root's length — the test passes a
    /// repository-relative path instead.</summary>
    public static void AssertNoCaseCarriesTheRoot(Assembly assembly)
    {
        var cases = Discover(assembly);
        Assert.True(cases.Count > 0, $"discovery found no case in {assembly.GetName().Name}: the audit saw nothing");
        var offenders = RootCarrying(cases.Select(c => (c.DisplayName, (object[]?)c.TestMethodArguments)), TestRepo.Root);
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} discovered case(s) of {assembly.GetName().Name} carry the repository root "
            + $"'{TestRepo.Root}'; pass a repository-relative path instead. First: "
            + string.Join(" | ", offenders.Take(3)));
    }

    /// <summary>The predicate of arm (5), separate so a drift test can plant offenders and watch it fire.
    /// <paramref name="root"/> is an ABSOLUTE root of either OS's shape — <c>E:\COBOL</c> or <c>/home/runner/COBOL</c>
    /// — and is matched as TEXT, never through the host's path rules: <c>Path.GetFullPath</c> treats a Windows root
    /// as RELATIVE on Linux and prefixes the working directory, so the audit matched nothing there (kb/Work PB1719,
    /// CI run 36533008383). Its forms are the root as written, with every <c>\</c> as <c>/</c>, and with every
    /// <c>\</c> doubled, as xunit escapes it in a string argument; a POSIX root's three forms coincide.</summary>
    public static List<string> RootCarrying(IEnumerable<(string DisplayName, object[]? Arguments)> cases,
        string root)
    {
        string trimmed = root.TrimEnd('/', '\\');
        if (trimmed.Length == 0)
        {
            // An empty form is contained in every name: the audit would report every case, not the offenders.
            throw new ArgumentException($"'{root}' is not a repository root", nameof(root));
        }

        string[] forms =
        [
            trimmed,
            trimmed.Replace('\\', '/'),
            trimmed.Replace("\\", @"\\", StringComparison.Ordinal),
        ];
        var offenders = new List<string>();
        foreach (var (display, args) in cases)
        {
            bool inName = forms.Any(f => display.Contains(f, StringComparison.OrdinalIgnoreCase));
            bool inArgument = args is not null && args.OfType<string>()
                .Any(a => forms.Any(f => a.Contains(f, StringComparison.OrdinalIgnoreCase)));
            if (inName || inArgument)
            {
                offenders.Add(display);
            }
        }

        return offenders;
    }

    /// <summary>xunit's own discovery of <paramref name="assembly"/>, theories pre-enumerated as the test runner
    /// does, so the names are the ones <c>dotnet test --list-tests</c> prints.</summary>
    private static List<ITestCase> Discover(Assembly assembly)
    {
        var sink = new DiscoverySink();
        using var discoverer = new XunitTestFrameworkDiscoverer(Reflector.Wrap(assembly),
            new NoSourceInformation(), sink);
        discoverer.Find(includeSourceInformation: false, sink, new DiscoveryOptions());
        sink.Finished.Wait();
        return sink.Cases;
    }

    /// <summary>Discovery runs with <c>includeSourceInformation: false</c>, so this is never asked.</summary>
    private sealed class NoSourceInformation : LongLivedMarshalByRefObject, ISourceInformationProvider
    {
        public ISourceInformation GetSourceInformation(ITestCase testCase) =>
            throw new NotSupportedException("the audit's discovery asks for no source information");

        public void Dispose()
        {
        }
    }

    private sealed class DiscoverySink : LongLivedMarshalByRefObject, IMessageSink
    {
        private readonly object _gate = new();
        public List<ITestCase> Cases { get; } = [];
        public ManualResetEventSlim Finished { get; } = new();

        public bool OnMessage(IMessageSinkMessage message)
        {
            switch (message)
            {
                case ITestCaseDiscoveryMessage found:
                    lock (_gate)
                    {
                        Cases.Add(found.TestCase);
                    }

                    break;
                case IDiscoveryCompleteMessage:
                    Finished.Set();
                    break;
            }

            return true;
        }
    }

    /// <summary>The runner's defaults, stated: theories pre-enumerated, names by class and method.</summary>
    private sealed class DiscoveryOptions : ITestFrameworkDiscoveryOptions
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal)
        {
            ["xunit.discovery.PreEnumerateTheories"] = true,
            ["xunit.discovery.MethodDisplay"] = nameof(TestMethodDisplay.ClassAndMethod),
        };

        public TValue GetValue<TValue>(string name) =>
            _values.TryGetValue(name, out object? v) && v is TValue t ? t : default!;

        public void SetValue<TValue>(string name, TValue value) => _values[name] = value;
    }
}
