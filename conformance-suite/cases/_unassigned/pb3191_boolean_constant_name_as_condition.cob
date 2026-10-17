*>--- conformance-case
*> id: pb3191_boolean_constant_name_as_condition
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3191 - a BOOLEAN constant-name is the boolean literal
      *> it substitutes (ISO 13.10.4 GR1: "the effect of specifying
      *> constant-name-1 in other than this entry is as if literal-1 ...
      *> were written where constant-name-1 is written"; 13.10.3 SR2: it
      *> "may be used anywhere that a format specifies a literal of the
      *> class and category of constant-name-1"). 8.8.2 lists "a boolean
      *> literal" as a boolean expression, so a boolean constant-name is
      *> a boolean-expression operand and, of length 1, a simple boolean
      *> condition (8.8.4.3.3 SR1; GR1: true iff the value is 1; GR2:
      *> NOT reverses it). It used to be refused COBOLNET1639 "'BK' is
      *> not defined". Each line prints iff its condition is TRUE:
      *>   BK = B"1", BZ = B"0", BK2 = B"10", B1 = 1, B0 = 0, B2 = B"10"
      *>   A  B1 = BK                      1 = 1 (a relation, already live)
      *>   B  BK B-AND B1                  1 B-AND 1 = 1
      *>      BZ B-AND B1                  0 B-AND 1 = 0 : no line
      *>   C  B2 = BK2                     10 = 10
      *>      B2 = BK2 B-OR B"01"          10 = (10 B-OR 01 = 11) : no line
      *>   COMPUTE-OK  COMPUTE B3 = BK B-AND B1 stores 1
      *>      NOT BK                       NOT 1 = 0 : no line
      *>   D  NOT BZ                       NOT 0 = 1
      *>   E  BK                           the bare simple boolean condition
      *>      BZ                           0 : no line
      *>   E2 (BK)                         parentheses change nothing
      *>   G  B1 AND BK                    1 AND 1
      *>   H  B0 OR BK                     0 OR 1
      *>   I  BK AND BZ is false, so the ELSE branch prints
      *>   J  EVALUATE TRUE WHEN BZ / WHEN BK : the first true WHEN is BK
      *>   K  EVALUATE BK WHEN B"1"        B"1" = B"1"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3191BKCOND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BK CONSTANT AS B"1".
       01 BZ CONSTANT AS B"0".
       01 BK2 CONSTANT AS B"10".
       01 B1 PIC 1 USAGE BIT VALUE B"1".
       01 B0 PIC 1 USAGE BIT VALUE B"0".
       01 B2 PIC 1(2) USAGE BIT VALUE B"10".
       01 B3 PIC 1 USAGE BIT.
       PROCEDURE DIVISION.
       MAIN-P.
           IF B1 = BK DISPLAY "A" END-IF.
           IF BK B-AND B1 DISPLAY "B" END-IF.
           IF BZ B-AND B1 DISPLAY "B-BAD" END-IF.
           IF B2 = BK2 DISPLAY "C" END-IF.
           IF B2 = BK2 B-OR B"01" DISPLAY "C2-BAD" END-IF.
           COMPUTE B3 = BK B-AND B1.
           IF B3 = B"1" DISPLAY "COMPUTE-OK" END-IF.
           IF NOT BK DISPLAY "D-BAD" END-IF.
           IF NOT BZ DISPLAY "D" END-IF.
           IF BK DISPLAY "E" END-IF.
           IF BZ DISPLAY "E-BAD" END-IF.
           IF (BK) DISPLAY "E2" END-IF.
           IF B1 AND BK DISPLAY "G" END-IF.
           IF B0 OR BK DISPLAY "H" END-IF.
           IF BK AND BZ DISPLAY "I-BAD" ELSE DISPLAY "I" END-IF.
           EVALUATE TRUE
              WHEN BZ DISPLAY "J-BAD"
              WHEN BK DISPLAY "J"
           END-EVALUATE.
           EVALUATE BK
              WHEN B"1" DISPLAY "K"
              WHEN OTHER DISPLAY "K-BAD"
           END-EVALUATE.
           STOP RUN.
