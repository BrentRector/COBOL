*>--- conformance-case
*> id: pb555_value_false_phrase
*> rules: [SR-13.18.63.3-27, SR-13.18.63.3-32, GR-13.18.63.4-20, SR-14.9.39.3-7, GR-14.9.39.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! FORMAT 3's THIRD OPERAND-BEARING CONSTITUENT: literal-4 IS BOUND, SCREENED AND STORED
      *> kb/Work PB555. ISO 13.18.63.2 format 3 prints three operand positions after VALUE/VALUES -
      *> the `literal-2 [THROUGH literal-3]` group, `[ IN alphabet-name-1 ]`, and, on the line below,
      *> `[ WHEN SET TO FALSE IS literal-4 ]`. The model carried the first two; literal-4 was parsed,
      *> category-screened and then DISCARDED, which held two rules shut at once: 13.18.63.3 SR27 had
      *> nothing to compare against, and `SET condition-name TO FALSE` had no value to store, so it
      *> staged loud (COBOLNET1756) and aborted the run unit.
      *>
      *> Expected values, computed from the standard.
      *> 13.18.63.4 GR20: "When a condition-name is referenced in a 'SET condition-name TO FALSE'
      *> statement, the value of literal-4 from the FALSE phrase is placed in the associated
      *> conditional-variable." 14.9.39.4 GR7 states the same store from the SET statement's side, in
      *> the SAME words 14.9.39.4 GR6 uses for the TRUE phrase - so TO TRUE and TO FALSE differ only
      *> in WHICH literal is placed.
      *> 8.8.4.5.1 tests a conditional variable "to determine whether or not its value is equal to one
      *> of the values associated with condition-name-1".
      *>
      *>   ST starts at 1.        IS-ON (VALUE 1, FALSE 8)    -> TRUE.
      *>   SET IS-ON TO FALSE  -> GR20 places 8; 8 is not 1   -> ST=8, IS-ON FALSE.
      *>   SET IS-ON TO TRUE   -> GR6 places the first VALUE  -> ST=1, IS-ON TRUE.
      *>   IN-BAND has VALUES 2 THRU 4 and literal-4 = 9. SR27 a) is satisfied (9 is outside 2..4).
      *>   SET IN-BAND TO TRUE -> GR6's "first literal" is the range START -> ST=2, IN-BAND TRUE.
      *>   SET IN-BAND TO FALSE-> GR7 places 9                -> ST=9, IN-BAND FALSE.
      *>   GRADE is PIC X. HIGH-GRADE has VALUES "A" THRU "C" and literal-4 = "F". Under the NATIVE
      *>   sequence 8.8.4.2.7 orders "A" < "C" and "F" is above both, so SR27 b) is satisfied.
      *>   SET HIGH-GRADE TO FALSE -> "F"; SET HIGH-GRADE TO TRUE -> the range start "A".
      *> The FALSE phrase and the SET ... TO FALSE arm are a COBOL-2002 introduction (constructs.json
      *> rows value-false-phrase-2002 / set-condition-false-2002), which is why this program lives in
      *> the 2002 corpus; negative/pb555-set-false-below-2002 pins the edge below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB555VALF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST              PIC 9 VALUE 1.
           88  IS-ON       VALUE 1 WHEN SET TO FALSE IS 8.
           88  IN-BAND     VALUES ARE 2 THRU 4 WHEN SET TO FALSE IS 9.
       01  GRADE           PIC X VALUE "B".
           88  HIGH-GRADE  VALUE "A" THRU "C" WHEN SET TO FALSE IS "F".
       PROCEDURE DIVISION.
       MAIN-P.
           IF IS-ON DISPLAY "ON0=yes" ELSE DISPLAY "ON0=no" END-IF
           SET IS-ON TO FALSE
           DISPLAY "ST1=" ST
           IF IS-ON DISPLAY "ON1=yes" ELSE DISPLAY "ON1=no" END-IF
           SET IS-ON TO TRUE
           DISPLAY "ST2=" ST
           IF IS-ON DISPLAY "ON2=yes" ELSE DISPLAY "ON2=no" END-IF
           SET IN-BAND TO TRUE
           DISPLAY "ST3=" ST
           IF IN-BAND DISPLAY "BAND3=yes" ELSE DISPLAY "BAND3=no" END-IF
           SET IN-BAND TO FALSE
           DISPLAY "ST4=" ST
           IF IN-BAND DISPLAY "BAND4=yes" ELSE DISPLAY "BAND4=no" END-IF
           IF HIGH-GRADE DISPLAY "HG0=yes" ELSE DISPLAY "HG0=no" END-IF
           SET HIGH-GRADE TO FALSE
           DISPLAY "GR1=" GRADE
           IF HIGH-GRADE DISPLAY "HG1=yes" ELSE DISPLAY "HG1=no" END-IF
           SET HIGH-GRADE TO TRUE
           DISPLAY "GR2=" GRADE
           IF HIGH-GRADE DISPLAY "HG2=yes" ELSE DISPLAY "HG2=no" END-IF
           DISPLAY "DONE"
           STOP RUN.
