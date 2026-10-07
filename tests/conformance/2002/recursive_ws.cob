      *> ISO 8.6.6 (:8819 a recursive program's internal data is initialized per 14.6.2; :8821
      *> functions are ALWAYS recursive) + 13.5.4 GR1 (the WS of a non-initial program or a function
      *> is STATIC data) + 14.6.2.3.3 (static data is in LAST-USED state: ONE copy shared across all
      *> concurrent and successive activations) + 13.6.4 GR1 / 14.6.2.3.2 (LOCAL-STORAGE is AUTOMATIC
      *> data: a fresh copy in INITIAL state on EVERY activation) + 14.9.5 GR3 (after CANCEL the next
      *> activation finds the initial state - including the static WS).
      *> Depth-3 self-recursion: WS-CTR accumulates ACROSS activations (D1 shows WS=03; the outer
      *> depths still see 03 after return - one shared copy), while LS-V re-initializes AT EACH DEPTH
      *> (D3/D2/D1 show 5+depth, never an accumulated value) and each activation KEEPS ITS OWN LS copy
      *> across the nested call (R-lines). CANCEL then re-calls: WS back to initial (D1 WS=01).
      *> The UDF twin: FCTR-P10RW's WS call-counter accumulates across three invocations (1, 2, 13).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FCTR-P10RW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CNT PIC 9(4) VALUE 0.
       LINKAGE SECTION.
       01 L-A PIC 9(2).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A RETURNING L-R.
       P.
           ADD 1 TO WS-CNT.
           COMPUTE L-R = WS-CNT + L-A.
           GOBACK.
       END FUNCTION FCTR-P10RW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWMAIN-P10RW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION FCTR-P10RW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC 9(1).
       01 WS-F PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO WS-D.
           CALL "RWREC-P10RW" USING WS-D.
           CANCEL "RWREC-P10RW".
           MOVE 1 TO WS-D.
           CALL "RWREC-P10RW" USING WS-D.
           COMPUTE WS-F = FUNCTION FCTR-P10RW(0).
           DISPLAY "U1=" WS-F.
           COMPUTE WS-F = FUNCTION FCTR-P10RW(0).
           DISPLAY "U2=" WS-F.
           COMPUTE WS-F = FUNCTION FCTR-P10RW(10).
           DISPLAY "U3=" WS-F.
           STOP RUN.
       END PROGRAM RWMAIN-P10RW.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWREC-P10RW RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CTR PIC 9(2) VALUE 0.
       LOCAL-STORAGE SECTION.
       01 LS-V PIC 9(2) VALUE 5.
       01 LS-N PIC 9(1).
       LINKAGE SECTION.
       01 L-D PIC 9(1).
       PROCEDURE DIVISION USING L-D.
       P.
           ADD 1 TO WS-CTR.
           ADD L-D TO LS-V.
           DISPLAY "D" L-D " WS=" WS-CTR " LS=" LS-V.
           IF L-D > 1
               COMPUTE LS-N = L-D - 1
               CALL "RWREC-P10RW" USING LS-N
           END-IF.
           DISPLAY "R" L-D " WS=" WS-CTR " LS=" LS-V.
           GOBACK.
       END PROGRAM RWREC-P10RW.

