      *> AN INITIALIZED DYNAMIC TABLE OF A PLAIN EXTERNAL RECORD SEEDS EACH CREATED
      *> OCCURRENCE WITH ITS OWN VALUE LITERAL (train 1021 review of PB1042).
      *> ISO/IEC 1989:2023 §13.18.63.4 GR4 a): the VALUE takes no effect on the external
      *> record's initial state, so GT opens at its FROM capacity, zero. MOVE "z" TO GV(3)
      *> creates occurrences 1-3 (§8.5.1.9.3), and §8.5.1.9.5 initializes the unreferenced
      *> ones "as though ... INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT", whose
      *> sending operand is "the literal in the VALUE clause that corresponds to the
      *> occurrence being initialized" (§14.9.20.4 GR6 a) 3.): GV(1) = A, GV(2) = B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042EI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB1042EIX EXTERNAL.
          05 GT OCCURS DYNAMIC CAPACITY IN GC TO 3 INITIALIZED.
             10 GV PIC X VALUE "A" "B" "C" FROM (1) TO (3).
       PROCEDURE DIVISION.
           MOVE "z" TO GV(3)
           DISPLAY "[" GV(1) GV(2) GV(3) "]"
           STOP RUN.
