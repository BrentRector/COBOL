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
        PROCEDURE DIVISION USING LK-A LK-B LK-C.
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

                long a = 4, c = 0;
                H1ALIAS.Call(ref a, ref a, ref c);
                Console.WriteLine($"A {a}");
                try { long b = 0; H1ALIAS.Call(ref a, ref b, ref a); Console.WriteLine("ACTIVATED"); }
                catch (ArgumentException e) { Console.WriteLine($"ALIAS {e.ParamName}"); }

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

                long x = 21;
                Console.WriteLine($"FN {H1FN.Call(ref x)}");
                return 0;
            }
        }
        """;

    [Fact]
    public void AHost_CallsTheGeneratedEntries_LandingExactlyAndDecodingByTheValidityRule()
    {
        string dir = CutRunner.NewTempDir("h1host");
        try
        {
            NonCobolElement.CompileCobol(dir, "H1PROGS", Programs);
            NonCobolElement.CompileCobol(dir, "H1FUNC", Function);
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
                "BAD 7 [XYZ] True\n" +
                "U=0012\n" +
                "W 999999999999999998 99999999999999999999\n" +
                "WIDE LK_L\n" +
                "SIGN LK_U\n" +
                "FN 42", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
