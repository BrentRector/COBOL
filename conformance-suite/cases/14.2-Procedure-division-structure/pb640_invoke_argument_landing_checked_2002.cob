*>--- conformance-case
*> id: pb640_invoke_argument_landing_checked_2002
*> rules: [GR-14.2.3-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB640 - the INVOKE lane of the SAME rule. ISO 14.2.3 GR9's second branch names
      *> "a method" alongside the prototyped/NESTED program, so a BY CONTENT / BY VALUE argument
      *> to a numeric method parameter is "a COMPUTE statement without the ROUNDED phrase" into a
      *> record of the formal's description, performed by the ACTIVATING runtime element -
      *> 14.8.2.3.3 rule 2 a) says the same for conformance: "If the formal parameter is numeric,
      *> the conformance rules are the same as for a COMPUTE statement with the argument as the
      *> sending operand and the corresponding formal parameter as the receiving operand".
      *>
      *> This lane ALREADY landed caller-side, and was the second arm of the defect: the landing
      *> was unconditionally the checking-OFF one, so a magnitude the formal cannot hold - 14.7.5
      *> case 3 - stored its low-order digits with no raise even under >>TURN EC-SIZE CHECKING ON,
      *> where no-phrase rule 4 sets EC-SIZE-TRUNCATION to exist. EC-SIZE-TRUNCATION is Table 13
      *> FATAL, so 14.6.13.1.3 runs the activating element's USE declarative and the method is not
      *> entered; RESUME AT NEXT STATEMENT continues after the INVOKE.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>  B1 - BIG is 10**30 into PIC S9(9)V9(9): aligned at scale 9 the result is 10**39, further
      *>    from zero than 18 digit positions permit => case 3 => EC-SIZE-TRUNCATION, and M= does
      *>    not print.
      *>  B2 - the LITERAL argument arm: 123456789012345678 aligned at scale 2 is
      *>    12345678901234567800, past the 6 digit positions of PIC S9(4)V99 => EC-SIZE-TRUNCATION.
      *>  B3 - the EXPRESSION argument arm: BIG + 0 is the same 10**30 => EC-SIZE-TRUNCATION.
      *>  B4 - the in-range control: 123.456 fits PIC S9(9)V9(9) exactly, so the method runs and
      *>    prints +000000123.456000000.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640IA02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB640IC02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB640IC02.
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
           INVOKE PB640IC02 "NEW" RETURNING OBJ
           DISPLAY "B1"
           INVOKE OBJ "WIDE" USING BY CONTENT BIG
           DISPLAY "B2"
           INVOKE OBJ "SMALL" USING BY CONTENT 123456789012345678
           DISPLAY "B3"
           INVOKE OBJ "WIDE" USING BY CONTENT (BIG + 0)
           DISPLAY "B4"
           INVOKE OBJ "WIDE" USING BY CONTENT FITS
           DISPLAY "B5"
           STOP RUN.
       END PROGRAM PB640IA02.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB640IC02 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WIDE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E1 PIC +9(9).9(9).
       LINKAGE SECTION.
       01 LW PIC S9(9)V9(9).
       PROCEDURE DIVISION USING LW.
       M1.
           MOVE LW TO E1
           DISPLAY "M=" E1.
       END METHOD WIDE.
       METHOD-ID. SMALL.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E2 PIC +9(4).99.
       LINKAGE SECTION.
       01 LS PIC S9(4)V99.
       PROCEDURE DIVISION USING LS.
       M2.
           MOVE LS TO E2
           DISPLAY "S=" E2.
       END METHOD SMALL.
       END OBJECT.
       END CLASS PB640IC02.
