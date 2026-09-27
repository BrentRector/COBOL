// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PB390 INVARIANT, the half PB236 left standing: a syntax rule the binder has ALREADY DECIDED is
/// reported as an ERROR naming the rule — never compiled into the program as a run-time refusal, and never
/// announced as a gap in WiseOwl COBOL. PB236 separated the carrier's three jobs at the ADD/CORRESPONDING and
/// file-name sites; the procedure-name family (PERFORM, GO TO, GO TO … DEPENDING, ALTER, RESUME AT,
/// SORT/MERGE INPUT and OUTPUT PROCEDURE), the SET Format-3 and Format-4 operand rules, and §14.9.25.3 SR12's
/// "shall not be reference-modified" half were still shipping their verdicts as <c>BoundUnsupported</c>.
/// <para>What that cost, measured on this tree before the fix: <c>PERFORM NO-SUCH-PARAGRAPH</c> COMPILED,
/// produced an assembly, and aborted the run unit with an unhandled .NET exception reading "a COBOL feature
/// that is not yet implemented was reached at run time" — the diagnosis sent to the wrong party, at the wrong
/// time, and on an unexecuted path not sent at all. ISO §4.2.2 ¶2: "An implementation shall provide a warning
/// mechanism that optionally may be invoked by the user at compile time to indicate violations of the general
/// formats and the explicit syntax rules of standard COBOL."</para>
/// <para>⛔ WHY THE 1756 ASSERTION IS IN EVERY ROW. The failure this guards is not "no diagnostic" — PB236's
/// announce would give one — but the WRONG KIND of diagnostic: a WARNING that says the compiler is
/// incomplete, on a program that still compiles and still ships. A row that only asserted "something was
/// reported" would pass in exactly the state this note opened in (feedback_green_gates_arent_evidence).</para>
/// <para>⛔ AND WHY THE LEGAL HALF IS HERE. Every rule below is a REFUSAL, and a compiler that refused these
/// constructs outright would pass all of them. The <see cref="LegalControl"/> rows are the ones that must
/// still COMPILE: an INVERTED <c>PERFORM P2 THRU P1</c> range (§14.9.28.4 GR6 — "there is no necessary
/// relationship between procedure-name-1 and procedure-name-2"), a SECTION target, a QUALIFIED
/// procedure-name, a level-88 <c>SET … TO TRUE</c>, a Format-3 switch set, and a plain group CORRESPONDING.
/// </para>
/// </summary>
public sealed class DecidedRuleStageDriftTests
{
    /// <summary>A whole program around <paramref name="body"/>, with the data division the rows share.</summary>
    private static string Program(string id, string body, string data = "", string specialNames = "") => $"""
IDENTIFICATION DIVISION.
PROGRAM-ID. {id}.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
{(specialNames.Length > 0 ? specialNames : "    SWITCH-1 IS SW-M ON STATUS IS SW-ON OFF STATUS IS SW-OFF.")}
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-N PIC 9(3) VALUE 1.
01 G1.
   05 A PIC 9(3) VALUE 1.
   05 B PIC 9(3) VALUE 2.
01 G2.
   05 A PIC 9(3) VALUE 0.
   05 B PIC 9(3) VALUE 0.
01 WS-FLAG PIC X VALUE "N".
   88 WS-FLAG-YES VALUE "Y".
{data}
PROCEDURE DIVISION.
S1 SECTION.
MAIN.
{body}
    STOP RUN.
P1.
    DISPLAY "P1".
P2.
    DISPLAY "P2".
""";

