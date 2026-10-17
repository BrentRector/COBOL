      *>--- conformance-case
      *> id: pb447_search_all_index_stays_in_table
      *> rules: [GR-14.9.37.4-9, DOC-A.1-170]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 — WHERE EACH SEARCH FORMAT LEAVES ITS SEARCH
      *> INDEX. The standard writes the scan twice and the two texts
      *> disagree, so this program asserts BOTH, in one run, at the
      *> edition both formats already existed in (kb/Work PB447).
      *>
      *> A — Format 2, unsuccessful. GR9: "At no time is it set to a
      *>     value that exceeds the value that corresponds to the last
      *>     element of the table or is less than the value that
      *>     corresponds to the first element of the table." The same
      *>     rule ends "the final setting of the search index is
      *>     undefined", so the program asserts the RANGE and not a
      *>     particular occurrence — being inside the table is the
      *>     promise; which occurrence is not.
      *> B — Format 1, unsuccessful. GR4: "the search index is
      *>     incremented by one occurrence number. The process is then
      *>     repeated using the new index setting unless the new value
      *>     for the search index corresponds to a table element
      *>     outside the permissible range of occurrence values." The
      *>     new value is formed and then judged, so the index ends at
      *>     6 over five occurrences — determined, and pinned exactly.
      *> C — Format 2 over an occurs-depending table. §13.18.38 GR7
      *>     makes the CURRENT count the last element, so GR9's bound
      *>     is 3 here although storage for 5 exists.
      *> D — Format 2, successful. GR1 a): the index "remains set at
      *>     the occurrence number that caused a WHEN condition to be
      *>     satisfied"; one occurrence holds 5, so GR7's "undefined
      *>     which one" does not arise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB447IX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 99.
       01 C  PIC 9 VALUE 3.
       01 T.
          05 EL OCCURS 5 TIMES
             ASCENDING KEY IS K INDEXED BY KX.
             10 K PIC 9.
       01 U.
          05 EY OCCURS 1 TO 5 TIMES DEPENDING ON C
             ASCENDING KEY IS J INDEXED BY JX.
             10 J PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO K (1).
           MOVE 3 TO K (2).
           MOVE 5 TO K (3).
           MOVE 7 TO K (4).
           MOVE 9 TO K (5).
           MOVE 1 TO J (1).
           MOVE 3 TO J (2).
           MOVE 5 TO J (3).
           SEARCH ALL EL
               AT END SET N TO KX
                   IF N >= 1 AND N <= 5
                       DISPLAY "A=IN-RANGE"
                   ELSE
                       DISPLAY "A=OUT-OF-RANGE " N
                   END-IF
               WHEN K (KX) = 4
                   DISPLAY "A=FOUND"
           END-SEARCH.
           SET KX TO 1.
           SEARCH EL
               AT END SET N TO KX
                   DISPLAY "B=" N
               WHEN K (KX) = 4
                   DISPLAY "B=FOUND"
           END-SEARCH.
           SEARCH ALL EY
               AT END SET N TO JX
                   IF N >= 1 AND N <= C
                       DISPLAY "C=IN-RANGE"
                   ELSE
                       DISPLAY "C=OUT-OF-RANGE " N
                   END-IF
               WHEN J (JX) = 4
                   DISPLAY "C=FOUND"
           END-SEARCH.
           SEARCH ALL EL
               AT END DISPLAY "D=ATEND"
               WHEN K (KX) = 5
                   SET N TO KX
                   DISPLAY "D=" N
           END-SEARCH.
           STOP RUN.
