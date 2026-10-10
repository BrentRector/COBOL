// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ RUN-UNIT STATE LIVES ON <see cref="RunUnit"/>, NEVER IN A PROCESS-GLOBAL MUTABLE STATIC (kb/Work PB307).
/// </summary>
/// <remarks>
/// <para><see cref="RunUnit"/> is "the single owner of all run-unit-lifetime state (ISO §14.6.1)", replacing five
/// process-global stores — and FUNCTION RANDOM's sequence was a SIXTH the consolidation missed:
/// <c>private static Random _random</c> on <c>CobolIntrinsics</c>, whose own doc-comment said "ONE current
/// pseudo-random sequence per run unit". §15.75.3 r4 scopes the implementor seed to "the first reference to this
/// function in the run unit", so a second run unit in one .NET process (the <see cref="RunUnit.Run"/> /
/// <see cref="RunUnit.Begin"/> host shape) must NOT continue the first's sequence — and it did.</para>
/// <para>kb/Work PB1069 is the same defect on the INSTANCE side: the emitted driver's run-unit start reset a
/// HAND LIST of members (programs, EXTERNAL, MODULE-NAME, RANDOM), so the switch, locale and report-flow state —
/// and every class's factory object, a process static — survived into the next run unit. A new run unit is now a
/// new <see cref="RunUnit"/> object, and <see cref="EveryMember_IsFreshInTheNextRunUnit"/> holds that by
/// reflection over the members, never by a list.</para>
/// <para>The second test is the structure that makes the next case automatic: it enumerates EVERY writable static
/// field the runtime assembly declares and fails on one that is not a documented PROCESS-lifetime store. A new
/// run-unit store written as a static cannot pass without someone writing, here, why it outlives the run unit.
/// (A <c>static readonly</c> reference to a mutable collection is the sibling blind spot — kb/Work PB1570 — and
/// <see cref="NoStaticMutableCollection_OutsideTheDocumentedProcessStores"/> closes it by the same register.)</para>
/// </remarks>
public sealed class RunUnitStateDriftTests
{
    /// <summary>The note's witness. It needs no knowledge of the implementor seed: it is an INEQUALITY against a
    /// value §15.75.4 r2 makes deterministic (draw 4 of the seed-7 sequence).</summary>
    [Fact]
    public void Random_DoesNotContinueAcrossARunUnitReset()
    {
        double draw4 = 0, afterReset = 0;
        RunUnit.Run(_ =>
        {
            CobolIntrinsics.Random((Int128)7);
            CobolIntrinsics.Random();
            CobolIntrinsics.Random();
            draw4 = new Random(7).Skip(3);
            RunUnit.Begin();                   // cross the run-unit boundary (the emitted driver's ProgramRegistry.Reset)
            afterReset = CobolIntrinsics.Random();
        });
        Assert.NotEqual(draw4, afterReset);
    }

    /// <summary>The same through the host lifecycle boundary: a fresh <see cref="RunUnit.Run"/> scope begins its
    /// own sequence, while INSIDE one run unit §15.75.3 r5 continues the current sequence and r3 restarts it.</summary>
    [Fact]
    public void Random_IsScopedToTheRunUnit_AndContinuesWithinIt()
    {
        double[] first = new double[3];
        double secondRunUnit = 0;
        RunUnit.Run(_ =>
        {
            first[0] = CobolIntrinsics.Random((Int128)7);
            first[1] = CobolIntrinsics.Random();
            first[2] = CobolIntrinsics.Random((Int128)7);   // r3: a new sequence from the same seed
        });
        RunUnit.Run(_ => secondRunUnit = CobolIntrinsics.Random());

        var reference = new Random(7);
        Assert.Equal(reference.NextDouble(), first[0]);
        Assert.Equal(reference.NextDouble(), first[1]);     // r5: the NEXT number of the current sequence
        Assert.Equal(first[0], first[2]);                   // r3 + §15.75.4 r2: same seed, same sequence
        Assert.NotEqual(new Random(7).Skip(2), secondRunUnit);
    }

