// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE LENGTH OF AN ANY LENGTH RETURNING ITEM IS THE ACTIVATOR'S (kb/Work PB1167). ISO §13.18.2.4 GR1 b) treats the
/// subject of the clause "as though there were n repetitions of the picture symbol in the character-string in its
/// PICTURE clause, where n is the length of the corresponding argument or returning item of the activating runtime
/// element", and GR1 a) makes it a zero-length item when that returning item is one. <see cref="CobolArgAdapt.ReturningSeed"/>
/// is the program ABI's seed of the activated unit's RETURNING item: n from the receiver's stated length, else from
/// an ANY LENGTH receiver's current carrier length (zero included), else — a CALL with no receiver, where GR1 is
/// silent — the declared PICTURE length (docs/CONFORMANCE.md).
/// </summary>
public sealed class ReturningSeedTests
{
    [Fact]
    public void Receiver_With_A_Stated_Length_Gives_That_Length()
    {
        var ret = new CobolArg(CobolPassMode.Reference, ManagedPointer<string>.Cell("ABCDE"), null, Length: 5);
        Assert.Equal("     ", CobolArgAdapt.ReturningSeed(ret, ' ', 1));
    }

    [Fact]
    public void Receiver_With_A_Stated_Length_Seeds_Boolean_Zero_Bits()
    {
        var ret = new CobolArg(CobolPassMode.Reference, ManagedPointer<string>.Cell("1111"), null, Length: 4);
        Assert.Equal("0000", CobolArgAdapt.ReturningSeed(ret, '0', 1));
    }

    [Fact]
    public void Any_Length_Receiver_Gives_Its_Current_Length()
    {
        var ret = new CobolArg(CobolPassMode.Reference, ManagedPointer<string>.Cell("XYZ"), null);
        Assert.Equal(3, CobolArgAdapt.ReturningSeed(ret, ' ', 1).Length);
    }

    [Fact]
    public void Zero_Length_Receiver_Gives_A_Zero_Length_Item()
    {
        // GR1 a): the corresponding returning item of the activating runtime element is a zero-length item.
        var ret = new CobolArg(CobolPassMode.Reference, ManagedPointer<string>.Cell(""), null);
        Assert.Equal("", CobolArgAdapt.ReturningSeed(ret, ' ', 1));
    }

    [Fact]
    public void No_Receiver_Gives_The_Declared_Picture_Length()
    {
        Assert.Equal(" ", CobolArgAdapt.ReturningSeed(null, ' ', 1));
    }
}
