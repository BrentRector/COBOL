      *> ISO §14.9.22.4 GR9 c)1. — INSPECT BACKWARD with AFTER; GR8 back
      *> GR9c1 "from the character position immediately to the left of
      *>   the leftmost character position of the first occurrence of
      *>   literal-2" -> OK  §14.9.22.4 9)  (General rules)
      *> GR9c "is never eligible to participate in the comparison
      *>   operation" -> OK  §14.9.22.4 9)  (General rules)
      *> GR8a "The operands of the TALLYING or REPLACING phrase are
      *>   considered in the order they are specified"
      *>   -> OK  §14.9.22.4 8)  (General rules) ("or, if BACKWARD is
      *>   specified, the rightmost character position"; 8c "immediately
      *>   to the left of the leftmost character position that
      *>   participated in the match"; NOTE 2 matching is left-to-right)
      *> GR8a3 "If the FIRST adjective applies to literal-1 and
      *>   literal-1 is the first occurrence"
      *>   -> OK  §14.9.22.4 8)  (General rules)
      *> GR17d "the leftmost occurrence of literal-1, or, if BACKWARD is
      *>   specified, the rightmost occurrence of literal-1"
      *>   -> OK  §14.9.22.4 17)  (General rules)
      *> BACKWARD is COBOL-2023 syntax, hence this 2023 golden.
      *> Derivations over T = "A12C21D12EF" (positions 1..11):
      *> K1 CHARACTERS AFTER "12": scanning from the right the first
      *>    "12" is at 8..9; region = left of its LEFTMOST char, 1..7
      *>    -> 07 (a forward scan would give 08).
      *> K2 CHARACTERS AFTER "C": C at 4 -> region 1..3 -> 03.
      *> K3 CHARACTERS AFTER "Z": never eligible -> 00.
      *> K4 ALL "12", no phrase: matching is left-to-right (NOTE 2), so
      *>    "21" at 5..6 is no match; "12" at 8..9 and 2..3 -> 02.
      *> K5 "ABABABXY" FIRST "AB" BY "**": the rightmost "AB" (5..6)
      *>    -> ABAB**XY.
      *> K6 "AAA" ALL "AA" BY "XY" (1-based positions): NOTE 2 compares
      *>    each literal LEFT-to-right from the current position, so at
      *>    3 "AA" cannot fit and at 2 it matches (2..3); 8c resumes
      *>    one position left of the match's leftmost character, at 1,
      *>    where "AA" (1..2) matches again; both read the original and
      *>    the later write wins position 2 -> XYY (forward gives XYA).
      *>    kb/Work PB1152 - the pattern-reversing scan gave AXY.
      *> K7 "ABCABC" ALL "A" BY "X" AFTER "B": first "B" from the
      *>    right is at 5 -> region 1..4 -> XBCXBC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C14D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(11) VALUE "A12C21D12EF".
       01 W3 PIC X(3).
       01 W6 PIC X(6).
       01 W8 PIC X(8).
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT BACKWARD T TALLYING N
               FOR CHARACTERS AFTER INITIAL "12".
           DISPLAY "K1=" N.
           MOVE 0 TO N.
           INSPECT BACKWARD T TALLYING N
               FOR CHARACTERS AFTER INITIAL "C".
           DISPLAY "K2=" N.
           MOVE 0 TO N.
           INSPECT BACKWARD T TALLYING N
               FOR CHARACTERS AFTER INITIAL "Z".
           DISPLAY "K3=" N.
           MOVE 0 TO N.
           INSPECT BACKWARD T TALLYING N FOR ALL "12".
           DISPLAY "K4=" N.
           MOVE "ABABABXY" TO W8.
           INSPECT BACKWARD W8 REPLACING FIRST "AB" BY "**".
           DISPLAY "K5=" W8.
           MOVE "AAA" TO W3.
           INSPECT BACKWARD W3 REPLACING ALL "AA" BY "XY".
           DISPLAY "K6=" W3.
           MOVE "ABCABC" TO W6.
           INSPECT BACKWARD W6 REPLACING ALL "A" BY "X"
               AFTER INITIAL "B".
           DISPLAY "K7=" W6.
           STOP RUN.
