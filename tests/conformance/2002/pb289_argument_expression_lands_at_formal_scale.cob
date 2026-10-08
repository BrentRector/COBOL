      *> kb/Work PB289 - an arithmetic-EXPRESSION argument is evaluated for the FORMAL's description.  14.2.3 GR9
      *>   (the second branch: a program with the NESTED phrase, a prototyped program, a method) and GR10 make the
      *>   crossing "a COMPUTE statement without the ROUNDED phrase" into a record of the formal's description, so
      *>   the argument expression is the COMPUTE's sending operand and the formal its receiving operand
      *>   (14.8.2.3.3 rule 2 a: "the conformance rules are the same as for a COMPUTE statement with the argument
      *>   as the sending operand and the corresponding formal parameter as the receiving operand").  The activating
      *>   element evaluated the expression receiver-less, at a 6-fraction-digit working scale, BEFORE the landing
      *>   - so a PIC S9(5)V9(20) formal received 1 / 3 as 0.333333 (and 2 / 3 as 0.666666).  Expected values,
      *>   a COMPUTE's with no ROUNDED phrase (implied TRUNCATION, 14.7.4.3 rule 2) into 20 fraction digits:
      *>   - 1 / 3 = 0.33333333333333333333      - 2 / 3 = 0.66666666666666666666 (truncated, not ...67)
      *>   - the nested wide difference of PB1900 (A*B - C*D over PIC 9(21)) = 1, into a PIC 9(9) formal
      *>   - a product at scale 20 (1.1234567891 * 1.1234567891) = 1.26215515697488187881 (exact)
      *>   The same four values cross a BY CONTENT argument and an INVOKE argument into a method formal.
      *>   cite.py --check 14.2.3 "a COMPUTE statement without the ROUNDED phrase" -> OK 14.2.3 GR9/GR10
      *>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a COMPUTE statement" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB289G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB289K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB289K.
       01 ONE   PIC 9 VALUE 1.
       01 TWO   PIC 9 VALUE 2.
       01 THREE PIC 9 VALUE 3.
       01 A  PIC 9(21) VALUE 100000000000000000001.
       01 B  PIC 9(21) VALUE 100000000000000000001.
       01 C  PIC 9(21) VALUE 100000000000000000000.
       01 D  PIC 9(21) VALUE 100000000000000000002.
       01 F  PIC 9V9(10) VALUE 1.1234567891.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB289V" AS NESTED USING BY VALUE ONE / THREE
           CALL "PB289V" AS NESTED USING BY VALUE TWO / THREE
           CALL "PB289C" AS NESTED USING BY CONTENT ONE / THREE
           CALL "PB289W" AS NESTED USING BY VALUE A * B - C * D
           CALL "PB289P" AS NESTED USING BY VALUE F * F
           INVOKE PB289K "NEW" RETURNING OBJ
           INVOKE OBJ "FRAC" USING BY CONTENT (ONE / THREE)
           INVOKE OBJ "FRAC" USING BY CONTENT (TWO / THREE)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB289V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC +9(5).9(20).
       LINKAGE SECTION.
       01 N PIC S9(5)V9(20).
       PROCEDURE DIVISION USING BY VALUE N.
           MOVE N TO E
           DISPLAY "VALUE=" E
           GOBACK.
       END PROGRAM PB289V.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB289C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC +9(5).9(20).
       LINKAGE SECTION.
       01 N PIC S9(5)V9(20).
       PROCEDURE DIVISION USING N.
           MOVE N TO E
           DISPLAY "CONTENT=" E
           GOBACK.
       END PROGRAM PB289C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB289W.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(9).
       PROCEDURE DIVISION USING BY VALUE N.
           DISPLAY "WIDE=" N
           GOBACK.
       END PROGRAM PB289W.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB289P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC +9(5).9(20).
       LINKAGE SECTION.
       01 N PIC S9(5)V9(20).
       PROCEDURE DIVISION USING BY VALUE N.
           MOVE N TO E
           DISPLAY "PRODUCT=" E
           GOBACK.
       END PROGRAM PB289P.
       END PROGRAM PB289G.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB289K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. FRAC.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E PIC +9(5).9(20).
       LINKAGE SECTION.
       01 LN PIC S9(5)V9(20).
       PROCEDURE DIVISION USING LN.
       M1.
           MOVE LN TO E
           DISPLAY "METHOD=" E.
       END METHOD FRAC.
       END OBJECT.
       END CLASS PB289K.
