*>--- conformance-case
*> id: pb52_intrinsic_argument_stage
*> rules: [RV-15.84.4-4, FMT-15.84.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> Fix-queue PB52: illegal intrinsic arguments that were decidable at BIND
      *> and reported at RUN TIME instead - the wrong-STAGE family, never a wrong
      *> answer. This golden is the LEGAL side: everything here must keep
      *> compiling, and it is the half that constrains the fix.
      *>
      *> 15.14.3 r1 (BYTE-LENGTH) and 15.50.3 r1 (LENGTH) both admit "a data item
      *> of any class or category", so only a LITERAL argument is restricted -
      *> a screen written on the operand's CLASS would reject the numeric ITEMS
      *> below, which is the PB1 direction and the expensive one.
      *>
      *> AND THE TWO CLAUSES DIFFER IN EXACTLY ONE PLACE, asserted here because a
      *> shared check would have got one of them wrong:
      *>   15.50.3 r1  alphanumeric, national, OR BOOLEAN literal
      *>   15.14.3 r1  alphanumeric or national literal   (no boolean)
      *> so FUNCTION LENGTH(B"101") is legal and FUNCTION BYTE-LENGTH(B"101") is
      *> not - the latter is in the negative fixture pb52-intrinsic-argument-stage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB52STAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9(6)V99.
       01 NI PIC 9(4)V99 VALUE 4.
       01 A  PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN.
      *> 1-2 - the numeric family, whose class rule is now screened at bind.
      *> A conforming argument is untouched: SQRT(4) = 2, SIGN(-3) = -1.
           COMPUTE N = FUNCTION SQRT(NI).
           DISPLAY "1=" N.
           COMPUTE N = FUNCTION SQRT(4).
           DISPLAY "2=" N.
      *> 3-4 - a LITERAL argument of an admitted class.
           COMPUTE N = FUNCTION LENGTH("ABC").
           DISPLAY "3=" N.
           COMPUTE N = FUNCTION BYTE-LENGTH("ABC").
           DISPLAY "4=" N.
      *> 5-6 - a DATA ITEM of any class, INCLUDING numeric: r1 admits it in as
      *> many words, and this is what a class screen would have broken.
           COMPUTE N = FUNCTION LENGTH(NI).
           DISPLAY "5=" N.
           COMPUTE N = FUNCTION BYTE-LENGTH(A).
           DISPLAY "6=" N.
      *> 7-8 - a FIGURATIVE constant, legal in both (8.3.3.6.4 GR1 makes it an
      *> alphanumeric character value; GR3b gives it length one). This is PB25's
      *> and PB48's result and must not regress.
           COMPUTE N = FUNCTION LENGTH(SPACE).
           DISPLAY "7=" N.
           COMPUTE N = FUNCTION BYTE-LENGTH(SPACE).
           DISPLAY "8=" N.
      *> 9-11 - THE ASYMMETRY. A boolean literal is admitted by 15.50.3 r1 and
      *> not by 15.14.3 r1; the national pair shows the byte-vs-character axis
      *> (D-N1: two bytes per national position).
           COMPUTE N = FUNCTION LENGTH(B"101").
           DISPLAY "9=" N.
           COMPUTE N = FUNCTION LENGTH(N"AB").
           DISPLAY "10=" N.
           COMPUTE N = FUNCTION BYTE-LENGTH(N"AB").
           DISPLAY "11=" N.
           STOP RUN.
