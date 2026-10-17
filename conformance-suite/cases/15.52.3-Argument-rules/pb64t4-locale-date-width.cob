*>--- conformance-case
*> id: pb64t4-locale-date-width
*> rules: [AR-15.52.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.52.3 r1: "Argument-1 shall be of class alphanumeric or national and shall be 8 character positions in
      *> length." A 7-position item draws the §15.3 argument-rule diagnostic (COBOLNET1627, the width half of the screen);
      *> kb/Work PB64 T4 - LOCALE-DATE is live (it was refused by name with COBOLNET1518 before T4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T4W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D7 PIC X(7) VALUE "2026081".
       01 S PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION LOCALE-DATE(D7) TO S.
           STOP RUN.
