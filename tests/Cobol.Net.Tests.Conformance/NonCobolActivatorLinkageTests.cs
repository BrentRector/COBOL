// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witness for Annex A.1 item 116 — "Linkage section (whether access to linkage section items is meaningful when
/// called from a non-COBOL program)" (cite.py OK §A.1 116)) — and the determination <c>docs/CONFORMANCE.md</c>
/// DOC-A.1-116 records for it (kb/Work PB2671). ISO/IEC 1989:2023 §13.7.4 3) (cite.py OK): "If a program is activated
/// by a non-COBOL runtime element, the implementor defines whether access to formal parameters and the returning item
/// is guaranteed"; §13.7.4 5) (cite.py OK): "If the runtime element containing the linkage section is activated by a
/// non-COBOL runtime element, the initial value of a linkage section data item is defined by the implementor".
/// <para>DETERMINATION PINNED: the only non-COBOL activator is a .NET host (item 141), and its one interface is the
/// program's generated host entry, <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call(...)</c> (DESIGN-external-repository §4.3),
/// which activates the program through the boundary a COBOL CALL crosses, with a USING phrase that names every formal.
/// Access IS guaranteed, exactly as under a COBOL CALL with a USING phrase: each <c>Call</c> parameter is the formal in
/// its position, whose initial value is the host's value landed into storage of the formal's description (a group's,
/// its published record's image) and whose stores reach the host's variable when the call ends; an OPTIONAL formal
/// whose parameter is <c>null</c> is OMITTED (§14.9.4.4 11), cite.py OK); and the RETURNING item is <c>Call</c>'s
/// return value. The RETURNING item's initial value is not this item's to define: §14.2.3 7) (cite.py OK) makes it
/// "undefined" for every activator, so the program below stores into LR before the host reads it.</para>
/// <para>Expected output, derived from the determination: the first call passes every argument, so each formal shows
/// the host's value — LA and the group LG (AB12) as given, LN 007, LB nonzero, LP the NULL the host passed, the
/// national LK the spaces the host passed (read through ADDRESS OF, the AREA arm of kb/Work PB2671), LF zero — and the
/// host reads back the program's stores (LA ZZZZ, LG GG13 after MOVE "GG" and ADD 1) and the RETURNING value RRRR. The
/// second call passes <c>null</c> for every OPTIONAL formal, so each is OMITTED. A linkage VALUE clause is not applied
/// (§13.18.63.4 3), cite.py OK: "In the linkage section, VALUE clauses take effect only during the execution of an
/// explicit or implicit INITIALIZE statement"): LA shows the host's value, never VVVV.</para>
/// <para>A program activated by the operating system (the <c>Main</c> of a <c>cobol</c> executable) is the other
/// §13.7.4 3) case, whose effect is undefined; WiseOwl COBOL gives each formal its category default there, which the
/// golden <c>conformance:cases/A.1-Implementor-defined-language-element-list/pb2671_omitted_formal_category_default</c> pins.</para>
/// </summary>
public sealed class NonCobolActivatorLinkageTests
{
    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A116LNK.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 LKP USAGE POINTER.
        LINKAGE SECTION.
        01 LA PIC X(4) VALUE "VVVV".
        01 LG.
           05 LGA PIC X(2).
           05 LGN PIC 9(2).
        01 LN PIC 9(3) VALUE 5.
        01 LB BINARY-LONG.
        01 LP USAGE POINTER.
        01 LK PIC N(2).
        01 LF COMP-2.
        01 LR PIC X(4).
        PROCEDURE DIVISION USING LA LG OPTIONAL LN OPTIONAL LB
                OPTIONAL LP OPTIONAL LK OPTIONAL LF RETURNING LR.
            DISPLAY "LA=[" LA "] LG=[" LG "]"
            IF LN OMITTED
                DISPLAY "LN OMITTED"
            ELSE
                DISPLAY "LN=[" LN "]"
            END-IF
            IF LB OMITTED
                DISPLAY "LB OMITTED"
            ELSE
                IF LB = 0
                    DISPLAY "LB ZERO"
                ELSE
                    DISPLAY "LB NONZERO"
                END-IF
            END-IF
            IF LP OMITTED
                DISPLAY "LP OMITTED"
            ELSE
                IF LP = NULL
                    DISPLAY "LP NULL"
                ELSE
                    DISPLAY "LP SET"
                END-IF
            END-IF
            IF LK OMITTED
                DISPLAY "LK OMITTED"
            ELSE
                SET LKP TO ADDRESS OF LK
                IF LK = SPACES
                    DISPLAY "LK SPACES"
                ELSE
                    DISPLAY "LK NOT SPACES"
                END-IF
            END-IF
            IF LF OMITTED
                DISPLAY "LF OMITTED"
            ELSE
                IF LF = 0
                    DISPLAY "LF ZERO"
                ELSE
                    DISPLAY "LF NONZERO"
                END-IF
            END-IF
            MOVE "ZZZZ" TO LA
            MOVE "GG" TO LGA
            ADD 1 TO LGN
            MOVE "RRRR" TO LR
            GOBACK.
        END PROGRAM A116LNK.
        """;

    /// <summary>The non-COBOL activating element: a .NET host calling the program's generated host entry.</summary>
    private const string Host = """
        using System;
        using CobolNet.Runtime;
        using Cobol.A116LNK;

