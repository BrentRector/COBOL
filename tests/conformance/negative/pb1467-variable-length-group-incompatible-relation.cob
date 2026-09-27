      *> reject-at: 2014 2023
      *> ISO 8.5.1.12.1 - "a variable-length group is not equivalent to an alphanumeric data item and may not
      *> undergo a comparison or a move operation, in either direction, explicitly or otherwise, unless the other
      *> operand is a compatible group." X1 is an elementary item, so the relation is refused (COBOLNET2492); before
      *> kb/Work PB1467 it compiled clean and aborted the run unit on a whole-group image G1 does not have. (A
      *> variable-length group is declarable only from COBOL-2014 - DYNAMIC LENGTH, 13.18.19.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1467-VLG-BAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1A PIC X(2) VALUE "AB".
          05 G1D PIC X DYNAMIC LENGTH.
       01 X1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF G1 = X1 DISPLAY "Y" ELSE DISPLAY "N" END-IF
           STOP RUN.
