// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1532 — AN UNREACHABLE OBJECT'S INSTANCE FILES ARE CLOSED IN THE RUN UNIT THAT CREATED IT.
/// <para>ISO §9.3.14.3 (cite.py --check OK): "An instance object is destroyed either when it is determined that the
/// object cannot take part in the continued execution of the run unit, or when the run unit terminates, whichever
/// occurs first", and §9.1.4's implicit-CLOSE list (cite.py --check OK): "For file connectors in an object when the
/// object is deleted." docs/CONFORMANCE.md DOC-A.1-113 determines the timing: the .NET garbage collector decides
/// unreachability, and the finalizer closes the object's files "in the run unit that created the object".</para>
/// <para>The finalizer runs on the GC finalizer thread, where the ambient <see cref="RunUnit.Current"/> (an
/// AsyncLocal) is empty. It used to resolve the registry there, so the close was queued on a fresh ORPHAN run unit
/// that nothing drains, the file stayed open, and the next OPEN OUTPUT of the same physical file in the creating run
/// unit reported 61 (file sharing conflict). Expected, derived: once the object is unreachable and finalized, the
/// creating run unit's next OPEN drains the close, so a second connector's OPEN OUTPUT of the same file is 00.</para>
/// </summary>
public sealed class InstanceFileFinalizerTests
{
    /// <summary>An object that owns one instance-file connector, exactly as the emitted constructor tracks it.</summary>
    private sealed class FileOwningObject : CobolObject
    {
        public FileOwningObject(string key) => __TrackInstanceFile(key);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OpenThroughAnObjectAndDropIt(string key, string host)
    {
        CobolFile.Register(key, host, 10, lineSequential: true, optional: false, varyMin: -1, varyMax: -1);
        CobolFile.OpenOutput(key, host, false, null);
        Assert.Equal("00", CobolFile.Status(key));
        _ = new FileOwningObject(key);   // unreachable the moment this frame returns
    }

    [Fact]
    public void AnUnreachableObjectsFile_IsClosedInItsOwnRunUnit_BeforeTheNextOpen()
    {
        string host = Path.Combine(Path.GetTempPath(), $"pb1532-{Guid.NewGuid():N}.txt");
        try
        {
            RunUnit.Run(_ =>
            {
                string key = CobolFile.MintInstanceKey("OBJF");
                OpenThroughAnObjectAndDropIt(key, host);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                CobolFile.Register("SECOND", host, 10, lineSequential: true, optional: false, varyMin: -1, varyMax: -1);
                CobolFile.OpenOutput("SECOND", host, false, null);
                Assert.Equal("00", CobolFile.Status("SECOND"));   // 61 while the orphan queue held the close
            });
        }
        finally { File.Delete(host); }
    }
}
