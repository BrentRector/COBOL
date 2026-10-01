      *> kb/Work PB1114 (train review finding) - a FLOATING-POINT argument BY CONTENT into a FIXED-POINT
      *> method formal. 14.8.2.3.3 2) a) makes a BY CONTENT numeric crossing conform "the same as for a
      *> COMPUTE statement with the argument as the sending operand", and 14.2.3 GR9 fills the formal's
      *> record by "a COMPUTE statement without the ROUNDED phrase" - a COMPUTE's sending operand may be a
      *> floating-point item, so there is no float exemption on the SENDER. The compiler rendered the
      *> image-carried formal's store from a MOVE with the formal's profile BARE (a Roslyn CS0103).
      *>
      *> EXPECTED OUTPUT, DERIVED (every sender is exactly representable in binary, so the truncation of
      *> 14.7.4.3 rule 2 is unambiguous):
      *>  C1  BIG = 123.755859375 into 9(3)V99 COMP: truncated to 123.75 (a rounding store would give
      *>      123.76): LK displays 12375.
      *>  C2  the same sender into an image-carried 9(3)V99 DISPLAY formal that has a REDEFINES view:
      *>      LI = 12375, LR = "12375" (the formal's own record holds the landed digits).
      *>  C3  SM = 7.25 (FLOAT-SHORT) into the same image formal: 00725.
      *>  C4  NEG = -3.5 into an UNSIGNED formal: the absolute value is stored (14.9.25.4 GR6, unsigned receiving item):
      *>      LI = 00350.
      *>  C5  HUGE = 12345.5 does not fit three integer digits: 14.7.5 under EC-SIZE checking,
      *>      rule 4 => EC-SIZE-TRUNCATION in the INVOKING element, the method is NOT entered,
      *>      RESUME AT NEXT STATEMENT continues after the INVOKE.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114FK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1114FC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB1114FC.
       01 BIG USAGE FLOAT-LONG VALUE 123.755859375.
       01 SM USAGE FLOAT-SHORT VALUE 7.25.
       01 NEG USAGE FLOAT-LONG VALUE -3.5.
       01 HUGE USAGE FLOAT-LONG VALUE 12345.5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       H-P.
           DISPLAY "EC=" FUNCTION TRIM(FUNCTION EXCEPTION-STATUS).
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB1114FC "NEW" RETURNING OBJ
           DISPLAY "C1"
           INVOKE OBJ "TAKEN" USING BY CONTENT BIG
           DISPLAY "C2"
           INVOKE OBJ "TAKEI" USING BY CONTENT BIG
           DISPLAY "C3"
           INVOKE OBJ "TAKEI" USING BY CONTENT SM
           DISPLAY "C4"
           INVOKE OBJ "TAKEI" USING BY CONTENT NEG
           DISPLAY "C5"
           INVOKE OBJ "TAKEI" USING BY CONTENT HUGE
           DISPLAY "C6"
           STOP RUN.
       END PROGRAM PB1114FK.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1114FC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(3)V99 COMP.
       PROCEDURE DIVISION USING LK.
       M1.
           DISPLAY "N LK=" LK.
       END METHOD TAKEN.
       METHOD-ID. TAKEI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LI PIC 9(3)V99.
       01 LR REDEFINES LI PIC X(5).
       PROCEDURE DIVISION USING LI.
       M1.
           DISPLAY "I LI=" LI " LR=" LR.
       END METHOD TAKEI.
       END OBJECT.
       END CLASS PB1114FC.
