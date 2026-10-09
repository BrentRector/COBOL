// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.CompilerServices;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB2659 — the activation's stack is the runtime resource ISO §14.9.4.4 GR3c, §8.4.3.2.4 GR6c and
/// §14.9.23.4 GR7b check (docs/CONFORMANCE.md DOC-A.1-14, -89, -102; <see cref="ActivationStack"/>). Before the fix a
/// RECURSIVE program died of a CLR stack overflow at a depth of about 250, which no handler can intercept. These facts
/// drive the LIMIT path at the runtime surface, on threads whose stacks are small enough to exhaust in a unit test:
/// each recursion must END in the defined condition and leave the run unit balanced, never take the process down
/// (a regression here is a crashed test host, which the gate reports as a red).</summary>
public sealed class ActivationStackTests
{
    /// <summary>A program whose every activation CALLs itself again through the run unit's program table — the shape
    /// of a RECURSIVE COBOL program that never stops, at the runtime surface.</summary>
    private sealed class SelfCaller(ProgramTable table, string name, string notFoundEc, Action onActivation) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning)
        {
            onActivation();
            table.CallProgram(name, name, [], null, notFoundEc);
        }
        public void Activate() { }
        public void CloseFiles() { }
    }

    /// <summary>Run <paramref name="body"/> on a host thread with a 1 MiB stack (not the run unit's own thread, so only
    /// the .NET runtime's margin applies) and return what escaped it.</summary>
    private static Exception? OnSmallStack(Action body)
    {
        Exception? escaped = null;
        var t = new Thread(() =>
        {
            try { body(); }
            catch (Exception e) { escaped = e; }
        }, 1024 * 1024);
        t.Start();
        t.Join();
        return escaped;
    }

    [Theory]
    [InlineData(false, "§14.9.4.4 GR3c")]   // a CALL
    [InlineData(true, "§8.4.3.2.4 GR6c")]   // a user-defined function activation
    public void RecursiveCall_PastTheStack_IsEcProgramResources_AndUnwindsBalanced(bool function, string rule)
    {
        var ru = new RunUnit();
        int depth = 0;
        string notFoundEc = function ? "EC-FUNCTION-NOT-FOUND" : "EC-PROGRAM-NOT-FOUND";
        ru.Programs.RegisterModule("Cobol.R.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi, () =>
            ru.Programs.Register("R", "R", null, initial: false, common: false, recursive: true,
                _ => new SelfCaller(ru.Programs, "R", notFoundEc, () => depth++), isFunction: function));

        var escaped = OnSmallStack(() => ru.Programs.CallProgram("R", "", [], null, notFoundEc));

        var cx = Assert.IsType<CobolCallException>(escaped);
        Assert.Equal("EC-PROGRAM-RESOURCES", cx.EcName);
        Assert.Contains(rule, cx.Message, StringComparison.Ordinal);
        Assert.True(depth > 10, $"the recursion stopped at depth {depth}, before the stack was used");
        // §14.9.4.4 GR3i: the condition crossed every activation boundary above the one that failed, so no CALL site
        // above it may take it as its own failure — the boundary marks it, through an exception FILTER (a catch-and-
        // rethrow nested one dispatch per boundary and overflowed the stack while unwinding).
        Assert.True(cx.ControlTransferred);
        // Every activation the recursion opened was closed again on the way out.
        Assert.Equal(0, ru.Modules.CurrentActivation);
    }

    /// <summary>§14.9.23.4 GR7b — the method arm: the emitted method prologue raises EC-OO-METHOD.</summary>
    [Fact]
    public void RecursiveMethod_PastTheStack_IsEcOoMethod()
    {
        int depth = 0;
        // Returns a value so the recursive call is never in tail position (a tail call would not grow the stack).
        int Method()
        {
            ActivationStack.RequireForMethod("DIVE", "C");
            depth++;
            return Method() + 1;
        }

        var fx = Assert.IsType<CobolFatalException>(OnSmallStack(() => Method()));
        Assert.Equal("EC-OO-METHOD", fx.EcName);
        Assert.Contains("§14.9.23.4 GR7b", fx.Message, StringComparison.Ordinal);
        Assert.True(depth > 10, $"the recursion stopped at depth {depth}, before the stack was used");
    }

    /// <summary>On the run unit's own thread an activation is refused while the .NET runtime's own margin still holds:
    /// the deepest activation keeps <see cref="ActivationStack.ActivationReserveBytes"/> for its own statements and for
    /// the handler of the activation that fails, so the stack cannot run out between two checks.</summary>
    [Fact]
    public void RunUnitThread_RefusesAnActivation_BeforeTheRuntimesOwnMarginIsReached()
    {
        bool runtimeMarginAtRefusal = false;
        int depth = 0;
        // Returns a value so the recursive call is never in tail position (a tail call would not grow the stack).
        int Dive()
        {
            if (!ActivationStack.IsAvailable())
            {
                runtimeMarginAtRefusal = RuntimeHelpers.TryEnsureSufficientExecutionStack();
                return 0;
            }
            depth++;
            return Dive() + 1;
        }

        ActivationStack.RunOnRunUnitThread(() => Dive());

        Assert.True(depth > 100_000, $"the run unit's thread refused at depth {depth}");
        Assert.True(runtimeMarginAtRefusal);
    }

    /// <summary>The run unit's thread is transparent to its caller: the ambient run unit and the culture flow into it,
    /// and what escapes it — the STOP RUN unwind the generated Main catches — is rethrown on the calling thread.</summary>
    [Fact]
    public void RunUnitThread_IsTransparent_ToTheCaller()
    {
        var caller = Thread.CurrentThread;
        var savedCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            RunUnit.Run(ru =>
            {
                RunUnit? seen = null;
                Thread? ranOn = null;
                string? culture = null;
                Assert.Throws<StopRun>(() => ActivationStack.RunOnRunUnitThread(() =>
                {
                    seen = RunUnit.Current;
                    ranOn = Thread.CurrentThread;
                    culture = CultureInfo.CurrentCulture.Name;
                    throw new StopRun();
                }));
                Assert.Same(ru, seen);
                Assert.NotSame(caller, ranOn);
                Assert.Equal("de-DE", culture);
            });
        }
        finally { CultureInfo.CurrentCulture = savedCulture; }
    }
}
