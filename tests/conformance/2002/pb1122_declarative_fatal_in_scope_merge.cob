      *> kb/Work PB1122 - §14.6.13.1.2 1): a declarative does not complete
      *> normally when "a fatal exception occurs within the scope of the
      *> declarative", and the MERGE whose implicit OPEN selected it is
      *> terminated (§14.9.24.4 GR12 a)).
      *>
      *> THE RULES.
      *> §14.6.13.1.2 1): "An EXIT PROGRAM, GOBACK, RESUME, or STOP
      *>   statement that is specified in this function, method, or
      *>   program is executed, or a fatal exception occurs within the
      *>   scope of the declarative."
      *>   OK  §14.6.13.1.2 1)  (Normal completion of a declarative procedure)
      *> §14.9.24.4 GR12 a): "If a fatal exception condition exists as a
      *>   result of this implicit OPEN statement and there is an
      *>   applicable USE procedure that completes normally, processing
      *>   for the file connector that caused the exception condition is
      *>   bypassed", and otherwise the MERGE statement is terminated.
      *>   OK  §14.9.24.4 12)  (General rules)
      *> §14.9.33.4 GR2 a) 2: RESUME AT NEXT STATEMENT in D-SUB returns
      *>   control to the implicit CONTINUE after the statement of
      *>   D-BADF that raised the condition.
      *>   OK  §14.9.33.4 2)  (General rules)
      *>
      *> DERIVATION. Every BAD-x file lives in a directory that does not
      *> exist, so the MERGE's implicit OPEN OUTPUT of it fails (status
      *> 30, fatal) and selects the Format 1 declarative naming it. The
      *> file after it, OUT-G, holds OLD before each leg, so whether the
      *> MERGE went on shows in OUT-G.
      *>   L1 (control): D-BADC falls off its end having raised nothing,
      *>      so it completes normally; BAD-C is bypassed and the MERGE
      *>      writes OUT-G: A B C D. Without this leg, a MERGE that
      *>      always stopped would pass the legs below.
      *>   L2: D-BADR executes RESUME AT NEXT STATEMENT, a RESUME was
      *>      executed, so it does not complete normally: the MERGE is
      *>      terminated and OUT-G still holds OLD.
      *>   L3: D-BADF falls off its end too, but on the way its checked
      *>      subscript WS-T (WS-N), WS-N = 9 over OCCURS 3, raises the
      *>      FATAL EC-BOUND-SUBSCRIPT within its scope (Table 13), which
      *>      D-SUB handles with RESUME AT NEXT STATEMENT. A fatal
      *>      exception occurred within D-BADF's scope, so it does NOT
      *>      complete normally and the MERGE is terminated exactly as in
      *>      L2: OUT-G still holds OLD.
      *>   L4: the declarative D-BADG is a GLOBAL one that PERFORMs the
      *>      local declarative D-RES (§14.9.49.3 SR4 allows a PERFORM
      *>      of another declarative section's procedure), whose RESUME
      *>      is executed within D-BADG's scope of execution and so is
      *>      a CONTINUE (§14.9.33.4 GR1: the statement after it, L4-USE-
      *>      RES-AFTER, runs). A RESUME was nevertheless EXECUTED, so
      *>      D-BADG does not complete normally (§14.6.13.1.2 1)) and the
      *>      MERGE is terminated: OUT-G still holds OLD.
      *>   OK  §14.9.33.4 1)  (General rules)
      *>   OK  §14.9.49.3 4)  (Syntax rules)
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1122MRG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRC-A ASSIGN TO "pb1122a.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SA-ST.
           SELECT SRC-B ASSIGN TO "pb1122b.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SB-ST.
           SELECT OUT-G ASSIGN TO "pb1122g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS OG-ST.
           SELECT BAD-C ASSIGN TO "pb1122-no-such-dir/c.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BC-ST.
           SELECT BAD-R ASSIGN TO "pb1122-no-such-dir/r.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BR-ST.
           SELECT BAD-F ASSIGN TO "pb1122-no-such-dir/f.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BF-ST.
           SELECT BAD-G ASSIGN TO "pb1122-no-such-dir/g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BG-ST.
           SELECT SRT-FILE ASSIGN TO "pb1122w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD SRC-A.
       01 SA-REC   PIC X(3).
       FD SRC-B.
       01 SB-REC   PIC X(3).
       FD OUT-G.
       01 OG-REC   PIC X(3).
       FD BAD-C.
       01 BC-REC   PIC X(3).
       FD BAD-R.
       01 BR-REC   PIC X(3).
       FD BAD-F.
       01 BF-REC   PIC X(3).
       FD BAD-G.
       01 BG-REC   PIC X(3).
       SD SRT-FILE.
       01 SRT-REC  PIC X(3).
       WORKING-STORAGE SECTION.
       01 SA-ST    PIC XX.
       01 SB-ST    PIC XX.
       01 OG-ST    PIC XX.
       01 BC-ST    PIC XX.
       01 BR-ST    PIC XX.
       01 BF-ST    PIC XX.
       01 BG-ST    PIC XX.
       01 LEG      PIC XX.
       01 EOF-FLAG PIC X.
       01 WS-N     PIC 9 VALUE 9.
       01 WS-TBL.
           05 WS-T PIC X OCCURS 3 TIMES VALUE "Q".
       01 WS-X     PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BADC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON BAD-C.
       D-BADC-P.
           DISPLAY LEG "-USE-BADC=" BC-ST.
       D-BADR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON BAD-R.
       D-BADR-P.
           DISPLAY LEG "-USE-BADR=" BR-ST.
           RESUME AT NEXT STATEMENT.
       D-BADF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON BAD-F.
       D-BADF-P.
           DISPLAY LEG "-USE-BADF-START".
           MOVE WS-T (WS-N) TO WS-X.
           DISPLAY LEG "-USE-BADF-END".
       D-BADG SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON BAD-G.
       D-BADG-P.
           DISPLAY LEG "-USE-BADG-START".
           PERFORM D-RES-P.
           DISPLAY LEG "-USE-BADG-END".
       D-RES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       D-RES-P.
           DISPLAY LEG "-USE-RES".
           RESUME AT NEXT STATEMENT.
           DISPLAY LEG "-USE-RES-AFTER".
       D-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-SUB-P.
           DISPLAY LEG "-USE-SUB".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       SEED.
           OPEN OUTPUT SRC-A.
           MOVE "A" TO SA-REC WRITE SA-REC.
           MOVE "C" TO SA-REC WRITE SA-REC.
           CLOSE SRC-A.
           OPEN OUTPUT SRC-B.
           MOVE "B" TO SB-REC WRITE SB-REC.
           MOVE "D" TO SB-REC WRITE SB-REC.
           CLOSE SRC-B.
       LEG-1.
           MOVE "L1" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-C OUT-G.
           DISPLAY "L1-AFTER=" BC-ST.
           PERFORM SHOW-OUT.
       LEG-2.
           MOVE "L2" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-R OUT-G.
           DISPLAY "L2-AFTER=" BR-ST.
           PERFORM SHOW-OUT.
       LEG-3.
           MOVE "L3" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-F OUT-G.
           DISPLAY "L3-AFTER=" BF-ST.
           PERFORM SHOW-OUT.
       LEG-4.
           MOVE "L4" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-G OUT-G.
           DISPLAY "L4-AFTER=" BG-ST.
           PERFORM SHOW-OUT.
           STOP RUN.
       UTIL-SECT SECTION.
       SEED-OUT.
           OPEN OUTPUT OUT-G.
           MOVE "OLD" TO OG-REC WRITE OG-REC.
           CLOSE OUT-G.
       SHOW-OUT.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT OUT-G.
           PERFORM UNTIL EOF-FLAG = "Y" OR OG-ST NOT = "00"
               READ OUT-G
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY LEG "-OUT=" OG-REC
               END-READ
           END-PERFORM.
           CLOSE OUT-G.
