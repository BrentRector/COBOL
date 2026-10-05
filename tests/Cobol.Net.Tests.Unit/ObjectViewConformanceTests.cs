// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE OBJECT-VIEW'S RUN-TIME CONFORMANCE CHECK (ISO §8.4.3.5.4 GR2–GR6; kb/Work PB1425), held at the runtime helper
/// every emitted view calls, <see cref="CobolObject.ObjectView{T}"/>. The conformance goldens
/// (<c>2002/pb1425_object_view</c>, <c>2002/pb1425_object_view_unchecked</c>) pin the enabled raise and the lenient
/// ONLY; the one outcome no COBOL golden can compare on stdout is an unrelated object with checking OFF, where the
/// standard sets nothing (§14.8.1 NOTE 3) and typed code cannot hold the object.
/// </summary>
public sealed class ObjectViewConformanceTests
{
    private class Animal : CobolObject;

    private sealed class Dog : Animal;

    private sealed class Rock : CobolObject;

    private static T? View<T>(object? value, bool exact, bool checking) where T : class
    {
        bool saved = ExceptionState.OoConformanceChecking;
        ExceptionState.OoConformanceChecking = checking;
        try { return CobolObject.ObjectView<T>(value, exact, "the probe's view"); }
        finally { ExceptionState.OoConformanceChecking = saved; }
    }

    /// <summary>A NULL reference references no object, so GR2–GR6 find nothing non-conforming.</summary>
    [Fact]
    public void NullReference_IsViewedAsNull_WithoutACheck() => Assert.Null(View<Dog>(null, exact: true, checking: true));

    /// <summary>GR2: an object of the class or a subclass conforms; GR4: ONLY admits exactly the class.</summary>
    [Fact]
    public void SubclassObject_ConformsToTheClass_AndExactlyTheClassUnderOnly()
    {
        var dog = new Dog();
        Assert.Same(dog, View<Animal>(dog, exact: false, checking: true));
        Assert.Same(dog, View<Dog>(dog, exact: true, checking: true));
        var ex = Assert.Throws<CobolFatalException>(() => View<Animal>(dog, exact: true, checking: true));
        Assert.Equal("EC-OO-CONFORMANCE", ex.EcName);
    }

    /// <summary>Checking OFF: nothing is set. An object that failed only ONLY's exact-class test is still a T and is
    /// viewed as one; an object of an unrelated class cannot be held by typed code, so the run unit stops with the
    /// implementor's fatal error — never an InvalidCastException.</summary>
    [Fact]
    public void CheckingOff_ProceedsWhenTypedCodeCanHoldTheObject_AndStopsOtherwise()
    {
        var dog = new Dog();
        Assert.Same(dog, View<Animal>(dog, exact: true, checking: false));
        Assert.Throws<CobolImplementorFatalException>(() => View<Animal>(new Rock(), exact: false, checking: false));
        var ex = Assert.Throws<CobolFatalException>(() => View<Animal>(new Rock(), exact: false, checking: true));
        Assert.Equal("EC-OO-CONFORMANCE", ex.EcName);
    }
}
