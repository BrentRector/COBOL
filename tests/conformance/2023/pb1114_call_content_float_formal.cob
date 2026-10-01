      *> kb/Work PB1114 - ISO 14.8.2.3.3 2) a): "If the formal parameter is numeric, the conformance rules are
      *> the same as for a COMPUTE statement with the argument as the sending operand and the corresponding
      *> formal parameter as the receiving operand." A FLOATING-POINT formal is numeric, so a CALL ... AS NESTED
      *> (rule 2's second leg) moves a BY CONTENT / BY VALUE argument into it by 14.2.3 GR9's "a COMPUTE
      *> statement without the ROUNDED phrase" (9: "if the formal parameter is numeric"). Before the fix the
      *> activator's COMPUTE covered fixed-point formals only, and the float formal read 0 (BY CONTENT), or the
      *> program was refused outright (BY VALUE formal: COBOLNET0899; a numeric literal or an expression:
      *> COBOLNET1688).
      *> EXPECTED VALUES, DERIVED - every value is exact in binary, so the COMPUTE is exact:
      *>  L1  A PIC 9(3)V99 = 12.5 BY CONTENT into FLOAT-LONG   => L = 12.5   => W = +012.50
      *>  L2  N PIC S9(3)V99 = -7.25 BY CONTENT                  => L = -7.25   => W = -007.25
      *>  L3  the numeric literal 3.75 BY CONTENT                => W = +003.75
      *>  L4  the expression A + 1 BY CONTENT (13.5)              => W = +013.50
      *>  L5  FS FLOAT-SHORT 0.5 BY CONTENT into FLOAT-LONG      => W = +000.50
      *>  S1  FL FLOAT-LONG 1.5 BY CONTENT into FLOAT-SHORT      => W = +001.50
      *>  V1  A BY VALUE into a BY VALUE FLOAT-LONG formal (14.2.2 SR2: class numeric; GR10: a detached record
      *>      of the formal's description): the callee sees 12.5, adds 1 to ITS copy (13.5), and the caller's A
      *>      is unchanged (01250) - the record "does not occupy the same storage area as the argument".
      *>  V2  the literal 4.5 BY VALUE into a BY VALUE FLOAT-SHORT formal => W = +004.50
      *>  T1  T PIC 9V9 = 0.1 into FLOAT-SHORT: 0.1 is not a binary32 value, and 14.7.4.3 rule 10 - "If the
      *>      TRUNCATION phrase is specified or implied, and the arithmetic value cannot be represented exactly
      *>      in the resultant identifier, the arithmetic value is rounded to the nearest value nearer to zero
      *>      that can be represented" - implied by the absence of ROUNDED, so the landed value is the largest
      *>      binary32 not above 0.1, which is BELOW 0.1. (Round-to-nearest would store 0.1000000015, not below.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3)V99 VALUE 12.5.
       01 N PIC S9(3)V99 VALUE -7.25.
       01 T PIC 9V9 VALUE 0.1.
       01 FL USAGE FLOAT-LONG VALUE 1.5.
       01 FS USAGE FLOAT-SHORT VALUE 0.5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1114L" AS NESTED USING BY CONTENT A
           CALL "PB1114L" AS NESTED USING BY CONTENT N
           CALL "PB1114L" AS NESTED USING BY CONTENT 3.75
           CALL "PB1114L" AS NESTED USING BY CONTENT A + 1
           CALL "PB1114L" AS NESTED USING BY CONTENT FS
           CALL "PB1114S" AS NESTED USING BY CONTENT FL
           CALL "PB1114V" AS NESTED USING BY VALUE A
           DISPLAY "A=" A
           CALL "PB1114W" AS NESTED USING BY VALUE 4.5
           CALL "PB1114T" AS NESTED USING BY CONTENT T
           CALL "PB1114I" AS NESTED USING BY CONTENT A
           CALL "PB1114J" AS NESTED USING BY VALUE A
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-LONG.
       PROCEDURE DIVISION USING LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "L=" R
           GOBACK.
       END PROGRAM PB1114L.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "S=" R
           GOBACK.
       END PROGRAM PB1114S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-LONG.
       PROCEDURE DIVISION USING BY VALUE LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "V=" R
           ADD 1 TO LK
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "V+1=" R
           GOBACK.
       END PROGRAM PB1114V.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING BY VALUE LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "W=" R
           GOBACK.
       END PROGRAM PB1114W.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       MAIN.
           IF LK < 0.1
               DISPLAY "T=BELOW"
           ELSE
               DISPLAY "T=NOT-BELOW"
           END-IF
           GOBACK.
       END PROGRAM PB1114T.

      *> I1/J1 - the same crossing into a formal that a REDEFINES makes IMAGE-CARRIED (it keeps a callee-local
      *> field and reads the landed record through its own IEEE description): BY CONTENT A and BY VALUE A are
      *> 12.5 => W = +012.50 each.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-LONG.
       01 LKX REDEFINES LK PIC X(8).
       PROCEDURE DIVISION USING LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "I=" R
           GOBACK.
       END PROGRAM PB1114I.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-LONG.
       01 LKX REDEFINES LK PIC X(8).
       PROCEDURE DIVISION USING BY VALUE LK.
       MAIN.
           COMPUTE W = LK
           MOVE W TO R
           DISPLAY "J=" R
           GOBACK.
       END PROGRAM PB1114J.
       END PROGRAM PB1114C.
