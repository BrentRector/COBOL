      *> kb/Work PB1627. ISO 8.3.3.6.3 SR2: the literal-1 of the figurative
      *> ALL literal-1 "shall be an alphanumeric, boolean, or national
      *> literal, any of which may be a concatenation expression". A
      *> concatenation expression's operands are every 8.8.3.2 SR1 operand:
      *> "a figurative constant may be specified as one or both operands",
      *> and 13.10.3 SR2 lets a constant-name stand "anywhere that a format
      *> specifies a literal of the class and category of constant-name-1"
      *> - so literal-1 may be a constant-name too (ALL K). Before PB1627
      *> ALL "A" & K and ALL "A" & SPACE drew COBOLNET1541 and ALL K drew
      *> the symbolic-character error COBOLNET1670.
      *> Expected values: 8.8.3.3 GR2 (the value is the concatenation of
      *> the operand values; a figurative is ONE character, 8.3.3.6.4 GR3a),
      *> GR1 (the class, folded pair by pair), and 8.3.3.6.4 GR2 (literal-1
      *> repeated character by character, then truncated to the receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1627ALLCAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
      *> Ordinal 43 of the native alphanumeric set is "*" (12.3.7.4 GR11).
           SYMBOLIC CHARACTERS STAR IS 43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K   CONSTANT AS "Q".
       01 K2  CONSTANT AS "QR".
       01 KN  CONSTANT AS N"R".
       01 W-X PIC X(7).
      *> VALUE positions: literal-1 "AQ", and the constant's "QR".
       01 W-V PIC X(6) VALUE ALL "A" & K.
       01 W-W PIC X(5) VALUE ALL K2.
       01 W-N PIC N(5).
       01 W-B PIC 1(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> 1 - a constant-name operand: literal-1 "AQ".
           MOVE ALL "A" & K TO W-X.
           DISPLAY "T1 [" W-X "]".
      *> 2 - a figurative operand: literal-1 "A " (SPACE is one char).
           MOVE ALL "A" & SPACE TO W-X.
           DISPLAY "T2 [" W-X "]".
      *> 3 - literal-1 IS a constant-name: "QR".
           MOVE ALL K2 TO W-X.
           DISPLAY "T3 [" W-X "]".
      *> 4 - a figurative as the FIRST operand: " B" (GR1a class).
           MOVE ALL SPACE & "B" TO W-X.
           DISPLAY "T4 [" W-X "]".
      *> 5 - a symbolic-character operand: "A*".
           MOVE ALL "A" & STAR TO W-X.
           DISPLAY "T5 [" W-X "]".
      *> 6 - the two VALUE clauses.
           DISPLAY "T6 [" W-V "][" W-W "]".
      *> 7 - national: N"S" & KN is national, & ZERO takes that class
      *>     (GR1a) - literal-1 "SR0".
           MOVE ALL N"S" & KN & ZERO TO W-N.
           DISPLAY "T7 [" FUNCTION DISPLAY-OF(W-N) "]".
      *> 8 - boolean: B"1" & ZERO is boolean "10" (GR1a).
           MOVE ALL B"1" & ZERO TO W-B.
           DISPLAY "T8 [" W-B "]".
      *> 9 - a comparison operand: W-X holds "ABQABQA".
           MOVE ALL "AB" & K TO W-X.
           IF W-X = ALL "AB" & K
               DISPLAY "T9 EQ"
           ELSE
               DISPLAY "T9 WRONG"
           END-IF.
           STOP RUN.
