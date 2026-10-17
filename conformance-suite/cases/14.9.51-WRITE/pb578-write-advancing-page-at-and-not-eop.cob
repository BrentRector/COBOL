*>--- conformance-case
*> id: pb578-write-advancing-page-at-and-not-eop
*> rules: [SR-14.9.51.3-18]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.51.3 SR18: "The phrases ADVANCING PAGE and END-OF-PAGE shall not both be specified in a
*> single WRITE statement." The END-OF-PAGE phrase is GR27 b)'s imperative-statement-1 (the AT phrase);
*> SR19 names it and the NOT END-OF-PAGE phrase separately, so a NOT-only phrase beside ADVANCING PAGE
*> is legal (tests/conformance/85/pb578_advancing_page_overflow_85.cob) -- but the PAIR still carries the
*> END-OF-PAGE phrase, and writing NOT END-OF-PAGE next to it does not lift SR18. Rejected at every
*> edition (COBOLNET0861); kb/Work PB578.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB578NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb578neg.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 4 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC AFTER ADVANCING PAGE
               AT END-OF-PAGE DISPLAY "EOP"
               NOT AT END-OF-PAGE DISPLAY "NOT-EOP"
           END-WRITE.
           CLOSE LPF.
           STOP RUN.
