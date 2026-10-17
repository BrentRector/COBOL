*>--- conformance-case
*> id: pb488-cond-var-strong-typedef
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "... g) A type declaration described with the STRONG phrase, or a
*> group item subordinate to such a type declaration."
*>
*> The subject here is the type DECLARATION itself. kb/Work PB488 measured the condition-name being cloned
*> onto every TYPE reference site and evaluating there. TYPEDEF STRONG is a COBOL-2002 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-STRONG-TYPEDEF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF STRONG.
       88 T-COND VALUE "AB".
          05 F1 PIC X(2).
       01 V TYPE T.
       PROCEDURE DIVISION.
           MOVE "AB" TO F1 OF V
           IF T-COND DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
