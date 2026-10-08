// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The callee half of the predefined NULL written as a CALL / function-activation argument (kb/Work PB1630). NULL is
/// an identifier of the FORMAL's class (ISO §8.4.3.1.2 Format 8; §8.4.3.10.3 SR1 a) "depending upon the associated
/// data item's class"), so it crosses as the storage-free <see cref="PredefinedNullArgument"/> carrier and the
/// formal's slot adapter supplies the null of its own carrier (§8.4.3.10.4 GR1–GR3, §8.4.3.7.4 GR1). These pin what
/// the end-to-end witness (conformance:2002/pb1630_null_argument_crossing) cannot reach: the §12.3.8.4 GR10 c)
/// crossing, where the activating element knows no formal and a non-slot formal must refuse the carrier at
/// activation as EC-PROGRAM-ARG-MISMATCH (§14.9.4.4 GR3 d)) rather than read it as characters.
/// </summary>
public sealed class PredefinedNullArgumentTests
{
    private static CobolArg[] Null(CobolPassMode mode) => [new CobolArg(mode, PredefinedNullArgument.Instance, null)];

    [Fact]
    public void IsAPresentArgument_NotTheOmittedOne()
    {
        // §14.9.4.4 GR11: OMITTED makes the omitted-argument condition true; a NULL argument does not.
        Assert.True(CobolArgAdapt.Present(Null(CobolPassMode.Content), 0));
    }

    [Theory]
    [InlineData(CobolPassMode.Content)]
    [InlineData(CobolPassMode.Value)]
    public void DataPointerFormal_ReceivesTheNullDataAddress_NotAClrNull(CobolPassMode mode)
    {
        var cell = mode is CobolPassMode.Value
            ? CobolArgAdapt.SlotValue<ManagedPointer>(Null(mode), 0)
            : CobolArgAdapt.Slot<ManagedPointer>(Null(mode), 0);
        Assert.Same(ManagedPointer.Null, cell.Value);
    }

    [Fact]
    public void ProgramAndFunctionPointerFormals_ReceiveTheirNullAddress()
    {
        Assert.True(CobolArgAdapt.Slot<ProgramPointer>(Null(CobolPassMode.Content), 0).Value.IsNull);
        Assert.True(CobolArgAdapt.SlotValue<FunctionPointer>(Null(CobolPassMode.Value), 0).Value.IsNull);
    }

    [Fact]
    public void ObjectReferenceFormal_ReceivesTheNullReference()
    {
        Assert.Null(CobolArgAdapt.Slot<CobolObject?>(Null(CobolPassMode.Content), 0).Value);
    }

    [Fact]
    public void EachActivation_GetsItsOwnDetachedCell()
    {
        // BY CONTENT / BY VALUE are copies (§14.2.3 GR9/GR10): a callee's SET of its formal must not leak into the
        // next activation's NULL.
        var args = Null(CobolPassMode.Content);
        var first = CobolArgAdapt.Slot<ProgramPointer>(args, 0);
        first.Value = new ProgramPointer("SOMEPROG");
        Assert.True(CobolArgAdapt.Slot<ProgramPointer>(args, 0).Value.IsNull);
    }

    [Fact]
    public void CharacterFormal_RefusesIt_AsArgMismatch()
    {
        // §14.8.2.3.3: a formal not of class object or pointer takes its argument by a MOVE, and §14.9.25.3 SR1 bars
        // class pointer from a MOVE — reached only when the activating element could not screen it at bind.
        var ex = Assert.Throws<CobolCallException>(() => CobolArgAdapt.Text(Null(CobolPassMode.Content), 0, 4, null, static () => ""));
        Assert.Equal("EC-PROGRAM-ARG-MISMATCH", ex.EcName);
    }
}
