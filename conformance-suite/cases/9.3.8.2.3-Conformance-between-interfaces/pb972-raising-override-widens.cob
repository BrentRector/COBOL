*>--- conformance-case
*> id: pb972-raising-override-widens
*> rules: [GR-9.3.8.2.3-9, SR-11.7.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB972 - REJECT half of 2002/pb972_raising_conformance.
      *> ISO 9.3.8.2.3 rule 9: "If the RAISING phrase is specified in the
      *> procedure division header of the method in interface-1, the
      *> corresponding method in interface-2 specifies the RAISING phrase
      *> following these rules" - the OVERRIDE arm. 11.7.3 SR9 holds an
      *> overriding method's header to 9.3.8.2.3 with the override as
      *> interface-1: PB972NDS's M raises FACTORY OF PB972NDE where the
      *> overridden M lists PB972NDE WITHOUT the FACTORY phrase - rule 9
      *> b) requires "the FACTORY phrase if and only if the RAISING phrase
      *> in interface-1 specifies the FACTORY phrase".
      *> Expected: COBOLNET0829 naming rule 9 b)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NDE.
       END CLASS PB972NDE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NDB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972NDE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION RAISING PB972NDE.
           DISPLAY "BASE M".
       END METHOD M.
       END OBJECT.
       END CLASS PB972NDB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NDS INHERITS PB972NDB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972NDB
           CLASS PB972NDE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       PROCEDURE DIVISION RAISING FACTORY OF PB972NDE.
           DISPLAY "SUB M".
       END METHOD M.
       END OBJECT.
       END CLASS PB972NDS.
