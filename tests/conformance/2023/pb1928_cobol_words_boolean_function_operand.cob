       >>COBOL-WORDS UNDEFINE "SUM"
       >>COBOL-WORDS SUBSTITUTE "BOOLEAN-OF-INTEGER" BY "BOI"
      *> kb/Work PB1928 - a boolean function operand is routed by the
      *> name the >>COBOL-WORDS directive left, resolved once. The
      *> routing predicate re-resolved the WRITTEN word, so a boolean
      *> user function named SUM, a word UNDEFINE removed, bound as a
      *> function but was refused as a boolean condition.
      *>
      *> 7.3.10.4 GR3: UNDEFINE removes the reserved word, so SUM is a
      *>   user-defined word naming the function SUM below; GR4:
      *>   SUBSTITUTE makes BOI usable wherever BOOLEAN-OF-INTEGER is.
      *> 12.3.8.4 12): "a reference to function-prototype-name-1 is a
      *>   reference to a user-defined function".  (cite.py: OK)
      *> 8.4.3.2.4 1): its temporary is boolean (PIC 1); 15.13 makes
      *>   BOOLEAN-OF-INTEGER(1 1) B"1" and (0 1) B"0".
      *>
      *>   C1  IF FUNCTION SUM(3)            T
      *>   C2  IF (FUNCTION SUM(3))          T
      *>   C3  IF FUNCTION SUM(3) = WB       T
      *>   C4  IF FUNCTION BOI(1 1)          T
      *>   C5  IF (FUNCTION BOI(0 1))        F
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SUM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"1" TO L-R.
           GOBACK.
       END FUNCTION SUM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1928CW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WB PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION SUM(3) DISPLAY "C1 T" ELSE DISPLAY "C1 F"
           END-IF.
           IF (FUNCTION SUM(3)) DISPLAY "C2 T" ELSE DISPLAY "C2 F"
           END-IF.
           IF FUNCTION SUM(3) = WB DISPLAY "C3 T"
           ELSE DISPLAY "C3 F" END-IF.
           IF FUNCTION BOI(1 1) DISPLAY "C4 T" ELSE DISPLAY "C4 F"
           END-IF.
           IF (FUNCTION BOI(0 1)) DISPLAY "C5 T" ELSE DISPLAY "C5 F"
           END-IF.
           STOP RUN.
       END PROGRAM PB1928CW.
