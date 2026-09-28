// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §14.8.4.1 EC-EXTERNAL "enabled in both" pairing at the activation boundaries. The ACTIVATING element's half is
/// the statement guard's checking flags (<see cref="ExceptionEngine.ExternalActivatingMask"/>), read by the boundary
/// BEFORE the activated element's own checking scope opens, and latched as <see cref="ExceptionEngine.ActivatorExternalMask"/>
/// for the activated element's registrations. It replaced a pending CALL-site mask register that every failure path
/// had to zero by hand (kb/Work PB133 found one path that did not; kb/Work PB1138 retired the register when the INVOKE
/// arm needed the same half): the flags are a saved-and-restored scope, so a failed attempt has nothing to leak.
/// </summary>
public sealed class ProgramActivationMaskTests
{
    private sealed class Fake : ICobolProgram
    {
        public System.Action? OnCall;
        public void Call(CobolArg[] args, CobolArg? returning) => OnCall?.Invoke();
        public void Activate() { }
        public void CloseFiles() { }
    }

    [Fact]
    public void FailedActivation_LeavesTheActivatorHalfUntouched()
    {
        var ru = new RunUnit();
        ru.Exceptions.ActivatorExternalMask = 0b0111;   // the CALLER's own activator half — must be untouched
        ru.Exceptions.ExternalFileMismatchChecking = true;
        Assert.Throws<CobolCallException>(() => ru.Programs.CallProgram("NOWHERE", "MAIN", [], null));
        Assert.Equal(0b0111, ru.Exceptions.ActivatorExternalMask);
    }

    [Fact]
    public void SuccessfulActivation_LatchesTheStatementFlagsAsTheActivatorHalf()
    {
        var ru = new RunUnit();
        int seenDuringCall = -1, flagsDuringCall = -1;
        var fake = new Fake();
        fake.OnCall = () =>
        {
            seenDuringCall = ru.Exceptions.ActivatorExternalMask;    // the activated element reads the CALL's half
            flagsDuringCall = ru.Exceptions.ExternalActivatingMask;  // its own statements start from all-off, so a
        };                                                           // nested CALL's guard sets its own half
        ru.Programs.Register("P1", "P1", null, initial: false, common: false, recursive: false, _ => fake);
        ru.Exceptions.ActivatorExternalMask = 0b0001;
        ru.Exceptions.ExternalDataMismatchChecking = true;
        ru.Exceptions.ExternalFileMismatchChecking = true;
        ru.Programs.CallProgram("P1", "MAIN", [], null);
        Assert.Equal((int)(ExternalChecks.DataMismatch | ExternalChecks.FileMismatch), seenDuringCall);
        Assert.Equal(0, flagsDuringCall);
        Assert.Equal(0b0001, ru.Exceptions.ActivatorExternalMask);   // restored on return
        Assert.Equal((int)(ExternalChecks.DataMismatch | ExternalChecks.FileMismatch), ru.Exceptions.ExternalActivatingMask);
    }

    [Fact]
    public void MethodActivation_PairsTheInvokeFlagsWithTheMethodMask_AndFailsAsAFatalCondition()
    {
        // §14.9.23.4 GR7 d) (kb/Work PB1138): the method twin of the CALL boundary. A conforming registration first,
        // then a nonconforming one: with EC-EXTERNAL-FORMAT-CONFLICT enabled in the INVOKE (the flag) AND in the method
        // (the self mask) the invocation is not successful — a CobolFatalException the INVOKE's guard selects on —
        // and the activator half is restored either way.
        var ru = RunUnit.Current;   // the run unit the ExternalStore facade reaches (an AsyncLocal)
        var checking = ru.Exceptions.SaveChecking();
        int savedActivator = ru.Exceptions.ActivatorExternalMask;
        try
        {
            ExternalStore.DescribeAtMethodActivation(self => ExternalStore.Describe("C1", "W68R-PB1138-EXX",
                new ExternalDescriptor("record", ByteCount: 8), self), 0);
            ru.Exceptions.ActivatorExternalMask = 0b1000;
            ru.Exceptions.ExternalFormatConflictChecking = true;
            int mask = (int)ExternalChecks.FormatConflict;
            void Nonconforming() => ExternalStore.DescribeAtMethodActivation(self => ExternalStore.Describe("C2",
                "W68R-PB1138-EXX", new ExternalDescriptor("record", ByteCount: 4), self), mask);
            var x = Assert.Throws<CobolFatalException>(Nonconforming);
            Assert.Equal("EC-EXTERNAL-FORMAT-CONFLICT", x.EcName);
            Assert.Equal(0b1000, ru.Exceptions.ActivatorExternalMask);
            ExternalStore.DescribeAtMethodActivation(self => ExternalStore.Describe("C2", "W68R-PB1138-EXX",
                new ExternalDescriptor("record", ByteCount: 4), self), 0);   // not enabled in the method: stores, no check
        }
        finally
        {
            ru.Exceptions.RestoreChecking(checking);   // the flags are a scope (kb/Work PB891) — leave none standing
            ru.Exceptions.ActivatorExternalMask = savedActivator;
        }
    }
}
