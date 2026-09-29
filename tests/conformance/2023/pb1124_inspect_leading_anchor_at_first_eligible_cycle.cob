      *> PB1124 - ISO 14.9.22.4 GR12 b) / GR17 c): the first LEADING
      *>   occurrence must be "at the point where comparison began in the
      *>   first comparison cycle in which literal-1 was eligible to
      *>   participate" - the FIRST CYCLE in the operand's region, whichever
      *>   operand that cycle matches.
      *> cite.py --check 14.9.22.4 "If the LEADING phrase is specified"
      *>   -> OK  14.9.22.4 12)
      *> Derivation (each string; ALL 'X' / ALL 'A' is an EARLIER operand):
      *>   T1 XAAB: cycle 1 (position 1) matches ALL 'X', so LEADING 'A'
      *>      had its first eligible cycle there and 'A' is not at that
      *>      point: NX=01 NA=00.
      *>   T2 REPLACING ALL 'X' BY 'Y' LEADING 'A' BY 'Z' on XAAB: the same
      *>      cycle, so LEADING replaces nothing: YAAB.
      *>   T3 ABBC: ALL 'A' takes position 1, LEADING 'B' had its first
      *>      cycle there: C1=01 C2=00. T4 REPLACING: aBBC.
      *>   T5 AAXA LEADING 'A': the run AA is leading, the last A is not: 02.
      *>   T6 XAAB LEADING 'A' AFTER 'X': the region starts at position 2, so
      *>      the first eligible cycle is there and the run AA counts: 02.
      *>   T7 AAXA REPLACING LEADING 'A' BY 'Z': ZZXA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1124.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(4).
       01 NX PIC 99.
       01 NA PIC 99.
       PROCEDURE DIVISION.
           MOVE 0 TO NX NA
           MOVE "XAAB" TO S1
           INSPECT S1 TALLYING NX FOR ALL "X" NA FOR LEADING "A"
           DISPLAY "T1 " NX " " NA
           MOVE "XAAB" TO S1
           INSPECT S1 REPLACING ALL "X" BY "Y" LEADING "A" BY "Z"
           DISPLAY "T2 " S1
           MOVE 0 TO NX NA
           MOVE "ABBC" TO S1
           INSPECT S1 TALLYING NX FOR ALL "A" NA FOR LEADING "B"
           DISPLAY "T3 " NX " " NA
           MOVE "ABBC" TO S1
           INSPECT S1 REPLACING ALL "A" BY "a" LEADING "B" BY "b"
           DISPLAY "T4 " S1
           MOVE 0 TO NA
           MOVE "AAXA" TO S1
           INSPECT S1 TALLYING NA FOR LEADING "A"
           DISPLAY "T5 " NA
           MOVE 0 TO NA
           MOVE "XAAB" TO S1
           INSPECT S1 TALLYING NA FOR LEADING "A" AFTER INITIAL "X"
           DISPLAY "T6 " NA
           MOVE "AAXA" TO S1
           INSPECT S1 REPLACING LEADING "A" BY "Z"
           DISPLAY "T7 " S1
           STOP RUN.
