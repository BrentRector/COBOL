      *> kb/Work PB1941 - a constant's LENGTH OF / BYTE-LENGTH OF operand may stand LATER inside the very record
      *> whose description demands the constant (ISO 13.10, 2002). 13.10.3 SR4 ("The length of data-name-1 or
      *> data-name-2 shall not be dependent, directly or indirectly, upon the value of constant-name-1") forbids
      *> only a circular dependence, and none of these lengths depends on its constant. Expected values
      *> (13.10.4 GR6 "determined as specified in the LENGTH intrinsic function", GR5 the BYTE-LENGTH one):
      *>   K  = 7    LENGTH OF W OF G; W of G is PIC X(7) (the W of H is PIC X(5) and is not the one named)
      *>   KG = 20   LENGTH OF G = W 7 + Z 7 (PIC X(K)) + T 3 x TE 2; G depends on K, never on KG
      *>   KT = 2    BYTE-LENGTH OF TE (2), PIC X(2), one byte per character
      *>   A is X(7), B is X(20), C is X(2): R = 7 + 20 + 20 + 5 + 2 + 3 = 57
      *> The negative half: negative/pb1941-constant-length-cycle-open-record (SR4's circular shape inside an
      *> open record).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941OPN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(K).
          05 B PIC X(KG).
          05 G.
             10 W PIC X(7) VALUE "SEVEN77".
                88 W-SEVEN VALUE "SEVEN77".
             10 Z PIC X(K) VALUE "ZZZZZZZ".
             10 T OCCURS 3.
                15 TE PIC X(2).
          05 H.
             10 W PIC X(5) VALUE "FIVE5".
          05 C PIC X(KT).
          05 F PIC X(3) VALUE "END".
       01 K CONSTANT AS LENGTH OF W OF G.
       01 KG CONSTANT AS LENGTH OF G.
       01 KT CONSTANT AS BYTE-LENGTH OF TE (2).
       PROCEDURE DIVISION.
           DISPLAY "K=" K " KG=" KG " KT=" KT
               " A=" FUNCTION LENGTH(A) " B=" FUNCTION LENGTH(B)
               " C=" FUNCTION LENGTH(C) " R=" FUNCTION LENGTH(R)
           MOVE "X" TO TE (3)
           DISPLAY G "|" W OF H "|" F
           IF W-SEVEN
               DISPLAY "W-SEVEN"
           END-IF
           STOP RUN.
