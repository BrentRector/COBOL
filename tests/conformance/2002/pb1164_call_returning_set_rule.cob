      *> ISO 14.8.3.3 rule 1: "If the returning item in the activated element is not described with an
      *> ACTIVE-CLASS phrase, the conformance rules are the same as if a SET statement were performed in the
      *> activated runtime element with the returning item in the activated element as the sending operand and
      *> the corresponding returning item in the activating element as the receiving operand", and 14.9.4.3 SR25
      *> imports 14.8.3 into a Format-2 CALL. SET SR12 a)2. admits a sender of a SUBCLASS into a receiver of the
      *> class, and SR8 constrains nothing for a universal receiver. The activated program returns an object
      *> reference described P1164DERV (a subclass of P1164BASE):
      *>   CLASS - into a receiver described P1164BASE: conforms, and INVOKE reaches the overriding method, DERIVED.
      *>   UNIV  - into a universal receiver: conforms (SR8), and INVOKE reaches the same method, DERIVED.
      *> 8.4.3.2.4 GR1 gives a function-identifier's temporary the RETURNING item's description with no category
      *> excluded, so an object-reference result is a legal function-identifier too:
      *>   FOBJ  - a function returning a new object compares NOT = NULL.
      *>   FNUL  - a function returning NULL compares = NULL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1164FOBJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1164DERV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-O USAGE OBJECT REFERENCE P1164DERV.
       PROCEDURE DIVISION USING L-X RETURNING L-O.
       P.
           INVOKE P1164DERV "NEW" RETURNING L-O.
           GOBACK.
       END FUNCTION P1164FOBJ.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1164FNUL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1164DERV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-O USAGE OBJECT REFERENCE P1164DERV.
       PROCEDURE DIVISION USING L-X RETURNING L-O.
       P.
           SET L-O TO NULL.
           GOBACK.
       END FUNCTION P1164FNUL.
       IDENTIFICATION DIVISION.
       CLASS-ID. P1164BASE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "BASE".
       END METHOD WHO.
       END OBJECT.
       END CLASS P1164BASE.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1164DERV INHERITS FROM P1164BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1164BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "DERIVED".
       END METHOD WHO.
       END OBJECT.
       END CLASS P1164DERV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1164MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1164BASE
           CLASS P1164DERV
           FUNCTION P1164FOBJ
           FUNCTION P1164FNUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE P1164BASE.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "P1164MKR" AS NESTED RETURNING B.
           INVOKE B "WHO".
           CALL "P1164MKR" AS NESTED RETURNING U.
           INVOKE U "WHO".
           IF FUNCTION P1164FOBJ(1) NOT = NULL DISPLAY "FOBJ=OBJECT"
               ELSE DISPLAY "FOBJ=NULL".
           IF FUNCTION P1164FNUL(1) = NULL DISPLAY "FNUL=NULL"
               ELSE DISPLAY "FNUL=OBJECT".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1164MKR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE P1164DERV.
       PROCEDURE DIVISION RETURNING L-O.
       P.
           INVOKE P1164DERV "NEW" RETURNING L-O.
           GOBACK.
       END PROGRAM P1164MKR.
       END PROGRAM P1164MAIN.

