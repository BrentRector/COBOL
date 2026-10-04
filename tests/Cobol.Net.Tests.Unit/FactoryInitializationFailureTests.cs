// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE RESOURCE AN INVOKE CHECKS: THE CLASS'S TYPE INITIALIZATION (ISO §14.9.23.4 GR7 b; Annex A.1 item 102,
/// docs/CONFORMANCE.md DOC-A.1-102; kb/Work PB1422). The standard gives "the resources necessary to execute the method are
/// not available" the same EC-OO-METHOD as "the method is not found" and leaves the checked resources to the implementor;
/// this implementation checks that the class initializes, which the first reference to its factory object triggers
/// (<see cref="RunUnit.FactoryObject{T}"/>). No COBOL source can make a generated class's initializer throw, so the
/// witness is a hand-written <see cref="CobolObject"/> whose static constructor does.
/// </summary>
public sealed class FactoryInitializationFailureTests
{
    private sealed class BrokenFactory : CobolObject
    {
        static BrokenFactory() => throw new InvalidOperationException("the class's static data could not be initialized");
    }

    private sealed class SoundFactory : CobolObject;

    private sealed class RaisingFactory : CobolObject
    {
        public RaisingFactory() => throw new CobolFatalException("EC-DATA-INCOMPATIBLE", "the factory's VALUE initialization");
    }

    /// <summary>A condition the generated constructor itself raises reaches the COBOL boundary AS ITSELF: `new T()` through a
    /// type parameter wraps it in a <see cref="System.Reflection.TargetInvocationException"/>, which the factory creation
    /// unwraps (it is not the class-initialization failure, so it is not EC-OO-METHOD).</summary>
    [Fact]
    public void ConditionTheConstructorRaises_ReachesTheBoundaryUnwrapped()
    {
        var ex = Assert.Throws<CobolFatalException>(() => new RunUnit().FactoryObject<RaisingFactory>());
        Assert.Equal("EC-DATA-INCOMPATIBLE", ex.EcName);
    }

    /// <summary>A class whose type initializer throws is EC-OO-METHOD (fatal, Table 13), and the CLR remembers the failure,
    /// so every later reference raises it again rather than a bare <see cref="TypeInitializationException"/>.</summary>
    [Fact]
    public void ClassWhoseTypeInitializerThrows_IsEcOoMethod_EveryTime()
    {
        var run = new RunUnit();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var ex = Assert.Throws<CobolFatalException>(() => run.FactoryObject<BrokenFactory>());
            Assert.Equal("EC-OO-METHOD", ex.EcName);
            Assert.Contains(nameof(BrokenFactory), ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>The check costs a class that initializes nothing: its factory object is created once and is the same
    /// object on every later reference (§9.3.14.2).</summary>
    [Fact]
    public void ClassThatInitializes_IsTheSameFactoryObjectEveryTime()
    {
        var run = new RunUnit();
        Assert.Same(run.FactoryObject<SoundFactory>(), run.FactoryObject<SoundFactory>());
    }
}