    // ── The rules that must be ERRORS at bind time, each naming its own rule ──────────────────────────────
    // ISO §14.9.28.3 SR12/SR13 · §8.4.2.1 + §8.4.6.1 (GO TO, ALTER, RESUME AT — those statements state no
    // procedure-name rule of their own) · §14.9.39.3 SR5/SR6 · §14.9.25.3 SR12 with §14.9.2.3 / §14.9.44.3 SR6.
    [Theory]
    // PERFORM procedure-name-1 and procedure-name-2 — two rules, one shape, BOTH arms (the two-arm defect).
    [InlineData("PB390D01", "    PERFORM NO-SUCH-PARA.", "COBOLNET1639", "§14.9.28.3 SR12")]
    [InlineData("PB390D02", "    PERFORM P1 THRU NO-SUCH-PARA.", "COBOLNET1639", "§14.9.28.3 SR13")]
    // The discriminating case SR12 exists for: the word IS declared, as something that is not a procedure.
    [InlineData("PB390D03", "    PERFORM WS-N.", "COBOLNET1639", "declared as a DATA ITEM")]
    // GO TO, both formats — the sibling carrier of the same resolution.
    [InlineData("PB390D04", "    GO TO NO-SUCH-PARA.", "COBOLNET1639", "GO TO 'NO-SUCH-PARA'")]
    [InlineData("PB390D05", "    GO TO P1 NO-SUCH-PARA DEPENDING ON WS-N.", "COBOLNET1639", "GO TO DEPENDING")]
    // SET Format 4 (§14.9.39.3 SR6) — the switch-status condition-name, which IS a condition-name.
    [InlineData("PB390D06", "    SET SW-ON TO TRUE.", "COBOLNET1757", "§14.9.39.3 SR6")]
    [InlineData("PB390D07", "    SET NOT-DECLARED TO TRUE.", "COBOLNET1639", "is not a condition-name")]
    // SET Format 3 (§14.9.39.3 SR5) — a name that is not an external-switch mnemonic.
    [InlineData("PB390D08", "    SET WS-N TO ON.", "COBOLNET1757", "§14.9.39.3 SR5")]
    // §14.9.25.3 SR12's SECOND half, in all three spellings of the CORRESPONDING operand rule.
    [InlineData("PB390D09", "    MOVE CORRESPONDING G1(1:3) TO G2.", "COBOLNET1757", "§14.9.25.3 SR12")]
    [InlineData("PB390D10", "    ADD CORRESPONDING G1(1:3) TO G2.", "COBOLNET1757", "§14.9.2.3 SR6")]
    [InlineData("PB390D11", "    SUBTRACT CORRESPONDING G1(1:3) FROM G2.", "COBOLNET1757", "§14.9.44.3 SR6")]
    public void DecidedRule_IsACompileTimeError_NamingTheRule_AndNeverTheDeferralWarning(
        string id, string body, string code, string fragment)
    {
        var (ok, errors, warnings) = Compile(Program(id, body));
        Assert.False(ok, "a violated syntax rule shall not compile: " + string.Join("\n", errors));
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal)
                                     && e.Contains(fragment, StringComparison.Ordinal));
        // ⛔ THE SEPARATION ITSELF: the source is wrong, so this is never the COBOLNET1756 deferral announce.
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>The RESUME AT arm (ISO §14.9.33) — its procedure-name goes through the same ONE resolution, and
    /// it needs a declarative to be written at all, so it gets its own program rather than a row above.</summary>
    [Fact]
    public void ResumeAt_UnknownProcedure_IsACompileTimeError()
    {
        var (ok, errors, warnings) = Compile("""
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390D12.
PROCEDURE DIVISION.
DECLARATIVES.
D-SEC SECTION.
    USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
D-PARA.
    RESUME AT NO-SUCH-PARA.
END DECLARATIVES.
MAIN-SEC SECTION.
MAIN.
    DISPLAY "X".
    STOP RUN.
""");
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1639", StringComparison.Ordinal)
                                     && e.Contains("RESUME AT 'NO-SUCH-PARA'", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>ALTER is ANSI X3.23-1985 only (deleted by ISO/IEC 1989:2002), so its two procedure-name
    /// operands are exercised at the 85 edition. Both arms: the paragraph to be altered, and the new
    /// destination — the second used to be the silent one.</summary>
    [Theory]
    [InlineData("PB390D13", "    ALTER NO-SUCH-PARA TO PROCEED TO P2.", "ALTER 'NO-SUCH-PARA'")]
    [InlineData("PB390D14", "    ALTER P1 TO PROCEED TO NO-SUCH-PARA.", "ALTER TO PROCEED TO 'NO-SUCH-PARA'")]
    public void Alter_UnknownProcedure_IsACompileTimeError_At85(string id, string stmt, string fragment)
    {
        var (ok, errors, warnings) = Compile($"""
IDENTIFICATION DIVISION.
PROGRAM-ID. {id}.
PROCEDURE DIVISION.
MAIN.
{stmt}
    STOP RUN.
P1.
    GO TO P2.
P2.
    DISPLAY "P2".
""", edition: 85);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1639", StringComparison.Ordinal)
                                     && e.Contains(fragment, StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>SEARCH's identifier-1 rules (ISO §14.9.37.3 SR1–SR3, ALL FORMATS; kb/Work PB443) — the same
    /// invariant on a statement that needs tables in its data division, so it gets its own program the way ALTER
    /// and RESUME AT do. Every one of these was a <c>BoundUnsupported</c> or nothing at all: SR2's first sentence
    /// was DECIDED and shipped as a run-time "not implemented" abort under a message miscited as SR1, and SR1,
    /// SR2's second sentence and SR3 drew no reaction of any kind because the binder read only identifier-1's
    /// BASE WORD and never saw a modifier or a subscript.</summary>
    [Theory]
    [InlineData("PB443D01", "    SEARCH E1(1:3) AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.", "SR1")]
    [InlineData("PB443D02", "    SEARCH E1(IX1) AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.", "SR2")]
    [InlineData("PB443D03", "    SEARCH WS-PLAIN AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.", "SR2")]
    [InlineData("PB443D04", "    SEARCH NE AT END CONTINUE WHEN NKK (1) = 3 CONTINUE END-SEARCH.", "SR2")]
    // Format 2 prints the SAME identifier-1 and is bound by the SAME ALL-FORMATS rules — the two-arm question.
    [InlineData("PB443D06", "    SEARCH ALL E1(1:3) AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.", "SR1")]
    [InlineData("PB443D07", "    SEARCH ALL WS-PLAIN AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.", "SR2")]
    public void SearchIdentifier1_IsACompileTimeError_NamingTheRule(string id, string body, string rule)
    {
        var (ok, errors, warnings) = Compile(SearchProgram(id, body));
        Assert.False(ok, "a violated syntax rule shall not compile: " + string.Join("\n", errors));
        Assert.Contains(errors, e => e.Contains("COBOLNET2075", StringComparison.Ordinal)
                                     && e.Contains("§14.9.37.3 " + rule, StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>The SEARCH falsification half — every spelling §14.9.37.3 ADMITS, including the two a blanket
    /// "identifier-1 takes no subscript" screen would wrongly refuse (SR3 makes the superordinate subscripting
    /// REQUIRED, so `SEARCH INNER (OX)` is the conforming form for a nested table).</summary>
    [Theory]
    [InlineData("PB443L01", "    SEARCH E1 AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.")]
    [InlineData("PB443L02", "    SEARCH E1 IN G-ONE AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.")]
    [InlineData("PB443L03", "    SEARCH INNER (OX) AT END CONTINUE WHEN NK (OX IX3) = 3 CONTINUE END-SEARCH.")]
    [InlineData("PB443L04", "    SEARCH E1 VARYING IX1 AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.")]
    // ⛔ THE BARE NESTED FORM IS LEGAL, and a lower bound read into SR3 would refuse it: §14.9.37.4 GR1 puts the
    // superordinate occurrence in the WHEN phrases ("the subscript that is used to determine the occurrence of
    // each superordinate table to search is specified by the user in the WHEN phrases"), and the CCVS suite
    // writes exactly this — NC233A's `SEARCH ALL GRP2-ENTRY … WHEN SEC (IDX-1, IDX-2)` over a table nested in
    // `GRP-ENTRY OCCURS 10`. A lower bound failed SIX NIST programs; this row is why it stays out.
    [InlineData("PB443L05", "    SEARCH INNER AT END CONTINUE WHEN NK (OX IX3) = 3 CONTINUE END-SEARCH.")]
    [InlineData("PB443L06", "    SEARCH ALL E1 AT END CONTINUE WHEN K1 (IX1) = 3 CONTINUE END-SEARCH.")]
    public void SearchIdentifier1_LegalControl(string id, string body)
    {
        var (ok, errors, warnings) = Compile(SearchProgram(id, body));
        Assert.True(ok, string.Join("\n", errors));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET2075", StringComparison.Ordinal)
                                           || e.Contains("COBOLNET1639", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    /// <summary>A program with the table shapes §14.9.37.3 SR1–SR3 are about: an indexed table inside a group,
    /// a NESTED indexed table, a table with NO index phrase, and a non-table.</summary>
    private static string SearchProgram(string id, string body) => $"""
IDENTIFICATION DIVISION.
PROGRAM-ID. {id}.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-PLAIN PIC X(4).
01 G-ONE.
   05 E1 OCCURS 4 TIMES ASCENDING KEY IS K1 INDEXED BY IX1.
      10 K1 PIC 9(3).
01 NEST.
   05 OUTER OCCURS 2 TIMES INDEXED BY OX.
      10 INNER OCCURS 4 TIMES INDEXED BY IX3.
         15 NK PIC 9(3).
01 NOIX.
   05 NE OCCURS 4 TIMES.
      10 NKK PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    SET IX1 TO 1.
    SET OX TO 1.
    SET IX3 TO 1.
{body}
    STOP RUN.
""";

    // ── The falsification half: these are LEGAL and shall still compile clean ─────────────────────────────
    [Theory]
    // §14.9.28.4 GR6 — an INVERTED THRU range is legal (NIST NC102A PFM-TEST-F1-10).
    [InlineData("PB390L01", "    PERFORM P2 THRU P1.")]
    // A SECTION name is a procedure-name (§8.4.2.2); so is a qualified paragraph-name.
    [InlineData("PB390L02", "    PERFORM S1.")]
    [InlineData("PB390L03", "    PERFORM MAIN OF S1.")]
    // §14.9.39.3 SR6, satisfied: a level-88 condition-name IS associated with a conditional variable.
    [InlineData("PB390L04", "    SET WS-FLAG-YES TO TRUE.")]
    // §14.9.39.3 SR5, satisfied: SW-M is the SPECIAL-NAMES mnemonic of an external switch.
    [InlineData("PB390L05", "    SET SW-M TO ON.")]
    // The CORRESPONDING operands are plain groups — no reference modifier, nothing to refuse.
    [InlineData("PB390L06", "    MOVE CORRESPONDING G1 TO G2.")]
    [InlineData("PB390L07", "    ADD CORRESPONDING G1 TO G2.")]
    // A group reference modification is legal in its own right (kb/Work PB70) — only CORRESPONDING refuses it.
    [InlineData("PB390L08", "    DISPLAY G1(1:3).")]
    public void LegalControl(string id, string body)
    {
        var (ok, errors, warnings) = Compile(Program(id, body));
        Assert.True(ok, string.Join("\n", errors));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1639", StringComparison.Ordinal)
                                           || e.Contains("COBOLNET1757", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1756", StringComparison.Ordinal));
    }

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) Compile(
        string source, int edition = 2023)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB390_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb390.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb390.dll"), DialectLevel: edition, CheckOnly: true));
            return (r.Success, r.Errors, r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
