*>--- conformance-case
*> id: pb403-exit-perform-below-2002
*> rules: [SR-14.9.14.3-8]
*> source-format: free
*> portable: unverified
*>---
*> The edition floor under the §14.9.14.3 SR8 placement rule (kb/Work PB403). EXIT PERFORM is a COBOL-2002
*> introduction (Annex E; ConstructRegistry row exit-perform-2002), so BELOW 2002 the statement is refused by
*> the introduction gate COBOLNET0900 before any placement question can be asked -- which is why the SR8
*> placement fixture (pb403-exit-perform-outside-perform) names 2002 2014 2023 and not 85.
*>
*> The statement here is written in an ADMITTED position (inside an inline PERFORM), so the only thing that can
*> reject this program at 85 is the edition gate: a gate regression that let 2002 syntax through at 85 would
*> show up here as a clean compile, and a placement regression could not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB403NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-N    PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 3
               EXIT PERFORM
           END-PERFORM.
           STOP RUN.
