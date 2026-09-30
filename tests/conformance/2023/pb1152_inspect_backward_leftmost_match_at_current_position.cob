      *> ISO §14.9.22.4 GR8 NOTE 2 + Annex D.25 EXAMPLE 5 — INSPECT BACKWARD: the keyword gives the
      *> direction of the SCAN, never of the MATCHING (kb/Work PB1152).
      *> NOTE 2 "The keyword BACKWARD specifies only the direction of the scan, not the direction of the matching. Matching always
      *>   takes place starting at the leftmost character at the current character position" -> OK  §14.9.22.4 8)  (General rules)
      *> GR8c "if BACKWARD is specified, the character position ... immediately to the left of the leftmost character position that
      *>   participated in the match is now considered to be the leftmost character position" -> OK  §14.9.22.4 8)  (General rules)
      *> So the current position walks right-to-left and each literal is compared LEFT-to-right from it: a two-character
      *> literal cannot match at the rightmost position (only one character is there), and after a hit the scan resumes ONE
      *> position to the left of the hit's leftmost character, so a later comparison can overlap the hit.
      *> E1 EXAMPLE 5 row 1, ITEM = ABABBCAB (A0 B1 A2 B3 B4 C5 A6 B7):
      *>    COUNT-0 ALL "AB" BEFORE "BC": the rightmost "BC" is at 4..5, region = 6..7.
      *>    COUNT-1 LEADING "B": first cycle is position 7 ('B' matches -> 1); COUNT-2 CHARACTERS AFTER "A" BEFORE "C":
      *>    the rightmost "A" is 6, region 0..5, and the rightmost "C" inside it is 5, so region 6..5 = empty -> 0.
      *>    Shared cycle: at 7 ALL "AB" cannot fit, LEADING "B" hits (1); the scan resumes at 6 where "AB" (6..7) hits (1).
      *>    Annex D.25 prints 1 / 1 / 0 (the reversed-text scan gave 1 / 0 / 0). REPLACING ALL "AB" BY "XY" BEFORE "BC" ->
      *>    ABABBCXY (the LEADING "B" AFTER "D" phrase is never eligible: no "D").
      *> E2 EXAMPLE 5 row 2, ITEM = ABDBABC: COUNT-0 0 (BC at 5..6, region 7..6 empty), COUNT-1 0 (position 6 is 'C'),
      *>    COUNT-2 = 4 (AFTER "A": rightmost A is 4, region 0..3; the only "C" is outside it, so BEFORE is as if absent).
      *>    REPLACING: ALL "AB" region empty; LEADING "B" BY "V" AFTER "D": D at 2, region 0..1, first eligible cycle is 1,
      *>    'B' hits -> V, then 'A' breaks the run -> AVDBABC. (The annex prints "AZDBABC": no phrase of the example produces a
      *>    'Z', a printed erratum — the expected value is derived from the rule text.)
      *> E3 EXAMPLE 5 row 3, ITEM = BCABCABD: COUNT-0 1 (BC at 3..4, region 5..7, "AB" at 5..6), COUNT-1 0 ('D' at 7).
      *>    COUNT-2: AFTER "A" region 0..4, the rightmost "C" inside it is 4, so BEFORE leaves 5..4 = empty -> 0 (the annex
      *>    prints 4, which no reading of GR9 derives — a second printed erratum; adjudicated against the rule text).
      *>    REPLACING ALL "AB" BY "XY" BEFORE "BC" / LEADING "B" BY "V" AFTER "D": the LEADING run hits B at 6 first (V),
      *>    then "AB" at 5..6 hits and, reading the ORIGINAL text, overwrites it -> BCABCXYD, exactly as the annex prints.
      *> E4 ALL "AB" then ALL "B" over "AB": at 1 "AB" cannot fit and "B" hits, at 0 "AB" hits -> 1 and 1 (a pattern-reversing
      *>    scan matched "AB" at the rightmost pair and starved "B": 1 and 0).
      *> E5 FIRST "AB" BY "**" over "ABXAB": the rightmost occurrence encountered (GR17d) -> ABX**.
      *> E6 CONVERTING "AB" TO "XY" AFTER "AB" over "ABCAB": AFTER's first occurrence encountered is the rightmost (3..4);
      *>    region 0..2 -> XYCAB.
      *> E7 "AAA" ALL "AA" BY "XY": at 2 "AA" cannot fit; at 1 it hits (1..2); the scan resumes at 0 where "AA" (0..1) hits
      *>    again — both read the original, the later write wins position 1 -> XYY (forward gives XYA).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1152BK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM1 PIC X(8) VALUE "ABABBCAB".
       01 ITEM2 PIC X(7) VALUE "ABDBABC".
       01 ITEM3 PIC X(8) VALUE "BCABCABD".
       01 W2 PIC X(2) VALUE "AB".
       01 W5 PIC X(5).
       01 W3 PIC X(3) VALUE "AAA".
       01 C0 PIC 9 VALUE 0.
       01 C1 PIC 9 VALUE 0.
       01 C2 PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT BACKWARD ITEM1 TALLYING
               C0 FOR ALL "AB" BEFORE "BC"
               C1 FOR LEADING "B"
               C2 FOR CHARACTERS AFTER "A" BEFORE "C".
           DISPLAY "E1T=" C0 C1 C2.
           INSPECT BACKWARD ITEM1 REPLACING
               ALL "AB" BY "XY" BEFORE "BC"
               LEADING "B" BY "V" AFTER "D".
           DISPLAY "E1R=" ITEM1.
           MOVE 0 TO C0 C1 C2.
           INSPECT BACKWARD ITEM2 TALLYING
               C0 FOR ALL "AB" BEFORE "BC"
               C1 FOR LEADING "B"
               C2 FOR CHARACTERS AFTER "A" BEFORE "C".
           DISPLAY "E2T=" C0 C1 C2.
           INSPECT BACKWARD ITEM2 REPLACING
               ALL "AB" BY "XY" BEFORE "BC"
               LEADING "B" BY "V" AFTER "D".
           DISPLAY "E2R=" ITEM2.
           MOVE 0 TO C0 C1 C2.
           INSPECT BACKWARD ITEM3 TALLYING
               C0 FOR ALL "AB" BEFORE "BC"
               C1 FOR LEADING "B"
               C2 FOR CHARACTERS AFTER "A" BEFORE "C".
           DISPLAY "E3T=" C0 C1 C2.
           INSPECT BACKWARD ITEM3 REPLACING
               ALL "AB" BY "XY" BEFORE "BC"
               LEADING "B" BY "V" AFTER "D".
           DISPLAY "E3R=" ITEM3.
           MOVE 0 TO C0 C1.
           INSPECT BACKWARD W2 TALLYING C0 FOR ALL "AB" C1 FOR ALL "B".
           DISPLAY "E4=" C0 C1.
           MOVE "ABXAB" TO W5.
           INSPECT BACKWARD W5 REPLACING FIRST "AB" BY "**".
           DISPLAY "E5=" W5.
           MOVE "ABCAB" TO W5.
           INSPECT BACKWARD W5 CONVERTING "AB" TO "XY" AFTER "AB".
           DISPLAY "E6=" W5.
           INSPECT BACKWARD W3 REPLACING ALL "AA" BY "XY".
           DISPLAY "E7=" W3.
           STOP RUN.
