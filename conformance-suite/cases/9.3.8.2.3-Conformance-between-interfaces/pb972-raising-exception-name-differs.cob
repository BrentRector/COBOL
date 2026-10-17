*>--- conformance-case
*> id: pb972-raising-exception-name-differs
*> rules: [GR-9.3.8.2.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB972 - REJECT half of 2002/pb972_raising_conformance.
      *> ISO 9.3.8.2.3 rule 9: "If the RAISING phrase is specified in the
      *> procedure division header of the method in interface-1, the
      *> corresponding method in interface-2 specifies the RAISING phrase
      *> following these rules" - a) an exception-name needs "the same
      *> exception-name". The class (interface-1, 9.3.11) raises
      *> EC-USER-PB972B where its interface lists only EC-USER-PB972A.
      *> Expected: COBOLNET0841 naming rule 9 a)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972NAI.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION RAISING EC-USER-PB972A.
       END METHOD WORK.
       END INTERFACE PB972NAI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NAC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB972NAI.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS PB972NAI.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION RAISING EC-USER-PB972B.
           DISPLAY "IN WORK".
       END METHOD WORK.
       END OBJECT.
       END CLASS PB972NAC.
