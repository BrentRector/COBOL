      *> kb/Work PB2117 (A6's instrument) -- PERFORM dispatch.
      *> Out-of-line PERFORM of a paragraph that PERFORMs another, a
      *> PERFORM ... THRU range left early by a GO TO, and a SECTION:
      *> the procedure-dispatch cost (the generated program's paragraph
      *> dispatcher) with almost no arithmetic in the bodies.
      *> The same source runs under GnuCOBOL for the external comparison
      *> (scripts/arch/perf_baseline.py), so it is plain COBOL 85.
      *> WITNESS (computed, not observed), N = LOOP-COUNT = 5000000:
      *> COUNT-A = N, COUNT-B = N (the GO TO skips the ADD 1000),
      *> COUNT-C = 2N, COUNT-D = 3N, COUNT-E = N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFHOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOOP-COUNT   PIC 9(9) COMP VALUE 5000000.
       01 I            PIC 9(9) COMP VALUE 0.
       01 COUNT-A      PIC 9(9) COMP VALUE 0.
       01 COUNT-B      PIC 9(9) COMP VALUE 0.
       01 COUNT-C      PIC 9(9) COMP VALUE 0.
       01 COUNT-D      PIC 9(9) COMP VALUE 0.
       01 COUNT-E      PIC 9(9) COMP VALUE 0.
       01 OUT-A        PIC 9(9).
       01 OUT-B        PIC 9(9).
       01 OUT-C        PIC 9(9).
       01 OUT-D        PIC 9(9).
       01 OUT-E        PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LOOP-COUNT
               PERFORM STEP-A
               PERFORM STEP-B THRU STEP-C
               PERFORM COUNT-SECTION
           END-PERFORM
           MOVE COUNT-A TO OUT-A
           MOVE COUNT-B TO OUT-B
           MOVE COUNT-C TO OUT-C
           MOVE COUNT-D TO OUT-D
           MOVE COUNT-E TO OUT-E
           DISPLAY "PERFHOT " OUT-A " " OUT-B " " OUT-C " " OUT-D
               " " OUT-E
           STOP RUN.
       STEP-A.
           ADD 1 TO COUNT-A
           PERFORM STEP-D.
       STEP-B.
           ADD 1 TO COUNT-B
           IF COUNT-B > 0
               GO TO STEP-C
           END-IF
           ADD 1000 TO COUNT-B.
       STEP-C.
           ADD 2 TO COUNT-C.
       STEP-D.
           ADD 3 TO COUNT-D.
       COUNT-SECTION SECTION.
       COUNT-PARA.
           ADD 1 TO COUNT-E.
