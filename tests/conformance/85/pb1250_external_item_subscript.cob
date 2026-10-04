      *> ISO 8.4.2.3.4 GR1 b): "If arithmetic-expression-1 is specified, the subscript is the
      *>   result of the evaluation of arithmetic-expression-1."  8.8.1.1: an arithmetic
      *>   expression "may be an identifier referencing a numeric data item".
      *>   cite.py --check 8.4.2.3.4 -> OK 8.4.2.3.4 1) b); 8.8.1.1 -> OK
      *> An EXTERNAL data item (13.18.22) is a numeric data item like any other, so it is a legal
      *>   subscript. The compiler emitted its bare C# name (the item owns no field: its storage is
      *>   a run-unit cell) and the backend refused the program with CS0103 (found while repairing
      *>   the function goldens for kb/Work PB1250).
      *> DERIVATION: WS-I = 2, WS-E(1) = 7, WS-E(2) = 9, so WS-E (WS-I) is WS-E(2) => 0009;
      *>   adding 1 to the EXTERNAL subscript selects WS-E(3) = 11                    => 0011
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1250X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-I    PIC 9(4) EXTERNAL.
       01  WS-T.
           05  WS-E PIC 9(4) OCCURS 3.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 7 TO WS-E (1).
           MOVE 9 TO WS-E (2).
           MOVE 11 TO WS-E (3).
           MOVE 2 TO WS-I.
           DISPLAY WS-E (WS-I).
           ADD 1 TO WS-I.
           DISPLAY WS-E (WS-I).
           STOP RUN.
