*> reject-at: 2023
*> kb/Work PB1160 - §14.9.33.3 SR2 is a FLAT prohibition: "The RESUME statement shall not be specified in a
*> declarative procedure for which the GLOBAL phrase is specified in the associated USE statement."
*>   OK  §14.9.33.3 2)  (Syntax rules)
*> A RESUME written in a WHEN phrase of an exception-checking PERFORM (§14.9.28.4) is still SPECIFIED in the
*> declarative procedure G whose USE statement carries GLOBAL: SR2 has no exception for a statement nested in a
*> WHEN phrase, and §14.9.33.3 SR1's permission for a WHEN phrase answers a different question (where a RESUME
*> may appear), not whether SR2 is excused. The binder used to ask the WHEN-phrase question first and so let this
*> RESUME through; it is now refused with COBOLNET0713 exactly as a RESUME written directly in G is.
*> (The exception-checking PERFORM is a COBOL 2023 construct, so 2023 is the introducing edition for this shape;
*> the direct form is pinned by ExceptionConditionConformanceTests.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1160NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "no-such-file-pb1160n.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       G SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       G1.
           PERFORM ADD 1 TO N WHEN EC-SIZE RESUME NEXT STATEMENT
           END-PERFORM.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           STOP RUN.
