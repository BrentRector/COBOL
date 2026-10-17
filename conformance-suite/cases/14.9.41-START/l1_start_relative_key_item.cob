      *>--- conformance-case
      *> id: l1_start_relative_key_item
      *> rules: [GR-14.9.41.4-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR10 — "The comparison described in General
      *> rule 9 uses the data item referenced by the RELATIVE KEY
      *> clause in the file control entry associated with file-name-1."
      *> The file is SPARSE — records at relative record numbers 3, 7
      *> and 9 — so the answer to a comparison is a value the file's
      *> shape cannot supply by accident.
      *>   S1  RK holds 5 and the relation is GREATER. §14.9.41.4 GR9a:
      *>       "the file position indicator is set to the relative
      *>       record number of the first logical record in the file
      *>       whose key satisfies the comparison searching the file
      *>       sequentially" — that is 7. The following READ NEXT makes
      *>       that record available (§14.9.30.4 GR21b) and §14.9.30.4
      *>       GR25 moves its relative record number into the RELATIVE
      *>       KEY item, so K1 prints 0007 and V1 prints R007.
      *>   S2  The KEY phrase OMITTED. §14.9.41.4 GR8: "the START
      *>       statement behaves as though KEY IS EQUAL TO data-name-1
      *>       had been specified, where data-name-1 is the name of the
      *>       key specified in the RELATIVE KEY clause" — so with RK
      *>       holding 7 the position is record 7 again. Both spellings
      *>       of the statement resolve to the SAME data item, which is
      *>       what GR10 asserts.
      *>   S3  RK holds 9 and the relation is GREATER: no record has a
      *>       higher relative record number, so §14.9.41.4 GR9c gives
      *>       the invalid key condition and '23' (§9.1.13.5 — of the
      *>       invalid-key values only '23' can arise from a START;
      *>       '21', '22' and '24' are all WRITE/REWRITE conditions).
      *>       S1 and S3 differ ONLY in the content of RK, so the
      *>       comparison demonstrably reads that item.
      *> R-VAL is overwritten with XXXXXX before each READ, so the
      *> record content printed can only have come from the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STR10.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "l1str10r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC.
          05 R-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4).
       01 ST-R PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FR
           MOVE 3 TO RK
           MOVE "R003" TO R-VAL
           WRITE R-REC
           MOVE 7 TO RK
           MOVE "R007" TO R-VAL
           WRITE R-REC
           MOVE 9 TO RK
           MOVE "R009" TO R-VAL
           WRITE R-REC
           CLOSE FR
           OPEN INPUT FR
      *> ---- KEY phrase naming the RELATIVE KEY item -------------
           MOVE "XXXX" TO R-VAL
           MOVE 5 TO RK
           START FR KEY IS > RK
               INVALID KEY DISPLAY "S1INV=YES"
               NOT INVALID KEY DISPLAY "S1INV=NO"
           END-START
           DISPLAY "S1=" ST-R
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "K1=" RK " V1=" R-VAL
      *> ---- KEY phrase omitted (GR8 defaults to the same item) --
           MOVE "XXXX" TO R-VAL
           MOVE 7 TO RK
           START FR
               INVALID KEY DISPLAY "S2INV=YES"
               NOT INVALID KEY DISPLAY "S2INV=NO"
           END-START
           DISPLAY "S2=" ST-R
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "K2=" RK " V2=" R-VAL
      *> ---- the same relation, a different RELATIVE KEY value ---
           MOVE 9 TO RK
           START FR KEY IS > RK
               INVALID KEY DISPLAY "S3INV=YES"
               NOT INVALID KEY DISPLAY "S3INV=NO"
           END-START
           DISPLAY "S3=" ST-R
           CLOSE FR
           STOP RUN.
