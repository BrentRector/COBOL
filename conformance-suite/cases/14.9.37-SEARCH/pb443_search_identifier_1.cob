      *>--- conformance-case
      *> id: pb443_search_identifier_1
      *> rules: [SR-14.9.37.3-1, SR-14.9.37.3-2, SR-14.9.37.3-3, SR-14.9.37.3-9, SR-14.9.37.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB443 - the LEGAL half of SEARCH's identifier-1 rules (ISO 14.9.37.3 SR1-SR3, ALL FORMATS),
      *> every case one the rule that refuses its siblings ADMITS. The expected output is derived from the
      *> rules, never from a run:
      *>   SEARCH E IN G2       - 8.4.2.2 says qualification is what establishes uniqueness, and 14.9.37.4
      *>                          GR1 says the statement varies "the first or only index associated with
      *>                          identifier-1", so identifier-1 is G2's E and the index varied is IX2.
      *>                          G1 is declared FIRST and holds NO matching key, so a base-word lookup that
      *>                          broke the tie by declaration order would scan G1 and reach AT END.
      *>   SEARCH E OF G2       - OF and IN are the same qualifier connective (8.4.2.2.2 Format 1), so this
      *>                          spelling must give the identical answer.
      *>   SEARCH ALL E IN G2   - SR1-SR3 are ALL FORMATS; Format 2 prints the same identifier-1 operand.
      *>                          14.9.37.4 GR9 leaves the technique to the implementor, so only the FOUND
      *>                          occurrence is asserted, which GR1a fixes: the index is left at the
      *>                          occurrence whose WHEN was satisfied.
      *>   SEARCH INNER (OX)    - SR2 forbids a subscript at the SEARCHED level, and ONLY there, so a
      *>                          superordinate one is admitted: a screen banning subscripts outright would
      *>                          reject this. OX is 2, and GR1's "the subscript that is used to determine the
      *>                          occurrence of each superordinate table to search is specified by the user in
      *>                          the WHEN phrases" makes the searched row the SECOND, K values 21..24.
      *>   SEARCH INNER (bare)  - THE SAME NESTED TABLE WITH NO SUBSCRIPT AT ALL, and it is equally legal.
      *>                          SR3 ("identifier-1 may be contained within one or more other tables, for
      *>                          which the subscripting is still required") reads like a demand on
      *>                          identifier-1, and GR1 says where that subscripting actually is: in the WHEN
      *>                          phrases. So the two spellings must give the SAME answer - written here with
      *>                          OX still 2, so the row searched is again the second. The CCVS suite writes
      *>                          only this form (NC233A: SEARCH ALL GRP2-ENTRY ... WHEN SEC (IDX-1, IDX-2)).
      *>   SEARCH ALL ... CN OF K2 - 8.4.2.2 Format 2 decides which level-88 a qualified condition-name
      *>                          names. The KEY phrase is "ASCENDING KEY IS K2 K1", so K2 is the MOST
      *>                          significant key (13.18.38.4 GR3) and referencing it alone satisfies SR11
      *>                          ("all preceding data-names ... shall also be referenced" - there are
      *>                          none). CN OF K2 has VALUE 07, and the table is sequenced on K2, so the
      *>                          match is the occurrence whose K2 is 07.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443ID1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> The DECOY is declared first: a first-match lookup on the base word 'E' resolves here.
       01  G1.
           05  E OCCURS 4 TIMES ASCENDING KEY IS K INDEXED BY IX1.
               10  K PIC 99.
       01  G2.
           05  E OCCURS 4 TIMES ASCENDING KEY IS K INDEXED BY IX2.
               10  K PIC 99.
       01  NEST.
           05  OUTER OCCURS 2 TIMES INDEXED BY OX.
               10  INNER OCCURS 4 TIMES INDEXED BY IX3.
                   15  NK PIC 99.
       01  CTAB.
           05  CE OCCURS 4 TIMES
               ASCENDING KEY IS K2 K1 INDEXED BY IX4.
               10  K1 PIC 99.
                   88  CN VALUE 03.
               10  K2 PIC 99.
                   88  CN VALUE 07.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 91 TO K IN G1 (1).
           MOVE 92 TO K IN G1 (2).
           MOVE 93 TO K IN G1 (3).
           MOVE 94 TO K IN G1 (4).
           MOVE 11 TO K IN G2 (1).
           MOVE 12 TO K IN G2 (2).
           MOVE 13 TO K IN G2 (3).
           MOVE 14 TO K IN G2 (4).
           MOVE 11 TO NK (1 1).
           MOVE 12 TO NK (1 2).
           MOVE 13 TO NK (1 3).
           MOVE 14 TO NK (1 4).
           MOVE 21 TO NK (2 1).
           MOVE 22 TO NK (2 2).
           MOVE 23 TO NK (2 3).
           MOVE 24 TO NK (2 4).
           MOVE 09 TO K1 (1). MOVE 05 TO K2 (1).
           MOVE 08 TO K1 (2). MOVE 06 TO K2 (2).
           MOVE 07 TO K1 (3). MOVE 07 TO K2 (3).
           MOVE 06 TO K1 (4). MOVE 08 TO K2 (4).
           SET IX1 TO 1.
           SET IX2 TO 1.
           SEARCH E IN G2
               AT END DISPLAY "SERIAL-IN NONE"
               WHEN K IN G2 (IX2) = 13
                   DISPLAY "SERIAL-IN " K IN G2 (IX2)
           END-SEARCH.
           SET IX1 TO 1.
           SET IX2 TO 1.
           SEARCH E OF G2
               AT END DISPLAY "SERIAL-OF NONE"
               WHEN K OF G2 (IX2) = 13
                   DISPLAY "SERIAL-OF " K OF G2 (IX2)
           END-SEARCH.
           SEARCH ALL E IN G2
               AT END DISPLAY "ALL-IN NONE"
               WHEN K IN G2 (IX2) = 12
                   DISPLAY "ALL-IN " K IN G2 (IX2)
           END-SEARCH.
           SET OX TO 2.
           SET IX3 TO 1.
           SEARCH INNER (OX)
               AT END DISPLAY "NESTED NONE"
               WHEN NK (OX IX3) = 23
                   DISPLAY "NESTED " NK (OX IX3)
           END-SEARCH.
           SET IX3 TO 1.
           SEARCH INNER
               AT END DISPLAY "NESTBARE NONE"
               WHEN NK (OX IX3) = 23
                   DISPLAY "NESTBARE " NK (OX IX3)
           END-SEARCH.
           SEARCH ALL CE
               AT END DISPLAY "COND NONE"
               WHEN CN OF K2 (IX4)
                   DISPLAY "COND " K1 (IX4) " " K2 (IX4)
           END-SEARCH.
           STOP RUN.
