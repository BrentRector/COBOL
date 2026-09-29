      *> kb/Work PB1153 / PB1199 / PB1318 - LINAGE-COUNTER, LINE-COUNTER and
      *> PAGE-COUNTER are DATA ITEMS: "a temporary unsigned integer data item
      *> of class and category numeric" (ISO 8.4.3.14.4 GR1, 8.4.3.15.4 GR1),
      *> and 8.4.3.15.3 SR1 admits the report counters "in any context where
      *> an integer data item may appear". So every character context - DISPLAY,
      *> MOVE to an alphanumeric item, a STRING sender, a relation with an
      *> alphanumeric operand - and every integer-item context - PERFORM ...
      *> TIMES, FUNCTION LENGTH - takes them like an unsigned integer item.
      *> Their implicit description is the documented one (docs/CONFORMANCE.md,
      *> "the counter registers' declared capacity"): PIC 9(d) USAGE DISPLAY,
      *> d = the digits of the LINAGE page size (LINAGE 20 -> 9(2)) and 18 for
      *> the report counters. Before the fix each character context compiled
      *> clean and aborted at run time ("computed expression in a string
      *> context"), PERFORM PAGE-COUNTER TIMES was refused as not an integer,
      *> and FUNCTION LENGTH(PAGE-COUNTER) was refused as "a numeric literal".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1153CRI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1153lpf.txt".
           SELECT RPTF ASSIGN TO "pb1153rpt.txt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 20 LINES.
       01 P-REC PIC X(10).
       FD RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WX4  PIC X(4).
       01 WX20 PIC X(20).
       01 W2   PIC X(2) VALUE "02".
       01 WL   PIC X(18) VALUE "000000000000000001".
       01 K    PIC 9 VALUE 0.
       REPORT SECTION.
       RD R1 PAGE LIMIT 10 LINES HEADING 1 FIRST DETAIL 2
             LAST DETAIL 8 FOOTING 9.
       01 D1 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN.
      *> LINAGE-COUNTER: 1 at OPEN OUTPUT, 2 after one WRITE without
      *> ADVANCING (13.18.34.4 GR7); its image is 9(2).
           OPEN OUTPUT LPF.
           MOVE "AA" TO P-REC.
           WRITE P-REC.
           DISPLAY "LNC=" LINAGE-COUNTER.
           DISPLAY "LNC-OF=" LINAGE-COUNTER OF LPF.
           MOVE LINAGE-COUNTER TO WX4.
           DISPLAY "LNC-MOVE=[" WX4 "]".
           MOVE SPACES TO WX20.
           STRING "L" LINAGE-COUNTER DELIMITED BY SIZE INTO WX20.
           DISPLAY "LNC-STRING=[" WX20 "]".
           IF LINAGE-COUNTER = W2
               DISPLAY "LNC-REL=EQ"
           ELSE
               DISPLAY "LNC-REL=NE".
           DISPLAY "LNC-LENGTH=" FUNCTION LENGTH(LINAGE-COUNTER).
           CLOSE LPF.
      *> PAGE-COUNTER is 1 after INITIATE; LINE-COUNTER is the line of the
      *> detail just printed, 2 (FIRST DETAIL 2). Their image is 9(18).
           OPEN OUTPUT RPTF.
           INITIATE R1.
           GENERATE D1.
           DISPLAY "PC=" PAGE-COUNTER.
           DISPLAY "LC=" LINE-COUNTER OF R1.
      *> A MOVE to a shorter alphanumeric item keeps the leftmost digits
      *> (14.9.25.4 GR6 - the unsigned integer's digit image, truncated on
      *> the right).
           MOVE LINE-COUNTER TO WX4.
           DISPLAY "LC-MOVE4=[" WX4 "]".
           MOVE LINE-COUNTER TO WX20.
           DISPLAY "LC-MOVE20=[" WX20 "]".
           MOVE SPACES TO WX20.
           STRING "P" PAGE-COUNTER DELIMITED BY SIZE INTO WX20.
           DISPLAY "PC-STRING=[" WX20 "]".
           IF PAGE-COUNTER = WL
               DISPLAY "PC-REL=EQ"
           ELSE
               DISPLAY "PC-REL=NE".
           PERFORM PAGE-COUNTER TIMES
               ADD 1 TO K
           END-PERFORM.
           DISPLAY "PC-TIMES=" K.
           DISPLAY "PC-LENGTH=" FUNCTION LENGTH(PAGE-COUNTER).
           TERMINATE R1.
           CLOSE RPTF.
           STOP RUN.
