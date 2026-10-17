*>--- conformance-case
*> id: pb972-raising-interface-not-inherited
*> rules: [GR-9.3.8.2.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB972 - REJECT half of 2002/pb972_raising_conformance.
      *> ISO 9.3.8.2.3 rule 9: "If the RAISING phrase is specified in the
      *> procedure division header of the method in interface-1, the
      *> corresponding method in interface-2 specifies the RAISING phrase
      *> following these rules" - c) an interface-name needs "the same
      *> interface-name or the name of an interface inherited by that
      *> interface". The class raises PB972NCA; its interface lists
      *> PB972NCB, which INHERITS PB972NCA - the WRONG direction.
      *> Expected: COBOLNET0841 naming rule 9 c)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972NCA.
       END INTERFACE PB972NCA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972NCB INHERITS PB972NCA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB972NCA.
       END INTERFACE PB972NCB.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972NCI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB972NCB.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION RAISING PB972NCB.
       END METHOD WORK.
       END INTERFACE PB972NCI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NCC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB972NCA
           INTERFACE PB972NCI.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS PB972NCI.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION RAISING PB972NCA.
           DISPLAY "IN WORK".
       END METHOD WORK.
       END OBJECT.
       END CLASS PB972NCC.
