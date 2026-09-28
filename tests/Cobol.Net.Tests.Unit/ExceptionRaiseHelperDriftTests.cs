// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.CodeGen;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB676 — the invariant that keeps ISO §14.6.13.1.1's raise rule TRUE PER CONDITION as conditions are
/// added: <b>the ambient flag a runtime raise helper READS is the flag the emitter SETS for the exception-name
/// that helper raises.</b>
///
/// <para><b>Why this test exists.</b> The rule is one sentence — "if checking for an exception that occurs is not
/// enabled, no exception condition is raised" — but it is applied once per (flag, name) pair, and the pair used to
/// be transcribed twice: once in <c>EcEmitter</c>'s gate table (which flag the statement guard sets when a TURN
/// enables the name) and once by hand in the helper body (which flag the raise site tests). Nothing compared them.
/// A helper written with a NEIGHBOUR's checking flag — one word, in a body identical to its twenty-one siblings —
/// is silent in both directions: <c>TURN {name} CHECKING ON</c> does not arm the raise, and enabling some
/// unrelated condition does. The pairing is asserted BEHAVIOURALLY here (invoke the helper, observe what it
/// raises and under which flag) rather than by reading names, so a helper that raises the right name through the
/// wrong flag cannot pass.</para>
///
/// <para>The fatality is checked the same way, against <see cref="ExceptionCatalog"/> — the machine form of
/// §14.6.13.1.6 Table 13 — so a helper whose <c>fatal:</c> argument disagrees with the standard's category fails
/// here rather than by terminating (or failing to terminate) a user's run unit.</para>
/// </summary>
public sealed class ExceptionRaiseHelperDriftTests
{
    /// <summary>Both <see cref="EcEmitter"/> gate tables as ONE (exception-name → ambient flag) map: the pairing
    /// the EMITTER commits to when a TURN enables a condition at a statement. A null flag is a gate row for an
    /// UNCONDITIONAL raise site (EC-OO-NULL, EC-OO-METHOD, the EC-SIZE family) — the row exists only so the
    /// statement still gets its try/catch.</summary>
    private static readonly Dictionary<string, string?> EmitterGates =
        EcEmitter.FatalAmbientGates
            .Concat(EcEmitter.NonfatalAmbientGates.Select(g => (Ec: g.Ec, Flag: (string?)g.Flag)))
            .ToDictionary(g => g.Ec, g => g.Flag, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every ambient checking flag on the engine — reflected ONCE, not once per probe.</summary>
    private static readonly PropertyInfo[] FlagProperties =
        [.. typeof(ExceptionEngine)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool) && p.Name.EndsWith("Checking", StringComparison.Ordinal))];

    /// <summary>The raise helpers: every public <c>…Error</c> method on the engine IS a §14.6.13.1.1 raise, so the
    /// census closes by construction — a nineteenth condition's helper joins it the moment it is written.</summary>
    private static readonly MethodInfo[] RaiseHelpers =
        [.. typeof(ExceptionEngine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.EndsWith("Error", StringComparison.Ordinal))];

    private readonly record struct Raise(string? Ec, bool Fatal, bool Threw);

    /// <summary>Invoke one helper on a fresh engine whose ambient flags are exactly those <paramref name="enabled"/>
    /// admits, and report what it raised.</summary>
    private static Raise Probe(MethodInfo helper, Func<string, bool> enabled)
    {
        var engine = new ExceptionEngine();
        foreach (var f in FlagProperties) f.SetValue(engine, enabled(f.Name));

        object[] args = [.. helper.GetParameters().Select(Argument)];
        try
        {
            helper.Invoke(engine, args);
        }
        catch (TargetInvocationException tie) when (tie.InnerException is CobolFatalException fatal)
        {
            // §14.6.13.1.3: the thrown signal and the last exception status describe ONE raise.
            Assert.Equal(engine.LastName, fatal.EcName);
            return new Raise(fatal.EcName, engine.LastFatal, Threw: true);
        }
        return new Raise(engine.LastName, engine.LastFatal, Threw: false);
    }

    private static object Argument(ParameterInfo p) =>
        p.ParameterType == typeof(string) ? "kb/Work PB676 drift probe"
        // PerformVaryingIndexError folds GR3's own test into its gate ("the data item's value is not positive"),
        // so the probe supplies a value that satisfies the precondition. No other helper takes a long.
        : p.ParameterType == typeof(long) ? -1L
        // ArgumentErrorSpaces' substitute width (docs/CONFORMANCE.md DOC-A.1-90); any positive width probes the raise.
        : p.ParameterType == typeof(int) ? 1
        : throw new InvalidOperationException(
            $"a raise helper grew a {p.ParameterType.Name} parameter this probe cannot supply — extend "
            + "Argument() so the helper census stays complete (kb/Work PB676).");

    /// <summary>THE PAIRING. Each helper raises the name it claims, is armed by exactly the flag the emitter sets
    /// for that name, and records Table 13's fatality for it.</summary>
    [Fact]
    public void EveryRaiseHelper_ReadsTheFlagTheEmitterSetsForTheNameItRaises()
    {
        Assert.NotEmpty(RaiseHelpers);

        foreach (var h in RaiseHelpers)
        {
            // 1. What does it raise with everything enabled? A helper that records NOTHING has lost its Set.
            string? raised = Probe(h, _ => true).Ec;
            Assert.True(raised is not null,
                $"{h.Name} raised no exception condition with every checking flag enabled — §14.6.13.1.1 raises the "
                + "condition when checking IS enabled, so this helper has lost its Set and EXCEPTION-STATUS, the "
                + "USE declarative and the PERFORM WHEN all see nothing.");
            string ec = raised!;

            // 2. The emitter has to know the name, and know a FLAG for it: a name with no flag is a raise no TURN
            //    can ever arm.
            Assert.True(EmitterGates.TryGetValue(ec, out string? flag),
                $"{h.Name} raises {ec}, which is in NEITHER EcEmitter gate table — the emitted statement guard would "
                + "never enable it, so the raise is unreachable from COBOL source.");
            Assert.True(flag is not null,
                $"{h.Name} raises {ec}, whose EcEmitter gate row carries FLAG = null (an unconditional raise site). "
                + "A helper that TESTS a flag and a gate row that declares there is none cannot both be right.");

            // 3. With every flag on EXCEPT the one the emitter sets for this name, the helper must raise NOTHING.
            //    That is the assertion the hand-written bodies never had: it fails if a helper reads a neighbour's
            //    flag, and it is why the check is behavioural rather than a name comparison.
            var without = Probe(h, n => !string.Equals(n, flag, StringComparison.Ordinal));
            Assert.True(without.Ec is null,
                $"{h.Name} raised {without.Ec} with {flag} OFF and every other flag ON — it is gated by a NEIGHBOUR's "
                + $"checking flag, so TURN {ec} CHECKING ON would not arm it and turning an unrelated condition on "
                + "would (kb/Work PB676).");

            // 4. …and that flag ALONE arms it.
            var only = Probe(h, n => string.Equals(n, flag, StringComparison.Ordinal));
            Assert.Equal(ec, only.Ec);

            // 5. The recorded fatality is Table 13's, read from the catalog rather than from the helper's own
            //    `fatal:` argument (§14.6.13.1.3 for fatal, §14.6.13.1.4 for nonfatal — one terminates the run
            //    unit when unhandled, the other continues).
            Assert.True(ExceptionCatalog.TryGet(ec, out var info), $"{ec} is not a Table 13 exception-name");
            Assert.Equal(3, info.Level);
            bool fatal = info.Fatality is not EcFatality.Nonfatal;
            Assert.True(only.Threw == fatal,
                $"{h.Name} raises {ec}, which Table 13 categorizes {info.Fatality}, but it "
                + (only.Threw ? "THREW a CobolFatalException" : "returned without throwing")
                + " — the §14.6.13.1.3 / §14.6.13.1.4 dispositions are not interchangeable.");
            Assert.True(only.Fatal == fatal,
                $"{h.Name} recorded LastFatal={only.Fatal} for {ec}, which Table 13 categorizes {info.Fatality} — "
                + "the `fatal:` argument disagrees with the standard's category.");
        }
    }

    /// <summary>Every FLAGGED gate row has a helper that reads it: a condition whose flag and emitter row exist but
    /// whose runtime raise was never written is enabled at every statement and raised at none.
    /// <para>⚠ This is the AMBIENT-GATE SLICE of a wider invariant — "every turnable name in
    /// <see cref="ExceptionCatalog"/> has a raise site", which kb/Work PB349 owns and which reaches the emitter's
    /// and the runtime's non-ambient raise sites too. When PB349's census lands it SUBSUMES this fact; delete this
    /// one then rather than keeping two tests for one job.</para></summary>
    [Fact]
    public void EveryFlaggedEmitterGate_HasARaiseHelperThatReadsIt()
    {
        var raised = RaiseHelpers
            .Select(h => Probe(h, _ => true).Ec)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach ((string ec, string? flag) in EmitterGates)
        {
            if (flag is null) continue;                  // an unconditional raise site — no helper by design
            // The ACTIVATING-HALF gates have no runtime helper, and the gate table says why: each condition is raised
            // only when checking is enabled in BOTH elements (§14.8.4.1; §14.9.23.4 GR7c), so the flag carries the
            // ACTIVATOR's half to an activation boundary, which tests it there. Their reader is asserted below
            // (EveryExternalActivatingFlag_SetsItsOwnBitOfTheActivatingMask) and by the goldens that raise them.
            if (EcEmitter.ActivatingHalfGates.Contains(ec)) continue;
            Assert.True(raised.Contains(ec),
                $"EcEmitter sets ExceptionState.{flag} for {ec}, but no ExceptionEngine …Error helper raises {ec} — "
                + "the condition would be armed at every statement that enables it and raised at none.");
        }
    }

    /// <summary>kb/Work PB1138 — the EC-EXTERNAL activating-half flags are read by the activation boundary through
    /// <see cref="ExceptionEngine.ExternalActivatingMask"/>, so the pairing a raise helper proves for the other gates
    /// is proved here the same BEHAVIOURAL way: the flag the emitter sets for an EC-EXTERNAL name, ALONE, turns on
    /// exactly that name's <see cref="CobolNet.Runtime.ExternalChecks"/> bit — never a neighbour's, and never none.</summary>
    [Fact]
    public void EveryExternalActivatingFlag_SetsItsOwnBitOfTheActivatingMask()
    {
        var external = EmitterGates.Where(g => g.Key.StartsWith("EC-EXTERNAL-", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, external.Count);   // the checkable trio (EC-EXTERNAL-IMP has no raise site)
        foreach (var (ec, flag) in external)
        {
            Assert.NotNull(flag);
            Assert.Contains(ec, EcEmitter.ActivatingHalfGates);
            var engine = new ExceptionEngine();
            foreach (var f in FlagProperties) f.SetValue(engine, string.Equals(f.Name, flag, StringComparison.Ordinal));
            var expected = ec switch
            {
                "EC-EXTERNAL-FORMAT-CONFLICT" => CobolNet.Runtime.ExternalChecks.FormatConflict,
                "EC-EXTERNAL-DATA-MISMATCH" => CobolNet.Runtime.ExternalChecks.DataMismatch,
                "EC-EXTERNAL-FILE-MISMATCH" => CobolNet.Runtime.ExternalChecks.FileMismatch,
                _ => CobolNet.Runtime.ExternalChecks.None,
            };
            Assert.Equal((int)expected, engine.ExternalActivatingMask);
        }
    }

    /// <summary>Every flag the emitter NAMES exists on the engine and on the emitted static shim. The emitter
    /// writes <c>ExceptionState.{flag} = true;</c> as TEXT, so a renamed or deleted flag is not a build error
    /// here — it is a GENERATED-C# compile error in whichever user program enables that one condition.</summary>
    [Fact]
    public void EveryEmitterGateFlag_ExistsOnTheEngineAndOnTheEmittedShim()
    {
        Assert.NotEmpty(EmitterGates);
        foreach ((string ec, string? flag) in EmitterGates)
        {
            if (flag is null) continue;

            var onEngine = typeof(ExceptionEngine).GetProperty(flag, BindingFlags.Public | BindingFlags.Instance);
            Assert.True(onEngine is { CanRead: true, CanWrite: true } && onEngine.PropertyType == typeof(bool),
                $"EcEmitter's gate for {ec} names ExceptionEngine.{flag}, which is not a settable bool property.");

            var onShim = typeof(ExceptionState).GetProperty(flag, BindingFlags.Public | BindingFlags.Static);
            Assert.True(onShim is { CanRead: true, CanWrite: true } && onShim.PropertyType == typeof(bool),
                $"EcEmitter emits `ExceptionState.{flag} = true;` for {ec}, but the static shim has no such "
                + "settable bool property — the generated C# would not compile.");
        }
    }
}
