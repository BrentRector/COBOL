*>--- conformance-case
*> id: pb215-rw-varying-index-data-item
*> rules: [SR-13.18.60.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215. ISO 13.18.60.3 SR10: "An index data item may be referenced
      *> explicitly only in a SEARCH or SET statement, a relation condition, an
      *> intrinsic function argument" ... - the report writer's VARYING (13.18.64)
      *> is not on the list, so FROM IDX is an arithmetic operand of class index,
      *> which 8.8.1.1 does not admit. It was bound under the SET/relation window.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb215n4.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       WORKING-STORAGE SECTION.
       01  IDX USAGE INDEX.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING K FROM IDX BY 1 SOURCE K.
       PROCEDURE DIVISION.
       MAIN.
           SET IDX TO 2
           OPEN OUTPUT PRT.
           INITIATE R-RE.
           GENERATE DET-A.
           TERMINATE R-RE.
           CLOSE PRT.
           STOP RUN.
