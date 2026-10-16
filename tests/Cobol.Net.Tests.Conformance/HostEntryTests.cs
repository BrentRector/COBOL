// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The host harness of DESIGN-external-repository §17.1 (kb/Work PB2099): a .NET host compiled against WiseOwl COBOL
/// modules calls their generated host entries (§4.3), the .NET counterpart of a CALL statement. ISO/IEC 1989:2023
/// §14.2.3 13) (cite.py OK): "When either the activating or the activated runtime element is other than a COBOL runtime
/// element, the implementor shall specify the restrictions and mechanisms for all supported language products"; the
/// entry is that mechanism, and its restriction — a host value is landed only when the landing loses nothing — refuses
/// a lossy value BEFORE anything is activated.
/// <para>Each line the host prints is derived from the standard: §14.2.3 9) lands a BY CONTENT argument by "a COMPUTE
/// statement without the ROUNDED phrase" (cite.py OK), so 100.25 lands exactly and 1.234 into two fraction positions
/// would lose a digit; §14.2.3 8) (cite.py OK) "operates as if the formal parameter occupies the same storage area as the
/// argument", so the activated program's stores reach the host's variable and one variable passed twice is one storage;
/// §14.9.4.4 11) (cite.py OK) makes an omitted OPTIONAL argument's omitted-argument condition true; WORKING-STORAGE keeps
/// its last-used state across activations in one run unit until a CANCEL (§14.9.5); and a numeric content is decoded
/// only when the class test NUMERIC (§8.8.4.4.4 3) n), cite.py OK) is true of it.</para>
/// <para>The non-COBOL element is real: a C# host compiled here with Roslyn against the modules and run as its own
/// process (<see cref="NonCobolElement"/>).</para>
/// </summary>
public sealed class HostEntryTests
{
    /// <summary>The programs a host activates: elementary formals of every landing row, BY REFERENCE and BY VALUE, an
    /// OPTIONAL formal and a RETURNING item; an alias witness; a validity witness whose callee leaves content that is not
    /// numeric in a numeric formal and in its RETURNING item through REDEFINES views.</summary>
    private const string Programs = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1PRG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-COUNT PIC 9(4) VALUE 0.
        LINKAGE SECTION.
        01 LK-AMOUNT PIC S9(7)V99.
        01 LK-NAME PIC X(10).
        01 LK-FLAG PIC S9(4) COMP-5.
        01 LK-NOTE PIC X(8).
        01 LK-RET PIC S9(9).
        PROCEDURE DIVISION USING BY REFERENCE LK-AMOUNT LK-NAME
            BY VALUE LK-FLAG BY REFERENCE OPTIONAL LK-NOTE
            RETURNING LK-RET.
        P1.
            ADD 1 TO WS-COUNT
            ADD LK-FLAG TO LK-AMOUNT
            DISPLAY "IN [" LK-NAME "]"
            MOVE "CHANGED" TO LK-NAME
            IF LK-NOTE IS OMITTED
                DISPLAY "NOTE OMITTED"
            ELSE
                DISPLAY "NOTE [" LK-NOTE "]"
            END-IF
            COMPUTE LK-RET = WS-COUNT * 10
            GOBACK.
        END PROGRAM H1PRG.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1ALIAS.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-A PIC 9(4).
        01 LK-B PIC 9(4).
        01 LK-C PIC 9(8).
        01 LK-I USAGE INDEX.
        PROCEDURE DIVISION USING LK-A LK-B LK-C LK-I.
        P1.
            ADD 1 TO LK-A
            DISPLAY "B=" LK-B
            GOBACK.
        END PROGRAM H1ALIAS.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1BAD.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-N PIC 9(4).
        01 LK-NX REDEFINES LK-N PIC X(4).
        01 LK-T PIC X(3).
        01 HRET PIC 9(4).
        01 HRETX REDEFINES HRET PIC X(4).
        PROCEDURE DIVISION USING LK-N LK-T RETURNING HRET.
        P1.
            MOVE "ABCD" TO LK-NX
            MOVE "XYZ" TO LK-T
            MOVE "ABC " TO HRETX
            GOBACK.
        END PROGRAM H1BAD.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1WIDE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-L PIC S9(18).
        01 LK-W PIC S9(20).
        01 LK-U PIC 9(4).
        PROCEDURE DIVISION USING LK-L LK-W BY VALUE LK-U.
        P1.
            SUBTRACT 1 FROM LK-L
            ADD 1 TO LK-W
            DISPLAY "U=" LK-U
            GOBACK.
        END PROGRAM H1WIDE.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1PTR.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-P USAGE POINTER.
        PROCEDURE DIVISION USING LK-P.
        P1.
            IF LK-P = NULL
                DISPLAY "P NULL"
            END-IF
            GOBACK.
        END PROGRAM H1PTR.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H1OPT.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-A PIC X(4).
        01 LK-B PIC X(4).
        01 LK-P USAGE POINTER.
        01 LK-Q USAGE POINTER.
        01 LK-R USAGE POINTER.
        01 LK-S USAGE POINTER.
        PROCEDURE DIVISION USING OPTIONAL LK-A LK-B OPTIONAL LK-P LK-Q
            LK-R OPTIONAL LK-S.
        P1.
            IF LK-A IS OMITTED
                DISPLAY "A OMITTED"
            ELSE
                DISPLAY "A [" LK-A "] B [" LK-B "]"
            END-IF
            IF LK-P IS OMITTED
                DISPLAY "P OMITTED"
            END-IF
            IF LK-Q = NULL
                DISPLAY "Q NULL"
            END-IF
            IF LK-R = NULL
                DISPLAY "R NULL"
            END-IF
            IF LK-S IS OMITTED
                DISPLAY "S OMITTED"
            END-IF
            GOBACK.
        END PROGRAM H1OPT.
        """;

    /// <summary>A function unit: its entry returns the RETURNING item and has no Cancel (CANCEL names programs).</summary>
    private const string Function = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. H1FN.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-X PIC 9(3).
        01 LK-R PIC 9(5).
        PROCEDURE DIVISION USING LK-X RETURNING LK-R.
        P1.
            COMPUTE LK-R = LK-X * 2
            GOBACK.
        END FUNCTION H1FN.
        """;