        public static class A116Host
        {
            public static int Main()
            {
                Console.WriteLine("CALL, EVERY ARGUMENT");
                string la = "ABCD";
                var lg = new A116LNK.LG_Record { LGA = "AB", LGN = 12 };
                long? ln = 7, lb = -5;
                ManagedPointer? lp = ManagedPointer.Null;
                string? lk = "  ";
                double? lf = 0;
                string r = A116LNK.Call(ref la, ref lg, ref ln, ref lb, ref lp, ref lk, ref lf);
                Console.WriteLine($"HOST LA={la} LG={lg.LGA}{lg.LGN} LN={ln} RETURNING={r}");
                Console.WriteLine("CALL, OPTIONAL FORMALS NULL");
                la = "WXYZ";
                lg = new A116LNK.LG_Record { LGA = "CD", LGN = 34 };
                ln = null; lb = null; lp = null; lk = null; lf = null;
                r = A116LNK.Call(ref la, ref lg, ref ln, ref lb, ref lp, ref lk, ref lf);
                Console.WriteLine($"HOST LA={la} LG={lg.LGA}{lg.LGN} LN={(ln is null ? "NULL" : ln)} RETURNING={r}");
                return 0;
            }
        }
        """;

    /// <summary>The four optional formals the second call passes as <c>null</c>, after LN.</summary>
    private const string OmittedTail = "LB OMITTED\nLP OMITTED\nLK OMITTED\nLF OMITTED\n";

    [Fact]
    public void ANetHost_CallsTheEntry_WhichGuaranteesLinkageAccessAndOmitsANullOptionalFormal()
    {
        string dir = CutRunner.NewTempDir("a116host");
        try
        {
            NonCobolElement.CompileCobol(dir, "A116LNK", Program);
            NonCobolElement.CompileCSharp(dir, "A116HOST", Host, "A116LNK", "A116LNK");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "A116HOST.dll"), dir);
            Assert.Equal(
                "CALL, EVERY ARGUMENT\n" +
                "LA=[ABCD] LG=[AB12]\n" +
                "LN=[007]\n" +
                "LB NONZERO\n" +
                "LP NULL\n" +
                "LK SPACES\n" +
                "LF ZERO\n" +
                "HOST LA=ZZZZ LG=GG13 LN=7 RETURNING=RRRR\n" +
                "CALL, OPTIONAL FORMALS NULL\n" +
                "LA=[WXYZ] LG=[CD34]\n" +
                "LN OMITTED\n" +
                OmittedTail +
                "HOST LA=ZZZZ LG=GG35 LN=NULL RETURNING=RRRR", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
