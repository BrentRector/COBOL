// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY FILE DESCRIPTION ENTRY THAT CAN BE OPENED WITH DATA HAS A RECORD AREA — including the one ISO
/// §13.4.5.3 SR3 lets you write with NO record description entries. §14.9.30.4 GR6 says what that area is: a READ
/// INTO on such a file "proceeds as though there were one record description entry describing an alphanumeric
/// group item of the maximum size established by the RECORD clause", and
/// <c>DataBinder.MaterializeImpliedRecord</c> makes that entry real at bind time
/// (<c>COBOLNET_FILES_DESIGN</c> D17).
/// <para>kb/Work PB345: before it, <see cref="FileModel.AreaRecord"/> answered null for such an FD and FIVE
/// consumers each carried their own null arm — registration on the sequential emitter (which served REPORT files
/// only), registration on the keyed emitter (which returned outright), the READ record-area store and the
/// implicit INTO move. Both organizations therefore produced NO file connector and the first I-O verb aborted the
/// run unit. These tests are the ratchet on the repair: the FIRST pins that the area exists, is a GROUP and is
/// the RECORD clause's width on both organizations; the SECOND pins that the record-less decision stays in ONE
/// place, so a future emitter cannot quietly grow a second null arm.</para>
/// </summary>
public sealed class ImpliedRecordDescriptionDriftTests
{
    /// <summary>The §14.9.30.4 GR6 entry, on BOTH organizations SR7 leaves it legal for (sequential and
    /// relative), for each of the three RECORD clause formats that establish a maximum: format 1's integer-1,
    /// format 2's integer-3 and format 3's integer-5.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "RECORD CONTAINS 20 CHARACTERS", 20)]              // §13.18.43.2 format 1
    [InlineData("SEQUENTIAL", "RECORD IS VARYING IN SIZE FROM 1 TO 30", 30)]     // format 2, integer-3
    [InlineData("SEQUENTIAL", "RECORD CONTAINS 5 TO 20 CHARACTERS", 20)]         // format 3, integer-5
    [InlineData("RELATIVE", "RECORD CONTAINS 5 CHARACTERS", 5)]
    public void RecordLessFd_GetsTheImpliedGroupOfTheRecordClauseMaximum(string org, string recordClause, int width)
    {
        var file = Single(Bind(org, recordClause));
        var area = file.AreaRecord;
        Assert.NotNull(area);
        // GR6 says "an alphanumeric GROUP item", and the word is observable: §14.9.25.4 GR4's second paragraph
        // makes a group sender's implicit MOVE an alphanumeric-to-alphanumeric elementary move, where an
        // elementary alphanumeric sender to a numeric receiver would instead convert. So a group it must be.
        Assert.False(area!.IsElementary);
        Assert.True(area.IsGroup);
        Assert.Equal(width, file.RecordWidth);
        // The entry is IMPLIED, so it carries no record-name and the program cannot reference it — which is
        // exactly why §13.4.5.3 SR3 b) and c) require the FILE … FROM and INTO phrases on the verbs.
        Assert.Null(area.CobolName);
    }

    /// <summary>An FD that DOES describe its records is untouched — the synthesis is an else-arm, not a rewrite
    /// (the probe that would pass for the wrong reason if <c>MaterializeImpliedRecord</c> ran unconditionally).</summary>
    [Fact]
    public void RecordBearingFd_KeepsItsOwnRecord()
    {
        var file = Single(Bind("SEQUENTIAL", "RECORD CONTAINS 20 CHARACTERS", "       01  OREC PIC X(20)."));
        Assert.Single(file.Records);
        Assert.Equal("OREC", file.AreaRecord!.CobolName);
    }

    /// <summary>⛔ THE RECORD-LESS ARM IS ASKED IN EXACTLY ONE PLACE. It used to be asked twice — once in
    /// <c>SequentialIoEmitter.EmitFileRegistration</c> and once in <c>KeyedIoEmitter.EmitRegistration</c> — and
    /// answered differently, which is the whole of kb/Work PB345 (feedback_two_arm_dispatch: the most reproducible
    /// defect shape in this repo is a two-arm dispatch with one arm fixed). The decision now lives above the
    /// organization split in the sequential emitter's single loop; the keyed emitter must not re-acquire one.</summary>
    [Fact]
    public void KeyedRegistration_HasNoRecordLessArmOfItsOwn()
    {
        string keyed = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "CodeGen", "Verbs", "KeyedIoEmitter.cs")));
        int start = keyed.IndexOf("public void EmitRegistration", StringComparison.Ordinal);
        Assert.True(start > 0, "KeyedIoEmitter.EmitRegistration was renamed — re-point this drift test.");
        int end = keyed.IndexOf("\n    /// <summary>", start, StringComparison.Ordinal);
        string body = end > start ? keyed[start..end] : keyed[start..];
        // ⛔ CODE ONLY. The method's comment QUOTES the deleted arm — that is the point of the comment — so a raw
        // substring scan finds the prose and reports the arm as present. (Measured: this test failed exactly that
        // way on its first run, which is also the proof it is looking at something. feedback_green_gates_arent_evidence.)
        string code = string.Join('\n', body.Split('\n')
            .Select(line => line.TrimStart())
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal)
                        && !line.StartsWith("///", StringComparison.Ordinal)));
        Assert.DoesNotContain("Records.Count == 0", code);
    }

    // ── harness ─────────────────────────────────────────────────────────────────────────────────────────────

    private static FileModel Single(DataBinder data) => Assert.Single(data.Files);

    private static DataBinder Bind(string organization, string recordClause, string records = "")
    {
        string relativeKey = organization == "RELATIVE"
            ? "               ACCESS MODE IS RANDOM\r\n               RELATIVE KEY IS RK\r\n"
            : "";
        string src = "       IDENTIFICATION DIVISION.\r\n"
            + "       PROGRAM-ID. IMPLREC.\r\n"
            + "       ENVIRONMENT DIVISION.\r\n"
            + "       INPUT-OUTPUT SECTION.\r\n"
            + "       FILE-CONTROL.\r\n"
            + "           SELECT F1 ASSIGN TO \"implrec.dat\"\r\n"
            + $"               ORGANIZATION IS {organization}\r\n"
            + relativeKey
            + "               FILE STATUS IS FS.\r\n"
            + "       DATA DIVISION.\r\n"
            + "       FILE SECTION.\r\n"
            + $"       FD  F1 {recordClause}.\r\n"
            + (records.Length == 0 ? "" : records + "\r\n")
            + "       WORKING-STORAGE SECTION.\r\n"
            + "       01  FS PIC XX.\r\n"
            + "       01  RK PIC 9(4).\r\n"
            + "       PROCEDURE DIVISION.\r\n"
            + "       MAIN-PARA.\r\n"
            + "           STOP RUN.\r\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_implrec_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend() { InitialFormat = InitialReferenceFormat.Auto }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder();
            data.Bind(program);
            return data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