    private const string Host = """
        using System;
        using Cobol.H1PROGS;
        using Cobol.H1FUNC;

        public static class H1HostProgram
        {
            public static int Main()
            {
                decimal amount = 100.25m; string name = "ALICE"; string? note = null;
                long r = H1PRG.Call(ref amount, ref name, 5, ref note);
                Console.WriteLine(FormattableString.Invariant($"1 {amount} [{name}] {r}"));
                note = "HELLO";
                r = H1PRG.Call(ref amount, ref name, -5, ref note);
                Console.WriteLine(FormattableString.Invariant($"2 {amount} [{name}] {r}"));
                H1PRG.Cancel();
                r = H1PRG.Call(ref amount, ref name, 0, ref note);
                Console.WriteLine($"3 {r}");
                try { decimal bad = 1.234m; H1PRG.Call(ref bad, ref name, 0, ref note); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"LOW {e.ParamName}"); }
                try { decimal bad = 12345678m; H1PRG.Call(ref bad, ref name, 0, ref note); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"HIGH {e.ParamName}"); }
                try { string longName = "ABCDEFGHIJK"; H1PRG.Call(ref amount, ref longName, 0, ref note); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"LENGTH {e.ParamName}"); }
                r = H1PRG.Call(ref amount, ref name, 0, ref note);
                Console.WriteLine($"4 {r}");

                long a = 4, c = 0, i = 1;
                H1ALIAS.Call(ref a, ref a, ref c, ref i);
                Console.WriteLine($"A {a}");
                try { long b = 0; H1ALIAS.Call(ref a, ref b, ref a, ref i); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException e) { Console.WriteLine($"ALIAS {e.ParamName}"); }
                // An index and a PIC 9(4) item are both `ref long` but different descriptions held in different
                // storages: one variable for both is refused like any two descriptions (NIST IC207A's shape).
                try { long b = 0; H1ALIAS.Call(ref a, ref b, ref c, ref a); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException e) { Console.WriteLine($"ALIAS {e.ParamName}"); }
                // The call's shape is decided before any value is landed (DESIGN-external-repository §4.3): 123456 is
                // one digit too many for LK-A, yet the call is refused as one variable for two descriptions.
                try { long big = 123456, b = 0; H1ALIAS.Call(ref big, ref b, ref big, ref i); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException e) { Console.WriteLine($"FIRST {e.GetType().Name} {e.ParamName}"); }

                long n = 7; string t = "AAA";
                try { H1BAD.Call(ref n, ref t); Console.WriteLine("DECODED"); }
                catch (FormatException e) { Console.WriteLine($"BAD {n} [{t}] {e.Message.Contains("'LK_N'")}"); }

                long l = 999999999999999999; Int128 w = Int128.Parse("99999999999999999998");
                H1WIDE.Call(ref l, ref w, 12);
                Console.WriteLine($"W {l} {w}");
                try { long big = 1000000000000000000; H1WIDE.Call(ref big, ref w, 0); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"WIDE {e.ParamName}"); }
                try { H1WIDE.Call(ref l, ref w, -1); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"SIGN {e.ParamName}"); }

                // A .NET null for a data pointer is the pointer NULL, never a null reference in the program.
                CobolNet.Runtime.ManagedPointer p = null!;
                H1PTR.Call(ref p);
                Console.WriteLine($"PTR {ReferenceEquals(p, CobolNet.Runtime.ManagedPointer.Null)}");

                // One variable for formals of one description, OPTIONAL and not, lands as distinct variables would
                // (DESIGN-external-repository §4.3): a null one omits each OPTIONAL formal, a non-OPTIONAL data
                // pointer lands it as NULL, and a non-OPTIONAL text refuses it.
                string? s = "ABCD"; CobolNet.Runtime.ManagedPointer? q = null;
                H1OPT.Call(ref s, ref s, ref q, ref q, ref q, ref q);
                try { s = null; H1OPT.Call(ref s, ref s, ref q, ref q, ref q, ref q); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentNullException e) { Console.WriteLine($"OPTNULL {e.ParamName}"); }

                long x = 21;
                Console.WriteLine($"FN {H1FN.Call(ref x)}");
                return 0;
            }
        }
        """;

