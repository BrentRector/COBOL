      *> ISO 1989:2023 §9.1.13.6 item 4 b) - I-O status '34' for a READ of a record made variable by OCCURS DEPENDING ON.
      *> "I-O status = 34, A permanent error exists because of a boundary violation: ... b) A READ statement is
      *> unsuccessfully executed because the records are variable in length because of an OCCURS DEPENDING ON clause
      *> in the record in the associated record description entry and the associated DEPENDING ON item contains a
      *> value that makes the number of bytes in the record exceed the value specified or implied by the file
      *> description entry."
      *> The FD below has no RECORD clause, so §13.18.43.4 GR5 implies the largest record description: OD-R is 2 bytes
      *> of count plus an X table of up to 10 = 12 bytes, and a record's size at count N is 2 + N (GR13 c)). So the
      *> READ is unsuccessful exactly when the count that lands in OD-N exceeds 10: 03 and 10 fit, 11 and 99 do not.
      *> An unsuccessful READ runs no NOT AT END (§9.1.13.1: for a fatal condition "Any NOT AT END or NOT INVALID KEY phrase specified for that statement is ignored"), and
      *> the permanent error it is (§9.1.13.1) REMAINS IN EFFECT for every later statement until the CLOSE
      *> (Annex A.1 item 105, DOC-A.1-105) - the fourth READ below fails although its own record is the legal 03.
      *>
      *> WHY EACH LEG CAN FAIL: the writer FD (a PIC X(12) record on the same physical file) gives the records any
      *> count; before the fix every READ answered '00' and delivered a count past the table's maximum.
      *>   SEQ  - a sequential-organization READ (SequentialIoEmitter): 03 -> 00, 10 -> 00, 11 -> 34, then the
      *>          error in effect on the next READ, the CLOSE ending it, a fresh OPEN reading 03 -> 00 again.
      *>   REL  - the keyed READ arm (KeyedIoEmitter) over a RELATIVE file: slot 3 (10) -> 00, slot 2 (11) -> 34,
      *>          slot 1 (a legal 03) -> 34 again for the condition in effect, CLOSE, reopen, slot 1 -> 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1513OD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OD ASSIGN TO "pb1513s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT ODW ASSIGN TO "pb1513s.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT ODR ASSIGN TO "pb1513r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKR FILE STATUS IS FS.
           SELECT ODRW ASSIGN TO "pb1513r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKW.
           SELECT SRT ASSIGN TO "pb1513srt.dat".
           SELECT SOUT ASSIGN TO "pb1513o.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT OPTIONAL CHK ASSIGN TO "pb1513o.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FSC.
       DATA DIVISION.
       FILE SECTION.
       FD  OD.
       01  OD-R.
           05 OD-N PIC 99.
           05 OD-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON OD-N.
       FD  ODW.
       01  ODW-R PIC X(12).
       FD  ODR.
       01  ODR-R.
           05 ODR-N PIC 99.
           05 ODR-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON ODR-N.
       FD  ODRW.
       01  ODRW-R PIC X(12).
       SD  SRT.
       01  SRT-R PIC X(12).
       FD  SOUT.
       01  SOUT-R PIC X(12).
       FD  CHK.
       01  CHK-R PIC X(12).
       WORKING-STORAGE SECTION.
       01  FSC PIC XX.
       01  FS  PIC XX.
       01  RKR PIC 9.
       01  RKW PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT ODW.
           MOVE "03ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           MOVE "10ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           MOVE "11ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           MOVE "03ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           CLOSE ODW.
           OPEN INPUT OD.
           READ OD NOT AT END DISPLAY "SEQ1 NAE N=" OD-N END-READ.
           DISPLAY "SEQ1 " FS.
           READ OD NOT AT END DISPLAY "SEQ2 NAE N=" OD-N END-READ.
           DISPLAY "SEQ2 " FS.
           READ OD NOT AT END DISPLAY "SEQ3 NAE" END-READ.
           DISPLAY "SEQ3 " FS.
           READ OD NOT AT END DISPLAY "SEQ4 NAE" END-READ.
           DISPLAY "SEQ4 " FS.
           CLOSE OD.
           DISPLAY "CLOSE " FS.
           OPEN INPUT OD.
           READ OD NOT AT END DISPLAY "SEQ5 NAE N=" OD-N END-READ.
           DISPLAY "SEQ5 " FS.
           CLOSE OD.
           OPEN OUTPUT ODRW.
           MOVE 1 TO RKW.
           MOVE "03ABCDEFGHIJ" TO ODRW-R.
           WRITE ODRW-R.
           MOVE 2 TO RKW.
           MOVE "11ABCDEFGHIJ" TO ODRW-R.
           WRITE ODRW-R.
           MOVE 3 TO RKW.
           MOVE "10ABCDEFGHIJ" TO ODRW-R.
           WRITE ODRW-R.
           CLOSE ODRW.
           OPEN INPUT ODR.
           MOVE 3 TO RKR.
           READ ODR NOT INVALID KEY DISPLAY "REL3 NIK N=" ODR-N END-READ.
           DISPLAY "REL3 " FS.
           MOVE 2 TO RKR.
           READ ODR NOT INVALID KEY DISPLAY "REL2 NIK" END-READ.
           DISPLAY "REL2 " FS.
           MOVE 1 TO RKR.
           READ ODR NOT INVALID KEY DISPLAY "REL1 NIK" END-READ.
           DISPLAY "REL1 " FS.
           CLOSE ODR.
           DISPLAY "CLOSE " FS.
           OPEN INPUT ODR.
           MOVE 1 TO RKR.
           READ ODR NOT INVALID KEY DISPLAY "REL1B NIK N=" ODR-N END-READ.
           DISPLAY "REL1B " FS.
           CLOSE ODR.
      *> SRT - the implicit READ of a SORT's USING transfer is "as if a READ statement": the same file, whose third
      *> record (11) is too long, ends the retrieval with '34' and the fatal status terminates the SORT, so no
      *> GIVING file is ever created (CHK finds it absent: '05'); the sort of the all-legal file DOES produce it.
           DELETE FILE SOUT.
           SORT SRT ON ASCENDING KEY SRT-R USING OD GIVING SOUT.
           OPEN INPUT CHK.
           DISPLAY "SORT1 CHK=" FSC.
           CLOSE CHK.
           CLOSE OD.
           OPEN OUTPUT ODW.
           MOVE "10ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           MOVE "03ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           CLOSE ODW.
           SORT SRT ON ASCENDING KEY SRT-R USING OD GIVING SOUT.
           OPEN INPUT CHK.
           DISPLAY "SORT2 CHK=" FSC.
           READ CHK.
           DISPLAY "SORT2 1=" CHK-R.
           READ CHK.
           DISPLAY "SORT2 2=" CHK-R.
           CLOSE CHK.
           DELETE FILE SOUT.
           STOP RUN.
