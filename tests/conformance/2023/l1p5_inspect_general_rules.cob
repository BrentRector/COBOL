      *> ISO §14.9.22.4 GR1 GR5 GR7 GR8a1 GR9c2 GR11 GR16 — INSPECT
      *> general rules over fixed inputs; every DISPLAY line is keyed
      *> to the rule it pins. Derivations (spec text is the oracle):
      *>
      *> GR1 "For purposes of determining its length, identifier-1 is
      *>   treated as a sending data item." G is an occurs-depending
      *>   group whose object GC lies INSIDE the group, so 13.18.38.4
      *>   GR8b gives a SENDING operand the current extent but a
      *>   RECEIVING operand the maximum length. Storage "5AAAAA",
      *>   then GC=2: the sending image is "2AA".
      *>   G1  TALLYING FOR CHARACTERS over "2AA"          -> 03
      *>   G2  REPLACING ALL "A" BY "B" over "2AA"         -> "2BB"
      *>   G3  GC=1, CONVERTING "AB" TO "yz" over "1B"     -> "1z"
      *>   G2/G3 are shown at the count they were inspected with:
      *>   13.18.38.4 GR7 leaves occurrences beyond the count
      *>   undefined, so a raised count displays nothing derivable.
      *>   G4  refill "5AAAAA", GC=2, BACKWARD REPLACING FIRST "A"
      *>       BY "B": GR17d replaces the rightmost "A" of the
      *>       3-character sending extent (position 3)      -> "2AB"
      *>       (a maximum-length extent would hit position 6 and
      *>       show "2AA")
      *> GR8a1 "If neither LEADING nor FIRST is specified" an ALL
      *>   operand matches wherever the characters are equal.
      *>   A1  "XAXAAX" ALL "A": positions 2,4,5, none leading -> 03
      *>   A2  same, REPLACING ALL "A" BY "b"        -> "XbXbbX"
      *> GR7 "each properly matched occurrence of literal-1 is tallied
      *>   (format 1) or replaced by literal-3 (format 2)".
      *>   P1  "ABCABCAB" ALL "AB": matches at 1,4,7          -> 03
      *>   P2  same, REPLACING ALL "AB" BY "xy"    -> "xyCxyCxy"
      *> GR5 identifier-3..7 act exactly as literal-1..5 with their
      *>   FULL content (a trailing space is a character).
      *>   I1  Q "A A AA", PAT "A " (identifier-3): matches at 1 and 3;
      *>       at 5 "AA" differs; at 6 one character remains     -> 02
      *>   I2  "ABABB " CHARACTERS BEFORE INITIAL DL4 = "B "
      *>       (identifier-4): first "B " at 5-6, region 1-4      -> 04
      *>   I3  Q REPLACING ALL PAT BY REP = "* " (identifier-5)
      *>                                               -> "* * AA"
      *>   I4  Q CONVERTING FR "A " TO TT "a_" (identifier-6/-7)
      *>                                               -> "a_a_aa"
      *> GR11 identifier-2 is not initialized by INSPECT.
      *>   C1  N5 VALUE 5, "AAB" ALL "A" adds 2                -> 07
      *> GR9c2 AFTER (no BACKWARD): eligible from the position right
      *>   of the rightmost character of the first literal-2; none
      *>   found -> "never eligible".
      *>   F1  "AB12CD12EF" CHARACTERS AFTER "12": first "12" at 3-4,
      *>       region 5-10                                        -> 06
      *>   F2  same, ALL "12" AFTER "12": only 7-8 is in region   -> 01
      *>   F3  same, CHARACTERS AFTER "Z": no "Z", never eligible -> 00
      *>   F4  "AAXA" NA FOR ALL "A" AFTER "X", NB FOR ALL "A": an
      *>       ineligible operand does not match, so NB takes 1-2 and
      *>       NA takes 4                                      -> 01 02
      *>   F5  "ABCXABCX" REPLACING ALL "A" BY "*" AFTER "X"
      *>                                             -> "ABCX*BCX"
      *> GR16 ALL/FIRST/LEADING carry to the following operands until
      *>   another one appears (with GR17d for FIRST).
      *>   T1  "ABAB" FIRST "A" BY "X" "B" BY "Y": B is FIRST too, so
      *>       only its leftmost occurrence changes         -> "XYAB"
      *>   T2  "ABCABC" FIRST "A" BY "X" ALL "B" BY "Y" "C" BY "Z": C
      *>       takes ALL (not FIRST); the second A is not the first
      *>                                                -> "XYZAYZ"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1P5INS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GC PIC 9.
          05 GE PIC X OCCURS 1 TO 5 DEPENDING ON GC.
       01 I     PIC 9.
       01 N     PIC 99.
       01 NA    PIC 99.
       01 NB    PIC 99.
       01 N5    PIC 99 VALUE 5.
       01 X1    PIC X(6).
       01 X2    PIC X(8).
       01 X3    PIC X(3).
       01 X4    PIC X(10).
       01 X5    PIC X(4).
       01 X6    PIC X(8).
       01 Q     PIC X(6).
       01 W6    PIC X(6).
       01 PAT   PIC X(2) VALUE "A ".
       01 DL4   PIC X(2) VALUE "B ".
       01 REP   PIC X(2) VALUE "* ".
       01 FR    PIC X(2) VALUE "A ".
       01 TT    PIC X(2) VALUE "a_".
       PROCEDURE DIVISION.
       MAIN.
      *> GR1
           MOVE 5 TO GC.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               MOVE "A" TO GE(I)
           END-PERFORM.
           MOVE 2 TO GC.
           MOVE 0 TO N.
           INSPECT G TALLYING N FOR CHARACTERS.
           DISPLAY "G1=" N.
           INSPECT G REPLACING ALL "A" BY "B".
           DISPLAY "G2=[" G "]".
           MOVE 1 TO GC.
           INSPECT G CONVERTING "AB" TO "yz".
           DISPLAY "G3=[" G "]".
           MOVE 5 TO GC.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               MOVE "A" TO GE(I)
           END-PERFORM.
           MOVE 2 TO GC.
           INSPECT BACKWARD G REPLACING FIRST "A" BY "B".
           DISPLAY "G4=[" G "]".
      *> GR8a1
           MOVE "XAXAAX" TO X1.
           MOVE 0 TO N.
           INSPECT X1 TALLYING N FOR ALL "A".
           DISPLAY "A1=" N.
           INSPECT X1 REPLACING ALL "A" BY "b".
           DISPLAY "A2=[" X1 "]".
      *> GR7
           MOVE "ABCABCAB" TO X2.
           MOVE 0 TO N.
           INSPECT X2 TALLYING N FOR ALL "AB".
           DISPLAY "P1=" N.
           INSPECT X2 REPLACING ALL "AB" BY "xy".
           DISPLAY "P2=[" X2 "]".
      *> GR5
           MOVE "A A AA" TO Q.
           MOVE 0 TO N.
           INSPECT Q TALLYING N FOR ALL PAT.
           DISPLAY "I1=" N.
           MOVE "ABABB " TO W6.
           MOVE 0 TO N.
           INSPECT W6 TALLYING N FOR CHARACTERS BEFORE INITIAL DL4.
           DISPLAY "I2=" N.
           INSPECT Q REPLACING ALL PAT BY REP.
           DISPLAY "I3=[" Q "]".
           MOVE "A A AA" TO Q.
           INSPECT Q CONVERTING FR TO TT.
           DISPLAY "I4=[" Q "]".
      *> GR11
           MOVE "AAB" TO X3.
           INSPECT X3 TALLYING N5 FOR ALL "A".
           DISPLAY "C1=" N5.
      *> GR9c2
           MOVE "AB12CD12EF" TO X4.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR CHARACTERS AFTER INITIAL "12".
           DISPLAY "F1=" N.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR ALL "12" AFTER INITIAL "12".
           DISPLAY "F2=" N.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR CHARACTERS AFTER INITIAL "Z".
           DISPLAY "F3=" N.
           MOVE "AAXA" TO X5.
           MOVE 0 TO NA.
           MOVE 0 TO NB.
           INSPECT X5 TALLYING NA FOR ALL "A" AFTER INITIAL "X"
                               NB FOR ALL "A".
           DISPLAY "F4=" NA " " NB.
           MOVE "ABCXABCX" TO X6.
           INSPECT X6 REPLACING ALL "A" BY "*" AFTER INITIAL "X".
           DISPLAY "F5=[" X6 "]".
      *> GR16
           MOVE "ABAB" TO X5.
           INSPECT X5 REPLACING FIRST "A" BY "X" "B" BY "Y".
           DISPLAY "T1=[" X5 "]".
           MOVE "ABCABC" TO W6.
           INSPECT W6 REPLACING FIRST "A" BY "X"
                                ALL "B" BY "Y" "C" BY "Z".
           DISPLAY "T2=[" W6 "]".
           STOP RUN.
       END PROGRAM L1P5INS.
