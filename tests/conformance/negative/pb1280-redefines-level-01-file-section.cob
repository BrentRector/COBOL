      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1280 - SR3 - REDEFINES in a level-1 FILE SECTION entry.
      *>   cite.py --check 13.18.44.3 "This clause shall not be specified in level 1 entries in the file section" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1280.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC X(10).
       01 R2 REDEFINES R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01 W PIC X.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
