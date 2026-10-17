      *>--- conformance-case
      *> id: pb421_move_general_formats
      *> rules: [FMT-14.9.25.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB421 — EVERY SPELLING §14.9.25.2's TWO GENERAL FORMATS ADMIT. `moveReceivingPhrase` carried a
      *> second alternative that composed with `MOVE moveSendingOperand moveReceivingPhrase` to admit
      *> `MOVE <sending-operand> CORRESPONDING id-3 TO id-4`, a shape NEITHER format prints; deleting it could
      *> have taken a legal spelling with it, and this is that guard. The printed formats (PDF page 694):
      *>   Format 1:  MOVE { identifier-1 | literal-1 } TO { identifier-2 } ...
      *>   Format 2:  MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4
      *> Exercised: a data item and a literal in the sending position; a function-identifier there (8.4.3.1.2
      *> Format 1 makes a function-identifier an identifier); the Format-1 receiving list with and without a
      *> comma separator; and BOTH spellings of the Format-2 keyword, which 14.9.25.3 rule 11 makes equivalent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB421FMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC X(4) VALUE "ABCD".
       01 B  PIC X(4).
       01 C  PIC X(4).
       01 D  PIC X(4).
       01 E  PIC X(4).
       01 G1.
          05 F1 PIC X(3) VALUE "XYZ".
          05 F2 PIC X(3) VALUE "PQR".
       01 G2.
          05 F1 PIC X(3).
          05 F9 PIC X(3).
       01 G3.
          05 F2 PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE A TO B C.
           MOVE "WXYZ" TO D, E.
           MOVE FUNCTION UPPER-CASE("ab") TO C.
           MOVE CORRESPONDING G1 TO G2.
           MOVE CORR G1 TO G3.
           DISPLAY "B=" B.
           DISPLAY "C=" C.
           DISPLAY "D=" D.
           DISPLAY "E=" E.
           DISPLAY "G2F1=" F1 OF G2 " G2F9=" F9 OF G2.
           DISPLAY "G3F2=" F2 OF G3.
           STOP RUN.
