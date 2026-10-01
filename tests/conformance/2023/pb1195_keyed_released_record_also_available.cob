      *> ISO 1989:2023 §14.9.51.4 GR4 (WRITE) and §14.9.35.4 GR6 (REWRITE) on the KEYED organizations - the twin of
      *> pb1195_released_record_also_available_out_of_line, which pins the sequential and sort arms.
      *> The released logical record "is also available as a record of other file-names referenced in the same SAME
      *> RECORD AREA clause ..., as well as the file associated with record-name-1" (§14.9.35.4 GR6; §14.9.51.4 GR4).
      *> K1's record is a DYNAMIC LENGTH elementary item (OUT OF LINE, docs/CONFORMANCE.md D-FRA: no window over the
      *> shared character area); K2's is a character record. The area is carried across by a store at each statement.
      *> The contents of the area BEYOND the released record are not defined by these rules, so only the released
      *> bytes are displayed.
      *>   KW1  - WRITE K1-DYN: also available as K2-REC.
      *>   KW2  - WRITE K2-REC: also available as K1-DYN, at the record's own length.
      *>   KRW1 - REWRITE K1-DYN: also available as K2-REC.
      *>   KRW2 - REWRITE K2-REC: also available as K1-DYN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1195KY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT K1 ASSIGN TO "pb1195k1.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK1.
           SELECT K2 ASSIGN TO "pb1195k2.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK2.
       I-O-CONTROL.
           SAME RECORD AREA FOR K1 K2.
       DATA DIVISION.
       FILE SECTION.
       FD  K1.
       01  K1-DYN PIC X DYNAMIC LENGTH.
       FD  K2.
       01  K2-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  RK1 PIC 9.
       01  RK2 PIC 9.
       01  WS-LEN PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT K1.
           MOVE 1 TO RK1.
           MOVE "HELLO" TO K1-DYN.
           WRITE K1-DYN.
           DISPLAY "KW1 " K2-REC (1:5).
           CLOSE K1.
           OPEN OUTPUT K2.
           MOVE 1 TO RK2.
           MOVE "WORLD123" TO K2-REC.
           WRITE K2-REC.
           MOVE FUNCTION LENGTH(K1-DYN) TO WS-LEN.
           DISPLAY "KW2 [" K1-DYN "] " WS-LEN.
           CLOSE K2.
           OPEN I-O K1.
           MOVE 1 TO RK1.
           MOVE "BBBBB" TO K1-DYN.
           REWRITE K1-DYN.
           DISPLAY "KRW1 " K2-REC (1:5).
           OPEN I-O K2.
           MOVE 1 TO RK2.
           MOVE "NEWVALUE" TO K2-REC.
           REWRITE K2-REC.
           DISPLAY "KRW2 [" K1-DYN "]".
           CLOSE K2.
           CLOSE K1.
           STOP RUN.
