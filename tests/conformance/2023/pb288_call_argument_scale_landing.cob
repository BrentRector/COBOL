      *> kb/Work PB288 - ISO 14.2.3 GR9/GR10: a CALL argument crossing to a NUMERIC formal
      *> parameter IS "a COMPUTE statement without the ROUNDED phrase" whose receiving operand is
      *> a data item of the FORMAL's own description (GR9's NESTED/prototyped branch allocates
      *> exactly that record; GR10's BY VALUE record is the same shape), and GR11 resolves every
      *> reference to the formal through that same linkage description. So a magnitude the formal
      *> cannot hold is 14.7.5 case 3 - "after radix point alignment ... further from zero than
      *> permitted for the associated resultant data item" - whose no-SIZE-ERROR-phrase disposition
      *> with EC-SIZE checking off is documented in CONFORMANCE.md DOC-A.1-70: execution continues
      *> and the receiver takes the LOW-ORDER digits of the result aligned at its scale. It is
      *> never the two's complement of an Int128 intermediate, which is not any rule.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>  REF/VAL rows 1-3 - the argument is 10**30 (14.8.2 admits the 31-digit literal; 8.3.3.3.2
      *>    requires 1 through 31 digits). The formal is PIC S9(9)V9(9), so the result aligned at
      *>    scale 9 is 10**39 and the receiver's 18 digit positions keep its low-order 18, all of
      *>    them zero => +000000000.000000000. The fixed-point and floating spellings denote the
      *>    same value and MUST AGREE; BY VALUE (GR10) and BY CONTENT (GR9) MUST AGREE too - they
      *>    are the same COMPUTE into the same description.
      *>  REF/VAL rows 4-5 - the binary64 lane. COMPUTE F = 1.0E+30 has no ROUNDED phrase, so
      *>    14.7.4.3 r2/r10 store the binary64 NEARER TO ZERO than 10**30 (kb/Work PB1196): the
      *>    double nearest 10**30 is 1000000000000000019884624838656, above it, and its predecessor
      *>    (one ulp = 2**47 below) is 999999999999999879147136483328. At scale 9 that is
      *>    999999999999999879147136483328000000000 and the low-order 18 digits are
      *>    136483328000000000 => +136483328.000000000. The two arms MUST AGREE.
      *>  SML - 123456789012345678 into PIC S9(4)V99: aligned at scale 2 that is
      *>    12345678901234567800, low-order 6 digits 567800 => +5678.00.
      *>  MOV/CMP - the inline comparators. GR9 names a COMPUTE, and 14.9.25.4 GR6's MOVE keeps the
      *>    same low-order digits here, so all three rows MUST print the same characters. This is
      *>    the one-rule assertion: the crossing is not a second, weaker store rule.
      *>  Last REF - the control. 123.456 fits the formal, so the landing is exact and NOT a
      *>    blanket zero => +000000123.456000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB288A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG.
       01 CHK PIC S9(4)V99.
       01 E2 PIC +9(4).99.
       PROCEDURE DIVISION.
           COMPUTE F = 1.0E+30
           CALL "PB288B" AS NESTED USING BY CONTENT
               1000000000000000000000000000000
           CALL "PB288B" AS NESTED USING BY CONTENT 1.0E+30
           CALL "PB288C" AS NESTED USING BY VALUE
               1000000000000000000000000000000
           CALL "PB288B" AS NESTED USING BY CONTENT (F + 0)
           CALL "PB288C" AS NESTED USING BY VALUE (F + 0)
           CALL "PB288D" AS NESTED USING BY CONTENT 123456789012345678
           MOVE 123456789012345678 TO CHK
           MOVE CHK TO E2
           DISPLAY "MOV=" E2
           COMPUTE CHK = 123456789012345678
           MOVE CHK TO E2
           DISPLAY "CMP=" E2
           CALL "PB288B" AS NESTED USING BY CONTENT 123.456
           GOBACK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB288B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC +9(9).9(9).
       LINKAGE SECTION.
       01 LR PIC S9(9)V9(9).
       PROCEDURE DIVISION USING LR.
           MOVE LR TO E1
           DISPLAY "REF=" E1
           GOBACK.
       END PROGRAM PB288B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB288C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E3 PIC +9(9).9(9).
       LINKAGE SECTION.
       01 LV PIC S9(9)V9(9).
       PROCEDURE DIVISION USING BY VALUE LV.
           MOVE LV TO E3
           DISPLAY "VAL=" E3
           GOBACK.
       END PROGRAM PB288C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB288D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E4 PIC +9(4).99.
       LINKAGE SECTION.
       01 LD PIC S9(4)V99.
       PROCEDURE DIVISION USING LD.
           MOVE LD TO E4
           DISPLAY "SML=" E4
           GOBACK.
       END PROGRAM PB288D.
       END PROGRAM PB288A.
