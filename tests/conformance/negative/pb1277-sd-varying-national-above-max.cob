      *> reject-at: 2002 2014 2023
      *> kb/Work PB1277 -- ISO 13.18.43.3 SR4 (FORMAT 2): "Record descriptions for the
      *> file shall describe neither records that contain a lesser number of bytes
      *> than that specified by integer-2 nor records that contain a greater number
      *> of bytes than that specified by integer-3", and for a sort-merge file
      *> description entry 13.4.6.4 GR1: "The number of characters is specified in
      *> terms of bytes". S-REC is PIC N(5) = 10 bytes (two per national position,
      *> determination D-N1), greater than integer-3 = 5. Until PB1277 the size was
      *> counted in character positions (5) and this program compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1277N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb1277n2.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S RECORD IS VARYING IN SIZE FROM 1 TO 5 CHARACTERS.
       01  S-REC PIC N(5).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
