      *> kb/Work PB1942 - 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a
      *> format specifies a literal of the class and category of constant-name-1", and 13.10.4 GR1: the effect is
      *> "as if literal-1 ... were written where constant-name-1 is written". Every literal position of the
      *> SPECIAL-NAMES paragraph (12.3.7.2) below is written as a constant-name whose entry stands LATER, in
      *> WORKING-STORAGE:
      *>   CURRENCY SIGN IS KL               literal-7 "L": one character, so both string and symbol (12.3.7.3
      *>                                     SR22); 1.5 in L9.99 reads L1.50 (13.18.40.4 GR14).
      *>   CURRENCY SIGN IS KE ... SYMBOL KU literal-7 N"EUR" is NATIONAL, so 'U' may define only a USAGE NATIONAL
      *>                                     numeric-edited item (SR28); 1.5 in U9.99 reads EUR1.50.
      *>   CLASS HEXD IS K0 THRU K9 KA THRU "F"   literal-5/-6: the characters 0-9 and A-F (12.3.7.4 GR12).
      *>   CLASS ONES IS K50                 an INTEGER constant is an ordinal (SR17 b2): ordinal 50 of the native
      *>                                     set is the character '1' (code 49), so ONES = {'1'}.
      *>   ALPHABET AB IS KZ ALSO "Y" "A" THRU "C"   literal-1 "Z" shares position 1 with "Y", then A, B, C
      *>                                     (12.3.7.4 GR7 k6, k5); as the program collating sequence, Z < A.
      *>   ORDER TABLE OT IS KT              literal-9: the default cultural ordering table, so no warning.
      *> Each leg fails if the constant-name is refused (the parse used to stop at KL with COBOL0305) or if its
      *> literal is not substituted with its class.
      *> Introduced in COBOL-2002 (constant entries, the PICTURE SYMBOL phrase, ORDER TABLE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1942CNL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS AB.
       SPECIAL-NAMES.
           CURRENCY SIGN IS KL
           CURRENCY SIGN IS KE WITH PICTURE SYMBOL KU
           CLASS HEXD IS K0 THRU K9 KA THRU "F"
           CLASS ONES IS K50
           ALPHABET AB IS KZ ALSO "Y" "A" THRU "C"
           ORDER TABLE OT IS KT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KL  CONSTANT AS "L".
       01 KE  CONSTANT AS N"EUR".
       01 KU  CONSTANT AS "U".
       01 K0  CONSTANT AS "0".
       01 K9  CONSTANT AS "9".
       01 KA  CONSTANT AS "A".
       01 K50 CONSTANT AS 50.
       01 KZ  CONSTANT AS "Z".
       01 KT  CONSTANT AS "ISO 14651_2020_TABLE1".
       01 LD  PIC L9.99.
       01 EN  PIC U9.99 USAGE NATIONAL.
       01 H1  PIC X(4) VALUE "1F0A".
       01 H2  PIC X(2) VALUE "1G".
       01 O1  PIC X(3) VALUE "111".
       01 CZ  PIC X VALUE "Z".
       01 CA  PIC X VALUE "A".
       PROCEDURE DIVISION.
           MOVE 1.5 TO LD EN.
           DISPLAY "LD=[" LD "] EN=[" EN "]".
           IF H1 IS HEXD DISPLAY "H1 HEXD" ELSE DISPLAY "H1 NOT" END-IF.
           IF H2 IS HEXD DISPLAY "H2 HEXD" ELSE DISPLAY "H2 NOT" END-IF.
           IF O1 IS ONES DISPLAY "O1 ONES" ELSE DISPLAY "O1 NOT" END-IF.
           IF CZ < CA DISPLAY "Z<A" ELSE DISPLAY "Z>=A" END-IF.
           STOP RUN.
