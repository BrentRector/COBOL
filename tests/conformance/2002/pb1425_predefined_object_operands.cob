      *> kb/Work PB1425 - SELF and the object-view as identifiers in every SENDING position, not only as an INVOKE
      *> receiver, a SET Format-5 sender or a RAISE operand.
      *>
      *> THE RULES. ISO/IEC 1989:2023 8.4.3.1.3 SR1: "whenever the format for an identifier allows another identifier
      *> to be specified, that other identifier may be any of the formats for an identifier"; 8.4.3.1.2 Format 5 is the
      *> object-view and Format 6 SELF / [object-class-name-1 OF] SUPER. 8.4.3.8.3 SR7: "SELF and SUPER are both
      *> implicitly described as class object and category object reference"; 8.4.3.8.4 GR1: they "reference the
      *> object that was used to invoke the method". 8.8.4.2.15: "An operand of class object may be compared with
      *> another operand of class object" (reference identity). 14.9.18.3 SR4 / 14.9.14.3 SR5: the RAISING phrase's
      *> identifier-1 "shall be an object reference" - any identifier format that is one.
      *>
      *> THE CLASSES. PB1425PA defines TAKE (a universal formal), TAKEV (a BY VALUE formal described PB1425PA), ISME
      *> (returns Y when its argument is SELF), SPEAK (ANIMAL), WORK1 and WORK2 (raise SELF), and a factory method FCMP.
      *> PB1425PB INHERITS FROM PB1425PA, overrides SPEAK (DOG) and defines DOIT. The program makes a PB1425PB.
      *>
      *> EXPECTED OUTPUT, line by line:
      *>   EQ1, EQ2   O and A were SET TO SELF, so each references the object SELF references (8.8.4.2.15).
      *>   EQ3        O AS PB1425PA (a view, 8.4.3.5.4 GR1) references that object too.
      *>   NN         SELF is not NULL: it references the object DOIT was invoked on.
      *>   EV         EVALUATE SELF WHEN O selects the first WHEN (a relation of class object operands).
      *>   TOOK DOG   (twice) USING SELF and USING BY CONTENT SELF pass the object; TAKE invokes SPEAK on it, and the
      *>              object is a PB1425PB, so the override runs (8.4.3.8.4 GR2's runtime-class binding).
      *>   TOOKV DOG  USING BY VALUE SELF into the PB1425PA formal.
      *>   TOOK DOG   USING BY CONTENT O AS PB1425PB - the view is an identifier-5 of class object.
      *>   TOOKV DOG  USING BY VALUE O AS PB1425PA.
      *>   Y, Y       SELF :: "ISME" (SELF) and SELF :: "ISME" (A AS UNIVERSAL) - inline-invocation arguments.
      *>   LEN        FUNCTION LENGTH(SELF) = FUNCTION LENGTH(O): SELF is a 15.3 type-11 Object argument ("An object
      *>              reference shall be specified; the predefined object reference SUPER shall not be specified").
      *>   FEQ        in the factory method FCMP, SELF is the factory object; F was SET TO it.
      *>   HANDLED SAME  (twice) WORK1 does GOBACK RAISING SELF and WORK2 EXIT METHOD RAISING U AS PB1425PA (U holds
      *>   AFTER-n       SELF); the declarative receives that object as EXCEPTION-OBJECT (14.9.18.4 GR1 b) 2.), it
      *>              is the program's OB, and control returns after the INVOKE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425PM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425PA
           CLASS PB1425PB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OB USAGE OBJECT REFERENCE PB1425PB.
       01 X USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PB1425PA.
       ERR-P.
           SET X TO EXCEPTION-OBJECT
           IF X = OB DISPLAY "HANDLED SAME" ELSE DISPLAY "HANDLED OTHER"
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB1425PB "NEW" RETURNING OB
           INVOKE OB "DOIT"
           INVOKE PB1425PA "FCMP"
           INVOKE OB "WORK1"
           DISPLAY "AFTER-1"
           INVOKE OB "WORK2"
           DISPLAY "AFTER-2"
           STOP RUN.
       END PROGRAM PB1425PM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425PA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1425PA.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FCMP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF PB1425PA.
       PROCEDURE DIVISION.
           SET F TO SELF
           IF F = SELF DISPLAY "FEQ" ELSE DISPLAY "FNE" END-IF.
       END METHOD FCMP.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING P.
           DISPLAY "TOOK"
           INVOKE P "SPEAK".
       END METHOD TAKE.
       METHOD-ID. TAKEV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P USAGE OBJECT REFERENCE PB1425PA.
       PROCEDURE DIVISION USING BY VALUE P.
           DISPLAY "TOOKV"
           INVOKE P "SPEAK".
       END METHOD TAKEV.
       METHOD-ID. ISME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P USAGE OBJECT REFERENCE.
       01 R PIC X.
       PROCEDURE DIVISION USING P RETURNING R.
           IF P = SELF MOVE "Y" TO R ELSE MOVE "N" TO R END-IF.
       END METHOD ISME.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "ANIMAL".
       END METHOD SPEAK.
       METHOD-ID. WORK1.
       PROCEDURE DIVISION RAISING PB1425PA.
           GOBACK RAISING SELF.
       END METHOD WORK1.
       METHOD-ID. WORK2.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION RAISING PB1425PA.
           SET U TO SELF
           EXIT METHOD RAISING U AS PB1425PA.
       END METHOD WORK2.
       END OBJECT.
       END CLASS PB1425PA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425PB INHERITS FROM PB1425PA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425PA
           CLASS PB1425PB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "DOG".
       END METHOD SPEAK.
       METHOD-ID. DOIT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 A USAGE OBJECT REFERENCE PB1425PA.
       01 W PIC X.
       01 L1 PIC 9(4).
       01 L2 PIC 9(4).
       PROCEDURE DIVISION.
           SET O TO SELF
           SET A TO SELF
           IF O = SELF DISPLAY "EQ1" END-IF
           IF SELF = A DISPLAY "EQ2" END-IF
           IF O AS PB1425PA = SELF DISPLAY "EQ3" END-IF
           IF NOT SELF = NULL DISPLAY "NN" END-IF
           EVALUATE SELF
             WHEN O DISPLAY "EV"
             WHEN OTHER DISPLAY "EV-BAD"
           END-EVALUATE
           INVOKE SELF "TAKE" USING SELF
           INVOKE SELF "TAKE" USING BY CONTENT SELF
           INVOKE SELF "TAKEV" USING BY VALUE SELF
           INVOKE SELF "TAKE" USING BY CONTENT O AS PB1425PB
           INVOKE SELF "TAKEV" USING BY VALUE O AS PB1425PA
           MOVE SELF :: "ISME" (SELF) TO W
           DISPLAY W
           MOVE SELF :: "ISME" (A AS UNIVERSAL) TO W
           DISPLAY W
           COMPUTE L1 = FUNCTION LENGTH(SELF)
           COMPUTE L2 = FUNCTION LENGTH(O)
           IF L1 = L2 DISPLAY "LEN" END-IF.
       END METHOD DOIT.
       END OBJECT.
       END CLASS PB1425PB.
