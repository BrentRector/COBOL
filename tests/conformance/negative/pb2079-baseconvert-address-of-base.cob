      *> reject-at: 2023
      *> kb/Work PB2079 - ISO/IEC 1989:2023 15.12.3 r1: argument-3 shall be a numeric integer literal or data
      *> item. ADDRESS OF X creates a unique data item of class pointer (8.4.3.11.4 GR1), not numeric, so it is
      *> refused as a base (COBOLNET1627). It used to compile and abort at run time with NotImplemented.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079ADR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 R PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" 16 ADDRESS OF X) TO R
           DISPLAY "R=" R
           STOP RUN.
