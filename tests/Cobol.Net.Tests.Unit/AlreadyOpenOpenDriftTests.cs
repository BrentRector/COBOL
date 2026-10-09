// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN OPEN OF AN OPEN FILE CONNECTOR ANSWERS '41' AND CHANGES NOTHING (kb/Work PB2748). ISO §14.9.27.4 GR2:
/// <i>"The file connector referenced by file-name-1 shall not be open. If it is open, the execution of the OPEN
/// statement is unsuccessful and the I-O status associated with file-name-1 is set to '41'"</i>; GR25: <i>"If the
/// execution of the OPEN statement is unsuccessful, the file is not affected"</i>.
/// <para>The registry used to arbitrate the request against Table 19 (answering '61' when a sibling connector's mode
/// refused it), register an OPEN phrase's record-locking posture, set the REVERSED request, associate the physical
/// file's state and hand down the request's sharing mode — all on the STILL-OPEN connector — and only then let
/// <see cref="FileConnector.Open"/> refuse it, so the unsuccessful OPEN changed how the open file shared, locked and
/// released its records. <c>FileRegistry.RefusedAsAlreadyOpen</c> now answers first, and the structural half below
/// keeps it first.</para>
/// </summary>
public sealed class AlreadyOpenOpenDriftTests
{
    private static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"pb2748-{tag}-{Guid.NewGuid():N}.dat");

    private static FileConnector Connector(FileRegistry reg, string name) =>
        (FileConnector)typeof(FileRegistry).GetMethod("Require", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(reg, [name])!;

    private static bool ReversedRequested(FileConnector c) =>
        (bool)typeof(SequentialConnector).GetProperty("ReversedRequested", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(c)!;

    /// <summary>A file opened EXTEND SHARING WITH ALL OTHER and then re-opened plainly (whose implementor default is
    /// SHARING WITH NO OTHER) answers '41' and keeps the sharing mode, the physical-file association and the
    /// record-locking posture of the OPEN that is still in force; a re-OPEN carrying a SHARING phrase registers no
    /// posture, and one carrying REVERSED requests no reversal.</summary>
    [Fact]
    public void AReOpen_Answers41_AndLeavesTheOpenConnectorAsItWas()
    {
        string host = Tmp("reopen");
        try
        {
            var reg = new FileRegistry();
            reg.Register("F", host, recordWidth: 4, lineSequential: false, optional: true, varyMin: -1, varyMax: -1);
            reg.OpenShared("F", FileOpenMode.Extend, hasSharingOverride: true, FileSharing.AllOther, FileRetryKind.None, 0,
                OpenTapePhrase.None, reg.HostPathOf("F"), assignDynamic: false, page: null);
            Assert.Equal("05", reg.Status("F"));
            var c = Connector(reg, "F");
            var physical = c.Physical;
            var shared = c.SharedPhysical;
            Assert.Equal(FileSharing.AllOther, c.HostSharing);

            reg.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal("41", reg.Status("F"));
            Assert.Equal(FileSharing.AllOther, c.HostSharing);
            Assert.Same(physical, c.Physical);
            Assert.Same(shared, c.SharedPhysical);

            reg.OpenTapeStatic("F", FileOpenMode.Input, OpenTapePhrase.Reversed);
            Assert.Equal("41", reg.Status("F"));
            Assert.False(ReversedRequested(c));
            reg.Close("F");

            // A clause-less connector re-opened WITH a SHARING phrase gains no record-locking posture.
            var plain = new FileRegistry();
            plain.Register("P", host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            plain.OpenStatic("P", FileOpenMode.Input);
            Assert.Equal("00", plain.Status("P"));
            plain.OpenShared("P", FileOpenMode.Input, hasSharingOverride: true, FileSharing.AllOther, FileRetryKind.None, 0,
                OpenTapePhrase.None, plain.HostPathOf("P"), assignDynamic: false, page: null);
            Assert.Equal("41", plain.Status("P"));
            Assert.Null(Connector(plain, "P").SharedPhysical);
            plain.Close("P");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>GR2 precedes the sharing rules (docs/CONFORMANCE.md DOC-A.1-104: the first failing general rule
    /// decides), so a re-OPEN whose request a SIBLING connector's sharing mode would refuse is still '41', never
    /// Table 19's '61'.</summary>
    [Fact]
    public void AReOpenThatASiblingWouldRefuse_IsStill41()
    {
        string host = Tmp("sibling");
        try
        {
            File.WriteAllText(host, "AAAABBBB");
            var reg = new FileRegistry();
            reg.Register("F", host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            reg.Register("G", host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            reg.OpenStatic("F", FileOpenMode.Input);
            reg.OpenStatic("G", FileOpenMode.Input);
            Assert.Equal("00", reg.Status("G"));
            reg.OpenStatic("F", FileOpenMode.IO);       // NO OTHER (the I-O default) against G's READ ONLY: Table 19 refuses
            Assert.Equal("41", reg.Status("F"));
            reg.Close("F");
            reg.Close("G");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>⛔ THE STRUCTURAL HALF: in the one OPEN dispatch, GR2's answer comes before every statement that touches
    /// the connector or the physical-file state — the association, the phrase posture, the tape and LINAGE requests,
    /// and the arbitrated attempt.</summary>
    [Fact]
    public void TheOneOpenDispatch_AsksGr2BeforeItTouchesTheConnector()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs"));
        int start = Array.FindIndex(lines, l => l.Contains("private void OpenCore(string name, FileOpenMode mode", StringComparison.Ordinal));
        Assert.True(start >= 0, "FileRegistry.OpenCore moved; re-point this drift test at the one OPEN dispatch.");
        int end = Array.FindIndex(lines, start + 1, l => l == "    }");
        var body = lines[start..end].Select(l => l.Split("//")[0]).ToList();
        int Line(string text)
        {
            int at = body.FindIndex(l => l.Contains(text, StringComparison.Ordinal));
            Assert.True(at >= 0, $"'{text}' is no longer in FileRegistry.OpenCore.");
            return at;
        }
        int guard = Line("RefusedAsAlreadyOpen(name, mode)");
        foreach (string touch in new[] { "RegisterSharing(", "Associate(name", "ReversedRequested", "HasLinageClause", "SharedOpenAttempt(" })
            Assert.True(Line(touch) > guard, $"OpenCore reaches '{touch}' before §14.9.27.4 GR2's already-open answer.");
    }
}
