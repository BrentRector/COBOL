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
/// <para>DETERMINATION PINNED, per entry the .NET host (the only non-COBOL activator, item 141) uses:</para>
/// <list type="bullet">
/// <item><b>A call</b> (<c>ProgramRegistry.CallProgram</c>, which activates through <c>ICobolProgram.Call</c>): access
/// IS guaranteed. Each argument maps positionally to its formal, whose initial value is the host's carrier and whose
/// stores reach it, and the RETURNING item is delivered into the <c>returning</c> carrier. An argument the host omits
/// (a <c>ManagedPointer.Null</c> carrier, or fewer arguments) makes its formal OMITTED.</item>
/// <item><b>The main-program entry</b> (<c>ProgramRegistry.RunMain</c>, which activates through
/// <c>ICobolProgram.Activate</c>): access is NOT guaranteed, as for activation by the operating system. On an
/// instance's first activation every formal refers to storage private to the instance holding the item's
/// category-default value — spaces for a character item, zero for a numeric item of any usage, NULL for a pointer, and
/// for a group each subordinate elementary item's — and is not OMITTED. After an earlier call on the same instance each
/// formal keeps what that call bound: the host's carrier for an elementary formal, the area that call filled from the
/// carrier for a group, and OMITTED for a formal that call omitted.</item>
/// </list>
/// <para>Expected output, derived from the determination: run unit 1 activates a fresh instance through the main
/// entry, so LA is four spaces and LN is zero although both carry a VALUE clause (§13.18.63.4 3), cite.py OK: "In the
/// linkage section, VALUE clauses take effect only during the execution of an explicit or implicit INITIALIZE
/// statement"), the group LG is its elementary items' defaults (LGA spaces, LGN zero), LB is zero, LP is NULL, the
/// ADDRESS OF-referenced national LK is national spaces and the floating-point LF is zero. LG, LK and LF are the
/// formals whose storage is an AREA rather than a carrier (a group, an addressed item, a floating-point item), so they
/// pin the record-image arm of the unbound value; LK is the one where a carrier-shaped image does not fit (kb/Work
/// PB2671's refuted first fix: two characters in a four-position cell, EC-BOUND-PTR).
/// Run unit 2 calls with every argument and then with the optional ones omitted in both ways, and the host reads back the program's stores and the RETURNING value. The
/// final main entry on the same instance sees the host's new LA content, the group area as the last call left it
/// (IJ78 after ADD 1 and MOVE "GG" is GG79), LN still the host's 042, and LB, LP, LK and LF OMITTED because the last call
/// omitted them; its MOVE to LA reaches the host's storage, and its stores to LG reach only that private area.</para>
/// <para>Before kb/Work PB2671 the main entry broke the determination twice: a group (AREA) formal had no storage, so
/// its first reference was the fatal EC-DATA-PTR-NULL, and an image-stored binary formal was seeded with spaces, a
/// nonzero value. The corpus cannot see either: its activator is always the operating system, where §13.7.4 3) makes
/// access undefined.</para>
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

    /// <summary>The non-COBOL activating element: two run units, each begun the way the generated driver begins one
    /// (<c>ProgramRegistry.Reset</c>, then the module's registrar).</summary>
    private const string Host = """
        using System;
        using System.IO;
        using CobolNet.Runtime;

        public static class A116Host
        {
            private static void BeginRunUnit()
            {
                ProgramRegistry.Reset();
                var asm = System.Reflection.Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "A116LNK.dll"));
                var registrar = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<CobolNet.Runtime.Repository.CobolRepositoryAttribute>(asm)!.Registrar!;
                asm.GetType(registrar)!.GetMethod("EnsureRegistered", Type.EmptyTypes)!.Invoke(null, null);
            }

            private static CobolArg Ref(ManagedPointer carrier) => new(CobolPassMode.Reference, carrier, null);

            private static ManagedPointer<string> Text(string image) => ManagedPointer<string>.Cell(image);

            public static int Main()
            {
                BeginRunUnit();
                Console.WriteLine("MAIN ENTRY, FRESH INSTANCE");
                ProgramRegistry.RunMain("A116LNK");

                BeginRunUnit();
                Console.WriteLine("CALL, EVERY ARGUMENT");
                var la = Text("ABCD");
                var lg = Text("AB12");
                var ret = Text("....");
                ProgramRegistry.CallProgram("A116LNK", "", new[] { Ref(la), Ref(lg), Ref(Text("007")) },
                    new CobolArg(CobolPassMode.Reference, ret, null, Length: 4));
                Console.WriteLine("HOST LA=" + la.Value + " LG=" + lg.Value + " RETURNING=" + ret.Value);
                Console.WriteLine("CALL, NULL CARRIER");
                ProgramRegistry.CallProgram("A116LNK", "", new[] { Ref(Text("WXYZ")), Ref(Text("CD34")), Ref(ManagedPointer.Null) }, null);
                Console.WriteLine("CALL, FEWER ARGUMENTS");
                ProgramRegistry.CallProgram("A116LNK", "", new[] { Ref(Text("MNOP")), Ref(Text("EF56")) }, null);
                Console.WriteLine("CALL, LN ONLY");
                var ld = Text("EFGH");
                var lgd = Text("IJ78");
                ProgramRegistry.CallProgram("A116LNK", "", new[] { Ref(ld), Ref(lgd), Ref(Text("042")) }, null);
                ld.Value = "QQQQ";
                lgd.Value = "KL90";
                Console.WriteLine("MAIN ENTRY, SAME INSTANCE");
                ProgramRegistry.RunMain("A116LNK");
                Console.WriteLine("HOST LA=" + ld.Value + " LG=" + lgd.Value);
                return 0;
            }
        }
        """;

    /// <summary>The four optional formals no host call in this test supplies.</summary>
    private const string OmittedTail = "LB OMITTED\nLP OMITTED\nLK OMITTED\nLF OMITTED\n";

    [Fact]
    public void ANetHost_CallGuaranteesLinkageAccess_MainEntrySeesCategoryDefaultsOrTheLastCallsBinding()
    {
        string dir = CutRunner.NewTempDir("a116host");
        try
        {
            NonCobolElement.CompileCobol(dir, "A116LNK", Program);
            NonCobolElement.CompileCSharp(dir, "A116HOST", Host, runtimeConfigOf: "A116LNK");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "A116HOST.dll"), dir);
            Assert.Equal(
                "MAIN ENTRY, FRESH INSTANCE\n" +
                "LA=[    ] LG=[  00]\n" +
                "LN=[000]\n" +
                "LB ZERO\n" +
                "LP NULL\n" +
                "LK SPACES\n" +
                "LF ZERO\n" +
                "CALL, EVERY ARGUMENT\n" +
                "LA=[ABCD] LG=[AB12]\n" +
                "LN=[007]\n" +
                OmittedTail +
                "HOST LA=ZZZZ LG=GG13 RETURNING=RRRR\n" +
                "CALL, NULL CARRIER\n" +
                "LA=[WXYZ] LG=[CD34]\n" +
                "LN OMITTED\n" +
                OmittedTail +
                "CALL, FEWER ARGUMENTS\n" +
                "LA=[MNOP] LG=[EF56]\n" +
                "LN OMITTED\n" +
                OmittedTail +
                "CALL, LN ONLY\n" +
                "LA=[EFGH] LG=[IJ78]\n" +
                "LN=[042]\n" +
                OmittedTail +
                "MAIN ENTRY, SAME INSTANCE\n" +
                "LA=[QQQQ] LG=[GG79]\n" +
                "LN=[042]\n" +
                OmittedTail +
                "HOST LA=ZZZZ LG=KL90", stdout);
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
