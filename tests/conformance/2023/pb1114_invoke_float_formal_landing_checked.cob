      *> kb/Work PB1114 - the INVOKE lane of pb1114_call_float_formal_landing_checked.
      *> 14.8.2.3.3 2) a) (a method is one of rule 2's activated elements) and 14.2.3 GR9 make a
      *> BY CONTENT argument into a FLOATING-POINT method formal "a COMPUTE statement without the
      *> ROUNDED phrase"; under EC-SIZE checking a value further from zero than the formal's format
      *> permits is 14.7.5 case 3, whose no-phrase rule 4 sets EC-SIZE-TRUNCATION in the INVOKING
      *> element, before the method is entered. With checking off, the no-phrase disposition
      *> (CONFORMANCE DOC-A.1-70) lands the value nearer to zero (14.7.4.3 rule 10), the largest
      *> finite binary32.
      *>
      *> EXPECTED OUTPUT, DERIVED:
      *>  C1  HUGE is 1.0E+300 > the binary32 maximum (about 3.4E+38) => EC-SIZE-TRUNCATION, the
      *>      method is NOT entered (no S= line), the declarative runs, RESUME AT NEXT STATEMENT
      *>      continues after the INVOKE.
      *>  C2  the expression HUGE + 0 is the same value through the expression arm: the same raise.
      *>  C3  OKV is 1.5, exactly representable: nothing raised, S=IN-RANGE.
      *>  C4  checking OFF: the largest finite binary32, above 1.0E+38 and below 1.0E+39:
      *>      S=MAX-FINITE.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114IK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1114IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB1114IC.
       01 HUGE USAGE FLOAT-LONG VALUE 1.0E+300.
       01 OKV USAGE FLOAT-LONG VALUE 1.5.
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
           INVOKE PB1114IC "NEW" RETURNING OBJ
           DISPLAY "C1"
           INVOKE OBJ "TAKE" USING BY CONTENT HUGE
           DISPLAY "C2"
           INVOKE OBJ "TAKE" USING BY CONTENT (HUGE + 0)
           DISPLAY "C3"
           INVOKE OBJ "TAKE" USING BY CONTENT OKV
           DISPLAY "C4"
       >>TURN EC-SIZE CHECKING OFF
           INVOKE OBJ "TAKE" USING BY CONTENT HUGE
           DISPLAY "C5"
           STOP RUN.
       END PROGRAM PB1114IK.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1114IC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       M1.
           EVALUATE TRUE
               WHEN LK > 1.0E+38 AND LK < 1.0E+39
                   DISPLAY "S=MAX-FINITE"
               WHEN LK = 1.5
                   DISPLAY "S=IN-RANGE"
               WHEN OTHER
                   DISPLAY "S=OTHER"
           END-EVALUATE.
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB1114IC.
