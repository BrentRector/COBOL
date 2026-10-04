      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1261 - ISO 13.18.38.3 SR20: data-name-1 "shall not occupy a byte position within
      *> the range of the first byte position defined by the data description entry containing the
      *> OCCURS clause and the last byte position defined by the record description entry". ISO
      *> 13.18.33.4 GR3 makes R1 and R2 implicit redefinitions of ONE area, so N (byte 1 of R2) is
      *> T's first byte. The same-record case was already refused; this is the other-record arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1261N20.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1261N20.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1.
           05 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       01 R2.
           05 N PIC 9.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
