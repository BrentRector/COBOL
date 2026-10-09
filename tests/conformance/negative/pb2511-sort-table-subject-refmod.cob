      *> reject-at: 2002 2014 2023
      *> THE FORMAT 2 SORT SUBJECT IS data-name-2, NEVER AN IDENTIFIER
      *> (kb/Work PB2511). ISO/IEC 1989:2023 14.9.40.2 Format 2 prints
      *> SORT data-name-2 and 14.9.40.3 SR13 admits only subscripting
      *> ("Subscripting shall be specified in accordance with 8.4.2.3");
      *> 8.4.3.3.3 NOTE: "where data-name-n is used in a general format or
      *> syntax rule, then reference-modification is not permitted". Each
      *> SORT below writes a reference modifier on the subject: on a plain
      *> table, on a table whose enclosing table is subscripted, with no
      *> length, and on a group element. Every one used to compile and sort
      *> the WHOLE table as if the modifier were not written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2511RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 TE OCCURS 4 TIMES PIC 9.
       01 T2.
          05 ROW OCCURS 2 TIMES.
             10 NE OCCURS 3 TIMES PIC 9.
       01 T6.
          05 BLK OCCURS 3 TIMES.
             10 KK PIC XX.
             10 VV PIC X.
       PROCEDURE DIVISION.
           MOVE "3142" TO T1
           MOVE "321654" TO T2
           MOVE "c2Xa3Yb1Z" TO T6
           SORT TE(1:2) ASCENDING KEY
           SORT NE(2)(1:1) ASCENDING KEY
           SORT TE(2:) ASCENDING KEY
           SORT BLK(1:3) ASCENDING KEY
           DISPLAY T1 " " T2 " " T6
           STOP RUN.
