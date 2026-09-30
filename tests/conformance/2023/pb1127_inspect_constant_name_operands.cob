      *> PB1127 - ISO 13.10.3 SR2: "constant-name-1 may be used anywhere that a format specifies a literal"
      *>   (cite.py --check 13.10.3 -> OK 2) and 13.10.4 GR1: the constant-name stands as if its literal were
      *>   written where it is written. INSPECT's literal-1 .. literal-5 positions are such positions, so a
      *>   constant-name there behaves exactly as the literal it names. It was refused COBOLNET1639 ("not defined").
      *> 14.9.22.3 SR3: "when identifier-1 is of class boolean, the figurative constant is of class boolean and
      *>   only the figurative constant ZERO may be specified" - ZERO itself stays legal (BZ below).
      *> WHY EACH LEG CAN FAIL:
      *>  T  - a constant-name as TALLYING literal-1: "AAXAA" holds four "A".
      *>  R  - as REPLACING literal-1 AND literal-3: A -> Z, one-character constants of one size.
      *>  C  - as CONVERTING literal-4 and literal-5: Z back to A.
      *>  R2 - a TWO-character constant as literal-1 with a literal-3 of the same size (SR6): "AX" -> "QQ".
      *>  B  - a constant-name as a BEFORE delimiter: only the "A" before the first "X" is tallied.
      *>  N  - a NATIONAL constant-name over a national identifier-1 (SR4: all operands national).
      *>  BZ - boolean identifier-1 with the figurative ZERO: B"0101" holds two '0' positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1127CN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "AAXAA".
       01 N PIC 99 VALUE 0.
       01 KA CONSTANT AS "A".
       01 KZ CONSTANT AS "Z".
       01 KAX CONSTANT AS "AX".
       01 KX CONSTANT AS "X".
       01 KN CONSTANT AS N"Q".
       01 NX PIC N(3) VALUE N"QRQ".
       01 B PIC 1(4) VALUE B"0101".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT X TALLYING N FOR ALL KA.
           DISPLAY "T=" N.
           INSPECT X REPLACING ALL KA BY KZ.
           DISPLAY "R=" X.
           INSPECT X CONVERTING KZ TO KA.
           DISPLAY "C=" X.
           INSPECT X REPLACING ALL KAX BY "QQ".
           DISPLAY "R2=" X.
           MOVE "AAXAA" TO X.
           MOVE 0 TO N.
           INSPECT X TALLYING N FOR ALL KA BEFORE KX.
           DISPLAY "B=" N.
           MOVE 0 TO N.
           INSPECT NX TALLYING N FOR ALL KN.
           DISPLAY "N=" N.
           MOVE 0 TO N.
           INSPECT B TALLYING N FOR ALL ZERO.
           DISPLAY "BZ=" N.
           STOP RUN.
