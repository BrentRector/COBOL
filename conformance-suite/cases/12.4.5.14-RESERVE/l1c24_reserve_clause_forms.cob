      *>--- conformance-case
      *> id: l1c24_reserve_clause_forms
      *> rules: [FMT-12.4.5.14.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.14.2 format — RESERVE integer-1 [AREA | AREAS]
      *> General format: RESERVE integer-1 [AREA|AREAS]; RESERVE is the
      *> required word, AREA/AREAS a bracketed optional one-of.
      *>   cite.py --check 12.4.5.14.2 "integer-1"
      *>     -> OK §12.4.5.14.2 (General format)
      *>   cite.py --check 5.5 "shall be unsigned and nonzero unless
      *>   otherwise specified in the associated rules"
      *>     -> OK §5.5 1) (Integer operands)
      *> Each of the three spellings the format admits is written on
      *> its own file: RESERVE 5 AREAS (F1), RESERVE 1 AREA (F2),
      *> RESERVE 3 with the optional word omitted (F3). The number of
      *> areas is implementor latitude with no observable effect, so the
      *> golden pins that every spelling is ACCEPTED and the file it
      *> describes works: each file is written with one record and read
      *> back, so the expected output is the three records' contents
      *> F1-REC / F2-REC / F3-REC, then END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C24F1.DAT"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 5 AREAS.
           SELECT F2 ASSIGN TO "L1C24F2.DAT"
               RESERVE 1 AREA
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "L1C24F3.DAT"
               RESERVE 3.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(6).
       FD F2.
       01 R2 PIC X(6).
       FD F3.
       01 R3 PIC X(6).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1 F2 F3.
           MOVE "F1-REC" TO R1.
           WRITE R1.
           MOVE "F2-REC" TO R2.
           WRITE R2.
           MOVE "F3-REC" TO R3.
           WRITE R3.
           CLOSE F1 F2 F3.
           OPEN INPUT F1 F2 F3.
           MOVE SPACES TO R1 R2 R3.
           READ F1 AT END DISPLAY "F1-EOF".
           READ F2 AT END DISPLAY "F2-EOF".
           READ F3 AT END DISPLAY "F3-EOF".
           DISPLAY R1.
           DISPLAY R2.
           DISPLAY R3.
           CLOSE F1 F2 F3.
           DISPLAY "END".
           STOP RUN.
