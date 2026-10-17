*>--- conformance-case
*> id: pb695-apply-commit-no-on
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORD MUST REACH THE DOCUMENTED REFUSAL, NOT A PARSE ERROR (PB695).
      *> ISO 12.4.6.3.2 prints `APPLY COMMIT ON [ [ file-name-1 ] [ identifier-1 ] ] ...`. Measured on
      *> printed page 363 / folio 333: APPLY carries an underline rectangle (x 143.99-177.13 beneath a
      *> box of 145.21-175.84, 100% cover) and COMMIT one at 96.8%, while ON's box 228.49-243.51 has
      *> NO rule in its band at all. 8.3.2.4.3 therefore makes `APPLY COMMIT F` a conforming spelling
      *> of the clause, and the grammar's own comment claimed the opposite until family 3 measured it.
      *> WHY THIS IS A NEGATIVE CASE. The APPLY COMMIT clause is the DECLINED COMMIT/ROLLBACK feature
      *> (Annex A.4.3 item 2), and 4.2.7 makes non-support conformant only when it is DIAGNOSED -
      *> COBOLNET1709 is that diagnosis. The ON-less spelling must draw the SAME refusal the written
      *> spelling draws; the diagnostic code is what proves the clause was RECOGNIZED rather than
      *> mis-parsed, because a program the parser could not read would report COBOL0001 instead.
      *> reject-at names 2023 ALONE, and that is not an omission: the whole commit-and-rollback
      *> facility is a COBOL-2023 addition (Annex E.3.2 item 2), so below 2023 APPLY and COMMIT are
      *> ordinary user-defined words and this clause does not exist - a syntax error there is the
      *> CORRECT answer, and the grammar hook is {is2023()}?-gated to give it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695APPNOON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb695-apply-no-on.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(4).
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
