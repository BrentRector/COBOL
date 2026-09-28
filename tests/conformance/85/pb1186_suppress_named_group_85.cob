      *> kb/Work PB1186 - SUPPRESS inhibits only the report group its
      *> USE procedure names, and only that group's CURRENT instance.
      *>
      *> "The SUPPRESS statement inhibits printing only for the report
      *> group named in the USE procedure within which the SUPPRESS
      *> statement appears."
      *>   cite.py: OK  §14.9.45.4 1)  (General rules)
      *> "The effect of the SUPPRESS statement is limited to the current
      *> instance of the report group."
      *>   cite.py: OK  §14.9.45.4 2)  (General rules)
      *> "Procedure-names within a declarative section may be referenced
      *> in a different declarative section or in a nondeclarative
      *> procedure only with a PERFORM statement."
      *>   cite.py: OK  §14.9.49.3 4)  (Syntax rules)
      *>
      *> DERIVATION. SUP-B's SUPPRESS names DET-B (its USE procedure).
      *> WS-N counts GENERATEs, so each printed line names its instance.
      *>  1. SUP-B is PERFORMed from the MAIN program with WS-SUP = "Y":
      *>     no instance of DET-B is current, so the SUPPRESS has no
      *>     effect - on anything. GENERATE DET-A (1), DET-B (2) with
      *>     WS-SUP = "N": both print.        -> "A1", "B2"
      *>  2. WS-SUP = "Y". GENERATE DET-A (3): DET-A's own declarative
      *>     PERFORMs SUP-B, whose SUPPRESS names DET-B - not the group
      *>     being produced - so DET-A prints.            -> "A3"
      *>     GENERATE DET-B (4): DET-B's own declarative executes the
      *>     SUPPRESS during DET-B's instance: suppressed.    -> nothing
      *>  3. WS-SUP = "N". GENERATE DET-B (5): a NEW instance; the
      *>     earlier SUPPRESS does not carry over (GR2).      -> "B5"
      *> Fails if a SUPPRESS reached outside its group's own hook
      *> suppresses the next group presented (the defect: "A1", "A3" and
      *> "B5" were each lost) or if GR1/GR2 are not honoured at all
      *> ("B4" appears).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1186S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1186s.txt".
           SELECT CHK ASSIGN TO "pb1186s.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-SUP  PIC X     VALUE "N".
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R-1.
       01  DET-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "A".
           02  COLUMN 2 PIC 9 SOURCE WS-N.
       01  DET-B TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "B".
           02  COLUMN 2 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP-A SECTION.
           USE BEFORE REPORTING DET-A.
       SUP-A-P.
           PERFORM SUP-B.
       SUP-B SECTION.
           USE BEFORE REPORTING DET-B.
       SUP-B-P.
           IF WS-SUP = "Y"
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE "Y" TO WS-SUP.
           PERFORM SUP-B.
           MOVE "N" TO WS-SUP.
           ADD 1 TO WS-N.
           GENERATE DET-A.
           ADD 1 TO WS-N.
           GENERATE DET-B.
           MOVE "Y" TO WS-SUP.
           ADD 1 TO WS-N.
           GENERATE DET-A.
           ADD 1 TO WS-N.
           GENERATE DET-B.
           MOVE "N" TO WS-SUP.
           ADD 1 TO WS-N.
           GENERATE DET-B.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           DISPLAY "[" WS-LINE(1:2) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
