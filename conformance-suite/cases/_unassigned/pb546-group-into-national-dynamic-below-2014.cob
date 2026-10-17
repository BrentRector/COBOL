*>--- conformance-case
*> id: pb546-group-into-national-dynamic-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB546 / design D-N8 - the DYNAMIC LENGTH clause is a COBOL-2014
      *> addition (ISO 8.5.1.10 / 13.18.19), so below 2014 the national dynamic-length
      *> receiver of a group MOVE is refused by the edition gate (COBOLNET0900)
      *> (positive golden 2014/pb546_group_into_national_dynamic).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB546N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G3.
          05 G3A PIC X(3) VALUE "ABC".
       01 DN PIC N DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           MOVE G3 TO DN
           STOP RUN.
