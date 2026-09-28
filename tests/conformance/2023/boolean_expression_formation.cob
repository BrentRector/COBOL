      *> ISO §8.8.2 boolean-expression FORMATION and EVALUATION, in BOTH lanes — the compile-time boolean
      *> expression (§7.3.7.2 SR1: "Compile-time boolean expressions shall be formed in accordance with 8.8.2";
      *> §7.3.7.3 GR1: its precedence and evaluation "are shown in 8.8.2") and the runtime COMPUTE / IF forms
      *> (kb/Work PB1370, PB1413). Every expected value is DERIVED from §8.8.2, never read off a run:
      *>   CT1/CT2/CT3, RT4/RT5, RT9  "a boolean expression enclosed in parentheses" is a boolean expression with
      *>        no operator in it: (B"101") is B"101". Fails if the operand is not recognized as boolean (it was
      *>        a malformed directive / a parse error — the discriminator looked only for B-operators).
      *>   CT4/RT1  rule 7b: a shift takes the precedence of THE PRECEDING OPERATION, and negation (B-NOT) is
      *>        one — 1010 B-AND B-NOT 0011 B-SHIFT-R 1 = 1010 B-AND ((B-NOT 0011) B-SHIFT-R 1)
      *>        = 1010 B-AND (1100 >> 1 = 0110) = 0010. Giving the shift B-AND's precedence yields 0100.
      *>   CT5/RT2  1010 B-OR B-NOT 0011 B-SHIFT-L 1 B-AND 1111: (B-NOT 0011) << 1 = 1000 (negation's
      *>        precedence), B-AND 1111 = 1000, B-OR 1010 = 1010. Giving the shift B-OR's precedence yields 1100.
      *>   CT6/RT3  the full rule-7b ladder B-NOT > B-AND > B-XOR > B-OR:
      *>        B-NOT 1100 B-OR 0101 B-XOR 0110 B-AND 0011 = 0011 B-OR (0101 B-XOR (0110 B-AND 0011))
      *>        = 0011 B-OR (0101 B-XOR 0010) = 0011 B-OR 0111 = 0111. Left-to-right would give 0001.
      *>   CT7, RT6, RT7, CT8/RT8  rule 5 "The second operand shall be an integer operand" — §5.5 2): an integer
      *>        literal (a leading sign is part of it, §8.3.3.3.2), an integer data item, an integer function, or
      *>        (compile time) a numeric compilation variable holding an integer literal (§7.3.11.4 GR1).
      *>        1100 B-SHIFT-RC 2 = 0011; 1100 B-SHIFT-L 1 = 1000; INTEGER(2.5) = 2, 1100 B-SHIFT-RC 2 = 0011;
      *>        1100 B-SHIFT-LC +1 = 1001 (rule 8: circular left moves the leftmost digit to the right end).
      *>   RT9  INTEGER-OF-BOOLEAN((B"1010")) = 10 — the parenthesized literal is a boolean argument too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66BXF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1010".
       01 B   PIC 1(4) VALUE B"0011".
       01 C   PIC 1(4) VALUE B"1100".
       01 R   PIC 1(4).
       01 F   PIC 1    VALUE B"1".
       01 N   PIC 9    VALUE 1.
       01 N2  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
       >>DEFINE P1 AS (B"101")
       >>IF P1 = B"101"
           DISPLAY "CT1=OK".
       >>ELSE
           DISPLAY "CT1=NO".
       >>END-IF
       >>IF (B"1") = B"1"
           DISPLAY "CT2=OK".
       >>ELSE
           DISPLAY "CT2=NO".
       >>END-IF
       >>EVALUATE (B"1")
       >>WHEN B"1"
           DISPLAY "CT3=OK".
       >>WHEN OTHER
           DISPLAY "CT3=NO".
       >>END-EVALUATE
       >>DEFINE P2 AS B"1010" B-AND B-NOT B"0011" B-SHIFT-R 1
       >>IF P2 = B"0010"
           DISPLAY "CT4=OK".
       >>ELSE
           DISPLAY "CT4=NO".
       >>END-IF
       >>DEFINE P3 AS B"1010" B-OR B-NOT B"0011" B-SHIFT-L 1 B-AND B"1111"
       >>IF P3 = B"1010"
           DISPLAY "CT5=OK".
       >>ELSE
           DISPLAY "CT5=NO".
       >>END-IF
       >>DEFINE P4 AS B-NOT B"1100" B-OR B"0101" B-XOR B"0110" B-AND B"0011"
       >>IF P4 = B"0111"
           DISPLAY "CT6=OK".
       >>ELSE
           DISPLAY "CT6=NO".
       >>END-IF
       >>DEFINE K AS 2
       >>DEFINE P5 AS B"1100" B-SHIFT-RC K
       >>IF P5 = B"0011"
           DISPLAY "CT7=OK".
       >>ELSE
           DISPLAY "CT7=NO".
       >>END-IF
       >>DEFINE P6 AS B"1100" B-SHIFT-LC +1
       >>IF P6 = B"1001"
           DISPLAY "CT8=OK".
       >>ELSE
           DISPLAY "CT8=NO".
       >>END-IF
           COMPUTE R = A B-AND B-NOT B B-SHIFT-R 1.
           DISPLAY "RT1=" R.
           COMPUTE R = A B-OR B-NOT B B-SHIFT-L 1 B-AND B"1111".
           DISPLAY "RT2=" R.
           COMPUTE R = B-NOT C B-OR B"0101" B-XOR B"0110" B-AND B"0011".
           DISPLAY "RT3=" R.
           IF (B"1") = F
               DISPLAY "RT4=OK"
           ELSE
               DISPLAY "RT4=NO"
           END-IF.
           IF F = (B"1")
               DISPLAY "RT5=OK"
           ELSE
               DISPLAY "RT5=NO"
           END-IF.
           COMPUTE R = C B-SHIFT-L N.
           DISPLAY "RT6=" R.
           COMPUTE R = C B-SHIFT-RC FUNCTION INTEGER(2.5).
           DISPLAY "RT7=" R.
           COMPUTE R = C B-SHIFT-LC +1.
           DISPLAY "RT8=" R.
           COMPUTE N2 = FUNCTION INTEGER-OF-BOOLEAN((B"1010")).
           DISPLAY "RT9=" N2.
           STOP RUN.
