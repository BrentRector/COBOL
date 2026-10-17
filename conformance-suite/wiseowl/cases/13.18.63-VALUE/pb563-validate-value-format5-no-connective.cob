*>--- conformance-case
*> id: pb563-validate-value-format5-no-connective
*> rules: [GR-13.18.63.4-24]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB563 - the CONNECTIVE-LESS spelling of the 13.18.63 format-5 content-validation entry, which the
*> two witnesses that existed did not write.
*> The printed Format 5 (rendered from the PDF, page 546 = printed 516) brackets the connective:
*> `{ VALUE | VALUES } { literal-5 [ THROUGH literal-6 ] } ... [ IN alphabet-name-1 ] [ IS | ARE ]
*> { INVALID | VALID } [ WHEN condition-1 ]` - so `VALUES 10 THRU 20 VALID.` is the standard's own spelling,
*> not a vendor one. The VALIDATE facility is an OPTIONAL element (Annex A.4.14 item 7) whose support this
*> implementation does not claim (docs/CONFORMANCE.md section 4 item 3, section 5) and which 2023 additionally
*> makes obsolete (4.2.13; Annex F.2 item 5), so the entry is refused BY NAME - COBOLNET1708 - exactly as its
*> IS-bearing twin is.
*> WHY IT EXISTS: `declined-validate-value-format5` and its -anon sibling BOTH write `IS VALID`, so the green
*> negative corpus was evidence about the spelling it returned and silent about the one it dropped. PB563
*> measured the connective-less entry compiling rc=0 with no diagnostic at all, and a reference to the
*> condition-name then leaking `error CS0103: The name 'VALIDL' does not exist in the current context` out of
*> Roslyn. The cause was VALID sitting as an UNGUARDED cobolWord alternative while 8.9 reserves it, so the
*> greedy VALUE operand loop ate it as a constant-name; kb/Work PB693 made the 8.9 reservation gate DERIVED
*> (`{userWordHere("VALID")}?`) and the hole closed with it. This case is what keeps it closed
*> (feedback_green_gates_arent_evidence).
*> The condition-name is REFERENCED on purpose: that is the half that reached the backend.
*> 2002+ ONLY: below COBOL-2002 VALID is an ordinary user-defined word (8.9), so `VALUES 10 THRU 20 VALID.`
*> is an ordinary syntax error there (COBOLNET1639, "the VALUE operand 'VALID' is not a literal") and not this
*> decline - which is the edition-correct answer, and pinning 85 here would assert the wrong rule.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB563NC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F PIC 9(2) VALUE 15.
   88 F-OK VALUES 10 THRU 20 VALID.
PROCEDURE DIVISION.
MAIN-P.
    IF F-OK
       DISPLAY "YES"
    END-IF.
    STOP RUN.
