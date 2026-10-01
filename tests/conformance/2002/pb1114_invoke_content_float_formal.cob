      *> kb/Work PB1114 (the INVOKE channel of the CALL lane's defect) - ISO 14.8.2.3.3 2) a): "If the formal
      *> parameter is numeric, the conformance rules are the same as for a COMPUTE statement with the argument
      *> as the sending operand and the corresponding formal parameter as the receiving operand." A method is one
      *> of rule 2's activated elements, and a FLOATING-POINT formal is numeric, so a BY CONTENT identifier,
      *> literal-2 or arithmetic expression of ANY numeric description is moved into it by 14.2.3 GR9's "a COMPUTE
      *> statement without the ROUNDED phrase". Before the fix every such INVOKE was refused at bind ("a float
      *> formal takes the identical float usage - a documented marshalling residue, not ISO 14.8.2.3.3").
      *> EXPECTED VALUES, DERIVED - all exact in binary, so the COMPUTE is exact:
      *>  FIXED  A PIC 9(3)V99 = 12.5 into FLOAT-LONG          => W = +012.50
      *>  NEG    N PIC S9(3)V99 = -7.25 into FLOAT-LONG         => W = -007.25
      *>  LIT    the numeric literal 3.75                       => W = +003.75
      *>  EXPR   the expression A + 1 (13.5)                    => W = +013.50
      *>  SHORT  FS FLOAT-SHORT 0.5 into a FLOAT-LONG formal    => W = +000.50
      *>  LONG   FL FLOAT-LONG 1.5 into a FLOAT-SHORT formal    => W = +001.50
      *>  TRUNC  T PIC 9V9 = 0.1 into FLOAT-SHORT: 0.1 is not a binary32 value; 14.7.4.3 rule 10 (implied
      *>         TRUNCATION, no ROUNDED) lands the nearest value nearer to zero, which is BELOW 0.1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114OO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CFLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CFLT.
       01 A PIC 9(3)V99 VALUE 12.5.
       01 N PIC S9(3)V99 VALUE -7.25.
       01 T PIC 9V9 VALUE 0.1.
       01 FL USAGE FLOAT-LONG VALUE 1.5.
       01 FS USAGE FLOAT-SHORT VALUE 0.5.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CFLT "NEW" RETURNING O.
           INVOKE O "TAKE-LONG" USING BY CONTENT A.
           INVOKE O "TAKE-LONG" USING BY CONTENT N.
           INVOKE O "TAKE-LONG" USING BY CONTENT 3.75.
           INVOKE O "TAKE-LONG" USING BY CONTENT A + 1.
           INVOKE O "TAKE-LONG" USING BY CONTENT FS.
           INVOKE O "TAKE-SHORT" USING BY CONTENT FL.
           INVOKE O "TAKE-TRUNC" USING BY CONTENT T.
           STOP RUN.
       END PROGRAM PB1114OO.

       IDENTIFICATION DIVISION.
       CLASS-ID. CFLT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE-LONG.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-LONG.
       PROCEDURE DIVISION USING LK.
       MAIN.
           COMPUTE W = LK.
           MOVE W TO R.
           DISPLAY "L=" R.
       END METHOD TAKE-LONG.

       METHOD-ID. TAKE-SHORT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC S9(3)V99.
       01 R PIC +999.99.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       MAIN.
           COMPUTE W = LK.
           MOVE W TO R.
           DISPLAY "S=" R.
       END METHOD TAKE-SHORT.

       METHOD-ID. TAKE-TRUNC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       MAIN.
           IF LK < 0.1
               DISPLAY "T=BELOW"
           ELSE
               DISPLAY "T=NOT-BELOW"
           END-IF.
       END METHOD TAKE-TRUNC.
       END OBJECT.
       END CLASS CFLT.
