      *> reject-at: 2002 2014 2023
      *> ISO §12.4.5.7.3 SR6 — a Format-2 (key-level) COLLATING SEQUENCE
      *> operand shall not be subscripted.
      *>   cite.py --check 12.4.5.7.3 "Data-name-1 and record-key-name-1
      *>     shall not be subscripted." -> OK 6)
      *> 'COLLATING SEQUENCE OF IX-KEY (1) IS REV' writes a subscript on
      *> data-name-1. Everything else about the entry is legal: the file
      *> is INDEXED, IX-KEY is its declared RECORD KEY data-name (SR4)
      *> and REV is an alphanumeric alphabet (SR7), so the subscript is
      *> the only ground for rejection. The §12.4.5.7.2 Format-2 operand
      *> is parsed as a data reference (a QUALIFIED data-name is legal,
      *> kb/Work PB1075), and the one data-name-n capture refuses the
      *> subscript by name: COBOLNET2024, which is what the .err holds
      *> (it was the parse error COBOL0312 while the grammar took a bare
      *> word).
      *> Reject-at names 2002 onward: the file-control COLLATING SEQUENCE
      *> clause is a COBOL-2002 introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1COLK06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "l1colk06.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               COLLATING SEQUENCE OF IX-KEY (1) IS REV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(1).
          05 IX-DATA PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           CLOSE IXF.
           STOP RUN.
