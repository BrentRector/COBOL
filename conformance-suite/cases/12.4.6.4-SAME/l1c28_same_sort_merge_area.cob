      *>--- conformance-case
      *> id: l1c28_same_sort_merge_area
      *> rules: [SR-12.4.6.4.3-1, GR-12.4.6.4.4-4, GR-12.4.6.4.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.6.4.3 SR1 / §12.4.6.4.4 GR4 — sort-merge-area SAME
      *> clauses, in both spellings, over sort files and non-sort files.
      *> SR1: "SORT and SORT-MERGE are equivalent."
      *>   cite.py: OK  §12.4.6.4.3 1)  (Syntax rules)
      *> GR4 a): "Any storage area allocated for the sorting or merging
      *>   of a sort or merge file specified in a sort-merge-area format
      *>   SAME clause is available for reuse in sorting or merging any
      *>   of the other sort or merge files specified in that
      *>   sort-merge-area format SAME clause."
      *>   cite.py: OK  §12.4.6.4.4 4) a)  (General rules)
      *> GR4 b): "Storage areas assigned to files specified in a
      *>   sort-merge-area format SAME clause that do not represent sort
      *>   or merge files may be allocated as needed for sorting or
      *>   merging ..."
      *>   cite.py: OK  §12.4.6.4.4 4) b)  (General rules)
      *> GR4 c): "Storage areas assigned to files specified in a
      *>   sort-merge-area format SAME clause other than sort or merge
      *>   files do not share the same storage area with each other."
      *>   cite.py: OK  §12.4.6.4.4 4) c)  (General rules)
      *> (GR5 is respected: F3/F4 are closed during every SORT.)
      *>   cite.py: OK  §12.4.6.4.4 5)  (General rules)
      *> Derivation, by output line:
      *>  1 "C F3-ONE F4-ONE": F3 and F4 are both in the SORT clause and
      *>    both open OUTPUT; GR4 c) says their areas are NOT shared,
      *>    so after MOVE F3-ONE / MOVE F4-ONE each area keeps its own
      *>    value (a shared area would show F4-ONE twice).
      *>  2-4 "S1 A".."S1 C": SORT S1 ascending over released C,A,B.
      *>  5-7 "S2 3".."S2 1": SORT S2 descending over released 1,3,2;
      *>    S2 shares S1's sort storage (GR4 a) - reuse is transparent,
      *>    the second sort orders correctly.
      *>  8-10 "S3 X".."S3 Z": S3 is named in a SORT-MERGE clause; by
      *>    SR1 that clause is the same sort-merge-area format, so the
      *>    program is legal and S3 sorts exactly as S1 does.
      *>  11 "R F3-ONE F4-ONE": F3/F4 storage may have been used for the
      *>    sorts (GR4 b); that never alters their FILES, and reading
      *>    both back (both open at once, GR4 c) gives each its own
      *>    record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C28B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "L1C28BS1.TMP".
           SELECT S2 ASSIGN TO "L1C28BS2.TMP".
           SELECT S3 ASSIGN TO "L1C28BS3.TMP".
           SELECT F3 ASSIGN TO "L1C28B3.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F4 ASSIGN TO "L1C28B4.DAT"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME SORT AREA FOR S1 S2 F3 F4
           SAME SORT-MERGE AREA FOR S3 F3.
       DATA DIVISION.
       FILE SECTION.
       SD S1.
       01 S1-REC PIC X.
       SD S2.
       01 S2-REC.
          05 S2-K PIC 9.
       SD S3.
       01 S3-REC PIC X.
       FD F3.
       01 F3-REC PIC X(6).
       FD F4.
       01 F4-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           OPEN OUTPUT F3 F4
           MOVE "F3-ONE" TO F3-REC
           MOVE "F4-ONE" TO F4-REC
           DISPLAY "C " F3-REC " " F4-REC
           WRITE F3-REC
           WRITE F4-REC
           CLOSE F3 F4
           SORT S1 ON ASCENDING KEY S1-REC
               INPUT PROCEDURE IS IN-1
               OUTPUT PROCEDURE IS OUT-1
           SORT S2 ON DESCENDING KEY S2-K
               INPUT PROCEDURE IS IN-2
               OUTPUT PROCEDURE IS OUT-2
           SORT S3 ON ASCENDING KEY S3-REC
               INPUT PROCEDURE IS IN-3
               OUTPUT PROCEDURE IS OUT-3
           OPEN INPUT F3 F4
           READ F3
           READ F4
           DISPLAY "R " F3-REC " " F4-REC
           CLOSE F3 F4
           STOP RUN.
       IN-1 SECTION.
       I1-1.
           MOVE "C" TO S1-REC
           RELEASE S1-REC
           MOVE "A" TO S1-REC
           RELEASE S1-REC
           MOVE "B" TO S1-REC
           RELEASE S1-REC.
       OUT-1 SECTION.
       O1-1.
           MOVE 0 TO WS-EOF
           PERFORM UNTIL WS-EOF = 1
               RETURN S1
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S1 " S1-REC
               END-RETURN
           END-PERFORM.
       IN-2 SECTION.
       I2-1.
           MOVE 1 TO S2-K
           RELEASE S2-REC
           MOVE 3 TO S2-K
           RELEASE S2-REC
           MOVE 2 TO S2-K
           RELEASE S2-REC.
       OUT-2 SECTION.
       O2-1.
           MOVE 0 TO WS-EOF
           PERFORM UNTIL WS-EOF = 1
               RETURN S2
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S2 " S2-K
               END-RETURN
           END-PERFORM.
       IN-3 SECTION.
       I3-1.
           MOVE "Z" TO S3-REC
           RELEASE S3-REC
           MOVE "X" TO S3-REC
           RELEASE S3-REC
           MOVE "Y" TO S3-REC
           RELEASE S3-REC.
       OUT-3 SECTION.
       O3-1.
           MOVE 0 TO WS-EOF
           PERFORM UNTIL WS-EOF = 1
               RETURN S3
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S3 " S3-REC
               END-RETURN
           END-PERFORM.
