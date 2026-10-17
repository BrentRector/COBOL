*>--- conformance-case
*> id: pb1029-string-all-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1029 - ISO 14.9.43.3 SR2: "Literal-1 or literal-2 shall not be a figurative
      *> constant that begins with the word ALL." The refusal was bound as an operand error node
      *> WITHOUT a diagnostic: the program compiled clean and aborted the run unit with
      *> NotImplementedCobolFeatureException when the STRING was reached. Expected: COBOLNET1757
      *> (statement-operand-rule) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1029NSA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(10).
       PROCEDURE DIVISION.
           STRING ALL "AB" DELIMITED BY SIZE INTO WS-A
           END-STRING
           DISPLAY WS-A
           STOP RUN.
