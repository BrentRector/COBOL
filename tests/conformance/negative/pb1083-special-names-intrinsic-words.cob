      *> reject-at: 2002 2014 2023
      *> ISO/IEC 1989:2023 §12.3.8.3 SR12 (kb/Work PB1083):
      *> "Intrinsic-function-name-1 shall not be specified as a
      *> user-defined word within the scope of this REPOSITORY paragraph"
      *> - of ANY §8.3.2.2 type: the alphabet-name PI, the class-name E
      *> and the symbolic-character ABS are user-defined words declared in
      *> the scope of FUNCTION PI E ABS INTRINSIC. The screen used to be
      *> asked only for data-, condition-, index-, file-, paragraph- and
      *> section-names, so this compiled and ran. COBOLNET1649.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET PI IS NATIVE
           CLASS E IS "A" THRU "Z"
           SYMBOLIC CHARACTERS ABS IS 66.
       REPOSITORY.
           FUNCTION PI E ABS INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "Q".
       PROCEDURE DIVISION.
           IF WS-X IS E DISPLAY "CLASS-E" END-IF
           STOP RUN.
