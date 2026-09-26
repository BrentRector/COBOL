// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;                             // ProcessObserver — the ONE child-process observer
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ACCEPT (ISO §14.9.1) — SPEC-PINNED facts. These are not legacy-differential: the device form needs a piped
/// stdin and the temporal forms a pinned clock (two seams the shared <c>ICompilerUnderTest</c> runner does not
/// thread), and the temporal STORE rule is a place where the legacy deviates from the spec (it stored the
/// temporal text left-justified-raw for every receiver; §14.9.1.4 GR6 says BY THE MOVE RULES — a numeric receiver
/// decimal-aligns and keeps LOW-order digits). Every expected value below is derived from the cited general rule.
/// The clock pins through the <c>COBOLNET_CLOCK</c> process seam (<c>AcceptSource.Now</c>'s default).
/// </summary>
public sealed class AcceptDifferentialTests
{
    /// <summary>Compile (at <paramref name="dialect"/>) and run with <paramref name="stdin"/> piped to the
    /// program's standard input and, when given, <paramref name="clock"/> pinned via <c>COBOLNET_CLOCK</c>.</summary>
    private static (bool ok, string stdout, string detail) AcceptRun(
        string source, string stdin = "", string? clock = null, int dialect = 85)
    {
        string dir = CutRunner.NewTempDir("acc");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);
            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialect));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            // The ONE child-process observer (tests/_shared/ProcessObservation.cs). This method used to carry
            // its own copy of the 30s-timeout-returns-empty-output shape; a timeout here surfaced as an
            // ACCEPT test seeing no input echoed back, which reads exactly like a semantic defect. See §11 A12.
            var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"") { WorkingDirectory = dir };
            if (clock is not null) psi.Environment["COBOLNET_CLOCK"] = clock;
            var obs = ProcessObserver.ObserveOrThrow(psi, stdin);
            return (obs.ExitCode == 0, CutRunner.Normalize(obs.Stdout), CutRunner.Normalize(obs.Stderr));
        }
        finally { CutRunner.TryDelete(dir); }
    }

    private static void AssertOutputs(string source, string expected, string stdin = "", string? clock = null, int dialect = 85)
    {
        var (ok, stdout, detail) = AcceptRun(source, stdin, clock, dialect);
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    private static string Program(string progId, string data, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{progId}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
            STOP RUN.
        """;

    // §14.9.1.4 GR4a: a transfer SMALLER than the receiver stores aligned left — the receiver's tail is filled
    // by the (space-padded) remainder of the 80-character card image (GR2), not by MOVE-rule editing.
    [Fact]
    public void Device_ShortLine_StoresLeftAlignedSpaceFilled()
        => AssertOutputs(
            Program("ACCDEV1", "01 WS-X PIC X(10).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: "ABC       ]",
            stdin: "ABC\n");

    // §14.9.1.4 GR4b: a transfer LARGER than the receiver keeps only the LEFTMOST characters that fit; the rest
    // of the record is IGNORED — the next ACCEPT starts a fresh record, never the leftover of this one.
    [Fact]
    public void Device_OverlongLine_KeepsLeftmost_RestOfRecordIgnored()
        => AssertOutputs(
            Program("ACCDEV2", """
                01 WS-A PIC X(3).
                01 WS-B PIC XX.
                """, """
                ACCEPT WS-A.
                ACCEPT WS-B.
                DISPLAY WS-A "]" WS-B "]".
            """),
            expected: "ABC]XY]",
            stdin: "ABCDEFGH\nXY\n");

    // §14.9.1.4 GR4a + GR2: a receiver larger than one 80-character record requests ADDITIONAL records until
    // full; each short record is padded to the full card image, so record 2 lands at position 81.
    [Fact]
    public void Device_WideReceiver_ReadsAdditionalRecords()
        => AssertOutputs(
            Program("ACCDEV3", "01 WS-X PIC X(100).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: "1" + new string(' ', 79) + "2" + new string(' ', 19) + "]",
            stdin: "1\n2\n");

    // §14.9.1.4 GR4a end-of-input: when the device can supply no further data, the unfilled remainder of the
    // receiver stays spaces (the legacy's NIST-proven EOF behavior; the leading "[" pins the full field width).
    [Fact]
    public void Device_EndOfInput_LeavesSpaces()
        => AssertOutputs(
            Program("ACCDEV4", "01 WS-X PIC X(5).", """
                ACCEPT WS-X.
                DISPLAY "[" WS-X "]".
            """),
            expected: "[     ]",
            stdin: "");

    // §14.9.1.4 GR1: device data REPLACES the receiver's content with implementor-defined conversion — ours
    // decodes the zoned digit image into the native numeric value, so the item is immediately computable.
    [Fact]
    public void Device_NumericReceiver_ConvertsAndComputes()
        => AssertOutputs(
            Program("ACCDEV5", "01 WS-N PIC 9(4).", """
                ACCEPT WS-N.
                ADD 1 TO WS-N.
                DISPLAY WS-N.
            """),
            expected: "0043",
            stdin: "0042\n");

    // §14.9.1.4 GR7: DATE is the conceptual 6-digit unsigned integer YYMMDD — an exact-width numeric receiver
    // shows precisely that format.
    [Fact]
    public void Date_FormatIsYYMMDD()
        => AssertOutputs(
            Program("ACCDAT1", "01 WS-D PIC 9(6).", """
                ACCEPT WS-D FROM DATE.
                DISPLAY WS-D.
            """),
            expected: "260610",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR6 + GR11: the temporal transfer follows THE MOVE RULES from a conceptual 8-digit integer
    // (TIME = HHMMSScc); a smaller NUMERIC receiver decimal-aligns and keeps the LOW-order digits (§14.9.25.4 —
    // 14304567 into 9(4) is 4567). The legacy's left-justified-raw store would give 1430 — the GR6 deviation
    // this fact pins to the spec.
    [Fact]
    public void Time_SmallNumericReceiver_KeepsLowOrderDigits()
        => AssertOutputs(
            Program("ACCTIM1", "01 WS-T PIC 9(4).", """
                ACCEPT WS-T FROM TIME.
                DISPLAY WS-T.
            """),
            expected: "4567",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR6 + GR9: into an ALPHANUMERIC receiver the MOVE rules apply alphanumerically — the conceptual
    // DAY image "26161" (YYDDD; 2026-06-10 is day 161) left-justifies and truncates on the RIGHT into X(4).
    [Fact]
    public void Day_AlphanumericReceiver_TruncatesRight()
        => AssertOutputs(
            Program("ACCDAY1", "01 WS-A PIC X(4).", """
                ACCEPT WS-A FROM DAY.
                DISPLAY WS-A "]".
            """),
            expected: "2616]",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR12: DAY-OF-WEEK is a 1-digit integer where 1 IS MONDAY … 7 is Sunday (NOT the .NET Sunday=0
    // convention): 2026-06-08 is a Monday, 2026-06-14 a Sunday.
    [Theory]
    [InlineData("2026-06-08T09:00:00", "1")]
    [InlineData("2026-06-14T09:00:00", "7")]
    public void DayOfWeek_MondayIs1_SundayIs7(string clock, string expected)
        => AssertOutputs(
            Program("ACCDOW1", "01 WS-W PIC 9.", """
                ACCEPT WS-W FROM DAY-OF-WEEK.
                DISPLAY WS-W.
            """),
            expected: expected,
            clock: clock);

    // §14.9.1.4 GR8 + the version-gating rule: the DATE YYYYMMDD (four-digit-year) phrase was introduced by
    // ISO/IEC 1989:2002 — rejected with the edition diagnostic at --std 85, and the 8-digit conceptual value at
    // 2002+ (feedback_version_test_matrix: a new construct is rejected below its introducing edition).
    [Fact]
    public void DateYYYYMMDD_RejectedAt85_RunsAt2002()
    {
        string source = Program("ACCDAT2", "01 WS-D PIC 9(8).", """
                ACCEPT WS-D FROM DATE YYYYMMDD.
                DISPLAY WS-D.
            """);
        var (ok85, diags) = EditionHarness.Compile(source, 85);
        Assert.False(ok85, "ACCEPT FROM DATE YYYYMMDD must be rejected at --std 85 (introduced by ISO 2002, §14.9.1)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0815");

        AssertOutputs(source, expected: "20260610", clock: "2026-06-10T14:30:45.67", dialect: 2002);
    }

    // §14.9.1.4 GR2: one transfer is EXACTLY one 80-character record — an input line longer than 80
    // characters is CONSECUTIVE records, each padded to the card image, so line characters 81+ land at
    // position 81 and their record PADS to 80 before the next line contributes. (The old whole-line append
    // put the next line flush at position 91 — the GR2 deviation kb/Work PB139 measured.)
    [Fact]
    public void Device_LongLine_SplitsAtRecordBoundary()
        => AssertOutputs(
            Program("ACCDEV6", "01 WS-X PIC X(170).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: new string('A', 80) + new string('B', 10) + new string(' ', 70) + "C" + new string(' ', 9) + "]",
            stdin: new string('A', 80) + new string('B', 10) + "\nC\n");

    // §14.9.1.4 GR4b: a receiver needing only record 1 ignores the long line's REMAINING records along with
    // the rest of record 1 — the next ACCEPT reads a fresh input line, never leftover characters 81+.
    [Fact]
    public void Device_LongLine_NarrowReceiver_RemainingRecordsIgnored()
        => AssertOutputs(
            Program("ACCDEV7", """
                01 WS-A PIC X(5).
                01 WS-B PIC XX.
                """, """
                ACCEPT WS-A.
                ACCEPT WS-B.
                DISPLAY WS-A "]" WS-B "]".
            """),
            expected: "ABCDE]XY]",
            stdin: "ABCDE" + new string('Z', 85) + "\nXY\n");

    // §14.9.1.4 GR1 + GR2 for a FLOATING-POINT receiver (kb/Work PB887 — this aborted the run unit with a
    // NotImplementedCobolFeatureException, although §14.9.1.3 SR1 excludes only index / message-tag / object /
    // pointer). The determination, CONFORMANCE.md §7 DOC-A.1-1 / DOC-A.1-5: ONE 80-character record, read as the
    // inverse of the DISPLAY image (the §15.69 NUMVAL-F argument format, spaces ignored); a record that does not
    // conform converts to zero with no exception. Expected values are that rule applied, then DISPLAYed by
    // DOC-A.1-56 (the invariant shortest-round-trip image): "-1.5E+03" is -1500; " +3.25 " into a binary32
    // FLOAT-SHORT is 3.25 exactly; "12ABC" and an empty record do not conform, so both are 0. Each ACCEPT
    // consumes one whole record, so the four lines land one per statement.
    [Fact]
    public void Device_FloatReceiver_ReadsTheDisplayImage()
        => AssertOutputs(
            Program("ACCFLT1", """
                01 F2 USAGE COMP-2.
                01 F1 USAGE FLOAT-SHORT.
                01 FX USAGE FLOAT-LONG.
                01 FZ USAGE COMP-2 VALUE 9.
                """, """
                ACCEPT F2.
                ACCEPT F1.
                ACCEPT FX.
                ACCEPT FZ.
                DISPLAY F2 "]" F1 "]" FX "]" FZ "]".
            """),
            expected: "-1500]3.25]0]0]",
            stdin: "-1.5E+03\n +3.25 \n12ABC\n\n",
            dialect: 2023);

    // The ROUND TRIP the determination is chosen for: what DISPLAY writes for a float, ACCEPT reads back as the
    // same value (DOC-A.1-1 is defined as DOC-A.1-56's inverse). 1.0E-5 displays "1E-05"; fed back, it compares
    // equal to the original. The original is set by a MOVE — the ONE correctly rounded (nearest) conversion of
    // §14.6.8.3 (DOC-A.1-81) — because a COMPUTE without ROUNDED is §14.7.4.3 rule 2's TRUNCATION, which stores the
    // binary64 just BELOW 1.0E-5 (kb/Work PB1196), a different value from the one ACCEPT's conversion reads.
    [Fact]
    public void Device_FloatReceiver_RoundTripsItsOwnDisplayImage()
        => AssertOutputs(
            Program("ACCFLT2", """
                01 F2 USAGE COMP-2.
                01 G2 USAGE COMP-2.
                """, """
                MOVE 1.0E-5 TO G2.
                ACCEPT F2.
                IF F2 = G2 DISPLAY "EQUAL " F2 ELSE DISPLAY "DIFFERENT " F2 " " G2.
            """),
            expected: "EQUAL 1E-05",
            stdin: "1E-05\n",
            dialect: 2023);

    // §14.9.1.4 GR6 — the temporal transfer is "according to the rules for the MOVE statement", so EVERY receiver
    // category behaves as the MOVE from the GR7–GR12 conceptual unsigned integer would (kb/Work PB887: the transfer
    // is now that bound MOVE, rendered by the MOVE emitter, where the emitter used to carry its own copy of the
    // rules). Clock 2026-06-10 14:30:45.67 (a Wednesday, day 161). Each value is §14.9.25.4 applied:
    // numeric-edited 9999/99/99 inserts (GR6 edited) → 2026/06/10; ZZZ,ZZ9.99 takes DAY YYYYDDD 2026161 with
    // HIGH-order truncation to six integer digits → " 26,161.00"; alphanumeric-edited XX/XX/XX takes DATE
    // "260610" by simple insertion → 26/06/10; a JUSTIFIED RIGHT alphanumeric right-aligns DAY → "     26161";
    // a national receiver takes the TIME digit image left-justified → "14304567  "; COMP-2 takes the algebraic
    // value 20260610 and COMP-1 DAY-OF-WEEK 3; a group receiver is an alphanumeric move (GR4) → G1 "202"; a
    // reference-modified slice is alphanumeric (§8.4.3.3.4 GR6) → "2606" into R(2:4); SIGN LEADING SEPARATE
    // numeric takes TIME at scale 0 → "+014304567".
    [Fact]
    public void Temporal_EveryReceiverCategory_StoresByTheMoveRules()
        => AssertOutputs(
            Program("ACCTMV1", """
                01 NE   PIC 9999/99/99.
                01 ZE   PIC ZZZ,ZZ9.99.
                01 AE   PIC XX/XX/XX.
                01 AJ   PIC X(10) JUSTIFIED RIGHT.
                01 N10  PIC N(10).
                01 F2   USAGE COMP-2.
                01 F1   USAGE COMP-1.
                01 G.
                   05 G1 PIC X(3).
                   05 G2 PIC X(5).
                01 R    PIC X(10) VALUE ALL "*".
                01 S9   PIC S9(9) SIGN LEADING SEPARATE.
                """, """
                ACCEPT NE FROM DATE YYYYMMDD.
                ACCEPT ZE FROM DAY YYYYDDD.
                ACCEPT AE FROM DATE.
                ACCEPT AJ FROM DAY.
                ACCEPT N10 FROM TIME.
                ACCEPT F2 FROM DATE YYYYMMDD.
                ACCEPT F1 FROM DAY-OF-WEEK.
                ACCEPT G FROM DATE YYYYMMDD.
                ACCEPT R(2:4) FROM DATE.
                ACCEPT S9 FROM TIME.
                DISPLAY NE "]" ZE "]" AE "]" AJ "]".
                DISPLAY N10 "]" F2 "]" F1 "]" G1 "]" R "]" S9 "]".
            """),
            expected: "2026/06/10] 26,161.00]26/06/10]     26161]\n14304567  ]20260610]3]202]*2606*****]+014304567]",
            clock: "2026-06-10T14:30:45.67",
            dialect: 2023);

    // §14.9.1.3 SR1's COMPLEMENT: the DEVICE format permits a class-alphabetic receiver — only SR3 (the
    // temporal format) excludes class alphabetic. The screen must not over-reject Format 1.
    [Fact]
    public void Device_AlphabeticReceiver_Permitted()
        => AssertOutputs(
            Program("ACCDEV8", "01 WS-A PIC A(6).", """
                ACCEPT WS-A.
                DISPLAY WS-A "]".
            """),
            expected: "WORD  ]",
            stdin: "WORD\n");

    // §14.9.1.4 GR1: the device-to-receiver conversion is implementor-defined; for a BOOLEAN receiver (which
    // SR1 does NOT exclude) ours converts each transferred '1' to boolean one and every other character —
    // including the card image's pad spaces — to boolean zero, keeping the §13.18.40.4 GR14 '0'/'1'
    // representation invariant. (The old default-arm raw store put pad SPACES into boolean storage.)
    [Fact]
    public void Device_BooleanReceiver_ConvertsToBooleanCharacters()
        => AssertOutputs(
            Program("ACCDEV9", "01 WS-B PIC 1(4).", """
                ACCEPT WS-B.
                DISPLAY WS-B "]".
            """),
            expected: "1000]",
            stdin: "10\n",
            dialect: 2002);

    // §14.9.1.4 GR6 + §14.6.8.5: a NATIONAL receiver takes the temporal digit image exactly like
    // alphanumeric on the character substrate — left-justified, space-filled (TIME's 8-digit image into
    // N(10)). NATIONAL is 2002+. The old emitter had no national arm and staged a loud reject of the
    // SR3-legal receiver (kb/Work PB139).
    [Fact]
    public void Time_NationalReceiver_StoresDigitImage()
        => AssertOutputs(
            Program("ACCTIM2", "01 WS-N PIC N(10).", """
                ACCEPT WS-N FROM TIME.
                DISPLAY WS-N "]".
            """),
            expected: "14304567  ]",
            clock: "2026-06-10T14:30:45.67",
            dialect: 2002);

    // §14.9.1.4 GR6 via §14.9.25.4 GR6c / §13.18.32: a JUSTIFIED receiver right-justifies the temporal
    // image — left space-fill when larger, LEFT truncation when smaller (DAY's image "26161" keeps its
    // low-order characters "161" in X(3)). The old emitter ignored JUSTIFIED (kb/Work PB139).
    [Fact]
    public void Day_JustifiedReceiver_RightJustifiesAndLeftTruncates()
        => AssertOutputs(
            Program("ACCDAY2", """
                01 WS-J PIC X(8) JUSTIFIED RIGHT.
                01 WS-K PIC X(3) JUSTIFIED RIGHT.
                """, """
                ACCEPT WS-J FROM DAY.
                ACCEPT WS-K FROM DAY.
                DISPLAY "[" WS-J "]" WS-K "]".
            """),
            expected: "[   26161]161]",
            clock: "2026-06-10T14:30:45.67");

    // kb/Work PB180 — a BINARY member of a REDEFINES class (a Tier-B byte-form window) accepts by the
    // SAME §14.9.1.4 GR1 conversion every numeric receiver takes: four typed device characters decode to
    // the VALUE 1234 and re-encode as the window's TWO radix-2 bytes (04 D2 — pinned via FUNCTION ORD on
    // the redefined X view, §15.70). The old image arm spliced the raw characters ("1234" → bytes 31 32 =
    // the value 12594) — a silent wrong answer on shipping Tier B, and the discriminating first line
    // would have read 12594 with ORD 50/51.
    [Fact]
    public void Device_TierBBinaryWindow_ConvertsThenReencodes()
        => AssertOutputs(
            Program("ACCPB180", """
                01 WS-G.
                   05 WS-G-N PIC 9(4) COMP.
                   05 WS-G-A PIC X(3).
                01 WS-R REDEFINES WS-G PIC X(5).
                """, """
                MOVE "abc" TO WS-G-A.
                ACCEPT WS-G-N.
                DISPLAY WS-G-N.
                DISPLAY FUNCTION ORD(WS-R(1:1)) " " FUNCTION ORD(WS-R(2:1)).
                DISPLAY WS-G-A.
            """),
            expected: "1234\n5 211\nabc",
            stdin: "1234",
            dialect: 2023);

    // kb/Work PB173 — the OMITTED-LENGTH ref-mod width over a GROUP-USAGE BIT receiver, the THIRD arm of the
    // pad family (PlaceRenderer.Write's boolean pad and MoveEmitter's RefModSlice fill were the first two).
    // §8.4.3.3.4 GR5c: "If length is not specified, the unique data item extends from and includes the position
    // identified by leftmost-position up to and including the rightmost position of the data item referenced by
    // identifier-1"; GR5a: "If the usage of identifier-1 is bit, positions used in evaluation are bit positions".
    // So the omitted-length transfer size is m − start + 1 in BIT positions (m = 8 by §13.18.29.4 GR1b's as-if
    // PICTURE 1(8)), and the omitted form must equal its explicit-length twin. The emitter read raw `Pic`, which
    // is NULL for any group, and fell back to ImageWidth = ceil(8/8) = 1 PACKED CHARACTER: `(3:)` computed
    // 1 − 3 + 1 = −1 and `(1:)` computed 1, so EVERY start under-transferred and zero-filled the remainder
    // (measured before the fix: 11000000 for the (3:) leg, against the 11101010 both other legs produce).
    // Each transferred character converts by the same §14.9.1.4 GR1 boolean conversion
    // Device_BooleanReceiver_ConvertsToBooleanCharacters pins.
    [Fact]
    public void Device_OmittedLengthBitGroupSlice_TransfersBitPositions()
        => AssertOutputs(
            Program("ACCPB173", """
                01 WS-XM GROUP-USAGE BIT.
                   05 WS-XM1 PIC 1(4) VALUE B"1100".
                   05 WS-XM2 PIC 1(4) VALUE B"1010".
                01 WS-XN GROUP-USAGE BIT.
                   05 WS-XN1 PIC 1(4) VALUE B"1100".
                   05 WS-XN2 PIC 1(4) VALUE B"1010".
                01 WS-XP GROUP-USAGE BIT.
                   05 WS-XP1 PIC 1(4) VALUE B"1100".
                   05 WS-XP2 PIC 1(4) VALUE B"1010".
                """, """
                ACCEPT WS-XM(3:).
                ACCEPT WS-XN(3:6).
                ACCEPT WS-XP(1:).
                DISPLAY WS-XM "]" WS-XN "]" WS-XP "]".
            """),
            expected: "11101010]11101010]10101010]",
            stdin: "101010\n101010\n10101010\n",
            dialect: 2023);

    // §14.9.1.4 GR3: "If a device is capable of transferring data of the same size as the receiving data
    // item, the transferred data is stored in the receiving data item." This implementation's transfer size
    // is one 80-character card-image record (GR2 — "the implementor shall define, for each device, the size
    // of a data transfer"), so an X(80) receiver is EXACTLY the same size as one transfer: the record is
    // stored WHOLE — not truncated, not edited, and no additional data is requested. Every other device
    // fact here lands in GR4a or GR4b (a receiver smaller or larger than the transfer); the exact-size case
    // is the one GR3 governs, and no other test in this class sits on the boundary (widths 2, 3, 4, 5, 10,
    // 100 and 170 all miss it).
    [Fact]
    public void Device_ExactSizeReceiver_StoresTheWholeTransfer()
        => AssertOutputs(
            Program("ACCDEV10", "01 WS-X PIC X(80).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: new string('A', 40) + new string('B', 40) + "]",
            stdin: new string('A', 40) + new string('B', 40) + "\n");

    // §14.9.1.4 GR3 + GR2, the DISCRIMINATING leg: the transferred data is one RECORD — a short input line
    // padded to the full card image — so it is still the same size as an X(80) receiver and GR3 applies,
    // never GR4a. The proof is the SECOND ACCEPT: because GR3 stored the transfer whole and requested no
    // additional data, WS-Y reads a FRESH input line. Had the transfer been sized to the five typed
    // characters instead, GR4a would have pulled line 2 into positions 6..80 of WS-X and WS-Y would have
    // met end-of-input (two spaces).
    [Fact]
    public void Device_ExactSizeReceiver_ShortLine_ConsumesExactlyOneRecord()
        => AssertOutputs(
            Program("ACCDEV11", """
                01 WS-X PIC X(80).
                01 WS-Y PIC XX.
                """, """
                ACCEPT WS-X.
                ACCEPT WS-Y.
                DISPLAY WS-X "]" WS-Y "]".
            """),
            expected: "HELLO" + new string(' ', 75) + "]ZZ]",
            stdin: "HELLO\nZZ\n");

    // §14.9.1.4 GR10 + the version-gating rule: DAY with the phrase YYYYDDD "behaves as if it had been
    // described as an unsigned elementary integer data item of usage display SEVEN digits in length",
    // character positions 1-4 "four numeric characters of the year in the Gregorian calendar" and 5-7
    // "three numeric characters of the day of the year in the range 001 through 366". 2026-06-10 is day
    // 161 (31+28+31+30+31+10 — 2026 is not a leap year), so the conceptual value is 2026161 and an
    // exact-width 9(7) receiver shows precisely that seven-digit format. The YYYYDDD phrase is an ISO 2002
    // introduction (the 1985 Format 2 lists only bare DATE / DAY / DAY-OF-WEEK / TIME), so --std 85 rejects
    // it. The gate's message ternary has TWO arms and only the DATE one was ever exercised
    // (DateYYYYMMDD_RejectedAt85_RunsAt2002), so the DAY spelling is asserted explicitly.
    [Fact]
    public void DayYYYYDDD_RejectedAt85_RunsAt2002()
    {
        string source = Program("ACCDAY3", "01 WS-D PIC 9(7).", """
                ACCEPT WS-D FROM DAY YYYYDDD.
                DISPLAY WS-D.
            """);
        var (ok85, diags) = EditionHarness.Compile(source, 85);
        Assert.False(ok85, "ACCEPT FROM DAY YYYYDDD must be rejected at --std 85 (introduced by ISO 2002, §14.9.1)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0815");
        EditionHarness.AssertHasDiagnostic(diags, "ACCEPT FROM DAY YYYYDDD");

        AssertOutputs(source, expected: "2026161", clock: "2026-06-10T14:30:45.67", dialect: 2002);
    }

    // kb/Work PB1013 — §14.9.1.4 GR3/GR4 size the device transfer by "the size of the receiving data item", and an
    // ANY LENGTH receiver's size is its ARGUMENT's: §13.18.2.4 GR1 b) treats it "as though there were n repetitions
    // of the picture symbol … where n is the length of the corresponding argument". So a PIC X ANY LENGTH bound to a
    // 6-character argument takes the record's leftmost SIX characters (GR4b ignores the rest), a PIC N one bound to
    // N(4) takes four, a PIC 1 one bound to 1(5) takes five (each '1' boolean one, every other character boolean
    // zero — the documented GR1 conversion), and a short record is stored aligned left, space-filled (GR4a with the
    // GR2 card image). The transfer used to be sized by the declared PICTURE — one symbol (§13.18.2.3 SR1) — so every
    // one of these stored ONE character. One record is consumed per ACCEPT.
    [Fact]
    public void Device_AnyLengthReceiver_TransfersTheArgumentsSize()
        => AssertOutputs("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCANYL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X6 PIC X(6) VALUE "ABCDEF".
            01 N4 PIC N(4) VALUE N"abcd".
            01 B5 PIC 1(5) VALUE B"00000".
            01 S6 PIC X(6) VALUE "ZZZZZZ".
            PROCEDURE DIVISION.
                CALL "ACCANYX" USING X6.
                CALL "ACCANYN" USING N4.
                CALL "ACCANYB" USING B5.
                CALL "ACCANYX" USING S6.
                DISPLAY X6 "]" N4 "]" B5 "]" S6 "]".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCANYX.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC X ANY LENGTH.
            PROCEDURE DIVISION USING L.
                ACCEPT L.
                GOBACK.
            END PROGRAM ACCANYX.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCANYN.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC N ANY LENGTH.
            PROCEDURE DIVISION USING L.
                ACCEPT L.
                GOBACK.
            END PROGRAM ACCANYN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCANYB.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC 1 ANY LENGTH.
            PROCEDURE DIVISION USING L.
                ACCEPT L.
                GOBACK.
            END PROGRAM ACCANYB.
            END PROGRAM ACCANYL.
            """,
            expected: "QRSTUV]wxyz]10110]AB    ]",
            stdin: "QRSTUVWX\nwxyzv\n1011011\nAB\n",
            dialect: 2002);

    // The same rule through a SEPARATELY COMPILED user-defined function (§13.18.2.3 SR2/SR4 — a function's BY
    // REFERENCE formal may be ANY LENGTH): the argument X5 is five characters, so ACCEPT stores five, and the
    // store is the argument's own storage (BY REFERENCE), which the caller then shows.
    [Fact]
    public void Device_AnyLengthFunctionFormal_TransfersTheArgumentsSize()
        => AssertOutputs("""
            IDENTIFICATION DIVISION.
            FUNCTION-ID. ACCANYF.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L PIC X ANY LENGTH.
            01 R PIC 9(4).
            PROCEDURE DIVISION USING BY REFERENCE L RETURNING R.
                ACCEPT L.
                MOVE FUNCTION LENGTH(L) TO R.
                GOBACK.
            END FUNCTION ACCANYF.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCANYM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION ACCANYF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X5 PIC X(5) VALUE "ABCDE".
            01 N PIC 9(4).
            PROCEDURE DIVISION.
                MOVE FUNCTION ACCANYF(X5) TO N.
                DISPLAY N "]" X5 "]".
                STOP RUN.
            END PROGRAM ACCANYM.
            """,
            expected: "0005]HIJKL]",
            stdin: "HIJKLMNOP\n",
            dialect: 2002);
}
