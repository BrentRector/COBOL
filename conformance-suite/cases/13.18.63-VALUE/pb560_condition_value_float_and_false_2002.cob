*>--- conformance-case
*> id: pb560_condition_value_float_and_false_2002
*> rules: [GR-13.18.63.4-17, GR-14.9.39.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB560 - the COBOL-2002 legs of the ONE level-88 VALUE recipe (ISO 14.9.39.4 GR6: the literal
      *> "is placed in the conditional variable according to the rules for the VALUE clause"). Both are round
      *> trips: 8.8.4.5.3 GR3 makes the condition true exactly when the stored value is one of the condition's
      *> values, so SET <cond> TO TRUE followed by IF <cond> is an IDENTITY and any disagreement between the
      *> STORE and the TEST shows up as a FALSE on the line after the SET.
      *>
      *> The expected values are COMPUTED FROM THE RULES, not measured:
      *>   F  13.18.63.4 GR17 - "General rule 1 above applies" - and GR1: "If the usage of the subject of the
      *>      entry is float-short, float-long or float-extended, the actual value given to the item is an
      *>      approximation of the arithmetic value of the literal". 0.5 is exactly representable in a binary
      *>      floating-point format, so the approximation IS 0.5. The SET emitter used to scale the literal at
      *>      the item's Scale (zero for a float usage), which TRUNCATED the fraction: F held 0 and F-HALF was
      *>      false immediately after SET F-HALF TO TRUE. The condition TEST arm already read it as a native
      *>      double, which is why the two disagreed.
      *>   G  13.18.63.4 GR20 - "When a condition-name is referenced in a 'SET condition-name TO FALSE'
      *>      statement, the value of literal-4 from the FALSE phrase is placed in the associated
      *>      conditional-variable" - through the SAME recipe as the TRUE arm; the two arms differ only in
      *>      which literal they select. 8.3.3.6.4 GR2 repeats ALL "XY" to the variable's 5 character
      *>      positions and truncates from the right: XYXYX.
      *> The WHEN SET TO FALSE phrase and SET condition-name TO FALSE are both COBOL-2002 additions, as is
      *> USAGE FLOAT-LONG, which is why these legs live here and not in the 85 copy.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB560-COND-VALUE-2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F USAGE FLOAT-LONG.
          88 F-HALF VALUE 0.5.
       01 WS-G PIC X(5).
          88 G-ON VALUE ALL "XY" WHEN SET TO FALSE IS "-----".
       PROCEDURE DIVISION.
           SET F-HALF TO TRUE
           DISPLAY "F=[" WS-F "]"
           IF F-HALF DISPLAY "F-TRUE" ELSE DISPLAY "F-FALSE" END-IF
           SET G-ON TO TRUE
           DISPLAY "G=[" WS-G "]"
           IF G-ON DISPLAY "G-TRUE" ELSE DISPLAY "G-FALSE" END-IF
           SET G-ON TO FALSE
           DISPLAY "GF=[" WS-G "]"
           IF G-ON DISPLAY "GF-TRUE" ELSE DISPLAY "GF-FALSE" END-IF
           STOP RUN.
