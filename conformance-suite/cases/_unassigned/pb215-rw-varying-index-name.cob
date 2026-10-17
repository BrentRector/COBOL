*>--- conformance-case
*> id: pb215-rw-varying-index-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215. ISO 13.18.64.2 writes the report writer's VARYING as
      *> FROM arithmetic-expression-1 BY arithmetic-expression-2, and 13.18.38.3
      *> r7 admits an index-name only "as a subscript; in the VARYING phrase of a
      *> PERFORM statement; in the VARYING phrase of a SEARCH statement; in the
      *> SET statement; as an operand in a relation condition" - not here. The
      *> RW VARYING was bound under the index-name window and compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb215n3.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       WORKING-STORAGE SECTION.
       01  TB.
           05 TE PIC X OCCURS 3 TIMES INDEXED BY IXR.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING K FROM IXR BY 1 SOURCE K.
       PROCEDURE DIVISION.
       MAIN.
           SET IXR TO 2
           OPEN OUTPUT PRT.
           INITIATE R-RE.
           GENERATE DET-A.
           TERMINATE R-RE.
           CLOSE PRT.
           STOP RUN.
