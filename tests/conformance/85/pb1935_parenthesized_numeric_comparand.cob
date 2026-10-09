      * kb/Work PB1935 - a parenthesized relation operand is an
      * arithmetic expression, never the item or literal it encloses.
      * Against a NUMERIC operand it compares numerically, as here;
      * against a character operand it is refused (the negatives
      * pb1935-paren-comparand-character-subject and
      * pb1935-evaluate-paren-object-character-subject).
      *
      * 8.8.1.1: an arithmetic expression may be "an arithmetic
      *   expression enclosed in parentheses".  (cite.py: OK 8.8.1.1)
      * 8.8.1.2 1): "the result is treated as a single operand".
      *   (cite.py: OK 8.8.1.2 1))
      * 8.8.4.12.4 1): A = B OR (C) means A = B OR A = (C); the
      *   "no parentheses" of 8.8.4.12.1 counts condition-level
      *   parentheses only, so (C) is a legal abbreviated object.
      * 14.9.13.4 4) a) 6.: an EVALUATE pair "is considered to be a
      *   conditional expression" subject = object.
      *   (cite.py: OK for each)
      *
      *   A=3 B=1 C=3 D=5 E=0
      *   P1  A = 1 OR (C) + 0        T
      *   P2  A = 1 OR ((C))          T
      *   P3  A = 1 OR (D) OR 3       T
      *   P4  (A) = 1 OR 3            T
      *   P5  A NOT = 3 OR (C)        F  (A NOT = 3 OR A NOT = 3)
      *   P6  A < D AND > (B)         T
      *   P7  E = 1 OR (ZERO)         T
      *   P8  A = B AND (C) OR D      F  (A = B AND A = C OR A = D)
      *   P9  A = (C)                 T
      *   P10 A = (3)                 T
      *   P11 EVALUATE A WHEN (C)     T
      *   P12 EVALUATE (C) WHEN A     T
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1935PN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 3.
       01 B PIC 9 VALUE 1.
       01 C PIC 9 VALUE 3.
       01 D PIC 9 VALUE 5.
       01 E PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = 1 OR (C) + 0 DISPLAY "P1 T"
           ELSE DISPLAY "P1 F" END-IF.
           IF A = 1 OR ((C)) DISPLAY "P2 T"
           ELSE DISPLAY "P2 F" END-IF.
           IF A = 1 OR (D) OR 3 DISPLAY "P3 T"
           ELSE DISPLAY "P3 F" END-IF.
           IF (A) = 1 OR 3 DISPLAY "P4 T"
           ELSE DISPLAY "P4 F" END-IF.
           IF A NOT = 3 OR (C) DISPLAY "P5 T"
           ELSE DISPLAY "P5 F" END-IF.
           IF A < D AND > (B) DISPLAY "P6 T"
           ELSE DISPLAY "P6 F" END-IF.
           IF E = 1 OR (ZERO) DISPLAY "P7 T"
           ELSE DISPLAY "P7 F" END-IF.
           IF A = B AND (C) OR D DISPLAY "P8 T"
           ELSE DISPLAY "P8 F" END-IF.
           IF A = (C) DISPLAY "P9 T"
           ELSE DISPLAY "P9 F" END-IF.
           IF A = (3) DISPLAY "P10 T"
           ELSE DISPLAY "P10 F" END-IF.
           EVALUATE A
             WHEN (C) DISPLAY "P11 T"
             WHEN OTHER DISPLAY "P11 F"
           END-EVALUATE.
           EVALUATE (C)
             WHEN A DISPLAY "P12 T"
             WHEN OTHER DISPLAY "P12 F"
           END-EVALUATE.
           STOP RUN.
