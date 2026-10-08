      *> kb/Work PB1954 - COMPILE-ONLY positive. ISO 8.5.1.3.2 (cite.py OK) asks numerically equal level-numbers of
      *> the items immediately subordinate to ONE group item, so each group chooses its own: D1's members are 03 and
      *> D2's are 07; in D1 the first LINE's columns are 07 and the second LINE's are 05, a different group each; and
      *> a level that returns to an earlier sibling's number after deeper nesting (the second 03 LINE) is that
      *> sibling's equal. ISO 13.18.33.1 (cite.py OK) gives report group entries the data hierarchy. Every entry here
      *> obeys the rule, so the compile must stay clean at every edition; the negatives
      *> pb1954-report-unequal-sibling-levels and pb1954-report-unequal-line-levels pin the refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB1954RPTOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "w37bpb1954ok.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               07  COLUMN 1 PIC X VALUE "A".
               07  COLUMN 3 PIC X VALUE "B".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X VALUE "C".
       01  D2 TYPE DE.
           07  LINE PLUS 1.
               09  COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           GENERATE D2.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
