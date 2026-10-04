      *> kb/Work PB892 - the INLINE-INVOCATION and OBJECT-PROPERTY arms of
      *> the operand-activation rule, and the EC-FUNCTION-NOT-FOUND arm of
      *> a per-evaluation function reference. ISO 14.9.33.4 GR2 a) 2.:
      *> "for an inline invocation or a function invocation, it is the
      *> statement in which the inline invocation or function invocation
      *> was specified" that RESUME AT NEXT STATEMENT resumes after;
      *> 14.9.18.4 GR1 b) raises a propagated condition in the activating
      *> element. 8.4.3.9.4 GR1 gets a property value "as though the
      *> associated get property method were invoked, in accordance with
      *> the rules of the INVOKE statement" - written as an operand, so it
      *> takes the operand-activation landing (PB892's determination).
      *>
      *> C1 COMPUTE X = S :: "WORK" + 1 (hoisted): X stays 5.
      *> C2 PERFORM UNTIL S :: "WORK" = 7 (per evaluation): after it.
      *> C3 INVOKE S "WORK" RETURNING X - an INVOKE STATEMENT is its own
      *>    applicable statement, and the result is returned before the
      *>    raise (GR1 b)), so X = 7.
      *> C4 COMPUTE X = P OF S + 1 (the GET accessor): X stays 7.
      *> C5 PERFORM UNTIL FUNCTION PB892MISS = 7 - no definition is
      *>    locatable, EC-FUNCTION-NOT-FOUND (8.4.3.2.4 GR6) is enabled
      *>    and its declarative resumes after the PERFORM.
      *> ISO 10.6.2 SR1: the function prototype precedes every other
      *> source unit of the compilation group.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB892MISS IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
       END FUNCTION PB892MISS.
       >>TURN EC-USER-PB2 EC-FUNCTION-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB892I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB892K
           FUNCTION PB892MISS
           PROPERTY P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE PB892K.
       01 X PIC 99 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HU SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PB2.
       HU-P.
           DISPLAY "DECL-U".
           RESUME AT NEXT STATEMENT.
       HF SECTION. USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
       HF-P.
           DISPLAY "DECL-NF".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB892K "NEW" RETURNING S.
           COMPUTE X = S :: "WORK" + 1.
           DISPLAY "X1=" X.
           PERFORM UNTIL S :: "WORK" = 7
               DISPLAY "BODY-2"
           END-PERFORM.
           DISPLAY "AFTER-2".
           INVOKE S "WORK" RETURNING X.
           DISPLAY "X3=" X.
           COMPUTE X = P OF S + 1.
           DISPLAY "X4=" X.
           PERFORM UNTIL FUNCTION PB892MISS = 7
               DISPLAY "BODY-5"
           END-PERFORM.
           DISPLAY "AFTER-5".
           STOP RUN.
       END PROGRAM PB892I.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB892K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 99.
       PROCEDURE DIVISION RETURNING LK-R RAISING EC-USER-PB2.
       W-P.
           MOVE 7 TO LK-R.
           GOBACK RAISING EXCEPTION EC-USER-PB2.
       END METHOD WORK.
       METHOD-ID. GET PROPERTY P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-P PIC 99.
       PROCEDURE DIVISION RETURNING LK-P RAISING EC-USER-PB2.
       P-P.
           MOVE 40 TO LK-P.
           GOBACK RAISING EXCEPTION EC-USER-PB2.
       END METHOD.
       END OBJECT.
       END CLASS PB892K.
