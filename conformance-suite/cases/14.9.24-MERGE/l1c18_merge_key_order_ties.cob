      *>--- conformance-case
      *> id: l1c18_merge_key_order_ties
      *> rules: [SR-14.9.24.3-8, GR-14.9.24.4-3, GR-14.9.24.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.24.4 GR3/GR4 + §14.9.24.3 SR8 — MERGE key order
      *> (major-to-minor, ASCENDING/DESCENDING transitive, KEY-phrase
      *> division irrelevant), equal-key return order across USING
      *> files, and THRU == THROUGH in the OUTPUT PROCEDURE range.
      *>
      *> GR3: "The data-names following the word KEY are listed from
      *> left to right in the MERGE statement in order of decreasing
      *> significance without regard to how they are divided into KEY
      *> phrases." a) ASCENDING: lowest to highest; b) DESCENDING:
      *> highest to lowest, "according to the rules for comparison of
      *> operands in a relation condition". "The words ASCENDING and
      *> DESCENDING are transitive across all occurrences of
      *> data-name-1 until another occurrence of the word ASCENDING or
      *> DESCENDING is encountered."
      *>   cite.py: OK  §14.9.24.4 3)  (General rules)
      *>   cite.py: OK  §14.9.24.4 3) b)  (General rules)
      *> GR4: equal-key records: "a) Follows the order of the
      *> associated input files as specified in the MERGE statement.
      *> b) Is such that all records associated with one input file
      *> are returned prior to the return of records from another
      *> input file."
      *>   cite.py: OK  §14.9.24.4 4) a)  (General rules)
      *>   cite.py: OK  §14.9.24.4 4) b)  (General rules)
      *> SR8: "The words THROUGH and THRU are equivalent."
      *>   cite.py: OK  §14.9.24.3 8)  (Syntax rules)
      *> Numeric keys compare "with respect to the algebraic value of
      *> the operands regardless of the manner in which their usage is
      *> described".
      *>   cite.py: OK  §8.8.4.2.4   (Comparison of numeric operands)
      *>
      *> Record = S-A X, S-B S9 (signed DISPLAY), S-C X, S-TAG X(4).
      *> Every input file is already in (A asc, B desc, C desc) order
      *> (GR6 is therefore not triggered):
      *>   F1: A 9 Z F1-1 / A 5 Q F1-2 / A -3 X F1-3 / B 5 A F1-4
      *>   F2: A 9 Z F2-1 / A 9 Z F2-2 / A 1 Z F2-3
      *>   F3: A 9 Z F3-1 / A 9 Y F3-2 / B 5 A F3-3 / B 5 A F3-4
      *> MERGE 1: ON ASCENDING KEY S-A ON DESCENDING KEY S-B S-C
      *>   (S-C descending by transitivity), OUTPUT PROCEDURE
      *>   OP-START THRU OP-END.
      *> MERGE 2: ON ASCENDING KEY S-A DESCENDING KEY S-B DESCENDING
      *>   KEY S-C -- the same key list divided into three KEY
      *>   phrases, so by GR3 the same sequence -- with THROUGH.
      *> Derivation of the 11 lines each merge shows:
      *>   A-group first (S-A ascending, GR3 a).  Within it S-B
      *>   descending (GR3 b): 9s, then 5, then 1, then -3 (algebraic,
      *>   §8.8.4.2.4: -3 < 1, so a byte compare of the sign-carrying
      *>   digit would misplace it).  Among S-B = 9, S-C descending by
      *>   transitivity: Z before Y.  The four A 9 Z records tie on
      *>   every key: GR4 a) F1 before F2 before F3; GR4 b) F2's two
      *>   records both precede F3-1 (never interleaved).  B-group:
      *>   three B 5 A ties -> F1-4, then F3-3, F3-4 (GR4 a/b).
      *> The RETURN loop lives in OP-MID, strictly inside the range
      *> OP-START THRU/THROUGH OP-END, so a range that did not span
      *> all three paragraphs would show no records (SR8 + GR9).
      *> COUNT=11 each time: GR1, every record of the three files.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1c18a1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "l1c18a2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "l1c18a3.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SM ASSIGN TO "l1c18as.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC        PIC X(7).
       FD  F2.
       01  F2-REC        PIC X(7).
       FD  F3.
       01  F3-REC        PIC X(7).
       SD  SM.
       01  SM-REC.
           05  S-A       PIC X.
           05  S-B       PIC S9.
           05  S-C       PIC X.
           05  S-TAG     PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  W-A       PIC X.
           05  W-B       PIC S9.
           05  W-C       PIC X.
           05  W-TAG     PIC X(4).
       01  EOF-SW        PIC X.
       01  D-B           PIC -9.
       01  CNT           PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM BUILD-FILES.
           DISPLAY "M1 THRU".
           MERGE SM ON ASCENDING KEY S-A
                    ON DESCENDING KEY S-B S-C
               USING F1 F2 F3
               OUTPUT PROCEDURE IS OP-START THRU OP-END.
           DISPLAY "COUNT=" CNT.
           DISPLAY "M2 THROUGH".
           MERGE SM ASCENDING KEY S-A
                    DESCENDING KEY S-B
                    DESCENDING KEY S-C
               USING F1 F2 F3
               OUTPUT PROCEDURE IS OP-START THROUGH OP-END.
           DISPLAY "COUNT=" CNT.
           STOP RUN.
       OP-START.
           MOVE "N" TO EOF-SW.
           MOVE 0 TO CNT.
       OP-MID.
           PERFORM OP-RET UNTIL EOF-SW = "Y".
       OP-END.
           EXIT.
       OP-RET.
           RETURN SM
               AT END MOVE "Y" TO EOF-SW
               NOT AT END
                   ADD 1 TO CNT
                   MOVE S-B TO D-B
                   DISPLAY S-A " " D-B " " S-C " " S-TAG
           END-RETURN.
       BUILD-FILES.
           OPEN OUTPUT F1.
           MOVE "A" TO W-A MOVE 9 TO W-B MOVE "Z" TO W-C
           MOVE "F1-1" TO W-TAG WRITE F1-REC FROM WS-REC.
           MOVE "A" TO W-A MOVE 5 TO W-B MOVE "Q" TO W-C
           MOVE "F1-2" TO W-TAG WRITE F1-REC FROM WS-REC.
           MOVE "A" TO W-A MOVE -3 TO W-B MOVE "X" TO W-C
           MOVE "F1-3" TO W-TAG WRITE F1-REC FROM WS-REC.
           MOVE "B" TO W-A MOVE 5 TO W-B MOVE "A" TO W-C
           MOVE "F1-4" TO W-TAG WRITE F1-REC FROM WS-REC.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "A" TO W-A MOVE 9 TO W-B MOVE "Z" TO W-C
           MOVE "F2-1" TO W-TAG WRITE F2-REC FROM WS-REC.
           MOVE "A" TO W-A MOVE 9 TO W-B MOVE "Z" TO W-C
           MOVE "F2-2" TO W-TAG WRITE F2-REC FROM WS-REC.
           MOVE "A" TO W-A MOVE 1 TO W-B MOVE "Z" TO W-C
           MOVE "F2-3" TO W-TAG WRITE F2-REC FROM WS-REC.
           CLOSE F2.
           OPEN OUTPUT F3.
           MOVE "A" TO W-A MOVE 9 TO W-B MOVE "Z" TO W-C
           MOVE "F3-1" TO W-TAG WRITE F3-REC FROM WS-REC.
           MOVE "A" TO W-A MOVE 9 TO W-B MOVE "Y" TO W-C
           MOVE "F3-2" TO W-TAG WRITE F3-REC FROM WS-REC.
           MOVE "B" TO W-A MOVE 5 TO W-B MOVE "A" TO W-C
           MOVE "F3-3" TO W-TAG WRITE F3-REC FROM WS-REC.
           MOVE "B" TO W-A MOVE 5 TO W-B MOVE "A" TO W-C
           MOVE "F3-4" TO W-TAG WRITE F3-REC FROM WS-REC.
           CLOSE F3.
