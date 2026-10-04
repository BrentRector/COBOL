      *> kb/Work PB1262 - A FORMAT 1 (FIXED) TABLE AND A PLAIN GROUP STAY LEGAL UNDER A CONSTANT RECORD, next to the
      *> refusals pb1262-constant-record-* (13.18.38.3 SR19, SR23, SR33 bar the DEPENDING and DYNAMIC formats only).
      *>   cite.py --check 13.18.38.3 "A format 2 OCCURS clause shall not be specified in any data item subordinate to a data item described with the CONSTANT RECORD clause" -> OK 19)
      *> Computed from the standard: 13.18.63.4 sets every occurrence of a table described with VALUE to that literal, so
      *> T(1) T(2) T(3) hold A and U holds BC; a CONSTANT RECORD reads as one alphanumeric group, "AAABC".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C CONSTANT RECORD.
          05 T PIC X OCCURS 3 VALUE "A".
          05 G.
             10 U PIC X(2) VALUE "BC".
       PROCEDURE DIVISION.
           DISPLAY C
           STOP RUN.
