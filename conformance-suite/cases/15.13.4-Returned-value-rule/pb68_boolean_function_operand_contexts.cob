*>--- conformance-case
*> id: pb68_boolean_function_operand_contexts
*> rules: [RV-15.13.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB68 - a BOOLEAN-result function reference (15.13.1 "The function type is boolean"; 15.2 item 2 makes
      *> its class and category boolean; 8.4.3.2.4 GR1 - a function-identifier references a temporary data
      *> item) in every class-boolean context, each of which used to re-derive the operand's class locally
      *> and had no arm for a computed operand:
      *> REL-*: 8.8.4.2.2 Format 2 / 8.8.4.2.8 - a boolean comparison, equality only, the shorter operand
      *>   right-extended with boolean zeros: BOOLEAN-OF-INTEGER(544, 6) = B"100000" (544 = 1000100000, its
      *>   low six positions), = a bit item holding 00000101, = another function reference, and (5, 4) = "0101"
      *>   against B"01010000" (zero-extended). Before PB68: COBOLNET0844 "may be compared only with another
      *>   boolean operand" (StatementValidation.CheckRelationalOperands' local switch).
      *> BAND/BXOR: 8.8.2 - "an identifier referencing a boolean data item" as a boolean-expression operand:
      *>   00000101 B-AND 00000100 = 00000100; 00000101 B-XOR 00000011 = 00000110. Before PB68: COBOLNET1511
      *>   (ConditionBinder.BindBoolOperandValue).
      *> LEN: 15.50.3 r1 admits a data item of any class; 15.50.4 r1 counts a boolean item's positions: 8.
      *>   Before PB68: COBOLNET1627 "is a numeric literal" (the LENGTH fold's default arm).
      *> BEXPR-REL / SIMPLE: a boolean expression whose operand is the function, as a relation operand and as a
      *>   simple boolean condition (8.8.4.3 - a length-1 result).
      *> MV-*: 14.9.25.3 Table 16 - a boolean sender moves to boolean and alphanumeric receivers (Yes) - the
      *>   function's temporary is the sender. EVAL: the function as an EVALUATE subject and as a WHEN object.
      *> The mirror negatives (pb68-boolean-function-vs-alphanumeric-literal, pb68-boolean-function-arithmetic)
      *> pin the two rejections: the illegal alphanumeric comparison, which used to be ACCEPTED and evaluate
      *> TRUE, and the arithmetic context, which used to compile clean and throw NotImplemented at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB68BOOLFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BV PIC 1(8) USAGE BIT VALUE B"00000101".
       01 BR PIC 1(8) USAGE BIT.
       01 BD PIC 1(8).
       01 X8 PIC X(8).
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) = B"100000"
               DISPLAY "REL-EQ=TRUE" ELSE DISPLAY "REL-EQ=FALSE" END-IF
           IF FUNCTION BOOLEAN-OF-INTEGER(5, 8) = BV
               DISPLAY "REL-ITEM=TRUE" ELSE DISPLAY "REL-ITEM=FALSE" END-IF
           IF FUNCTION BOOLEAN-OF-INTEGER(5, 8) = FUNCTION BOOLEAN-OF-INTEGER(5, 8)
               DISPLAY "REL-FF=TRUE" ELSE DISPLAY "REL-FF=FALSE" END-IF
           IF FUNCTION BOOLEAN-OF-INTEGER(5, 4) = B"01010000"
               DISPLAY "REL-EXT=TRUE" ELSE DISPLAY "REL-EXT=FALSE" END-IF
           IF FUNCTION BOOLEAN-OF-INTEGER(6, 8) NOT = BV
               DISPLAY "REL-NE=TRUE" ELSE DISPLAY "REL-NE=FALSE" END-IF
           COMPUTE BR = FUNCTION BOOLEAN-OF-INTEGER(5, 8) B-AND B"00000100"
           DISPLAY "BAND=" BR
           COMPUTE BR = BV B-XOR FUNCTION BOOLEAN-OF-INTEGER(3, 8)
           DISPLAY "BXOR=" BR
           COMPUTE L = FUNCTION LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5, 8))
           DISPLAY "LEN=" L
           IF FUNCTION BOOLEAN-OF-INTEGER(5, 8) B-AND B"00000001" = B"00000001"
               DISPLAY "BEXPR-REL=TRUE" ELSE DISPLAY "BEXPR-REL=FALSE" END-IF
           IF FUNCTION BOOLEAN-OF-INTEGER(1, 1)
               DISPLAY "SIMPLE=TRUE" ELSE DISPLAY "SIMPLE=FALSE" END-IF
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8) TO BR
           DISPLAY "MV-BIT=" BR
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8) TO BD
           DISPLAY "MV-DSP=" BD
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8) TO X8
           DISPLAY "MV-X=" X8
           EVALUATE FUNCTION BOOLEAN-OF-INTEGER(5, 8)
               WHEN B"00000101" DISPLAY "EVAL=HIT"
               WHEN OTHER DISPLAY "EVAL=MISS"
           END-EVALUATE
           EVALUATE TRUE
               WHEN FUNCTION BOOLEAN-OF-INTEGER(1, 1) DISPLAY "EVALT=HIT"
               WHEN OTHER DISPLAY "EVALT=MISS"
           END-EVALUATE
           STOP RUN.
       END PROGRAM PB68BOOLFN.
