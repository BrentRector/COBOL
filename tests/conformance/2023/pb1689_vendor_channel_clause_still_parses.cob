      *> PB1689 control - un-reserving CHANNEL must not break the SPECIAL-NAMES CHANNEL n IS clause,
      *>   and a SORT ending in a GIVING file-name is ended by the next statement (it has no explicit
      *>   scope terminator: ISO 14.5.1 Table 12; `SORT ... END-SORT` is the PB758 negative
      *>   pb758-sort-end-sort-is-not-a-terminator).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CHANNEL 1 IS TOP-OF-CH.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "vtsf.tmp".
           SELECT IN1 ASSIGN TO "vtin.tmp".
           SELECT OUT1 ASSIGN TO "vtout.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SF-REC PIC X(2).
       FD  IN1.
       01  IN1-REC PIC X(2).
       FD  OUT1.
       01  OUT1-REC PIC X(2).
       PROCEDURE DIVISION.
           OPEN OUTPUT IN1
           MOVE "B1" TO IN1-REC WRITE IN1-REC
           MOVE "A1" TO IN1-REC WRITE IN1-REC
           CLOSE IN1
           SORT SF ON ASCENDING KEY SF-REC USING IN1 GIVING OUT1
           DISPLAY "SORTED"
           STOP RUN.
