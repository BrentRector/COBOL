      *>--- conformance-case
      *> id: read-previous-after-open-indexed
      *> rules: [GR-14.9.30.4-21]
      *> accept: [2002, 2014, 2023]
      *> constructs: [read-previous-2002]
      *> source-format: fixed
      *>---
      *> ISO 14.9.30.4 GR 21, the indexed-file block, rule d) 3: "If no
      *> such record is found or PREVIOUS is specified and the previous
      *> operation on the file was an OPEN statement, the at end
      *> condition exists". That is the 2023 rule. Annex E.2 item 22
      *> records the rule it amended: "the rule itself stated that the
      *> first record would be retrieved". So P1 reads the lowest key
      *> K002 at 2002 and 2014 (this test's .out) and is at end, status
      *> 10, at 2023 (its .2023.out). P2, READ NEXT after the same OPEN,
      *> reads K002 at every edition. READ PREVIOUS is a 2002 feature.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSRPOI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX ASSIGN TO "CSRPOI"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS I-K
               FILE STATUS IS I-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IDX.
       01 I-REC.
          05 I-K PIC X(4).
          05 I-V PIC X(4).
       WORKING-STORAGE SECTION.
       01 I-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX
           MOVE "K002" TO I-K
           MOVE "BBBB" TO I-V
           WRITE I-REC
           MOVE "K004" TO I-K
           MOVE "DDDD" TO I-V
           WRITE I-REC
           CLOSE IDX
           OPEN INPUT IDX
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P1=ATEND|" I-ST
               NOT AT END DISPLAY "P1=" I-K "|" I-ST
           END-READ
           CLOSE IDX
           OPEN INPUT IDX
           READ IDX NEXT RECORD
               AT END DISPLAY "P2=ATEND|" I-ST
               NOT AT END DISPLAY "P2=" I-K "|" I-ST
           END-READ
           CLOSE IDX
           STOP RUN.
