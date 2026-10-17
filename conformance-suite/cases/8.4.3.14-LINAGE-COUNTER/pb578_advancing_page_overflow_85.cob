      *>--- conformance-case
      *> id: pb578_advancing_page_overflow_85
      *> rules: [GR-8.4.3.14.4-2, GR-13.18.34.4-7, GR-14.9.51.4-28]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB578 -- WRITE ADVANCING PAGE on a logical page IS A
      *> PAGE OVERFLOW, so the end-of-page condition occurs and a NOT
      *> END-OF-PAGE imperative does not run.
      *>
      *> THE RULE CHAIN, VERBATIM:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "During the
      *>   resetting of LINAGE-COUNTER to the value one, the value of
      *>   LINAGE-COUNTER is implicitly incremented to exceed the value
      *>   specified by integer-1 or the data item referenced by
      *>   data-name-1."
      *>                       -> OK  §13.18.34.4 7) c) 1.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "This occurs
      *>   when the associated LINAGE-COUNTER is equal to or exceeds the
      *>   page size."           -> OK  §14.9.51.4 26)  (arm a)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If, during the
      *>   successful execution of a WRITE statement with the NOT
      *>   END-OF-PAGE phrase, the end-of-page condition does not occur"
      *>                       -> OK  §14.9.51.4 28)
      *> The counter passes the page size while it is reset, which is
      *> GR26 a)'s trigger, so GR28's NOT arm is not taken. The phrase
      *> itself is legal beside ADVANCING PAGE: SR18 bars the
      *> END-OF-PAGE phrase (GR27 b)'s imperative-statement-1), and
      *> SR19 names it and the NOT END-OF-PAGE phrase separately:
      *>   python scripts/spec/cite.py --check 14.9.51.3 "If the
      *>   END-OF-PAGE or the NOT END-OF-PAGE phrase is specified, the
      *>   LINAGE clause shall be specified in the file description
      *>   entry associated with the write file."
      *>                       -> OK  §14.9.51.3 19)
      *> docs/CONFORMANCE.md §4 "DETERMINATION -- WRITE ADVANCING PAGE
      *> on a logical page is a page overflow".
      *>
      *> EXPECTED, from the rules (page size 5, no FOOTING phrase):
      *> W1  OPEN OUTPUT set the counter to one (GR7 d); AFTER
      *>     ADVANCING 1 LINE adds one (GR7 c) 2): 2 fits the page
      *>     body, no end-of-page, so the NOT arm runs: W1 NOT-EOP, 002.
      *> W2  AFTER ADVANCING PAGE: overflow, the NOT arm does not run;
      *>     the counter is one on the next page (GR7 c) 1): W2 001.
      *> W3  BEFORE ADVANCING PAGE: the same: W3 001.
      *> W4  a plain WRITE adds one (GR7 c) 3) and fits: W4 NOT-EOP,
      *>     002 -- the NOT arm is live, so W2/W3's silence is the
      *>     end-of-page condition and not a dead branch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB578P85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb578p85.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 5 LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 LC PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC AFTER ADVANCING 1 LINE
               NOT AT END-OF-PAGE DISPLAY "W1 NOT-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "W1 " LC.
           MOVE "BBBB" TO P-REC.
           WRITE P-REC AFTER ADVANCING PAGE
               NOT AT END-OF-PAGE DISPLAY "W2 NOT-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "W2 " LC.
           MOVE "CCCC" TO P-REC.
           WRITE P-REC BEFORE ADVANCING PAGE
               NOT AT EOP DISPLAY "W3 NOT-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "W3 " LC.
           MOVE "DDDD" TO P-REC.
           WRITE P-REC
               NOT AT END-OF-PAGE DISPLAY "W4 NOT-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "W4 " LC.
           CLOSE LPF.
           STOP RUN.
