      *> kb/Work PB1232 - an INTEGER constant-name as a subscript of the LENGTH OF / BYTE-LENGTH OF operand
      *> (ISO 13.10, 2002). 13.10.3 SR3: "All subscripts of data-name-1 and data-name-2 shall be literals"; SR2:
      *> "constant-name-1 may be used anywhere that a format specifies a literal of the class and category of
      *> constant-name-1", and GR1/GR3 make the reference "as if [the] literal were written" - so KI, an integer
      *> constant, is a literal subscript. A subscript never changes the length (every occurrence shares one
      *> description):
      *>   K1 = 3    E (KI): E is PIC X(3)
      *>   K2 = 4    BYTE-LENGTH OF C OF R (KI 1): C is PIC N(2), 2 bytes per national position
      *>   K3 = 3    E (KJ), KJ = KI + 1 (an expression constant is an integer, GR3/GR4)
      *> The negative half: negative/pb1232-constant-length-noninteger-subscript (a non-integer constant).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1232CSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KI CONSTANT AS 2.
       01 KJ CONSTANT AS KI + 1.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 TT.
          05 R OCCURS 3.
             10 C PIC N(2) OCCURS 2.
       01 K1 CONSTANT AS LENGTH OF E (KI).
       01 K2 CONSTANT AS BYTE-LENGTH OF C OF R (KI 1).
       01 K3 CONSTANT AS LENGTH OF E (KJ).
       PROCEDURE DIVISION.
           DISPLAY "K1=" K1 " K2=" K2 " K3=" K3
           STOP RUN.
