      *> !! A TYPEDEF CLONE CARRIES THE WHOLE FORMAT-3 STATE, NOT JUST THE VALUE SET
      *> kb/Work PB555, sibling sweep. ISO 13.18.58.4 GR1 makes a type's condition-names part of the
      *> type, so cloning one onto a TYPE-referencing item must reproduce every constituent
      *> 13.18.63.2 format 3 prints. The clone copier used to copy the VALUE set and NOTHING else.
      *>
      *> ⛔ THE DROPPED ALPHABET WAS A SILENT WRONG ANSWER, AND THIS PROGRAM IS THE MEASUREMENT.
      *> Under MYALPH the ordinals are Z=1, Y=2, X=3, A=4, so "Z" THRU "X" is ASCENDING and CONTAINS
      *> "Y". Under the NATIVE sequence the same written range is INVERTED ('Z'=0x5A after 'X'=0x58)
      *> and 14.7.8 rule 2 makes it EMPTY. So the two orderings give OPPOSITE answers for G1 = "Y",
      *> and a clone that lost the IN alphabet-name-1 phrase answered HI-G=no where the template
      *> answers HI-G=yes. MEASURED both ways on this program.
      *>
      *> Expected values, computed from the standard.
      *>   G1 = "Y". 14.7.8 rule 2 orders "Z" THRU "X" in MYALPH -> "Y" is inside -> HI-G is TRUE
      *>   (8.8.4.5.1: the conditional variable's value is one of the values associated with the
      *>   condition-name).
      *>   SET HI-G TO FALSE places literal-4 (13.18.63.4 GR20 / 14.9.39.4 GR7) -> G1 = "A".
      *>   "A" is OUTSIDE "Z" THRU "X" under MYALPH (ordinal 4 > 3), which is 13.18.63.3 SR27 b)
      *>   satisfied, so HI-G is then FALSE.
      *>   SET HI-G TO TRUE places the first VALUE literal, the range start (14.9.39.4 GR6) -> "Z",
      *>   and HI-G is TRUE again.
      *> The FALSE phrase and the SET ... TO FALSE arm are a COBOL-2002 introduction, which is why
      *> this program lives in the 2002 corpus.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB555TDCL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET MYALPH IS "Z" "Y" "X" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-GRADE     TYPEDEF PIC X.
           88  HI-G    VALUE "Z" THRU "X" IN MYALPH WHEN SET TO FALSE IS "A".
       01  G1          TYPE T-GRADE.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "Y" TO G1
           IF HI-G DISPLAY "HI0=yes" ELSE DISPLAY "HI0=no" END-IF
           SET HI-G TO FALSE
           DISPLAY "G1A=" G1
           IF HI-G DISPLAY "HI1=yes" ELSE DISPLAY "HI1=no" END-IF
           SET HI-G TO TRUE
           DISPLAY "G1B=" G1
           IF HI-G DISPLAY "HI2=yes" ELSE DISPLAY "HI2=no" END-IF
           DISPLAY "DONE"
           STOP RUN.
