      *> kb/Work PB2117 (A6's instrument) -- the MOVE-heavy hot path.
      *> Each pass moves one value through every storage form a typed-
      *> native record carries: DISPLAY, packed, binary, numeric-edited,
      *> alphanumeric (whole, truncated, reference-modified), a group.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed): RB-CODE is I cut to four
      *> digits, so CHECKSUM is 200 cycles of 0..9999 = 200 * 49995000 =
      *> 9999000000; the last NUM-EDIT is "   2,000,000", left-justified
      *> and truncated into ALPHA-B (31:10) as "   2,000,0".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MOVEHOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 2000000.
       01 I            PIC 9(9) COMP VALUE 0.
       01 NUM-DISP     PIC S9(9) VALUE 0.
       01 NUM-PACK     PIC S9(9) COMP-3 VALUE 0.
       01 NUM-BIN      PIC S9(9) COMP VALUE 0.
       01 NUM-EDIT     PIC -ZZZ,ZZZ,ZZ9.
       01 ALPHA-A      PIC X(40)
                       VALUE "THE QUICK BROWN FOX JUMPS OVER THE LAZY".
       01 ALPHA-B      PIC X(40).
       01 ALPHA-SHORT  PIC X(10).
       01 REC-A.
          05 RA-NAME   PIC X(20) VALUE "ALPHA".
          05 RA-AMT    PIC S9(7)V99 VALUE 0.
          05 RA-CODE   PIC 9(4) VALUE 0.
       01 REC-B.
          05 RB-NAME   PIC X(20).
          05 RB-AMT    PIC S9(7)V99.
          05 RB-CODE   PIC 9(4).
       01 CHECKSUM     PIC 9(18) COMP VALUE 0.
       01 CHECKSUM-OUT PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LOOP-COUNT
               MOVE I TO NUM-DISP
               MOVE NUM-DISP TO NUM-PACK
               MOVE NUM-PACK TO NUM-BIN
               MOVE NUM-BIN TO NUM-EDIT
               MOVE ALPHA-A TO ALPHA-B
               MOVE ALPHA-B TO ALPHA-SHORT
               MOVE NUM-EDIT TO ALPHA-B (31:10)
               MOVE ALPHA-SHORT TO RA-NAME
               MOVE NUM-PACK TO RA-AMT
               MOVE NUM-BIN TO RA-CODE
               MOVE REC-A TO REC-B
               ADD RB-CODE TO CHECKSUM
           END-PERFORM
           MOVE CHECKSUM TO CHECKSUM-OUT
           DISPLAY "MOVEHOT " CHECKSUM-OUT " [" ALPHA-B (27:14) "]"
           STOP RUN.
