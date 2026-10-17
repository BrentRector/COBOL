*>--- conformance-case
*> id: pb399-search-when-pointer-ordering
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The SAME rule at a THIRD surface, to prove the band now lives at the checkpoint every relation
      *> reaches and not in one caller of it (kb/Work PB399).  ISO §8.8.4.2.2 Format 3
      *> (message-tag-object-or-pointer-reference) prints only `IS [NOT] EQUAL TO` / `=` / `<>`; a SEARCH
      *> WHEN phrase carries an ordinary relation condition (§14.9.37.3), so an ordering operator over a
      *> class-pointer operand is inadmissible there exactly as it is in an IF.
      *>
      *> ⚠ THE TABLE ELEMENT IS NUMERIC, NOT A POINTER, ON PURPOSE.  ISO §13.18.60.3 SR14 admits a USAGE
      *> POINTER item only "for an elementary data item at level 1 or an elementary data item subordinate to
      *> a type declaration that includes the STRONG phrase", so a table OF pointers is illegal source and
      *> this case would then reject for §13.18.60's reason while reading as evidence for §8.8.4.2's.  The
      *> pointer pair is carried in the WHEN CONDITION, which is the surface under test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399SRCH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       01 WS-Q USAGE POINTER.
       01 WS-T.
          05 WS-E OCCURS 3 TIMES INDEXED BY WS-I.
             10 WS-E-N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-I TO 1.
           SEARCH WS-E
               AT END
                   DISPLAY "NOT-FOUND"
               WHEN WS-P >= WS-Q
                   DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
