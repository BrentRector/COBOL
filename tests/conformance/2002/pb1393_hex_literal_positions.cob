      *> kb/Work PB1393 - a literal's own syntax rules (ISO 8.3.3.2.3
      *> SR1/SR6, 8.3.3.4.3 SR1, 8.3.3.5.3 SR1/SR5) are now asked of
      *> every literal TOKEN by one pass, and the keyword-omitted
      *> intrinsic argument is screened as its SUBSCRIPT-mode token -
      *> which needed a new SUB_HEXLIT twin for X"...". This golden pins
      *> that the WELL-FORMED literals at the newly screened positions
      *> keep their values:
      *>   LENGTH(X"4142")      = 2  (8.3.3.2.4: two digits per
      *>                              alphanumeric character)
      *>   LENGTH(NX"00410042") = 2  (8.3.3.5.4 GR4: four digits per
      *>                              national character, D-N1)
      *>   "Z" & X"41"          = ZA (8.8.3.3 GR2), padded to X(4)
      *>   VALUE ALL "A" & X"42"     = ABAB (8.3.3.6.3 SR2 literal-1 is
      *>                              the concatenation "AB"; 8.3.3.6.4
      *>                              repeats it to the item's length)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1393HEXPOS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(4).
       01 W PIC X(4).
       01 V PIC X(4) VALUE ALL "A" & X"42".
       PROCEDURE DIVISION.
       MAIN.
           MOVE LENGTH(X"4142") TO L.
           DISPLAY "X=" L.
           MOVE LENGTH(NX"00410042") TO L.
           DISPLAY "NX=" L.
           MOVE "Z" & X"41" TO W.
           DISPLAY "C=[" W "]".
           DISPLAY "V=[" V "]".
           STOP RUN.
