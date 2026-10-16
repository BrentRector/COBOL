// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ RUN-UNIT TERMINATION'S IMPLICIT CLOSE IS THE CLOSE STATEMENT WITHOUT ANY PHRASES, EXECUTED FOR EVERY OPEN FILE
/// AND ONLY FOR THE OPEN ONES, EVEN WHEN ONE OF THEM FAILS (kb/Work PB603). ISO §14.6.11 2): <i>"An implicit CLOSE
/// statement without any phrases is executed for each file that is in the open mode. These implicit close statements
/// shall be executed for all open files in the run unit, even when an error occurs during the execution of one or
/// more of the CLOSE statements."</i>
/// <para>So <see cref="FileRegistry.CloseAll"/> reaches each connector through the ONE CLOSE dispatch
/// (<c>FileRegistry.CloseByFormat</c>, Table 14's plain CLOSE row): no member of <see cref="FileRegistry"/> but that
/// dispatch calls a connector's own <c>Close()</c>, so the implicit CLOSE cannot drift from the explicit one in its
/// not-open guard, its §9.1.15 file-lock removal or its §9.1.16 record-lock release. A connector that is not open is
/// not touched (the "sequential connectors close unconditionally" behavior stamped '42' on every closed sequential
/// file at termination), and one close that throws neither stops the others nor is swallowed.</para>
/// <para>A second <see cref="FileRegistry"/> IS a second run unit here (<see cref="RunUnitFileLockDriftTests"/>): it
/// shares no table and no handle with the first, so what it can open and lock is what the first one's implicit
/// CLOSE really gave back.</para>
/// </summary>
public sealed class RunUnitImplicitCloseDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb603-{tag}-{Guid.NewGuid():N}.dat");

    private static void RegisterSequential(FileRegistry reg, string name, string host) =>
        reg.Register(name, host, recordWidth: 8, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);

    private static void RegisterIndexed(FileRegistry reg, string name, string host, FileSharing? sharing = null)
    {
        reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            primeOffset: 0, primeLength: 4, varyMin: -1, varyMax: -1);
        if (sharing is { } s) reg.RegisterSharing(name, s, FileLockMode.Manual, multiple: false);
    }

    private static string Write(FileRegistry reg, string name, string image) =>
        reg.WriteShared(name, image, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);

    private static List<string> Records(string host, bool indexed)
    {
        var reader = new FileRegistry();
        if (indexed) RegisterIndexed(reader, "R", host); else RegisterSequential(reader, "R", host);
        reader.OpenStatic("R", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("R"));
        var all = new List<string>();
        while (reader.ReadShared("R", previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
                   FileRetryKind.None, 0, out string image) == "00")
            all.Add(image);
        reader.Close("R");
        return all;
    }

    private static void Delete(params string[] hosts)
    {
        foreach (string host in hosts)
            try { File.Delete(host); } catch (IOException) { }
    }

    /// <summary>§14.6.11 2)'s "even when an error occurs": the FIRST open file's close fails with an exception no
    /// I-O status describes (an injected held-line drain, the report writer's hook on every close path), and every
    /// later open file is still closed — its records persisted, its file lock given back to the next run unit — while
    /// the failure itself still reaches the caller rather than being swallowed. The failing connector's own closing
    /// operations ran too: the next run unit opens its physical file for OUTPUT.</summary>
    [Fact]
    public void AFailingImplicitClose_StillClosesEveryOtherOpenFile_AndIsNotSwallowed()
    {
        string failing = Tmp("failing"), indexed = Tmp("indexed"), sequential = Tmp("sequential");
        try
        {
            var runUnit = new FileRegistry();
            RegisterSequential(runUnit, "F", failing);     // registered FIRST, so it is closed first
            RegisterIndexed(runUnit, "I", indexed);
            RegisterSequential(runUnit, "S", sequential);
            foreach (string name in new[] { "F", "I", "S" })
            {
                runUnit.OpenStatic(name, FileOpenMode.Output);
                Assert.Equal("00", runUnit.Status(name));
            }
            Assert.Equal("00", Write(runUnit, "I", "0001IDX1"));
            Assert.Equal("00", Write(runUnit, "S", "SEQREC01"));
            var injected = new InvalidOperationException("PB603 probe: a close-time failure no I-O status describes");
            runUnit.HoldLine("F", () => throw injected);

            var escaped = Assert.Throws<InvalidOperationException>(runUnit.CloseAll);
            Assert.Same(injected, escaped);

            foreach (string name in new[] { "F", "I", "S" })
                Assert.True(runUnit.OpenModeIfOpen(name) is null,
                    $"'{name}' is still open after the run unit's implicit CLOSE — §14.6.11 2) closes ALL open files "
                    + "even when an error occurs during one of the CLOSE statements.");
            Assert.Equal(["0001IDX1"], Records(indexed, indexed: true));
            Assert.Equal(["SEQREC01"], Records(sequential, indexed: false));

            var next = new FileRegistry();
            RegisterSequential(next, "N", failing);
            next.OpenStatic("N", FileOpenMode.Output);
            Assert.True(next.Status("N") == "00",
                $"The next run unit's OPEN OUTPUT of the failed file answered '{next.Status("N")}': its closing "
                + "operations did not run, so its handle and §9.1.15 file lock outlived the run unit.");
            next.Close("N");
        }
        finally { Delete(failing, indexed, sequential); }
    }

    /// <summary>§14.6.11 2) closes each file "that is in the open mode" — and nothing else. A connector that was never
    /// opened, and one the program already CLOSEd, keep the I-O status they had: the implicit CLOSE of the run unit
    /// is not a CLOSE of a not-open file, which would be §14.9.6.4 GR1's unsuccessful '42'.</summary>
    [Fact]
    public void TheImplicitClose_LeavesAConnectorThatIsNotOpenUntouched()
    {
        string closed = Tmp("closed"), never = Tmp("never");
        try
        {
            var runUnit = new FileRegistry();
            RegisterSequential(runUnit, "C", closed);
            RegisterSequential(runUnit, "N", never);
            runUnit.OpenStatic("C", FileOpenMode.Output);
            runUnit.Close("C");
            Assert.Equal("00", runUnit.Status("C"));
            string neverStatus = runUnit.Status("N");

            runUnit.CloseAll();

            Assert.Equal("00", runUnit.Status("C"));
            Assert.Equal(neverStatus, runUnit.Status("N"));
        }
        finally { Delete(closed, never); }
    }

    /// <summary>§9.1.16: <i>"all record locks established for a file are released by the execution of an explicit or
    /// implicit CLOSE statement for the file"</i>. A record another run unit holds answers '51'; once that run unit
    /// terminates — its implicit CLOSE, never an explicit one — the record is available.</summary>
    [Fact]
    public void TheImplicitClose_ReleasesTheRunUnitsRecordLocks()
    {
        string host = Tmp("locks");
        try
        {
            var seed = new FileRegistry();
            RegisterIndexed(seed, "S", host);
            seed.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal("00", Write(seed, "S", "0001AAAA"));
            seed.Close("S");

            var a = new FileRegistry();
            var b = new FileRegistry();
            RegisterIndexed(a, "A", host, FileSharing.AllOther);
            RegisterIndexed(b, "B", host, FileSharing.AllOther);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.ReadKeyedShared("A", keyIndex: -1, "0001    ", FileRecordLock.WithLock,
                ignoringLock: false, FileRetryKind.None, 0, out _));
            Assert.Equal("51", b.ReadKeyedShared("B", keyIndex: -1, "0001    ", FileRecordLock.WithLock,
                ignoringLock: false, FileRetryKind.None, 0, out _));

            a.CloseAll();

            Assert.Equal("00", b.ReadKeyedShared("B", keyIndex: -1, "0001    ", FileRecordLock.WithLock,
                ignoringLock: false, FileRetryKind.None, 0, out _));
            b.CloseAll();
        }
        finally { Delete(host); }
    }

    /// <summary>⛔ THE STRUCTURAL HALF: the CLOSE dispatch is the only member of <see cref="FileRegistry"/> that calls
    /// a connector's own <c>Close()</c>. Every implicit close — run-unit termination (<c>CloseAll</c>), a new run
    /// unit's <c>Reset</c>, a displaced registration, CANCEL's <c>CloseIfOpen</c>, an object's
    /// <c>CloseAndDrop</c> — is an entry over it, so none can skip its guard or its lock release again. Before PB603
    /// three members (<c>CloseAll</c>, <c>Reset</c>, <c>CloseDisplaced</c>) each called <c>Close()</c> directly.</summary>
    [Fact]
    public void OnlyTheCloseDispatch_CallsAConnectorsOwnClose()
    {
        string path = TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs");
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
        var callers = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Close" }
                           && call.ArgumentList.Arguments.Count == 0)
            .Select(call => call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText
                            ?? "<outside a method>")
            .ToList();
        Assert.True(callers.Count > 0, "FileRegistry no longer calls a connector's Close(); re-point this drift test.");
        Assert.True(callers.All(m => m == "CloseByFormat"),
            "A connector's Close() is called outside the ONE CLOSE dispatch, from: "
            + string.Join(", ", callers.Where(m => m != "CloseByFormat").Distinct())
            + " — route it through Close(name) / CloseIfOpen(name) (§14.6.11 2), kb/Work PB603).");
    }
}
