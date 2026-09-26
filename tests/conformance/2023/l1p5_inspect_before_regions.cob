      *> ISO §14.9.22.4 GR9 a) b) — INSPECT eligibility regions: no
      *> phrase, a zero-length identifier-4, and the BEFORE phrase.
      *> Derivations (spec text is the oracle):
      *> GR9a "If neither the BEFORE nor AFTER phrase is specified or
      *>   identifier-4 references a zero-length item" the operand
      *>   participates over the whole item, first eligible at the
      *>   leftmost (BACKWARD: rightmost) character position.
      *>   R1  "ABCDE" CHARACTERS, no phrase                    -> 05
      *>   R2  same, BEFORE INITIAL DZ where DZ is a DYNAMIC LENGTH
      *>       item at length zero (8.5.4 item 4)               -> 05
      *>   R3  same, AFTER INITIAL DZ: GR9a governs, not GR9c's
      *>       "never eligible"                                 -> 05
      *> GR9b BEFORE: eligible "up to, but not including, the first
      *>   occurrence encountered of literal-2", that position fixed
      *>   "before the first cycle"; no occurrence -> "as though the
      *>   BEFORE phrase had not been specified"; an ineligible
      *>   operand "is considered not to match".
      *>   R4  "ABCXDXE" CHARACTERS BEFORE "X": first X at 4    -> 03
      *>   R5  same, BEFORE "Z": no Z, the whole 7 characters   -> 07
      *>   R6  "AAXA" REPLACING ALL "A" BY "X" BEFORE "X": the first
      *>       X is at 3, fixed before position 1 is replaced, so the
      *>       region stays 1-2                             -> "XXXA"
      *>   R7  "AAXAA" NA FOR ALL "A" BEFORE "X", NB FOR ALL "A": NA
      *>       takes 1-2; at 4-5 NA's operand is ineligible, so NB
      *>       takes them                                    -> 02 02
      *>   R8  NOTE 1 of GR3: BACKWARD "A12C21D12EF" CHARACTERS
      *>       BEFORE "12": first eligible at 11, first "12" met from
      *>       the right is 8-9, region 10-11                   -> 02
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1P5RGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DZ    PIC X DYNAMIC LENGTH LIMIT IS 5.
       01 N     PIC 99.
       01 NA    PIC 99.
       01 NB    PIC 99.
       01 X1    PIC X(5)  VALUE "ABCDE".
       01 X2    PIC X(7)  VALUE "ABCXDXE".
       01 X3    PIC X(4)  VALUE "AAXA".
       01 X4    PIC X(5)  VALUE "AAXAA".
       01 X5    PIC X(11) VALUE "A12C21D12EF".
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO N.
           INSPECT X1 TALLYING N FOR CHARACTERS.
           DISPLAY "R1=" N.
           MOVE "" TO DZ.
           MOVE 0 TO N.
           INSPECT X1 TALLYING N FOR CHARACTERS BEFORE INITIAL DZ.
           DISPLAY "R2=" N.
           MOVE 0 TO N.
           INSPECT X1 TALLYING N FOR CHARACTERS AFTER INITIAL DZ.
           DISPLAY "R3=" N.
           MOVE 0 TO N.
           INSPECT X2 TALLYING N FOR CHARACTERS BEFORE INITIAL "X".
           DISPLAY "R4=" N.
           MOVE 0 TO N.
           INSPECT X2 TALLYING N FOR CHARACTERS BEFORE INITIAL "Z".
           DISPLAY "R5=" N.
           INSPECT X3 REPLACING ALL "A" BY "X" BEFORE INITIAL "X".
           DISPLAY "R6=[" X3 "]".
           MOVE 0 TO NA.
           MOVE 0 TO NB.
           INSPECT X4 TALLYING NA FOR ALL "A" BEFORE INITIAL "X"
                               NB FOR ALL "A".
           DISPLAY "R7=" NA " " NB.
           MOVE 0 TO N.
           INSPECT BACKWARD X5 TALLYING N
               FOR CHARACTERS BEFORE INITIAL "12".
           DISPLAY "R8=" N.
           STOP RUN.
       END PROGRAM L1P5RGN.
