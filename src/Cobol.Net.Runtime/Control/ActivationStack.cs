// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// THE RUN UNIT'S EXECUTION STACK — the one runtime resource an activation needs that the host can run out of
/// (kb/Work PB2659; DESIGN-runtime-library §2.1 "The run unit's thread and the activation resource check";
/// docs/CONFORMANCE.md DOC-A.1-14, DOC-A.1-89, DOC-A.1-102).
/// <para>Every COBOL activation — a CALL, a user-defined function reference, an INVOKE or an inline method invocation —
/// is a chain of .NET frames on the thread that runs the run unit, so the depth a RECURSIVE program can reach is the
/// stack that thread has. Two things follow, and both live here:</para>
/// <list type="number">
/// <item>The run unit's MAIN program runs on a thread of its own whose stack is sized for COBOL activation frames
/// (<see cref="RunUnitThreadStackBytes"/>; <see cref="RunOnRunUnitThread"/>, called by
/// <see cref="ProgramTable.RunMain"/>), never on the host's default stack (about 1 MB for a Windows main thread), where a
/// self-CALLing program died at a depth of about 250.</item>
/// <item>Each activation asks <see cref="IsAvailable"/> before control is transferred. ISO §14.9.4.4 GR3c: "If the
/// program is located but the resources necessary to execute the program are not available, the EC-PROGRAM-RESOURCES
/// exception condition is set to exist, the program call is not successful"; §8.4.3.2.4 GR6c says the same of a
/// function activation and §14.9.23.4 GR7b gives a method EC-OO-METHOD. "The runtime resources that are checked … are
/// defined by the implementor" (Annex A.1 items 14, 89 and 102): this implementation checks exactly one, the stack
/// headroom of the running thread. So a recursion deeper than the stack can hold ends in a DEFINED way — the
/// activation is not successful and the condition takes its ON EXCEPTION phrase, a declarative, or the §14.6.12
/// abnormal termination with its §14.6.11 implicit CLOSE — instead of the CLR killing the process, which no handler
/// can intercept.</item>
/// </list>
/// </summary>
public static class ActivationStack
{
    /// <summary>The stack the run unit's thread is given: 384 MiB of reserved address space, which the operating system
    /// commits only as deep as the run unit actually goes. Chosen from measurement (kb/Work PB2659; the table is in
    /// DESIGN-runtime-library §2.1): the target is the depth GnuCOBOL 3.2 reaches on its default 8 MB stack with a
    /// minimal self-CALLing program (74,687 activations, after which it dies of SIGSEGV), reached by the SAME program
    /// under its most expensive frame shape here, <c>&gt;&gt;TURN EC-ALL CHECKING ON</c>, about 3.9 KB per activation
    /// on the JIT's first-tier frames, which a deep recursion mostly runs in. Measured at this size: 104,585 activations
    /// on Windows and 92,137 on Linux; unchecked, 256,126; and 32,566 for a program with twenty statements and
    /// LOCAL-STORAGE under full checking (12.4 KB per activation).</summary>
    public const int RunUnitThreadStackBytes = 384 * 1024 * 1024;

    /// <summary>The stack every activation on the run unit's thread is guaranteed beyond its own entry: the check fails
    /// once less than this remains. It is the room the deepest activation has for its own statements — its PERFORM
    /// nesting, a SORT's procedures, a declarative — and for the ON EXCEPTION phrase or declarative that handles the
    /// activation that fails, so the stack is not exhausted between two checks. On any other thread the .NET runtime's
    /// own margin applies (<see cref="RuntimeHelpers.TryEnsureSufficientExecutionStack"/>, 128 KB on a 64-bit
    /// process).</summary>
    public const int ActivationReserveBytes = 1024 * 1024;

    /// <summary>On the run unit's thread, the stack address below which an activation is refused
    /// (<see cref="ActivationReserveBytes"/> above the bottom of the thread's stack); 0 on every other thread, which
    /// leaves only the runtime's own margin. [ThreadStatic]: a property of the THREAD, set once when the run unit's
    /// thread starts.</summary>
    [ThreadStatic] private static nint t_activationFloor;

    /// <summary>Run <paramref name="body"/> — the run unit's main program and its termination epilogue — on a new
    /// thread whose stack is <see cref="RunUnitThreadStackBytes"/>, and wait for it. The thread is transparent to the
    /// caller: the ambient <see cref="RunUnit"/> (an <see cref="AsyncLocal{T}"/>) flows to it with the execution
    /// context, it takes the caller's culture, and whatever escapes <paramref name="body"/> — the
    /// <see cref="StopRun"/> unwind the generated <c>Main</c> catches, or a defect's exception — is rethrown on the
    /// calling thread with its original stack trace.</summary>
    internal static void RunOnRunUnitThread(Action body)
    {
        ExceptionDispatchInfo? escaped = null;
        var thread = new Thread(() =>
        {
            // The thread's entry frame is within a few KB of the top of its stack, which the reserve absorbs.
            t_activationFloor = StackAddress() - RunUnitThreadStackBytes + ActivationReserveBytes;
            try { body(); }
            catch (Exception e) { escaped = ExceptionDispatchInfo.Capture(e); }
        }, RunUnitThreadStackBytes)
        {
            Name = "COBOL run unit",
            CurrentCulture = CultureInfo.CurrentCulture,
            CurrentUICulture = CultureInfo.CurrentUICulture,
        };
        thread.Start();
        thread.Join();
        escaped?.Throw();
    }

    /// <summary>The §14.9.4.4 GR3c / §8.4.3.2.4 GR6c / §14.9.23.4 GR7b resource check: does the running thread's stack
    /// have the headroom one more activation needs? Always the .NET runtime's own measure against the thread's real
    /// stack bounds ("enough stack to execute the average .NET function", <see cref="RuntimeHelpers.TryEnsureSufficientExecutionStack"/>),
    /// so the check holds on any host thread that activates COBOL directly; on the run unit's own thread also the
    /// larger <see cref="ActivationReserveBytes"/>.</summary>
    internal static bool IsAvailable() =>
        RuntimeHelpers.TryEnsureSufficientExecutionStack()
        && (t_activationFloor == 0 || StackAddress() > t_activationFloor);

    /// <summary>What every arm's diagnostic says about the exhausted resource, after it names the activation.</summary>
    internal const string Exhausted =
        "is not available — the run unit's activations have used the stack the implementation provides";

    /// <summary>The method arm of the resource check (§14.9.23.4 GR7b), emitted at the head of every method body that
    /// executes statements, before its §14.9.23.4 GR7d external-item check and before the method's activation frame is
    /// pushed: "If the method is not found or the resources necessary to execute the method are not available, the
    /// EC-OO-METHOD exception condition is set to exist, the method invocation is not successful". The CALL and function
    /// arm is <see cref="ProgramTable.CallProgram"/>'s, which raises EC-PROGRAM-RESOURCES through the CALL carrier.</summary>
    public static void RequireForMethod(string method, string className)
    {
        if (!IsAvailable())
            throw new CobolFatalException("EC-OO-METHOD",
                $"INVOKE '{method}' of class '{className}': the stack the method needs {Exhausted} "
                + "(ISO §14.9.23.4 GR7b; docs/CONFORMANCE.md DOC-A.1-102)");
    }

    /// <summary>The address of a local of this frame — the running thread's stack position (the stack grows down on
    /// every platform .NET supports). Taken as a byte offset from the null reference, which needs no unsafe context.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint StackAddress()
    {
        byte probe = 0;
        return Unsafe.ByteOffset(ref Unsafe.NullRef<byte>(), ref probe);
    }
}
