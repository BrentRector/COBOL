      *> kb/Work PB1477 - a level-88 condition-name on an occurs-depending group item compares
      *> the conditional variable at its CURRENT extent, exactly as the relation condition does.
      *> ISO 1989:2023 8.8.4.5.3 GR2: "The rules for comparing a conditional variable with a
      *> condition-name value are the same as those specified for relation conditions."
      *> 13.18.38.4 GR8 a): when an occurs-depending group item is referenced and the object is
      *> outside the group, "only that part of the table area that is specified by the value of
      *> the data item referenced by data-name-1 at the start of the operation will be used".
      *> 8.3.3.6.4 GR2 repeats a figurative constant to "the number of character positions in the
      *> associated data item" - here the group as referenced, i.e. N character positions.
      *> Before the fix the 88's figurative VALUE was repeated to the group's MAXIMUM size (5)
      *> while the group was read at its current size, so every leg marked (*) printed F.
      *> Expected values, derived (GV holds "XXYYY"; N selects the part compared):
      *>  N=2 "XX": GV = ALL "X" T; 88 ALL "X" T (*); 88 ALL "XX" T (*);
      *>            88 ALL "X" THRU "Z" T (*) ("XX" >= "XX"); EVALUATE WHEN GV-ALLX T (*)
      *>  N=3 "XXY": 88 ALL "X" F; GV = ALL "X" F (the control: the extent is really read)
      *>  N=2 HIGH-VALUES moved: 88 VALUE HIGH-VALUE T (*); 88 VALUE ALL "X" F
      *>  N=2 "  ": 88 VALUE SPACES T; 88 VALUE ZERO F
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1477ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       01 GV.
          88 GV-ALLX   VALUE ALL "X".
          88 GV-ALLXX  VALUE ALL "XX".
          88 GV-RNGX   VALUE ALL "X" THRU "Z".
          88 GV-HI     VALUE HIGH-VALUE.
          88 GV-SP     VALUE SPACES.
          88 GV-ZR     VALUE ZERO.
          05 GE PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           MOVE ALL "Y" TO GV.
           MOVE 2 TO N.
           MOVE ALL "X" TO GV.
           IF GV = ALL "X" DISPLAY "N2-REL-ALLX T"
                      ELSE DISPLAY "N2-REL-ALLX F" END-IF.
           IF GV-ALLX DISPLAY "N2-88-ALLX T"
                ELSE DISPLAY "N2-88-ALLX F" END-IF.
           IF GV-ALLXX DISPLAY "N2-88-ALLXX T"
                 ELSE DISPLAY "N2-88-ALLXX F" END-IF.
           IF GV-RNGX DISPLAY "N2-88-RNGX T"
                ELSE DISPLAY "N2-88-RNGX F" END-IF.
           EVALUATE TRUE
               WHEN GV-ALLX DISPLAY "N2-EVAL-ALLX T"
               WHEN OTHER DISPLAY "N2-EVAL-ALLX F"
           END-EVALUATE.
           MOVE 3 TO N.
           IF GV-ALLX DISPLAY "N3-88-ALLX T"
                ELSE DISPLAY "N3-88-ALLX F" END-IF.
           IF GV = ALL "X" DISPLAY "N3-REL-ALLX T"
                      ELSE DISPLAY "N3-REL-ALLX F" END-IF.
           MOVE 2 TO N.
           MOVE HIGH-VALUES TO GV.
           IF GV-HI DISPLAY "N2-88-HI T"
              ELSE DISPLAY "N2-88-HI F" END-IF.
           IF GV-ALLX DISPLAY "N2-HI-88-ALLX T"
                ELSE DISPLAY "N2-HI-88-ALLX F" END-IF.
           MOVE SPACES TO GV.
           IF GV-SP DISPLAY "N2-88-SP T"
              ELSE DISPLAY "N2-88-SP F" END-IF.
           IF GV-ZR DISPLAY "N2-88-ZR T"
              ELSE DISPLAY "N2-88-ZR F" END-IF.
           STOP RUN.
