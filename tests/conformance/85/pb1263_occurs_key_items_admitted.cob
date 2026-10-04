      *> kb/Work PB1263 - the KEY data-names ISO 13.18.38.3 ADMITS, beside the four it refuses
      *> (SR4 / SR6 / SR8 / SR9; the negatives pb1263-occurs-key-*). Admitted here: a group key
      *> G that is subordinate to the table through no OCCURS group (SR4 bites only on a group
      *> that contains an OCCURS clause), a second key K2 beside a sibling table W (SR6 bites
      *> only on the KEY item's own entry), and the subject itself as its own key (SR6: "except
      *> when data-name-2 is the subject of the entry"). Both SEARCH ALLs find their element:
      *> T is ascending on G ("1", "2", "3" - K2 is B at the match), E is descending (9, 5, 1 -
      *> the WHEN value 1 is element 3). Fails if any admitted key is refused, or a search misses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T OCCURS 3 ASCENDING KEY G K2 INDEXED BY IX.
               10 G.
                   15 K1 PIC 9.
               10 K2 PIC X.
               10 W  PIC X OCCURS 2.
       01 S.
           05 E PIC 9 OCCURS 3 DESCENDING KEY E INDEXED BY EX.
       PROCEDURE DIVISION.
           MOVE "1A..2B..3C.." TO R
           SEARCH ALL T AT END DISPLAY "NONE"
               WHEN G (IX) = "2" DISPLAY "FOUND " K2 (IX)
           END-SEARCH
           MOVE 9 TO E (1) MOVE 5 TO E (2) MOVE 1 TO E (3)
           SEARCH ALL E AT END DISPLAY "NONE"
               WHEN E (EX) = 1 DISPLAY "FOUND " E (EX)
           END-SEARCH
           STOP RUN.