    /// <summary>Programs with GROUP formals and RETURNING items, which a host passes as published record structs
    /// (DESIGN-external-repository §4.3's group rows): nested groups, fixed tables of leaves and of groups, FILLER, a
    /// REDEFINES view (no member), COMP-3 and COMP members; national and USAGE BIT members (two bit members share a
    /// byte, §8.5.1.6.3); a validity witness; an OPTIONAL group; and a function whose RETURNING item is a group.</summary>
    private const string GroupPrograms = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H2REC.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-REC.
           05 LK-ID PIC 9(4).
           05 FILLER PIC X(2).
           05 LK-NAME PIC X(10).
           05 LK-AMT PIC S9(5)V99 COMP-3.
           05 LK-SUB.
              10 LK-X PIC S9(4) COMP.
              10 LK-Y PIC X(3).
           05 LK-TAB PIC 9(2) OCCURS 3.
           05 LK-PAIR OCCURS 2.
              10 LK-P1 PIC X.
              10 LK-P2 PIC 9.
        01 LK-OUT.
           05 LK-COUNT PIC 9(3).
           05 LK-TEXT PIC X(5).
        PROCEDURE DIVISION USING LK-REC RETURNING LK-OUT.
        P1.
            DISPLAY "IN " LK-ID " [" LK-NAME "] [" LK-Y "] " LK-TAB(1) LK-TAB(2) LK-TAB(3) " " LK-P1(2) LK-P2(2)
            ADD 1 TO LK-ID
            MOVE "BOB" TO LK-NAME
            SUBTRACT 1 FROM LK-AMT
            ADD 1 TO LK-TAB(2)
            MOVE "Z" TO LK-P1(1)
            MOVE 123 TO LK-COUNT
            MOVE "DONE" TO LK-TEXT
            GOBACK.
        END PROGRAM H2REC.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H2BAD.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-G.
           05 LK-N PIC 9(4).
           05 LK-NX REDEFINES LK-N PIC X(4).
           05 LK-T PIC X(3).
        PROCEDURE DIVISION USING LK-G.
        P1.
            MOVE "ABCD" TO LK-NX
            MOVE "XYZ" TO LK-T
            GOBACK.
        END PROGRAM H2BAD.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H2NB.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-NB.
           05 LK-N PIC N(3).
           05 LK-B PIC 1(3) USAGE BIT.
           05 LK-C PIC 1(2) USAGE BIT.
           05 LK-D PIC 9(2).
        PROCEDURE DIVISION USING LK-NB.
        P1.
            DISPLAY "NB " LK-B " " LK-C " " LK-D
            MOVE N"XY" TO LK-N
            MOVE B"011" TO LK-B
            MOVE B"10" TO LK-C
            GOBACK.
        END PROGRAM H2NB.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H2OPT.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-O.
           05 LK-O1 PIC X(2).
        PROCEDURE DIVISION USING BY REFERENCE OPTIONAL LK-O.
        P1.
            IF LK-O IS OMITTED
                DISPLAY "O OMITTED"
            ELSE
                DISPLAY "O [" LK-O1 "]"
                MOVE "ZZ" TO LK-O1
            END-IF
            GOBACK.
        END PROGRAM H2OPT.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H2AS.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-NG GROUP-USAGE NATIONAL.
           05 LK-NG1 PIC N(2).
           05 LK-NG2 PIC N(1).
        01 LK-F.
           05 LK-F1 USAGE FLOAT-LONG.
           05 LK-F2 USAGE INDEX.
           05 LK-F3 PIC X(4) JUSTIFIED RIGHT.
        PROCEDURE DIVISION USING LK-NG LK-F.
        P1.
            MOVE N"QR" TO LK-NG1
            COMPUTE LK-F1 = LK-F1 * 2
            DISPLAY "F3 [" LK-F3 "]"
            GOBACK.
        END PROGRAM H2AS.
        """;

    /// <summary>A function whose RETURNING item is a group: its entry returns the published record.</summary>
    private const string GroupFunction = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. H2FN.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-X PIC 9(3).
        01 LK-R.
           05 LK-R1 PIC 9(5).
           05 LK-R2 PIC X(4).
        PROCEDURE DIVISION USING LK-X RETURNING LK-R.
        P1.
            COMPUTE LK-R1 = LK-X * 2
            MOVE "FN" TO LK-R2
            GOBACK.
        END FUNCTION H2FN.
        """;