    /// <summary>kb/Work PB1069 — the structure: EVERY instance field of <see cref="RunUnit"/> (reflected, so a member
    /// added tomorrow is covered today) is fresh in the run unit <see cref="RunUnit.Begin"/> starts, except the
    /// declared <see cref="RunUnit.HostConfiguration"/>, which carries over. Each field of the first run unit is
    /// DIRTIED first where a COBOL statement can dirty it (a switch set ON, a termination status, the host
    /// configuration changed), so "fresh" cannot pass by the old object's state happening to be the default.</summary>
    [Fact]
    public void EveryMember_IsFreshInTheNextRunUnit()
    {
        RunUnit first = null!, second = null!;
        var fixedClock = new FixedClockForTest();
        RunUnit.Run(ru =>
        {
            first = ru;
            ru.Switches.Set("SWITCH-1", true);             // §12.3.7 GR4 NOTE 1 — run-unit scope
            ru.ExitStatus = 7;                            // a STOP RUN WITH STATUS 7
            ru.Clock = fixedClock;                        // host configuration: carried over
            ru.DebugMode = false;
            _ = ru.FactoryObject<ProbeFactory>();         // §9.3.14.2 — created in THIS run unit
            second = RunUnit.Begin();                     // the emitted driver's ProgramRegistry.Reset()
            Assert.Same(second, RunUnit.Current);
            Assert.Equal(0, Environment.ExitCode);        // the flushed copy follows the NEW run unit's status
        });

        var hostBacking = RunUnit.HostConfiguration.Select(p => $"<{p}>k__BackingField").ToHashSet(StringComparer.Ordinal);
        Assert.Equal(RunUnit.HostConfiguration.Count, hostBacking.Count(n =>
            typeof(RunUnit).GetField(n, BindingFlags.Instance | BindingFlags.NonPublic) is not null));
        var stale = new List<string>();
        foreach (FieldInfo f in typeof(RunUnit).GetFields(BindingFlags.Instance | BindingFlags.Public
                                                           | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            object? a = f.GetValue(first), b = f.GetValue(second);
            if (hostBacking.Contains(f.Name))
            {
                if (!Equals(a, b)) stale.Add($"{f.Name}: host configuration was NOT carried over ({a} -> {b})");
                continue;
            }
            bool fresh = f.FieldType.IsValueType ? Equals(b, f.GetValue(new RunUnit())) : !ReferenceEquals(a, b);
            if (!fresh) stale.Add($"{f.Name}: carried from the previous run unit");
        }
        Assert.True(stale.Count == 0, "run-unit member(s) not fresh after RunUnit.Begin (kb/Work PB1069):\n  "
            + string.Join("\n  ", stale));

        // The dirtied values, read back through the public surface.
        Assert.False(second.Switches.Get("SWITCH-1"));
        Assert.Equal(0, second.ExitStatus);
        Assert.Same(fixedClock, second.Clock);
        Assert.False(second.DebugMode);
        Assert.NotSame(first.FactoryObject<ProbeFactory>(), second.FactoryObject<ProbeFactory>());
        Assert.Same(second.FactoryObject<ProbeFactory>(), second.FactoryObject<ProbeFactory>());
    }

    /// <summary>kb/Work PB1069's remainder — STATIC storage is the one kind of run-unit state a new
    /// <see cref="RunUnit"/> object cannot make fresh by construction, so the run unit ADOPTS each unit's static reset:
    /// it runs at adoption (§14.6.2.3.2 1) — initial state at the first activation in a run unit), once per reset
    /// however many factories on a subclass chain adopt it, and again at <see cref="RunUnit.Terminate"/> (§14.6.11 3/4/6 —
    /// what the static held is released). Both run-unit boundaries end in that one epilogue.</summary>
    [Fact]
    public void StaticStorage_IsResetAtAdoption_OnceEach_AndAgainAtTermination()
    {
        StaticProbe.Resets = 0;
        RunUnit.Run(ru =>
        {
            ru.AdoptStaticStorage(StaticProbe.Reset);
            ru.AdoptStaticStorage(StaticProbe.Reset);      // a second factory on the chain — no second reset
            Assert.Equal(1, StaticProbe.Resets);
        });
        Assert.Equal(2, StaticProbe.Resets);                // RunUnit.Run's termination ran the adopted reset
    }

    private static class StaticProbe
    {
        public static int Resets;
        public static void Reset() => Resets++;
    }

    private sealed class ProbeFactory : CobolObject;

    private sealed class FixedClockForTest : IClock
    {
        public DateTimeOffset Now() => new(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);
    }

    /// <summary>Every writable static field in <c>Cobol.Net.Runtime</c>, with the reason it is PROCESS state.
    /// Adding an entry is a claim that the state must survive a run-unit boundary — write the reason.</summary>
    private static readonly Dictionary<string, string> ProcessLifetimeStatics = new(StringComparer.Ordinal)
    {
        ["CobolNet.Runtime.IO.StandardStreams::_applied"] =
            "the console encoding is a property of the PROCESS's standard streams, applied once",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_initialized"] =
            "idempotence latch for the process-wide collation subsystem initialization",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_hostConfigured"] =
            "records that the HOST configured the process-wide collation caches",
        ["CobolNet.Runtime.Collation.CollationRuntime::s_lastWarmup"] =
            "diagnostic status of the process-wide collation warm-up",
        ["CobolNet.Runtime.Collation.Cache.CollationKeyCache::s_defaultConfig"] =
            "process-wide cache sizing for collation keys derived from immutable collators",
        ["CobolNet.Runtime.CobolTable+Scratch`1::s_cell"] =
            "the out-of-range reference scratch cell: [ThreadStatic] (one per thread, so concurrent run units never "
            + "share it — kb/Work PB1069) and overwritten before EVERY use, so it carries nothing between uses",
        ["CobolNet.Runtime.ActivationStack::t_activationFloor"] =
            "the activation floor of the run unit's THREAD: [ThreadStatic], set once when ActivationStack starts the "
            + "thread from that thread's own stack bounds, and a property of the thread's stack, not of any run unit's "
            + "state (kb/Work PB2659)",
        ["CobolNet.Runtime.PointerImage::s_nextBase"] =
            "the pointer-image base allocator (kb/Work PB970 arm 2, DOC-A.1-216): its bases key the process-wide "
            + "area and name tables beside it, so it must count per PROCESS — a per-run-unit restart would hand a "
            + "second run unit bases the tables already hold for other areas, and two distinct pointers would image equal",
    };

    [Fact]
    public void NoWritableStatic_OutsideTheDocumentedProcessStores()
    {
        var offenders = new List<string>();
        foreach (Type t in typeof(RunUnit).Assembly.GetTypes())
        {
            if (IsCompilerGenerated(t)) continue;   // lambda / method-group caches: immutable delegates
            foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                                 | BindingFlags.DeclaredOnly))
            {
                if (f.IsInitOnly || f.IsLiteral || f.IsDefined(typeof(CompilerGeneratedAttribute))
                    && !f.Name.Contains("BackingField", StringComparison.Ordinal)) continue;
                string key = $"{t.FullName}::{f.Name}";
                if (!ProcessLifetimeStatics.ContainsKey(key)) offenders.Add(key);
            }
        }
        Assert.True(offenders.Count == 0,
            "writable static field(s) in Cobol.Net.Runtime — run-unit state belongs on RunUnit (ISO §14.6.1; "
            + "kb/Work PB307); a genuinely process-lifetime store is documented in ProcessLifetimeStatics:\n  "
            + string.Join("\n  ", offenders));

