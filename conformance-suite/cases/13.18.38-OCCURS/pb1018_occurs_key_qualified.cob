      *>--- conformance-case
      *> id: pb1018_occurs_key_qualified
      *> rules: [SR-13.18.38.3-3, SR-14.9.37.3-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
      *> kb/Work PB1018 - an OCCURS KEY data-name is resolved WITH its
      *> qualifiers (ISO 13.18.38.3 SR3; 8.4.2.2.3 SR1). The element E
      *> holds two items named K, so KEY IS K OF B names B's K and only
      *> B's; the key used to be captured as the bare word K and looked
      *> up as the FIRST K under the table (A's), which refused this
      *> legal SEARCH ALL (COBOLNET1965) and would have keyed on A.
      *> B's K ascends 1 2 3 while A's K descends 9 8 7, so a search
      *> keyed on the wrong K finds nothing at all.
       PROGRAM-ID. PB1018-OCCURS-KEY-QUALIFIED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 ASCENDING KEY IS K OF B INDEXED BY IX.
             10 A.
                15 K PIC 9.
             10 B.
                15 K PIC 9.
       01 N PIC 9.
       PROCEDURE DIVISION.
       P0.
           MOVE 9 TO K OF A (1). MOVE 1 TO K OF B (1).
           MOVE 8 TO K OF A (2). MOVE 2 TO K OF B (2).
           MOVE 7 TO K OF A (3). MOVE 3 TO K OF B (3).
           SEARCH ALL E
               AT END DISPLAY "K OF B = 2 NOT-FOUND"
               WHEN K OF B (IX) = 2
                   SET N TO IX
                   DISPLAY "K OF B = 2 FOUND-AT " N
           END-SEARCH.
           SEARCH ALL E
               AT END DISPLAY "K OF B = 8 NOT-FOUND"
               WHEN K OF B (IX) = 8
                   SET N TO IX
                   DISPLAY "K OF B = 8 FOUND-AT " N
           END-SEARCH.
           STOP RUN.