    private const string GroupHost = """
        using System;
        using Cobol.H2PROGS;
        using Cobol.H2FUNC;

        public static class H2HostProgram
        {
            public static int Main()
            {
                var r = new H2REC.LK_REC_Record { LK_ID = 41, LK_NAME = "ALICE", LK_AMT = -123.45m };
                r.LK_SUB = new H2REC.LK_REC_Record.LK_SUB_Record { LK_X = -7, LK_Y = "AB" };
                r.LK_TAB[1] = 9;
                r.LK_PAIR[1] = new H2REC.LK_REC_Record.LK_PAIR_Record { LK_P1 = "Q", LK_P2 = 5 };
                var o = H2REC.Call(ref r);
                Console.WriteLine(FormattableString.Invariant(
                    $"1 {r.LK_ID} [{r.LK_NAME}] {r.LK_AMT} {r.LK_SUB.LK_X} [{r.LK_SUB.LK_Y}] {string.Join(",", r.LK_TAB)} {r.LK_PAIR[0].LK_P1}{r.LK_PAIR[0].LK_P2} {o.LK_COUNT} [{o.LK_TEXT}]"));
                try { var bad = r with { LK_AMT = 1.234m }; H2REC.Call(ref bad); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentOutOfRangeException e) { Console.WriteLine($"LOW {e.ParamName} {e.Message.Contains("'LK_AMT'")}"); }
                try { var bad = r with { LK_TAB = new long[2] }; H2REC.Call(ref bad); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException e) { Console.WriteLine($"OCC {e.ParamName}"); }
                try { var bad = r with { LK_NAME = null! }; H2REC.Call(ref bad); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentNullException e) { Console.WriteLine($"NULL {e.ParamName}"); }
                string image = r.AsImage();
                string filled = image.Substring(0, 4) + "##" + image.Substring(6);
                var back = H2REC.LK_REC_Record.FromImage(filled);
                Console.WriteLine($"RT {back.AsImage() == filled} {back.LK_ID} {image.Substring(4, 2) == "  "}");

                var g = new H2BAD.LK_G_Record { LK_N = 7, LK_T = "AAA" };
                try { H2BAD.Call(ref g); Console.WriteLine("DECODED"); }
                catch (FormatException e) { Console.WriteLine($"BAD {g.LK_N} [{g.LK_T}] {e.Message.Contains("'LK_G.LK_N'")}"); }

                var nb = new H2NB.LK_NB_Record { LK_N = "ABC", LK_B = "101", LK_C = "01", LK_D = 12 };
                H2NB.Call(ref nb);
                Console.WriteLine($"NB2 [{nb.LK_N}] {nb.LK_B} {nb.LK_C} {nb.LK_D}");

                H2OPT.LK_O_Record? opt = null;
                H2OPT.Call(ref opt);
                opt = new H2OPT.LK_O_Record { LK_O1 = "AB" };
                H2OPT.Call(ref opt);
                Console.WriteLine($"O2 [{opt.Value.LK_O1}]");

                var ng = new H2AS.LK_NG_Record { LK_NG1 = "AB", LK_NG2 = "C" };
                var fl = new H2AS.LK_F_Record { LK_F1 = 1.5, LK_F2 = 4, LK_F3 = "XY" };
                H2AS.Call(ref ng, ref fl);
                Console.WriteLine(FormattableString.Invariant($"AS [{ng.LK_NG1}{ng.LK_NG2}] {fl.LK_F1} {fl.LK_F2} [{fl.LK_F3}]"));

                long x = 21;
                var fr = H2FN.Call(ref x);
                Console.WriteLine($"FN {fr.LK_R1} [{fr.LK_R2}]");
                return 0;
            }
        }
        """;

