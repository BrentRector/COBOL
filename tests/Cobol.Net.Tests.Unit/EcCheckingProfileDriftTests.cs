// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ §14.9.18.4 GR1 b)'s ENABLEMENT QUESTION IS ASKED IN THE ACTIVATING RUNTIME ELEMENT, AND ONLY THERE
/// (kb/Work PB408).
///
/// <para><b>The rule.</b> "If the RAISING phrase is specified, an exception condition is raised in the activating
/// runtime element if checking for that exception condition is enabled in the activating runtime element" — one
/// element, named twice. The compiler used to fold the <b>callee's</b> own <c>&gt;&gt;TURN</c> state into
/// <c>BoundRaising.Enabled</c> at bind time and branch on it, so a declarative in an activator that had enabled
/// the condition never ran when the callee had it off, and one in an activator that had disabled it ran when the
/// callee had it on. §7.3.25.4 GR6/GR8 scope a directive to the statements "that follow in the compilation
/// group", so the two elements differ inside ONE file and both wrong answers are reachable without separate
/// compilation.</para>
///
/// <para><b>What is structurally guarded here.</b> Three things the behavioural goldens cannot see.
/// (1) <see cref="BoundRaising"/> carries NO bind-time enablement decision at all — the field's absence is the
/// fix, not a value it happens to hold. (2) Every <see cref="IActivatingStatement"/> really carries and
/// round-trips a profile, so the central stamp at the one <c>BindStatement</c> exit reaches it. (3) The RUNTIME
/// fold and the COMPILE-TIME fold agree for every catalogued level-3 name over every directive shape — they are
/// two readers of §7.3.25.4's expansion, and <c>ExceptionCatalog.DirectiveCovers</c> exists so the expansion is
/// written once; this test is what keeps the two ANSWERS equal after that.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EcCheckingProfileDriftTests
{
    /// <summary>(1) The absence IS the fix. A bind-time "is checking enabled" flag on the RAISING phrase can only
    /// ever be the RAISING element's answer to a question GR1 b) asks about the ACTIVATING element — and under
    /// separate compilation the activator is not even in the compilation group being bound.</summary>
    [Fact]
    public void BoundRaising_CarriesNoBindTimeEnablementDecision()
    {
        var offenders = typeof(BoundRaising)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .Where(n => n.Contains("Enabled", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Checking", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(offenders.Count == 0,
            "BoundRaising must carry no bind-time enablement decision (ISO §14.9.18.4 GR1 b) localises the test "
            + "in the ACTIVATING runtime element, which is unknown here). Found: " + string.Join(", ", offenders));
    }

    /// <summary>(2) Every activating node carries the profile and round-trips it, so the one stamping site
    /// (<c>EcBinder.EcWrap</c>, the exit every statement funnels through) can reach a new one through the
    /// interface alone.</summary>
    [Fact]
    public void EveryActivatingStatement_RoundTripsItsProfile()
    {
        var kinds = typeof(BoundRaising).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IActivatingStatement).IsAssignableFrom(t))
            .ToList();
        Assert.True(kinds.Count >= 3,
            "expected at least the CALL, INVOKE and universal-INVOKE activating nodes; found "
            + string.Join(", ", kinds.Select(t => t.Name)));
        var profile = EcCheckingProfile.FromEvents([("EC-USER", true)]);
        foreach (var t in kinds)
        {
            var blank = (IActivatingStatement)System.Runtime.CompilerServices
                .RuntimeHelpers.GetUninitializedObject(t);
            Assert.True(blank.WithActivatorChecking(profile) is IActivatingStatement stamped
                && ReferenceEquals(stamped.ActivatorChecking, profile),
                $"{t.Name}.WithActivatorChecking must return a node carrying the profile it was given");
        }
    }

    /// <summary>(3) THE AGREEMENT. For every directive shape and every catalogued level-3 name, the runtime
    /// profile answers exactly what the compile-time fold answers. Two folds over one rule is how a hierarchy
    /// expansion (EC-ALL / a level-2 family / EC-I-O-WARNING's explicit-only carve-out, §7.3.25.4 GR2/GR3/GR4)
    /// comes to mean different things on the two sides of an activation boundary.</summary>
    [Fact]
    public void TheRuntimeProfileAgreesWithTheCompileTimeFold_ForEveryCatalogedName()
    {
        string[] names = [ExceptionCatalog.EcAll, "EC-USER", "EC-I-O", "EC-SIZE", "EC-I-O-WARNING",
                          "EC-SIZE-OVERFLOW", "EC-USER-Q", "EC-RAISING", "EC-RAISING-NOT-SPECIFIED"];
        var edition = new EditionContext(2023);
        var level3 = ExceptionCatalog.Level3Rows.Select(r => r.Name).Concat(["EC-USER-Q", "EC-USER-Z"]).ToList();
        Assert.NotEmpty(level3);   // a vacuous agreement is not agreement (feedback_green_gates_arent_evidence)

        int compared = 0;
        foreach (string a in names)
            foreach (string b in names)
                foreach (bool onA in (bool[])[true, false])
                    foreach (bool onB in (bool[])[true, false])
                    {
                        var turn = TurnState.Build(
                            [new TurnEvent(1, [(a, null)], onA, false),
                             // a FILE-scoped event, which the profile must drop exactly as the file: null fold does
                             new TurnEvent(2, [("EC-I-O", "F1")], true, false),
                             new TurnEvent(3, [(b, null)], onB, false)],
                            edition);
                        var profile = turn.ProfileAt(10);
                        foreach (string n in level3)
                        {
                            Assert.Equal(turn.Enabled(n, null, 10), profile.Enabled(n));
                            compared++;
                        }
                    }
        Assert.True(compared > 1000, $"the agreement matrix collapsed to {compared} comparisons");
    }

    /// <summary>(4) No site may re-acquire a propagation pickup that asks nothing about the activator. The pickup
    /// takes its activating statement, so a new activating verb cannot be wired up without supplying one.</summary>
    [Fact]
    public void NoPropagationPickupIsEmittedWithoutItsActivatingStatement()
    {
        var bare = Directory
            .EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (File: f, Line: i + 1, Text: l)))
            .Where(x => !x.Text.TrimStart().StartsWith("//", StringComparison.Ordinal)
                     && (x.Text.Contains("EmitPropagationPickup()", StringComparison.Ordinal)
                      || x.Text.Contains("EmitInvocationReturned()", StringComparison.Ordinal)))
            .Select(x => $"{Path.GetFileName(x.File)}:{x.Line}")
            .ToList();
        Assert.True(bare.Count == 0,
            "a propagation pickup emitted with no activating statement cannot apply ISO §14.9.18.4 GR1 b)'s "
            + "enablement test (kb/Work PB408). Found: " + string.Join(", ", bare));
    }
}
