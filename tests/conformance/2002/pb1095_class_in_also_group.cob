      *> kb/Work PB1095 - ISO 12.3.7.4 GR7 k6 + GR12 a: a CLASS clause's numeric literal under IN names EVERY character
      *> at that ordinal of the coded character set, not only literal-1 of an ALSO group.
      *> GR7 k6 (cite.py --check 12.3.7.4 "the characters of the native character set specified by the value of
      *> literal-1 and literal-3 are assigned to the same ordinal position"): AL = "B" ALSO "A" ALSO "Z" puts B, A and
      *> Z at ordinal position 1 of AL's coded character set. GR12 a (cite.py --check 12.3.7.4 "the ordinal number of a
      *> character within the relevant native character set, or, when the IN phrase is specified, within the character
      *> set referenced by alphabet-name-4"): CLASS C1 IS 1 IN AL therefore consists of B, A and Z - and only for a
      *> SYMBOLIC CHARACTERS reference is "only literal-1 used" (the last sentence of k6; SB below is B alone).
      *> The singular "a character" of GR12 a leaves the reading open; the plural one is taken because the other would
      *> make k6's SYMBOLIC-CHARACTERS-only exception redundant (docs/CONFORMANCE.md DOC-A.1-186).
      *> Positions after the group are untouched: ordinal 2 is the next specified character, "C" (SECOND).
      *> What each line proves (a wrong answer on any one prints the other word):
      *>   B-IN-C1 / A-IN-C1 / Z-IN-C1 - all three group members are in the class (A and Z were dropped before).
      *>   Q-NOT-C1 - a character outside the group is not.
      *>   C-NOT-C1 / C-IN-C2 - ordinal 2 is C alone, so the neighbouring position is not swallowed by the group.
      *>   SB-IS-B - SYMBOLIC CHARACTERS SB IS 1 IN AL is the exception's own case: B only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1095GRP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" ALSO "A" ALSO "Z" "C"
           CLASS C1 IS 1 IN AL
           CLASS C2 IS 2 IN AL
           SYMBOLIC CHARACTERS SB IS 1 IN AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CA PIC X VALUE "A".
       01  CB PIC X VALUE "B".
       01  CZ PIC X VALUE "Z".
       01  CQ PIC X VALUE "Q".
       01  CC PIC X VALUE "C".
       01  SBV PIC X VALUE SB.
       PROCEDURE DIVISION.
           IF CB IS C1 DISPLAY "B-IN-C1" ELSE DISPLAY "B-NOT-C1" END-IF
           IF CA IS C1 DISPLAY "A-IN-C1" ELSE DISPLAY "A-NOT-C1" END-IF
           IF CZ IS C1 DISPLAY "Z-IN-C1" ELSE DISPLAY "Z-NOT-C1" END-IF
           IF CQ IS C1 DISPLAY "Q-IN-C1" ELSE DISPLAY "Q-NOT-C1" END-IF
           IF CC IS C1 DISPLAY "C-IN-C1" ELSE DISPLAY "C-NOT-C1" END-IF
           IF CC IS C2 DISPLAY "C-IN-C2" ELSE DISPLAY "C-NOT-C2" END-IF
           IF CA IS C2 DISPLAY "A-IN-C2" ELSE DISPLAY "A-NOT-C2" END-IF
           IF SBV = "B" DISPLAY "SB-IS-B" ELSE DISPLAY "SB-NOT-B" END-IF
           STOP RUN.
