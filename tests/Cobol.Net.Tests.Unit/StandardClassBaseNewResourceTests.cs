// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1524 — ISO/IEC 1989:2023 §16.2.1.2 GR2: "If resources needed to create a new object are not available,
/// the returned object reference is set to NULL, and the EC-OO-RESOURCE exception condition is set to exist and is
/// propagated back to the runtime element that invoked the New method." Every New form reaches the ONE body
/// <see cref="BASE__FACTORY.__New"/>; a golden cannot exhaust memory, so these cases inject the allocation failure
/// through the creation seam <c>__Create</c> that every emitted factory overrides.
/// </summary>
public sealed class StandardClassBaseNewResourceTests
{
    private sealed class Thing : BASE
    {
        protected override BASE__FACTORY __FactoryOfClass => ThingFactory.Instance;
    }

    private sealed class ThingFactory(bool exhausted) : BASE__FACTORY
    {
        public static readonly ThingFactory Instance = new(exhausted: false);

        protected override BASE __Create() => exhausted ? throw new OutOfMemoryException() : new Thing();
    }

    [Fact]
    public void New_CreatesTheFactorysClass_AndFactoryObjectReturnsThatFactory()
    {
        // New's result is the universal object type (it crosses as §16.2's ACTIVE-CLASS returning item, kb/Work PB1582).
        var made = Assert.IsType<Thing>(ThingFactory.Instance.__New());
        Assert.Same(ThingFactory.Instance, made.FACTORYOBJECT());   // §16.2.2.2 GR1
    }

    [Fact]
    public void New_WhenResourcesAreUnavailable_AndCheckingIsOff_ReturnsNull()
    {
        ExceptionState.OoResourceChecking = false;
        Assert.Null(new ThingFactory(exhausted: true).__New());
    }

    [Fact]
    public void New_WhenResourcesAreUnavailable_AndCheckingIsOn_RaisesEcOoResource()
    {
        ExceptionState.OoResourceChecking = true;
        try
        {
            var ex = Assert.Throws<CobolFatalException>(() => new ThingFactory(exhausted: true).__New());
            Assert.Equal("EC-OO-RESOURCE", ex.EcName);
        }
        finally { ExceptionState.OoResourceChecking = false; }
    }
}
