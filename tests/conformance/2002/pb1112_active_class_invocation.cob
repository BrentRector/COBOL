      *> kb/Work PB1112 -- an ACTIVE-CLASS formal parameter or returning
      *> item conforms against HOW THE METHOD IS INVOKED.
      *> ISO 14.8.2.3.2 rule 4 (BY REFERENCE): the argument shall be
      *>  a) "an object reference described with the ACTIVE-CLASS phrase
      *>     ... and the method to be activated shall be invoked with the
      *>     predefined object references SELF or SUPER, or with an object
      *>     reference described with the ACTIVE-CLASS phrase", or
      *>  b) "an object reference described with an object-class-name and
      *>     the ONLY phrase ... and the method to be activated shall be
      *>     invoked with that object-class-name or with an object
      *>     reference described with that object-class-name and the ONLY
      *>     phrase".
      *> ISO 14.8.2.3.3 states the BY CONTENT twin as the same two
      *> invocation conditions plus a SET (1: into an ACTIVE-CLASS
      *> receiver, 2: into "that object-class-name and the ONLY phrase").
      *> ISO 14.8.3.3 rule 2 b): an ACTIVE-CLASS returning item sends
      *> "that same object-class-name and an ONLY phrase" when invoked
      *> through it (1.), "an ACTIVE-CLASS phrase" through SELF (2.), and
      *> the reference's own description otherwise (4.).
      *> DERIVATION of the output. PB12A invokes M on X (described C1
      *> ONLY) with X itself BY REFERENCE and BY CONTENT -- alternative b)
      *> / 2): M displays its counter, M#1 and M#2. MR through X returns
      *> a C1 ONLY sender (rule 2 b) 1.-4.), which SET SR12 a)1. admits
      *> into Y (C1 ONLY): Y and X reference the same object, SAME. N runs
      *> in the object: SELF-invoked M with an ACTIVE-CLASS argument BY
      *> REFERENCE, BY CONTENT and SELF itself (alternative a) / 1)) gives
      *> M#3 M#4 M#5, and MR through SELF sends ACTIVE-CLASS into the
      *> ACTIVE-CLASS Z (SR14 a)), which is then that object: N-SAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C12A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X USAGE OBJECT REFERENCE C12A ONLY.
       01 Y USAGE OBJECT REFERENCE C12A ONLY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE C12A "NEW" RETURNING X
           INVOKE X "M" USING BY REFERENCE X
           INVOKE X "M" USING BY CONTENT X
           INVOKE X "MR" RETURNING Y
           IF Y = X
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFFERENT"
           END-IF
           INVOKE X "N"
           STOP RUN.
       END PROGRAM PB12A.
       IDENTIFICATION DIVISION.
       CLASS-ID. C12A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK.
           ADD 1 TO CNT
           DISPLAY "M#" CNT.
       END METHOD M.
       METHOD-ID. MR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF.
       END METHOD MR.
       METHOD-ID. N.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Z USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 W USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
           SET W TO SELF
           INVOKE SELF "M" USING BY REFERENCE W
           INVOKE SELF "M" USING BY CONTENT W
           INVOKE SELF "M" USING SELF
           INVOKE SELF "MR" RETURNING Z
           IF Z = W
               DISPLAY "N-SAME"
           ELSE
               DISPLAY "N-DIFFERENT"
           END-IF.
       END METHOD N.
       END OBJECT.
       END CLASS C12A.
