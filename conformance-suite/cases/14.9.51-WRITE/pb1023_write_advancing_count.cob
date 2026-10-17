      *>--- conformance-case
      *> id: pb1023_write_advancing_count
      *> rules: [SR-14.9.51.3-14, SR-14.9.51.3-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1023 - the WRITE ... ADVANCING count, both of its
      *> alternatives at their admitted shapes. ISO 14.9.51.3 SR14:
      *> "Identifier-2 shall reference an integer data item." SR15:
      *> "Integer-1 shall be positive or zero." The screen that now
      *> refuses a PIC X or PIC 9V9 identifier-2 and a 1.5 / "2" / -1
      *> integer-1 (COBOLNET2365, negative/pb1023-write-advancing-count)
      *> must admit every integer USAGE and the zero count.
      *> Expected values: 13.18.34.4 GR7 - "the LINAGE-COUNTER is
      *> incremented by the value of the integer specified in the
      *> ADVANCING phrase or the contents of the data item referenced by
      *> the identifier specified in the ADVANCING phrase", and "set to
      *> one at the time an OPEN statement with the OUTPUT phrase is
      *> executed". So: OPEN -> 1; +3 (DISPLAY integer) -> 4; +0
      *> (integer-1 zero) -> 4; +2 (signed BINARY) -> 6; +1 (PACKED)
      *> -> 7. The page (LINAGE 20) is never exceeded.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1023AC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb1023ac.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 20 LINES.
       01 PR PIC X(5).
       WORKING-STORAGE SECTION.
       01 N-DISP PIC 9 VALUE 3.
       01 N-BIN PIC S9(4) COMP VALUE 2.
       01 N-PACK PIC 9(3) COMP-3 VALUE 1.
       01 LC PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTF.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "OPEN LC=" LC.
           MOVE "LINE1" TO PR.
           WRITE PR AFTER ADVANCING N-DISP LINES.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "DISPLAY-ITEM LC=" LC.
           WRITE PR AFTER ADVANCING 0 LINES.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "ZERO-LITERAL LC=" LC.
           WRITE PR BEFORE ADVANCING N-BIN LINES.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "BINARY-ITEM LC=" LC.
           WRITE PR AFTER ADVANCING N-PACK LINE.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "PACKED-ITEM LC=" LC.
           CLOSE PRTF.
           STOP RUN.
