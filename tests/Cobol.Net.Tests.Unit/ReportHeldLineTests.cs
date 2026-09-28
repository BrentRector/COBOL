// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The report writer's HELD-BACK PRINT LINE is a property of the FILE CONNECTOR, not of one report (kb/Work PB1247).
/// ISO §13.18.35.4 GR3 lets a relative line with an integer-2 of zero overwrite the preceding line's characters, and a
/// print stream cannot rewrite a line it has emitted, so <c>CobolReport</c> keeps its most recent line unwritten.
/// The connector holds at most ONE such line (<see cref="FileConnector.HeldLineDrain"/>): a producer that is about to
/// write claims the device first (<see cref="FileRegistry.HoldLine"/>), which writes out a DIFFERENT holder's line —
/// produced earlier, so it must reach the medium first — and every CLOSE writes the holder's line before the file
/// closes. These facts reach the arms no golden can today: a second producer on one file (several reports on one
/// file, §13.4.5 REPORTS ARE, is not yet supported) and the re-claim by the same holder.
/// </summary>
public sealed class ReportHeldLineTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb1247-{tag}-{Guid.NewGuid():N}.txt");

    [Fact]
    public void HoldLine_DrainsADifferentHolderFirst_AndCloseDrainsTheLast()
    {
        var reg = new FileRegistry();
        string host = Tmp("held");
        reg.Register("P", host, 20, lineSequential: true, optional: false, -1, -1);
        try
        {
            reg.OpenStatic("P", FileOpenMode.Output);
            var order = new List<string>();
            Action a = () => order.Add("A");
            Action b = () => order.Add("B");

            reg.HoldLine("P", a);
            reg.HoldLine("P", a);            // the same holder re-claiming is not drained
            Assert.Empty(order);

            reg.HoldLine("P", b);            // a different holder: A's line reaches the medium first
            Assert.Equal(["A"], order);

            reg.Close("P");                  // every CLOSE path writes the holder's line before closing
            Assert.Equal(["A", "B"], order);

            reg.OpenStatic("P", FileOpenMode.Output);
            reg.Close("P");                  // one-shot: the next OPEN starts with nothing held
            Assert.Equal(["A", "B"], order);
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
