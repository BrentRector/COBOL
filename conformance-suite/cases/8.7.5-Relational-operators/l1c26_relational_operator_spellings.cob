      *>--- conformance-case
      *> id: l1c26_relational_operator_spellings
      *> rules: [SR-8.7.5.2-1, SR-8.7.5.2-3, SR-8.7.5.2-5, SR-8.7.5.2-7,
      *>    SR-8.7.5.2-8, SR-8.7.5.2-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.7.5.2 SR1 SR3 SR5 SR7 SR8 SR10 — relop spellings agree
      *> SR1:  "Format 1 specifies simple relational operators."
      *> SR3:  "> is an abbreviation for GREATER THAN."
      *> SR5:  "< is an abbreviation for LESS THAN."
      *> SR7:  "= is an abbreviation for EQUAL TO."
      *> SR8:  "NOT = is an abbreviation for NOT EQUAL."
      *> SR10: "<= is an abbreviation for LESS THAN OR EQUAL TO."
      *> cite.py --check:
      *>   OK  §8.7.5.2 1)  (Syntax rules)
      *>   OK  §8.7.5.2 3)  (Syntax rules)
      *>   OK  §8.7.5.2 5)  (Syntax rules)
      *>   OK  §8.7.5.2 7)  (Syntax rules)
      *>   OK  §8.7.5.2 8)  (Syntax rules)
      *>   OK  §8.7.5.2 10)  (Syntax rules)
      *>   OK  §8.8.4.12.4 1)  (General rule)  abbreviated combined
      *>       relation: "the last stated relational operator were
      *>       inserted in place of the omitted relational operator"
      *> §8.7.5.1 (general format): every operator is "IS <op>" with IS
      *> optional (not underlined) and THAN / TO optional; GREATER,
      *> LESS, EQUAL, NOT, OR are the required words. §8.8.4.12.2 lets a
      *> simple-relational-operator follow AND / OR with the subject
      *> omitted (SR1 is what makes Format 1 eligible there).
      *> A = 5 is compared with X = 3, 5, 7 in turn; each line shows the
      *> truth value (T/F) at X = 3, 5, 7. An abbreviation means exactly
      *> the full form, so each symbol line equals its word line:
      *>   > / GREATER THAN      : 5>3 T, 5>5 F, 5>7 F           -> TFF
      *>   < / LESS THAN         : 5<3 F, 5<5 F, 5<7 T           -> FFT
      *>   = / EQUAL TO          : F, T, F                       -> FTF
      *>   NOT = / NOT EQUAL     : negation of EQUAL             -> TFT
      *>   <= / LESS THAN OR EQUAL TO : 5<=3 F, 5<=5 T, 5<=7 T   -> FTT
      *> SR1 (Format 1 spellings in the abbreviated position):
      *>   A > X AND < 9  = (A > X) AND (A < 9)                  -> TFF
      *>   A = X OR > X   = (A = X) OR (A > X)                   -> TTF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C26C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A                       PIC 9 VALUE 5.
       01  X                       PIC 9.
       01  I                       PIC 9.
       01  RT.
           05  RROW OCCURS 22.
               10  RC              PIC X OCCURS 3.
       PROCEDURE DIVISION.
       M-1.
           PERFORM EVAL-ALL VARYING I FROM 1 BY 1 UNTIL I > 3
           DISPLAY "GT >                       " RROW(1)
           DISPLAY "GT IS >                    " RROW(2)
           DISPLAY "GT GREATER THAN            " RROW(3)
           DISPLAY "GT IS GREATER              " RROW(4)
           DISPLAY "LT <                       " RROW(5)
           DISPLAY "LT IS <                    " RROW(6)
           DISPLAY "LT LESS THAN               " RROW(7)
           DISPLAY "LT IS LESS                 " RROW(8)
           DISPLAY "EQ =                       " RROW(9)
           DISPLAY "EQ IS =                    " RROW(10)
           DISPLAY "EQ EQUAL TO                " RROW(11)
           DISPLAY "EQ IS EQUAL                " RROW(12)
           DISPLAY "NE NOT =                   " RROW(13)
           DISPLAY "NE IS NOT =                " RROW(14)
           DISPLAY "NE NOT EQUAL               " RROW(15)
           DISPLAY "NE IS NOT EQUAL TO         " RROW(16)
           DISPLAY "LE <=                      " RROW(17)
           DISPLAY "LE IS <=                   " RROW(18)
           DISPLAY "LE LESS THAN OR EQUAL TO   " RROW(19)
           DISPLAY "LE IS LESS OR EQUAL        " RROW(20)
           DISPLAY "AB > X AND < 9             " RROW(21)
           DISPLAY "AB = X OR > X              " RROW(22)
           STOP RUN.
       EVAL-ALL.
           COMPUTE X = 2 * I + 1
           IF A > X MOVE "T" TO RC(1, I)
               ELSE MOVE "F" TO RC(1, I) END-IF
           IF A IS > X MOVE "T" TO RC(2, I)
               ELSE MOVE "F" TO RC(2, I) END-IF
           IF A GREATER THAN X MOVE "T" TO RC(3, I)
               ELSE MOVE "F" TO RC(3, I) END-IF
           IF A IS GREATER X MOVE "T" TO RC(4, I)
               ELSE MOVE "F" TO RC(4, I) END-IF
           IF A < X MOVE "T" TO RC(5, I)
               ELSE MOVE "F" TO RC(5, I) END-IF
           IF A IS < X MOVE "T" TO RC(6, I)
               ELSE MOVE "F" TO RC(6, I) END-IF
           IF A LESS THAN X MOVE "T" TO RC(7, I)
               ELSE MOVE "F" TO RC(7, I) END-IF
           IF A IS LESS X MOVE "T" TO RC(8, I)
               ELSE MOVE "F" TO RC(8, I) END-IF
           IF A = X MOVE "T" TO RC(9, I)
               ELSE MOVE "F" TO RC(9, I) END-IF
           IF A IS = X MOVE "T" TO RC(10, I)
               ELSE MOVE "F" TO RC(10, I) END-IF
           IF A EQUAL TO X MOVE "T" TO RC(11, I)
               ELSE MOVE "F" TO RC(11, I) END-IF
           IF A IS EQUAL X MOVE "T" TO RC(12, I)
               ELSE MOVE "F" TO RC(12, I) END-IF
           IF A NOT = X MOVE "T" TO RC(13, I)
               ELSE MOVE "F" TO RC(13, I) END-IF
           IF A IS NOT = X MOVE "T" TO RC(14, I)
               ELSE MOVE "F" TO RC(14, I) END-IF
           IF A NOT EQUAL X MOVE "T" TO RC(15, I)
               ELSE MOVE "F" TO RC(15, I) END-IF
           IF A IS NOT EQUAL TO X MOVE "T" TO RC(16, I)
               ELSE MOVE "F" TO RC(16, I) END-IF
           IF A <= X MOVE "T" TO RC(17, I)
               ELSE MOVE "F" TO RC(17, I) END-IF
           IF A IS <= X MOVE "T" TO RC(18, I)
               ELSE MOVE "F" TO RC(18, I) END-IF
           IF A LESS THAN OR EQUAL TO X MOVE "T" TO RC(19, I)
               ELSE MOVE "F" TO RC(19, I) END-IF
           IF A IS LESS OR EQUAL X MOVE "T" TO RC(20, I)
               ELSE MOVE "F" TO RC(20, I) END-IF
           IF A > X AND < 9 MOVE "T" TO RC(21, I)
               ELSE MOVE "F" TO RC(21, I) END-IF
           IF A = X OR > X MOVE "T" TO RC(22, I)
               ELSE MOVE "F" TO RC(22, I) END-IF.
