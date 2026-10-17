      *>--- conformance-case
      *> id: l1_search_all_gr5_preconditions
      *> rules: [GR-14.9.37.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.4 GR5 SEARCH statement, FORMAT 2 (all) — "At the
      *> start of the execution of a SEARCH statement with the ALL
      *> phrase specified, the following conditions shall be true:
      *> a) The contents of each key data item referenced in the WHEN
      *> phrase shall be sequenced in the table according to the
      *> ASCENDING or DESCENDING phrase associated with that key data
      *> item. ... b) If identifier-1 is subordinate to one or more
      *> data description entries that contain an OCCURS clause, the
      *> evaluation of the conditions within a WHEN phrase that
      *> reference a key data item subordinate to identifier-1 shall
      *> result in the same occurrence number for any subscripts
      *> associated with a given level of the superordinate tables."
      *>   python scripts/spec/cite.py --check 14.9.37.4 "The contents
      *>   of each key data item referenced in the WHEN phrase shall be
      *>   sequenced in the table according to the ASCENDING or
      *>   DESCENDING phrase associated with that key data item."
      *>   -> OK  §14.9.37.4 5) a)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.37.4 "If
      *>   identifier-1 is subordinate to one or more data description
      *>   entries that contain an OCCURS clause, the evaluation of the
      *>   conditions within a WHEN phrase that reference a key data
      *>   item subordinate to identifier-1 shall result in the same
      *>   occurrence number for any subscripts associated with a given
      *>   level of the superordinate tables."
      *>   -> OK  §14.9.37.4 5) b)  (General rules)
      *>
      *> ⛔ WHAT IS OBSERVABLE ABOUT A RULE ADDRESSED TO THE PROGRAM.
      *> GR5 states conditions the PROGRAM shall meet, and GR6 defines
      *> what happens when they are not met — so the implementation
      *> obligation GR5 carries is the one on its SATISFIED side: when
      *> both conditions hold, the undefinedness of GR6 does not apply
      *> and the search shall find the element. Annex A.2 item 49 says
      *> exactly this from the other direction, listing the SEARCH ALL
      *> outcome as undefined "unless" GR5's b) and c) hold. With a
      *> unique match GR7 does not apply either, so the outcome is
      *> fully determined: GR9 — "If all the conditions are satisfied,
      *> the search operation is successful and execution proceeds as
      *> indicated in General rule 1a." — and GR1a — "the index being
      *> varied by the search operation remains set at the occurrence
      *> number that caused a WHEN condition to be satisfied".
      *>
      *> BOTH LEGS OF GR5a ARE WRITTEN. WS-E declares ASCENDING KEY IS
      *> WS-K1 WS-K2 and holds keys in that order; WS-DE declares
      *> DESCENDING KEY IS WS-DK and holds 9, 7, 5, 3, 1. Each table's
      *> contents are sequenced according to ITS OWN phrase, which is
      *> what GR5a demands, and the two directions are searched
      *> separately.
      *>   ⛔ GR5a IS PER KEY, NOT MERELY COMPOSITE — do not "simplify"
      *> the literals below. GR5a reads "the contents of EACH key data
      *> item ... shall be sequenced ... according to the ASCENDING or
      *> DESCENDING phrase associated with THAT key data item", and
      *> §13.18.38.4 GR3 says it the same way for the OCCURS side: "the
      *> contents of the data items referenced by data-name-2 shall be
      *> in ascending order if the ASCENDING phrase is specified".
      *>   python scripts/spec/cite.py --check 13.18.38.4 "the contents
      *>   of the data items referenced by data-name-2 shall be in
      *>   ascending order if the ASCENDING phrase is specified"
      *>   -> OK  §13.18.38.4 3)  (General rules)
      *> WS-K2 is referenced in a WHEN condition, so its OWN column has
      *> to ascend, not just the composite (K1,K2). The data below is
      *> laid out so BOTH readings hold at once — outer 1 carries
      *> (1,1)(2,7)(3,8)(4,9) and outer 2 carries (1,3)(2,5)(2,7)(3,9),
      *> each of K1 and K2 ascending down its own column. If K2 were
      *> allowed to fall (e.g. a trailing (3,1)) the fixture would sit
      *> on GR6's undefined branch — the branch A.2 item 49 resolves to
      *> GR-14.9.37.4-6 — and the expected index below would not be
      *> derivable at all.
      *>
      *> ⛔ GR5b IS THE BRANCH NO EXISTING GOLDEN REACHES. WS-E is
      *> subordinate to WS-O, which has an OCCURS clause, so
      *> §14.9.37.3 SR3 ("Identifier-1 may be contained within one or
      *> more other tables, for which the subscripting is still
      *> required") makes SEARCH ALL WS-E (OX) the correct spelling and
      *> §14.9.37.4 GR1 puts the superordinate occurrence in the
      *> programmer's hands: "The subscript that is used to determine
      *> the occurrence of each superordinate table to search is
      *> specified by the user in the WHEN phrases." Both WHEN
      *> conditions here name OX — the same occurrence number at the
      *> outer level — so GR5b is satisfied NON-VACUOUSLY: two
      *> conditions, two subscripts, equal.
      *>   The data makes the leg distinguishable. OX is set to 2, and
      *> the pair (2,7) sits at INNER occurrence 3 of outer occurrence
      *> 2 but at inner occurrence 2 of outer occurrence 1. An
      *> implementation that ignored the superordinate subscript and
      *> searched occurrence 1 would print G5B-HIT=2 and G5B-D=B.
      *>   OX itself must come back unchanged: GR1 modifies "only the
      *> setting of an index associated with identifier-1", and GR8
      *> adds "Any other indexes associated with identifier-1 remain
      *> unchanged by the search operation."
      *>
      *> EXPECTED, DERIVED:
      *>   G5B-HIT=3   the unique element of WS-E (2, *) whose K1 is 2
      *>               and whose K2 is 7 is occurrence 3 (GR1a).
      *>   G5B-D=R     that element's data character, confirming the
      *>               index identifies the intended row and not a
      *>               same-keyed row of another outer occurrence.
      *>   G5B-OX=2    the superordinate index is untouched (GR8).
      *>   G5A-HIT=4   in the DESCENDING table 9,7,5,3,1 the element
      *>               whose key is 3 is occurrence 4.
      *>   G5A-V=Y     that element's data character.
      *> SET IX/DX TO 1 before each search is harmless under GR9 ("The
      *> initial setting of the search index is ignored"); it is
      *> written so the fixture cannot depend on an unset index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRAG5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-OUT.
          05 WS-O OCCURS 2 TIMES INDEXED BY OX.
             10 WS-E OCCURS 4 TIMES
                     ASCENDING KEY IS WS-K1 WS-K2
                     INDEXED BY IX.
                15 WS-K1 PIC 9.
                15 WS-K2 PIC 9.
                15 WS-D  PIC X.
       01 WS-DT.
          05 WS-DE OCCURS 5 TIMES
                   DESCENDING KEY IS WS-DK
                   INDEXED BY DX.
             10 WS-DK PIC 9.
             10 WS-DV PIC X.
       01 R1 PIC 9.
       01 R2 PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "11A" TO WS-E (1, 1).
           MOVE "27B" TO WS-E (1, 2).
           MOVE "38C" TO WS-E (1, 3).
           MOVE "49D" TO WS-E (1, 4).
           MOVE "13P" TO WS-E (2, 1).
           MOVE "25Q" TO WS-E (2, 2).
           MOVE "27R" TO WS-E (2, 3).
           MOVE "39S" TO WS-E (2, 4).
           MOVE "9V" TO WS-DE (1).
           MOVE "7W" TO WS-DE (2).
           MOVE "5X" TO WS-DE (3).
           MOVE "3Y" TO WS-DE (4).
           MOVE "1Z" TO WS-DE (5).
           SET OX TO 2.
           SET IX TO 1.
           SEARCH ALL WS-E (OX)
               AT END DISPLAY "G5B=ATEND"
               WHEN WS-K1 (OX, IX) = 2
                AND WS-K2 (OX, IX) = 7
                   SET R1 TO IX
                   DISPLAY "G5B-HIT=" R1
                   DISPLAY "G5B-D=" WS-D (OX, IX)
           END-SEARCH.
           SET R2 TO OX.
           DISPLAY "G5B-OX=" R2.
           SET DX TO 1.
           SEARCH ALL WS-DE
               AT END DISPLAY "G5A=ATEND"
               WHEN WS-DK (DX) = 3
                   SET R1 TO DX
                   DISPLAY "G5A-HIT=" R1
                   DISPLAY "G5A-V=" WS-DV (DX)
           END-SEARCH.
           STOP RUN.