    /// <summary>A host passes and receives GROUPS as published record structs (DESIGN-external-repository §4.3): each
    /// member lands exactly into the record image at the place the activated program's description gives it, or the
    /// call is refused with the member named before anything is activated; the activated program's stores come back
    /// through the record (§14.2.3 8), cite.py OK); FILLER content round-trips through <c>FromImage</c>/<c>AsImage</c>;
    /// a record any numeric member of which fails the class test NUMERIC (§8.8.4.4.4 3) n), cite.py OK) is not decoded,
    /// and the host's variable is unchanged.</summary>
    [Fact]
    public void AHost_PassesGroupsAsPublishedRecords_LandingEachMemberExactlyAndDecodingByTheValidityRule()
    {
        string dir = CutRunner.NewTempDir("h2host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H2PROGS", GroupPrograms);
            NonCobolElement.CompileCobol(dir, "H2FUNC", GroupFunction);
            NonCobolElement.CompileCSharp(dir, "H2HOST", GroupHost, "H2PROGS", "H2PROGS", "H2FUNC");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "H2HOST.dll"), dir);
            Assert.Equal(
                "IN 0041 [ALICE     ] [AB ] 000900 Q5\n" +
                "1 42 [BOB       ] -124.45 -7 [AB ] 0,10,0 Z0 123 [DONE ]\n" +
                "LOW LK_REC True\n" +
                "OCC LK_REC\n" +
                "NULL LK_REC\n" +
                "RT True 42 True\n" +
                "BAD 7 [AAA] True\n" +
                "NB 101 01 12\n" +
                "NB2 [XY ] 011 10 12\n" +
                "O OMITTED\n" +
                "O [AB]\n" +
                "O2 [ZZ]\n" +
                "F3 [  XY]\n" +
                "AS [QRC] 3 4 [  XY]\n" +
                "FN 42 [FN  ]", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Programs whose group formals hold OCCURS DEPENDING ON tables: one counted by an item of the record (in a
    /// subordinate group, so another record struct presents it), one counted by an item outside the record, and one
    /// whose count the activated program sets past the table's bounds.</summary>
    private const string OdoPrograms = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H3ODO.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-HREC.
           05 LK-HDR.
              10 LK-CNT PIC 9(2).
           05 LK-NAME PIC X(3).
           05 LK-ITEM OCCURS 1 TO 4 DEPENDING ON LK-CNT.
              10 LK-CODE PIC X(2).
              10 LK-QTY PIC 9(3).
        01 LK-N PIC 9.
        01 LK-EXT.
           05 LK-E PIC X(2) OCCURS 0 TO 3 DEPENDING ON LK-N.
        PROCEDURE DIVISION USING LK-HREC LK-N LK-EXT.
        P1.
            DISPLAY "ODO " LK-CNT " " LK-CODE(1) LK-QTY(1) " " LK-CODE(LK-CNT) LK-QTY(LK-CNT)
            ADD 1 TO LK-CNT
            MOVE "NW" TO LK-CODE(LK-CNT)
            MOVE 7 TO LK-QTY(LK-CNT)
            MOVE "Z" TO LK-NAME
            DISPLAY "EXT " LK-N " [" LK-EXT "]"
            MOVE "QQ" TO LK-E(1)
            GOBACK.
        END PROGRAM H3ODO.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H3BIG.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-B.
           05 LK-BC PIC 9.
           05 LK-BT PIC X OCCURS 0 TO 2 DEPENDING ON LK-BC.
        PROCEDURE DIVISION USING LK-B.
        P1.
            MOVE 9 TO LK-BC
            GOBACK.
        END PROGRAM H3BIG.
        """;

    private const string OdoHost = """
        using System;
        using Cobol.H3PROGS;

        public static class H3HostProgram
        {
            public static int Main()
            {
                var h = new H3ODO.LK_HREC_Record();
                Console.WriteLine($"NEW {h.LK_HDR.LK_CNT} {h.LK_ITEM.Length} {new H3ODO.LK_EXT_Record().LK_E.Length}");
                h.LK_HDR = h.LK_HDR with { LK_CNT = 2 };
                h.LK_ITEM = [new() { LK_CODE = "AA", LK_QTY = 1 }, new() { LK_CODE = "BB", LK_QTY = 2 }];
                h.LK_NAME = "ABC";
                long n = 2;
                var e = new H3ODO.LK_EXT_Record();
                e.LK_E[0] = "X1";
                e.LK_E[1] = "X2";
                H3ODO.Call(ref h, ref n, ref e);
                Console.WriteLine($"OUT {h.LK_HDR.LK_CNT} {h.LK_ITEM.Length} {h.LK_ITEM[2].LK_CODE}{h.LK_ITEM[2].LK_QTY} [{h.LK_NAME}] [{string.Join(",", e.LK_E)}]");
                try { var bad = h with { LK_ITEM = new H3ODO.LK_HREC_Record.LK_ITEM_Record[2] }; H3ODO.Call(ref bad, ref n, ref e); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException x) { Console.WriteLine($"OCC {x.ParamName} {x.Message.Contains("has 3 entries")}"); }
                try { var bad = h with { LK_HDR = h.LK_HDR with { LK_CNT = 5 } }; H3ODO.Call(ref bad, ref n, ref e); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException x) { Console.WriteLine($"BOUNDS {x.ParamName} {x.Message.Contains("1 to 4")}"); }
                string image = h.AsImage();
                var back = H3ODO.LK_HREC_Record.FromImage(image);
                Console.WriteLine($"RT {back.AsImage() == image} {back.LK_ITEM.Length} {image.Length}");

                var b = new H3BIG.LK_B_Record();
                try { H3BIG.Call(ref b); Console.WriteLine("DECODED"); }
                catch (FormatException x) { Console.WriteLine($"ODOBAD {b.LK_BC} {b.LK_BT.Length} {x.Message.Contains("'LK_B.LK_BT'")}"); }
                return 0;
            }
        }
        """;

    /// <summary>A host passes groups holding OCCURS DEPENDING ON tables (DESIGN-external-repository §4.3): a table counted
    /// by an item of the record is an array of the DEPENDING ON value's entries, "the current number of occurrences"
    /// (ISO §13.18.38.4 7), cite.py OK), so an array of another length, or a count outside the table's bounds, is refused
    /// before anything is activated, and the write-back decodes as many entries as the activated program's count says;
    /// a table counted by an item outside the record is its whole area; a count the activated program left outside the
    /// bounds leaves the host's variable unchanged.</summary>
    [Fact]
    public void AHost_PassesOccursDependingTables_AsArraysOfTheirCurrentOccurrences()
    {
        string dir = CutRunner.NewTempDir("h3host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H3PROGS", OdoPrograms);
            NonCobolElement.CompileCSharp(dir, "H3HOST", OdoHost, "H3PROGS", "H3PROGS");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "H3HOST.dll"), dir);
            Assert.Equal(
                "NEW 1 1 3\n" +
                "ODO 02 AA001 BB002\n" +
                "EXT 2 [X1X2]\n" +
                "OUT 3 3 NW7 [Z  ] [QQ,X2,  ]\n" +
                "OCC LK_HREC True\n" +
                "BOUNDS LK_HREC True\n" +
                "RT True 3 25\n" +
                "ODOBAD 0 0 True", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Programs whose group formal and RETURNING item are VARIABLE-LENGTH groups (ISO §8.5.1.12): a dynamic-length
    /// item with a LIMIT, a dynamic-capacity table of numeric leaves with a minimum capacity, a dynamic-capacity table whose
    /// elements hold a dynamic-length item (a table of variable-length elements), and a fixed table whose occurrences hold
    /// one each; and a group whose OCCURS DEPENDING ON table follows a dynamic-length item, as its trailing storage. The
    /// activated program grows both dynamic-capacity tables by storing past their capacity, stores past the LIMITs and
    /// raises the DEPENDING ON count.</summary>
    private const string VarPrograms = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H5VAR.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-V.
           05 LK-ID PIC 9(3).
           05 LK-NAME PIC X DYNAMIC LENGTH LIMIT IS 10.
           05 LK-NUMS PIC 9(2) OCCURS DYNAMIC CAPACITY IN LK-NC FROM 1.
           05 LK-ROWS OCCURS DYNAMIC CAPACITY IN LK-RC.
              10 LK-RK PIC X(2).
              10 LK-RN PIC X DYNAMIC LENGTH.
           05 LK-PAIR OCCURS 2.
              10 LK-PN PIC 9.
              10 LK-PT PIC X DYNAMIC LENGTH LIMIT IS 4.
           05 LK-TAG PIC X(2).
        PROCEDURE DIVISION USING LK-V.
        P1.
            DISPLAY "IN " LK-ID " [" LK-NAME "] " LK-NUMS(1) LK-NUMS(2) " " LK-RK(1) "[" LK-RN(1) "] "
                LK-PN(1) "[" LK-PT(1) "]" LK-PN(2) "[" LK-PT(2) "] " LK-TAG
            ADD 1 TO LK-ID
            MOVE "LONGER NAMES" TO LK-NAME
            MOVE 33 TO LK-NUMS(3)
            MOVE "R2" TO LK-RK(2)
            MOVE "second" TO LK-RN(2)
            MOVE "abcdef" TO LK-PT(1)
            MOVE "Z" TO LK-TAG
            GOBACK.
        END PROGRAM H5VAR.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H5RET.
        DATA DIVISION.
        LINKAGE SECTION.
        01 SR.
           05 SP PIC X(3).
           05 SDYN PIC X DYNAMIC LENGTH.
           05 ST PIC X(2) OCCURS DYNAMIC CAPACITY IN SC.
        PROCEDURE DIVISION RETURNING SR.
        P1.
            MOVE "RET" TO SP
            MOVE "dynamic" TO SDYN
            MOVE "t1" TO ST(1)
            MOVE "t2" TO ST(2)
            GOBACK.
        END PROGRAM H5RET.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H5ODO.
        DATA DIVISION.
        LINKAGE SECTION.
        01 LK-O.
           05 LK-OC PIC 9.
           05 LK-OD PIC X DYNAMIC LENGTH.
           05 LK-OT PIC X(2) OCCURS 1 TO 3 DEPENDING ON LK-OC.
        PROCEDURE DIVISION USING LK-O.
        P1.
            DISPLAY "ODO " LK-OC " [" LK-OD "] " LK-OT(1) LK-OT(2)
            MOVE 3 TO LK-OC
            MOVE "C3" TO LK-OT(3)
            MOVE "more" TO LK-OD
            GOBACK.
        END PROGRAM H5ODO.
        """;

    private const string VarHost = """
        using System;
        using Cobol.H5PROGS;

        public static class H5HostProgram
        {
            private static void Refused(string what, H5VAR.LK_V_Record v)
            {
                try { H5VAR.Call(ref v); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException x) { Console.WriteLine($"{what} {x.ParamName} {x.GetType().Name}"); }
            }

            public static int Main()
            {
                var v = new H5VAR.LK_V_Record();
                Console.WriteLine($"NEW {v.LK_NUMS.Length} {v.LK_ROWS.Length} [{v.LK_NAME}] {v.LK_PAIR.Length}");
                v.LK_ID = 7;
                v.LK_NAME = "Ann";
                v.LK_NUMS = [11, 22];
                v.LK_ROWS = [new() { LK_RK = "AA", LK_RN = "note" }];
                v.LK_PAIR[0] = new() { LK_PN = 1, LK_PT = "p" };
                v.LK_PAIR[1] = new() { LK_PN = 2, LK_PT = "qq" };
                v.LK_TAG = "XY";
                Refused("NAME", v with { LK_NAME = "ELEVEN CHARS" });
                Refused("MIN", v with { LK_NUMS = [] });
                Refused("NULL", v with { LK_ROWS = null! });
                var pairs = (H5VAR.LK_V_Record.LK_PAIR_Record[])v.LK_PAIR.Clone();
                pairs[1] = pairs[1] with { LK_PT = "five5" };
                Refused("PAIR", v with { LK_PAIR = pairs });
                H5VAR.Call(ref v);
                Console.WriteLine($"OUT {v.LK_ID} [{v.LK_NAME}] {string.Join(",", v.LK_NUMS)} "
                    + $"{string.Join(",", Array.ConvertAll(v.LK_ROWS, r => r.LK_RK + "/" + r.LK_RN))} "
                    + $"{string.Join(",", Array.ConvertAll(v.LK_PAIR, p => p.LK_PN + "/" + p.LK_PT))} [{v.LK_TAG}]");
                var r = H5RET.Call();
                Console.WriteLine($"RET [{r.SP}] [{r.SDYN}] {string.Join(",", r.ST)}");
                var o = new H5ODO.LK_O_Record();
                o.LK_OC = 2;
                o.LK_OD = "d";
                o.LK_OT = ["A1", "B2"];
                H5ODO.Call(ref o);
                Console.WriteLine($"ODOOUT {o.LK_OC} [{o.LK_OD}] {string.Join(",", o.LK_OT)}");
                return 0;
            }
        }
        """;

    /// <summary>A host passes and receives VARIABLE-LENGTH groups (ISO §8.5.1.12; DESIGN-external-repository §4.3): the
    /// published record presents a dynamic-length item as a string and a dynamic-capacity table as an array of its current
    /// capacity, and crosses as the CALL lane's variable-length carrier of its description. Expected values, derived: a
    /// dynamic-length item stored past its LIMIT is "truncated on the right as necessary" (§8.5.1.10.4), so "LONGER NAMES"
    /// returns as its first 10 characters and "abcdef" as its first 4; a store past a dynamic-capacity table's capacity
    /// creates the occurrence (§8.5.1.9.3), so the tables return with 3 and 2 entries; a landing that would lose content —
    /// a string past the LIMIT, an array below the minimum capacity ("Integer-4 is the minimum capacity of the table",
    /// §13.18.38.4 16)), a null array — is refused before anything is activated.</summary>
    [Fact]
    public void AHost_PassesVariableLengthGroups_AsTheCallLanesVariableLengthCarrier()
    {
        string dir = CutRunner.NewTempDir("h5host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H5PROGS", VarPrograms);
            NonCobolElement.CompileCSharp(dir, "H5HOST", VarHost, "H5PROGS", "H5PROGS");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "H5HOST.dll"), dir);
            Assert.Equal(
                "NEW 1 0 [] 2\n" +
                "NAME LK_V ArgumentOutOfRangeException\n" +
                "MIN LK_V ArgumentException\n" +
                "NULL LK_V ArgumentNullException\n" +
                "PAIR LK_V ArgumentOutOfRangeException\n" +
                "IN 007 [Ann] 1122 AA[note] 1[p]2[qq] XY\n" +
                "OUT 8 [LONGER NAM] 11,22,33 AA/note,R2/second 1/abcd,2/qq [Z ]\n" +
                "RET [RET] [dynamic] t1,t2\n" +
                "ODO 2 [d] A1B2\n" +
                "ODOOUT 3 [more] A1,B2,C3", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>A program whose group formal is a strongly-typed group holding a data pointer, a program pointer and a
    /// table: the first call sets both pointers, the second reads storage through the data pointer the host passed back.</summary>
    private const string SlotPrograms = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H4PTR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T TYPEDEF STRONG.
           05 TA PIC X(3).
           05 TP USAGE POINTER.
           05 TQ USAGE PROGRAM-POINTER.
           05 TN PIC 9(2) OCCURS 2.
        01 WS-X PIC X(4) VALUE "DATA".
        LINKAGE SECTION.
        01 LK-G TYPE T.
        01 LK-X PIC X(4) BASED.
        PROCEDURE DIVISION USING LK-G.
        P1.
            IF TP OF LK-G = NULL
                DISPLAY "TP NULL " TA OF LK-G " " TN OF LK-G (2)
                SET TP OF LK-G TO ADDRESS OF WS-X
                SET TQ OF LK-G TO ENTRY "H4PTR"
            ELSE
                SET ADDRESS OF LK-X TO TP OF LK-G
                DISPLAY "TP " LK-X
            END-IF
            MOVE "XYZ" TO TA OF LK-G
            ADD 1 TO TN OF LK-G (2)
            GOBACK.
        END PROGRAM H4PTR.
        """;

    private const string SlotHost = """
        using System;
        using CobolNet.Runtime;
        using Cobol.H4PROGS;

        public static class H4HostProgram
        {
            public static int Main()
            {
                var g = new H4PTR.LK_G_Record { TA = "ABC" };
                g.TN[1] = 4;
                H4PTR.Call(ref g);
                Console.WriteLine($"1 [{g.TA}] {g.TN[1]} {g.TP is { IsNull: false }} {!g.TQ.IsNull}");
                H4PTR.Call(ref g);
                Console.WriteLine($"2 [{g.TA}] {g.TN[1]}");
                var image = H4PTR.LK_G_Record.FromImage(g.AsImage());
                Console.WriteLine($"IMG [{image.TA}] {image.TP is null || image.TP.IsNull}");
                try { var bad = g with { TN = new long[1] }; H4PTR.Call(ref bad); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException x) { Console.WriteLine($"OCC {x.ParamName}"); }
                return 0;
            }
        }
        """;

    /// <summary>A host passes a strongly-typed group whose pointer members ride managed slots (DESIGN-external-repository
    /// §4.3's last host-type row): the record crosses as the CALL lane's cell area, so the activated program's formal
    /// occupies that storage (ISO §14.2.3 8), cite.py OK) — its references as well as its characters — and the pointers
    /// it sets come back to the host, which can pass them in again.</summary>
    [Fact]
    public void AHost_PassesAStrongGroupWithPointers_AsTheCellAreaOfItsReferences()
    {
        string dir = CutRunner.NewTempDir("h4host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H4PROGS", SlotPrograms);
            NonCobolElement.CompileCSharp(dir, "H4HOST", SlotHost, "H4PROGS", "H4PROGS");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "H4HOST.dll"), dir);
            Assert.Equal(
                "TP NULL ABC 04\n" +
                "1 [XYZ] 5 True True\n" +
                "TP DATA\n" +
                "2 [XYZ] 6\n" +
                "IMG [XYZ] True\n" +
                "OCC LK_G", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Whether the module <paramref name="name"/> in <paramref name="dir"/> has an entry point, and whether it
    /// has a runtime configuration — an executable has both, a library neither.</summary>
    private static (bool EntryPoint, bool RuntimeConfig) Launchable(string dir, string name)
    {
        using var pe = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(Path.Combine(dir, name + ".dll")));
        return (pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress != 0,
            File.Exists(Path.Combine(dir, name + ".runtimeconfig.json")));
    }

    [Fact]
    public void AHost_CallsTheGeneratedEntries_LandingExactlyAndDecodingByTheValidityRule()
    {
        string dir = CutRunner.NewTempDir("h1host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H1PROGS", Programs);
            NonCobolElement.CompileCobol(dir, "H1FUNC", Function);
            // A group with no main program is a LIBRARY (DESIGN-external-repository §7.2): no entry point and no runtime
            // configuration, loaded by the host that calls its entries; a group with one is an executable.
            Assert.Equal((false, false), Launchable(dir, "H1FUNC"));
            Assert.Equal((true, true), Launchable(dir, "H1PROGS"));
            NonCobolElement.CompileCSharp(dir, "H1HOST", Host, "H1PROGS", "H1PROGS", "H1FUNC");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "H1HOST.dll"), dir);
            Assert.Equal(
                "IN [ALICE     ]\n" +
                "NOTE OMITTED\n" +
                "1 105.25 [CHANGED   ] 10\n" +
                "IN [CHANGED   ]\n" +
                "NOTE [HELLO   ]\n" +
                "2 100.25 [CHANGED   ] 20\n" +
                "IN [CHANGED   ]\n" +
                "NOTE [HELLO   ]\n" +
                "3 10\n" +
                "LOW LK_AMOUNT\n" +
                "HIGH LK_AMOUNT\n" +
                "LENGTH LK_NAME\n" +
                "IN [CHANGED   ]\n" +
                "NOTE [HELLO   ]\n" +
                "4 20\n" +
                "B=0005\n" +
                "A 5\n" +
                "ALIAS LK_C\n" +
                "ALIAS LK_I\n" +
                "FIRST ArgumentException LK_C\n" +
                "BAD 7 [XYZ] True\n" +
                "U=0012\n" +
                "W 999999999999999998 99999999999999999999\n" +
                "WIDE LK_L\n" +
                "SIGN LK_U\n" +
                "P NULL\n" +
                "PTR True\n" +
                "A [ABCD] B [ABCD]\n" +
                "P OMITTED\n" +
                "Q NULL\n" +
                "R NULL\n" +
                "S OMITTED\n" +
                "OPTNULL LK_B\n" +
                "FN 42", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
