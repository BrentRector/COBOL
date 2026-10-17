*>--- conformance-case
*> id: pb2463_call_object_reference_set_conformance
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2463 (sibling arms) - a Format-1 CALL whose program
      *> is located at run time checks an OBJECT REFERENCE argument and
      *> RETURNING item by the SET rules. 14.9.4.4 GR3 d) (cite.py
      *> --check -> OK 14.9.4.4 3)): "the rules for conformance specified
      *> in 14.8.2, Parameters and 14.8.3, Returning items apply"; a
      *> violation is EC-PROGRAM-ARG-MISMATCH. 14.8.2.3.3 (OK 14.8.2.3.3
      *> 2)): a formal that is "an object reference described without
      *> the ACTIVE-CLASS phrase" conforms "as if a SET statement were
      *> performed ... with the argument as the sending operand"; 14.8.3.3
      *> rule 1 (OK 14.8.3.3 1)) the same with the activated program's
      *> returning item as the sender. The SET is 14.9.39.3 SR12 a)2.:
      *> "the same class or a subclass" (OK 14.9.39.3 12)).
      *> C2463T INHERITS C2463R; C2463U is unrelated. DERIVATION:
      *>   1 BY CONTENT C2463T -> formal C2463T   same class       OK
      *>   2 BY CONTENT C2463U -> formal C2463T   unrelated        EXC
      *>   3 RETURNING C2463T  -> receiver C2463R a subclass       OK
      *>   4 RETURNING C2463T  -> receiver C2463U unrelated        EXC
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2463C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463R
           CLASS C2463T
           CLASS C2463U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OR1 USAGE OBJECT REFERENCE C2463R.
       01 OT1 USAGE OBJECT REFERENCE C2463T.
       01 OU1 USAGE OBJECT REFERENCE C2463U.
       PROCEDURE DIVISION.
           INVOKE C2463T "NEW" RETURNING OT1
           INVOKE C2463U "NEW" RETURNING OU1
           CALL "PB2463CT" USING BY CONTENT OT1
               ON EXCEPTION DISPLAY "1 T->T CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "1 T->T CONTENT OK"
           END-CALL
           CALL "PB2463CT" USING BY CONTENT OU1
               ON EXCEPTION DISPLAY "2 U->T CONTENT EXC"
               NOT ON EXCEPTION DISPLAY "2 U->T CONTENT OK"
           END-CALL
           CALL "PB2463CQ" RETURNING OR1
               ON EXCEPTION DISPLAY "3 T->R RETURNING EXC"
               NOT ON EXCEPTION DISPLAY "3 T->R RETURNING OK"
           END-CALL
           CALL "PB2463CQ" RETURNING OU1
               ON EXCEPTION DISPLAY "4 T->U RETURNING EXC"
               NOT ON EXCEPTION DISPLAY "4 T->U RETURNING OK"
           END-CALL
           STOP RUN.
       END PROGRAM PB2463C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2463CT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT USAGE OBJECT REFERENCE C2463T.
       PROCEDURE DIVISION USING BY VALUE LT.
           GOBACK.
       END PROGRAM PB2463CT.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2463CQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ USAGE OBJECT REFERENCE C2463T.
       PROCEDURE DIVISION RETURNING LQ.
           INVOKE C2463T "NEW" RETURNING LQ
           GOBACK.
       END PROGRAM PB2463CQ.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463R INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS C2463R.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463T INHERITS FROM C2463R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463R.
       END CLASS C2463T.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463U INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS C2463U.
