      *> reject-at: 2023
      *> kb/Work PB812 - ROLLBACK written in an INVERTED sort input procedure.
      *>   cite.py --check 14.9.36.3 "This statement shall not be specified in the input or
      *>     output procedure of a MERGE or SORT statement." -> OK §14.9.36.3 2)
      *> INPUT PROCEDURE IS IN-B THRU IN-A names an inverted range: IN-A stands before IN-B,
      *> which §14.9.28.4 GR6 permits. The ROLLBACK in IN-A is written in the input
      *> procedure, so SR2 refuses it, AT the ROLLBACK statement (the diagnostic used to carry
      *> no source position). A numeric Start..End interval over this pair is empty, which
      *> is how the statement once escaped the ban; PcRange.Spans is the lexical test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB812RB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SWK ASSIGN TO "PB812RB.tmp".
           SELECT F3 ASSIGN TO "PB812RB3.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SWK.
       01 SW-REC.
          05 SW-KEY PIC 9(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SWK ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-B THRU IN-A
               GIVING F3
           STOP RUN.
       IN-A.
           ROLLBACK.
       IN-B.
           MOVE 1 TO SW-KEY
           RELEASE SW-REC.
