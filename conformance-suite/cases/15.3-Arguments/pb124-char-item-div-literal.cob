*>--- conformance-case
*> id: pb124-char-item-div-literal
*> rules: [AR-15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 type 6: "An arithmetic expression that will always result in an integer value or an
      *> integer data item shall be specified." I / 2 over an item is provably NOT always-integral (I = 3
      *> gives 1.5 - the witness the screen's soundness rests on); the old screen failed open on every
      *> computed operand (kb/Work PB124, AR-15.3-6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 6.
       01 S PIC 9V9 VALUE 1.5.
       01 RS PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR(I / 2) TO RS
           STOP RUN.
