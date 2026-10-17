*>--- conformance-case
*> id: pb466-ambiguous-resume-at
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - RESUME AT procedure-name, the exception-declarative arm of the same resolution step. The
*> statement is the COBOL-2002 introduction of the exception-handling module, so 85 is not in the reject set:
*> there the source is refused for the construct, not for the reference, and a reject-at that named 85 would
*> be measuring a different rule.
*> ISO 14.9.33.2 prints procedure-name-1 and 8.4.2.2.1 governs the reference like any other: rule 1 is false
*> for DUP-R (S-ONE and S-TWO both declare it) and rule 6 cannot excuse it, because the reference is written
*> in the declarative section H, which declares no DUP-R. 8.4.2.2.3 SR1's "sequence of qualifiers that
*> precludes any ambiguity of reference" is owed and absent.
*> Worth its own witness because the consequence is invisible at the site: RESUME AT names where the run unit
*> CONTINUES after a fatal exception condition, so an arbitrary pick resumes ordinary execution in a paragraph
*> the author never nominated, with the exception already reported as handled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466RESUME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5) VALUE "HELLO".
       01 WS-Y PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT".
           RESUME AT DUP-R.
       END DECLARATIVES.
       S-REF SECTION.
       P-REF.
           MOVE WS-X(7:2) TO WS-Y.
           STOP RUN.
       S-ONE SECTION.
       DUP-R.
           DISPLAY "ONE-R".
           STOP RUN.
       S-TWO SECTION.
       DUP-R.
           DISPLAY "TWO-R".
           STOP RUN.
