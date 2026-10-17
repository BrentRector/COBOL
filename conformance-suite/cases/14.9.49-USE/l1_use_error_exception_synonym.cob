*>--- conformance-case
*> id: l1_use_error_exception_synonym
*> rules: [SR-14.9.49.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR6 — "The words ERROR and EXCEPTION are synonymous and may be used
      *> interchangeably." (FORMAT 1.) Two declaratives differ in NOTHING but that one word: the
      *> two files are declared identically, both are provoked by the identical statement, and
      *> both handlers do the identical thing. If the two spellings were not one statement form,
      *> the EXCEPTION-spelled section could not be a Format 1 file-exception declarative and the
      *> HANDLER-VIA-EXCEPTION line could not appear.
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · §14.9.6.4 GR1 — "If the file connector is not open, the CLOSE statement is
      *>    unsuccessful and the I-O status indicator for the file connector is set to '42'";
      *>    §9.1.13.7 rule 2 names the same value for "a CLOSE or UNLOCK statement … attempted
      *>    for a file connector that is not in an open mode". Neither file is ever opened.
      *>  · §14.9.49.4 GR6 a) — "If file-name-1 is specified, the associated procedure is
      *>    executed when the condition described in the USE statement occurs" — runs each
      *>    file-scoped declarative once, so both handler lines appear, each reporting 42.
      *>  · SR6 is what makes the second declarative a Format 1 USE at all; therefore the
      *>    ERROR leg and the EXCEPTION leg must be byte-identical but for the tag.
      *>  · Control returns by §14.9.33.4 GR2 a) (RESUME AT NEXT STATEMENT), so the AFTER-
      *>    lines show the same 42 the handler saw — the two spellings agree after the
      *>    declarative as well as inside it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE6A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use6a-1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "l1use6a-2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       FD F2.
       01 R2 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SPELLED-ERROR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       SPELLED-ERROR-PARA.
           DISPLAY "HANDLER-VIA-ERROR=" ST1
           RESUME AT NEXT STATEMENT.
       SPELLED-EXCEPTION-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       SPELLED-EXCEPTION-PARA.
           DISPLAY "HANDLER-VIA-EXCEPTION=" ST2
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           CLOSE F1
           DISPLAY "AFTER-F1=" ST1
           CLOSE F2
           DISPLAY "AFTER-F2=" ST2
           DISPLAY "DONE"
           STOP RUN.
