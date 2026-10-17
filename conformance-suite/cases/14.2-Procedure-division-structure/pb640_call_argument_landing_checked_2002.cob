*>--- conformance-case
*> id: pb640_call_argument_landing_checked_2002
*> rules: [GR-14.2.3-9, GR-14.2.3-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB640 - the ACTIVATING element performs the GR9/GR10 argument crossing, so a
      *> BY CONTENT / BY VALUE numeric argument that overflows its formal's description raises
      *> EC-SIZE-TRUNCATION in the CALLER, under the CALLER's checking, before control transfers.
      *>
      *> THE RULE. ISO 14.2.3 GR9 (second branch: a prototyped program, an AS NESTED CALL, a
      *> method or a function) and GR10 both say the linkage record is "allocated by the
      *> activating runtime element during the process of initiating the activation", and both
      *> make the argument the sending operand of - "if the formal parameter is numeric, a
      *> COMPUTE statement without the ROUNDED phrase" - into it. A magnitude the formal cannot
      *> hold is 14.7.5 case 3, "after radix point alignment ... further from zero than permitted
      *> for the associated resultant data item", whose no-SIZE-ERROR-phrase rule 4 states: "if
      *> the result of the arithmetic statement is a value further from zero than permitted for
      *> the associated resultant data item, the EC-SIZE-TRUNCATION exception condition is set to
      *> exist". EC-SIZE-TRUNCATION is Table 13 FATAL, and 14.9.4.4 GR3 g) transfers control to
      *> the called program only "if a fatal exception condition has not been raised" - so the
      *> callee is NOT entered, GR3 h) item 2 runs the activating element's applicable exception
      *> processing statements (this USE declarative), and RESUME AT NEXT STATEMENT continues
      *> after the CALL. Before PB640 the whole crossing ran CALLEE-side, where the caller's
      *> checking state and declaratives are out of scope, so every row below printed the callee's
      *> low-order digits silently.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>  A1/A2 - BIG is 10**30 and the formal is PIC S9(9)V9(9). Aligned at scale 9 the result
      *>    is 10**39, which is further from zero than 18 digit positions permit => case 3 =>
      *>    EC-SIZE-TRUNCATION. BY CONTENT (GR9) and BY VALUE (GR10) are the SAME COMPUTE, so the
      *>    two rows must agree, and neither REF= nor VAL= may print.
      *>  A3 - the in-range control: 123.456 fits PIC S9(9)V9(9) exactly, so the landing raises
      *>    nothing and the callee prints +000000123.456000000.
      *>  A4 - the GR8 control: BY REFERENCE is "as if the formal parameter occupies the same
      *>    storage area as the argument" - no allocated record, no COMPUTE, no size error - and
      *>    the descriptions are identical, so the same characters print.
      *>  A5 - the LITERAL argument arm. 123456789012345678 aligned at scale 2 is
      *>    12345678901234567800, past the 6 digit positions of PIC S9(4)V99 => case 3 =>
      *>    EC-SIZE-TRUNCATION, and SML= does not print.
      *>  FUNCTION EXCEPTION-STATUS is the level-3 name (14.6.13.1.6), blank-padded to its
      *>    declared length.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640CA02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(31) VALUE 1000000000000000000000000000000.
       01 FITS PIC S9(9)V9(9) VALUE 123.456.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       H-P.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "A1"
           CALL "PB640CB02" AS NESTED USING BY CONTENT BIG
           DISPLAY "A2"
           CALL "PB640CC02" AS NESTED USING BY VALUE BIG
           DISPLAY "A3"
           CALL "PB640CB02" AS NESTED USING BY CONTENT FITS
           DISPLAY "A4"
           CALL "PB640CB02" AS NESTED USING BY REFERENCE FITS
           DISPLAY "A5"
           CALL "PB640CD02" AS NESTED USING BY VALUE 123456789012345678
           DISPLAY "A6"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640CB02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC +9(9).9(9).
       LINKAGE SECTION.
       01 LR PIC S9(9)V9(9).
       PROCEDURE DIVISION USING LR.
       M1.
           MOVE LR TO E1
           DISPLAY "REF=" E1
           GOBACK.
       END PROGRAM PB640CB02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640CC02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E2 PIC +9(9).9(9).
       LINKAGE SECTION.
       01 LV PIC S9(9)V9(9).
       PROCEDURE DIVISION USING BY VALUE LV.
       M2.
           MOVE LV TO E2
           DISPLAY "VAL=" E2
           GOBACK.
       END PROGRAM PB640CC02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640CD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E3 PIC +9(4).99.
       LINKAGE SECTION.
       01 LD PIC S9(4)V99.
       PROCEDURE DIVISION USING BY VALUE LD.
       M3.
           MOVE LD TO E3
           DISPLAY "SML=" E3
           GOBACK.
       END PROGRAM PB640CD02.
       END PROGRAM PB640CA02.
