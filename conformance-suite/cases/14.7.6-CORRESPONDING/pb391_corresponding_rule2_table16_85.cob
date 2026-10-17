      *>--- conformance-case
      *> id: pb391_corresponding_rule2_table16_85
      *> rules: [GR-14.7.6-2, SR-14.9.25.3-13, GR-14.9.25.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB391. ISO 1989:2023 14.7.6 rule 2: "In a MOVE statement, at least one of the data items is
      *> an elementary data item and the resulting move is valid according to the rules for the MOVE
      *> statement." The MOVE rules' category half is 14.9.25.3 SR10, table 16, and a pair whose move is
      *> INVALID simply DOES NOT CORRESPOND - a silent non-selection, never a diagnostic - so the receiving
      *> item keeps the content it had before the statement.
      *> Every cell below is read from table 16 AS PRINTED (specs/ISO_COBOL.md, 14.9.25.3):
      *>   Numeric/Integer      -> Alphabetic                        = No   NI2 keeps ZZZZZ
      *>   Alphabetic           -> Numeric                           = No   AL2 keeps 77777
      *>   Numeric/Noninteger   -> Alphanumeric                      = No   NN2 keeps PPPPP
      *>   Alphanumeric-edited  -> Numeric                           = No   AE2 keeps 88888
      *>   Alphanumeric         -> Alphanumeric-edited, Alphanumeric = Yes  OK2 becomes MOVE
      *>   Numeric/Integer      -> Numeric, Numeric-edited           = Yes  NU2 becomes 00042
      *> Rule 2 and the whole Format-2 surface are COBOL-85, and every category here is COBOL-85, so this is
      *> the rule at its introducing edition. The two Yes cells are the controls: without them a filter that
      *> refused EVERYTHING would print the same first four lines.
      *> Before PB391 the binder answered rule 2 from a SECOND, private, partial copy of table 16 instead of
      *> asking MoveTable16 - the copy had no alphabetic row or column at all - and the first four lines read
      *> NI2=[12345], AL2=[00000], NN2=[12.34], AE2=[00000].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB391CORRT16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 NI PIC 9(5) VALUE 12345.
          05 AL PIC A(5) VALUE "ABCDE".
          05 NN PIC 9(2)V99 VALUE 12.34.
          05 AE PIC XXBXX.
          05 OK PIC X(4) VALUE "MOVE".
          05 NU PIC 9(5) VALUE 42.
       01 G2.
          05 NI PIC A(5) VALUE "ZZZZZ".
          05 AL PIC 9(5) VALUE 77777.
          05 NN PIC X(5) VALUE "PPPPP".
          05 AE PIC 9(5) VALUE 88888.
          05 OK PIC X(4) VALUE "----".
          05 NU PIC 9(5) VALUE 99999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO AE OF G1.
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY "NI2=[" NI OF G2 "]".
           DISPLAY "AL2=[" AL OF G2 "]".
           DISPLAY "NN2=[" NN OF G2 "]".
           DISPLAY "AE2=[" AE OF G2 "]".
           DISPLAY "OK2=[" OK OF G2 "]".
           DISPLAY "NU2=[" NU OF G2 "]".
           STOP RUN.
