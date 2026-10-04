      *> kb/Work PB1087 - THE LEGAL SPELLINGS NEXT TO THE SAME-CLAUSE RULES (negatives pb1087-same-*, pb1242-same-*).
      *>   12.4.6.4.3 SR7: a file other than a report/sort/merge file may be in one file-area, one record-area and one
      *>     or more sort-merge-area clauses. SR1: SORT and SORT-MERGE are equivalent (S1 uses SORT, S2 SORT-MERGE).
      *>   SR6: a sort file may be in one record-area and one sort-merge-area clause. SR5: a report file in ONE
      *>     file-area clause. SR9: the file-area clause (F1 F2) lies wholly inside the record-area clause (F1 F2 F3 S1)
      *>     and, by SR10, inside each sort-merge-area clause naming F1 or F2. SR8: both sort-merge-area clauses name a
      *>     sort file. F3 in a record-area clause AND a sort-merge-area clause is SR7's third permission.
      *>   GR2: the record-area clause makes F1, F2, F3 and S1 share ONE area, so a record moved into F1-REC is
      *>     what F2-REC, F3-REC and S1-REC hold (implicit redefinition aligned on the leftmost byte).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1087OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1087ok1.dat".
           SELECT F2 ASSIGN TO "pb1087ok2.dat".
           SELECT F3 ASSIGN TO "pb1087ok3.dat".
           SELECT F4 ASSIGN TO "pb1087ok4.dat".
           SELECT S1 ASSIGN TO "pb1087oks1.dat".
           SELECT S2 ASSIGN TO "pb1087oks2.dat".
           SELECT R1 ASSIGN TO "pb1087okr.dat".
       I-O-CONTROL.
           SAME AREA FOR F1 F2.
           SAME RECORD AREA FOR F1 F2 F3 S1.
           SAME SORT AREA FOR S1 F1 F2.
           SAME SORT-MERGE AREA FOR S2 F1 F2 F3.
           SAME AREA FOR R1 F4.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(6).
       FD F2.
       01 F2-REC PIC X(6).
       FD F3.
       01 F3-REC PIC X(6).
       FD F4.
       01 F4-REC PIC X(6).
       SD S1.
       01 S1-REC PIC X(6).
       SD S2.
       01 S2-REC PIC X(6).
       FD R1 REPORT IS RPT.
       REPORT SECTION.
       RD RPT.
       01 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "SHARED" TO F1-REC
           DISPLAY F2-REC " " F3-REC " " S1-REC
           STOP RUN.
