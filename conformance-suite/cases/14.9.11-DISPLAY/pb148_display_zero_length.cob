      *>--- conformance-case
      *> id: pb148_display_zero_length
      *> rules: [GR-14.9.11.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>REF-MOD-ZERO-LENGTH ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148ZL.
      *> kb/Work PB148 - ISO 14.9.11.4 GR1: "If an operand is a
      *> zero-length data item or a zero-length literal, no data is
      *> transferred for that operand." Three shapes between the
      *> brackets: the zero-length literal, a zero-occurrence ODO group,
      *> and a zero-length reference-modification slice (legal under
      *> >>REF-MOD-ZERO-LENGTH ON, 8.4.3.3.3).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 G.
          05 T OCCURS 0 TO 3 TIMES DEPENDING ON N PIC X.
       01 WS-A PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "[" "" G WS-A(1:0) "]"
           STOP RUN.
