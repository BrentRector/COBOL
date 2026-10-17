      *>--- conformance-case
      *> id: l1_delete_fpi_unaffected
      *> rules: [GR-14.9.10.4-9]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.10.4 GR9 - "The file position indicator is not
      *> affected by the execution of a DELETE RECORD statement."
      *> (9.1.11: "The setting of the file position indicator is
      *> affected only by the CLOSE, OPEN, READ, and START statements" -
      *> DELETE is not in that list.)
      *> The FPI is observable only through the record the NEXT
      *> sequential READ selects, so each leg reads twice (14.9.30.4
      *> GR21 relative rule f leaves the FPI at the RRN made available,
      *> here 2), deletes, then reads again: GR21 relative rule c then
      *> selects "the first existing record in the physical file whose
      *> relative key number is greater than the file position
      *> indicator", i.e. RRN 3.
      *> LEG A (ACCESS SEQUENTIAL, GR2 - the deleted record IS the one
      *> at the FPI): READ3 must be R003. A DELETE that invalidated the
      *> FPI would give '46'/'10'; one that restarted it would give
      *> R001; one that advanced it would skip to R004.
      *> LEG B (ACCESS DYNAMIC, GR4 - the deleted record is named by the
      *> RELATIVE KEY item and is NOT the one at the FPI): deleting RRN
      *> 4 while the FPI holds 2 leaves the FPI at 2, so GREAD3 is S003
      *> and the following READ is the at-end '10' (RRN 4 is gone).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DEL03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1del03a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS F-ST.
           SELECT G ASSIGN TO "l1del03b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS G-K
               FILE STATUS IS G-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 G-ST PIC XX.
       01 G-K  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> LEG A - sequential access.
           OPEN OUTPUT F
           MOVE "R001" TO F-REC
           WRITE F-REC
           MOVE "R002" TO F-REC
           WRITE F-REC
           MOVE "R003" TO F-REC
           WRITE F-REC
           MOVE "R004" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
           READ F AT END CONTINUE END-READ
           DISPLAY "READ1=" F-ST " " F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "READ2=" F-ST " " F-REC
           DELETE F RECORD
           DISPLAY "DEL=" F-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ3=" F-ST " " F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "READ4=" F-ST " " F-REC
           READ F AT END DISPLAY "EOF=" F-ST END-READ
           CLOSE F
      *> LEG B - dynamic access; the deleted record is not the one the
      *> file position indicator designates.
           OPEN OUTPUT G
           MOVE 1 TO G-K
           MOVE "S001" TO G-REC
           WRITE G-REC
           MOVE 2 TO G-K
           MOVE "S002" TO G-REC
           WRITE G-REC
           MOVE 3 TO G-K
           MOVE "S003" TO G-REC
           WRITE G-REC
           MOVE 4 TO G-K
           MOVE "S004" TO G-REC
           WRITE G-REC
           CLOSE G
           OPEN I-O G
           READ G NEXT AT END CONTINUE END-READ
           DISPLAY "GREAD1=" G-ST " " G-REC
           READ G NEXT AT END CONTINUE END-READ
           DISPLAY "GREAD2=" G-ST " " G-REC
           MOVE 4 TO G-K
           DELETE G RECORD
           DISPLAY "GDEL=" G-ST
           READ G NEXT AT END CONTINUE END-READ
           DISPLAY "GREAD3=" G-ST " " G-REC
           READ G NEXT AT END DISPLAY "GEOF=" G-ST END-READ
           CLOSE G
           STOP RUN.
