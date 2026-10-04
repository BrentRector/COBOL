      *> kb/Work PB1294 - ISO 13.18.54: a SUM clause's data-name-1 names an entry of the REPORT SECTION (a rolled
      *> total). Before this it was refused (COBOLNET0899 report-sum-rolled-total) and 13.18.53.4 GR3's exception
      *> for an unprintable SOURCE entry was unreachable.
      *> 13.18.54.3 SR4: "Data-name-1 shall be the name of a numeric data item in the report section."
      *>   cite.py: OK  13.18.54.3 4)  (Syntax rules)
      *> 13.18.54.4 GR6: "If data-name-1 specifies an item whose entry contains a SUM clause, the value added is that
      *>   of the corresponding sum counter. ... If data-name-1 specifies an item whose entry has a SOURCE or VALUE
      *>   clause, the value added is that of the operand of the SOURCE or VALUE clause."
      *>   cite.py: OK  13.18.54.4 6)  (General rules)
      *> GR7 a): "If data-name-1 is the name of an entry in a different report group description, adding takes place
      *>   when the report group description containing data-name-1 is processed."
      *>   cite.py: OK  13.18.54.4 7) a)  (General rules)
      *> GR11: "If the operand is data-name-1 and is declared to be absent as a result of a PRESENT WHEN clause ...
      *>   data-name-1 is not added into the sum counter during the processing of that instance of the report group
      *>   in which data-name-1 is defined."
      *>   cite.py: OK  13.18.54.4 11)  (General rules)
      *> 13.18.53.4 GR3: "If the entry containing the SOURCE clause contains no COLUMN clause and therefore defines an
      *>   unprintable item, the SOURCE clause causes no action, except where the entry is referred to by means of a
      *>   SUM clause."
      *>   cite.py: OK  13.18.53.4 3)  (General rules)
      *> 13.18.54.4 GR2: a counter is "reset to zero ... at the end of the processing of the report group in which it
      *>   is printed".   cite.py: OK  13.18.54.4 2)  (General rules)
      *> 14.9.16.4 GR5: a GENERATE after the first "causes the following actions to take place in order: a) ... each
      *>   control footing and control heading is printed ... up to the level of the control break. b) The specified
      *>   detail is printed".   cite.py: OK  14.9.16.4 5)  (General rules)
      *>
      *> DERIVATION. CONTROLS ARE FINAL WS-C (FINAL major, WS-C minor). Records (WS-C, WS-AMT): (A,4) (A,8) (B,2) (B,9).
      *> The detail DET has: DA (printable, SOURCE WS-AMT), DU (UNPRINTABLE, SOURCE WS-AMT), DV (printable, VALUE 3) and
      *> DP (printable, SOURCE WS-AMT, PRESENT WHEN WS-AMT > 5). CF1 (WS-C) sums DA, DU, DV, DP into T1..T4; the FINAL
      *> footing sums T1 (a SUM entry of a LOWER level footing: SR4 f) permits it) and DA (an entry of a detail).
      *> Each DET processing adds its entries' values to the counters that name them (GR7 a)): A: DA 4+8=12, DU 12, DV
      *> 3+3=6, DP 8 (the record 4 is not > 5, so DP is absent and adds nothing: GR11). The break at the first (B,2)
      *> record processes CF1 BEFORE the detail (14.9.16.4 GR5): it prints 012 012 006 008 at columns 1, 5, 9, 13 and,
      *> being processed, adds T1 = 12 into G1 (GR7 a)); its counters reset at its end (GR2). B: DA 2+9=11, DU 11,
      *> DV 6, DP 9 -> CF1 prints 011 011 006 009 at TERMINATE and adds 11 to G1 = 23. G2 sums DA over all four details:
      *> 4+8+2+9 = 23. The FINAL footing prints G1 and G2 at columns 1 and 6: 0023 0023.
      *> T5 (column 17) writes TWO addends, `SUM DA WS-AMT`: DA is data-name-1 (a rolled total, added when DET is
      *> processed) and WS-AMT is identifier-1 (added on every GENERATE, GR7 c) 1.). GR9: "If the SUM clause specifies
      *> more than one addend, the result is the same as when all the addends were summed separately according to the
      *> above rules and the results added together." Each record therefore adds its WS-AMT twice: A: 2 x (4+8) = 024,
      *> B: 2 x (2+9) = 022.   cite.py: OK  13.18.54.4 9)  (General rules)
      *> Lines (unpaged, each group LINE PLUS 1): 1 DET(A,4) `04  3`; 2 DET(A,8) `08  3  08`; 3 CF1(A) `012 012 006 008
      *> 024`; 4 DET(B,2) `02  3`; 5 DET(B,9) `09  3  09`; 6 CF1(B) `011 011 006 009 022`; 7 FINAL `0023 0023`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1294ROLL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1294ROLL.TXT".
           SELECT CHK ASSIGN TO "PB1294ROLL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-C    PIC X     VALUE SPACE.
       01  WS-AMT  PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROLS ARE FINAL WS-C.
       01  DET TYPE DE LINE PLUS 1.
           05  DA COLUMN 1 PIC 99 SOURCE WS-AMT.
           05  DU PIC 99 SOURCE WS-AMT.
           05  DV COLUMN 5 PIC 9 VALUE 3.
           05  DP COLUMN 8 PIC 99 SOURCE WS-AMT PRESENT WHEN WS-AMT > 5.
       01  CF1 TYPE CF WS-C LINE PLUS 1.
           05  T1 COLUMN 1 PIC 999 SUM DA.
           05  T2 COLUMN 5 PIC 999 SUM DU.
           05  T3 COLUMN 9 PIC 999 SUM DV.
           05  T4 COLUMN 13 PIC 999 SUM DP.
           05  T5 COLUMN 17 PIC 999 SUM DA WS-AMT.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           05  G1 COLUMN 1 PIC 9999 SUM T1.
           05  G2 COLUMN 6 PIC 9999 SUM DA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE "A" TO WS-C.
           MOVE 4 TO WS-AMT.
           GENERATE DET.
           MOVE 8 TO WS-AMT.
           GENERATE DET.
           MOVE "B" TO WS-C.
           MOVE 2 TO WS-AMT.
           GENERATE DET.
           MOVE 9 TO WS-AMT.
           GENERATE DET.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:20) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