        // The register must not rot: every documented entry still exists.
        var live = typeof(RunUnit).Assembly.GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                         | BindingFlags.DeclaredOnly).Select(f => $"{t.FullName}::{f.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        var stale = ProcessLifetimeStatics.Keys.Where(k => !live.Contains(k)).ToList();
        Assert.True(stale.Count == 0, "ProcessLifetimeStatics names field(s) that no longer exist: " + string.Join(", ", stale));
    }

    /// <summary>Every <c>static readonly</c> MUTABLE COLLECTION in <c>Cobol.Net.Runtime</c>, with the reason it is
    /// PROCESS state (kb/Work PB1570). <see cref="NoWritableStatic_OutsideTheDocumentedProcessStores"/> cannot see
    /// these — the field is read-only, its CONTENTS are not — and that blind spot hid <c>CobolSort</c>'s sort-file
    /// store (a <c>static readonly Dictionary</c> of every executing SORT/MERGE statement, shared by every run unit
    /// in the process) until two run units sorted at once. A new entry is a claim that what the collection holds
    /// is immutable-keyed derived data or a once-per-process fact, never a run unit's state: write the reason.</summary>
    private static readonly Dictionary<string, string> ProcessLifetimeCollections = new(StringComparer.Ordinal)
    {
        ["CobolNet.Runtime.Collation.Cache.CollationKeyCache::s_perCollator"] =
            "memo of collation keys per collator: derived from immutable collators, keyed by the collator object",
        ["CobolNet.Runtime.Collation.Cldr.CldrLocaleLoader::s_cache"] =
            "memo of parsed CLDR locale data, keyed by locale name: derived from immutable embedded data",
        ["CobolNet.Runtime.Collation.CollationEngine::s_locales"] =
            "memo of resolved locale collations, keyed by locale name: derived from immutable CLDR data",
        ["CobolNet.Runtime.Collation.CollationEngine::s_collators"] =
            "memo of collators, keyed by (table, options): derived from immutable inputs",
        ["CobolNet.Runtime.Collation.Locale.LocaleManager::s_infos"] =
            "memo of locale facts, keyed by locale name: derived from immutable platform data",
        ["CobolNet.Runtime.Globalization.LocaleFacts::s_cache"] =
            "memo of locale facts, keyed by locale name: derived from immutable platform data",
        ["CobolNet.Runtime.LocaleCollation::s_orders"] =
            "memo of order vectors, keyed by the (immutable) collator",
        ["CobolNet.Runtime.Globalization.MonetaryFacts::s_cache"] =
            "memo of monetary facts, keyed by the (immutable) LocaleFacts object",
        ["CobolNet.Runtime.Globalization.TimeFacts::s_cache"] =
            "memo of LC_TIME facts, keyed by the (immutable) LocaleFacts object",
        ["CobolNet.Runtime.CobolIntrinsics::s_orderingCollators"] =
            "memo of collators for FUNCTION ORDERING, keyed by (table, level): derived from immutable inputs",
        ["CobolNet.Runtime.Exceptions.EcCheckingProfile::Cache"] =
            "memo of checking profiles, keyed by the profile's immutable name",
        ["CobolNet.Runtime.Exceptions.ExceptionCatalog::Table"] =
            "the standard's exception-condition table (Table 12), built once by the static initializer and never mutated",
        ["CobolNet.Runtime.PointerImage::s_areaBases"] =
            "see PointerImage::s_nextBase in " + nameof(ProcessLifetimeStatics) + ": pointer-image bases count per PROCESS",
        ["CobolNet.Runtime.PointerImage::s_nameBases"] =
            "see PointerImage::s_nextBase in " + nameof(ProcessLifetimeStatics) + ": pointer-image bases count per PROCESS",
        ["CobolNet.Runtime.IO.RecordLayoutNotice::Reported"] =
            "the (file, host path) pairs already noticed on standard error: a once-per-process diagnostic latch, locked",
    };

    private static readonly HashSet<string> MutableCollectionDefinitions =
    [
        "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.List`1", "System.Collections.Generic.Queue`1",
        "System.Collections.Generic.Stack`1", "System.Collections.Generic.SortedDictionary`2",
        "System.Collections.Generic.SortedSet`1", "System.Collections.Generic.LinkedList`1",
        "System.Collections.Concurrent.ConcurrentDictionary`2", "System.Collections.Concurrent.ConcurrentQueue`1",
        "System.Collections.Concurrent.ConcurrentStack`1", "System.Collections.Concurrent.ConcurrentBag`1",
        "System.Runtime.CompilerServices.ConditionalWeakTable`2",
    ];

    /// <summary>kb/Work PB1570 — the structure that makes the next case automatic: a <c>static readonly</c> mutable
    /// collection in the runtime must be a documented PROCESS-lifetime store, so a run unit's state cannot hide in
    /// one (<see cref="RunUnit"/> owns it instead).</summary>
    [Fact]
    public void NoStaticMutableCollection_OutsideTheDocumentedProcessStores()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (Type t in typeof(RunUnit).Assembly.GetTypes())
        {
            if (IsCompilerGenerated(t)) continue;
            foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                                 | BindingFlags.DeclaredOnly))
            {
                if (f.IsLiteral || f.IsDefined(typeof(CompilerGeneratedAttribute))) continue;
                Type ft = f.FieldType;
                if (ft.IsGenericType && MutableCollectionDefinitions.Contains(ft.GetGenericTypeDefinition().FullName!))
                    found.Add($"{t.FullName}::{f.Name}");
            }
        }
        var offenders = found.Where(k => !ProcessLifetimeCollections.ContainsKey(k)).Order().ToList();
        Assert.True(offenders.Count == 0,
            "static mutable collection(s) in Cobol.Net.Runtime — run-unit state belongs on RunUnit (ISO §14.6.1; "
            + "kb/Work PB1570); a genuinely process-lifetime store is documented in ProcessLifetimeCollections:\n  "
            + string.Join("\n  ", offenders));
        var stale = ProcessLifetimeCollections.Keys.Where(k => !found.Contains(k)).Order().ToList();
        Assert.True(stale.Count == 0,
            "ProcessLifetimeCollections names field(s) that are no longer static mutable collections: " + string.Join(", ", stale));
    }

    private static bool IsCompilerGenerated(Type t)
    {
        for (Type? x = t; x is not null; x = x.DeclaringType)
            if (x.IsDefined(typeof(CompilerGeneratedAttribute))) return true;
        return false;
    }
}

file static class RandomDraws
{
    /// <summary>The value the <paramref name="n"/>+1-th draw of <paramref name="r"/> returns.</summary>
    public static double Skip(this Random r, int n)
    {
        for (int i = 0; i < n; i++) r.NextDouble();
        return r.NextDouble();
    }
}
