*>--- conformance-case
*> id: pb972-raising-class-not-in-interface
*> rules: [GR-9.3.8.2.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB972 - REJECT half of 2002/pb972_raising_conformance.
      *> ISO 9.3.8.2.3 rule 9: "If the RAISING phrase is specified in the
      *> procedure division header of the method in interface-1, the
      *> corresponding method in interface-2 specifies the RAISING phrase
      *> following these rules" - b) an object-class-name needs the same
      *> class or a superclass (same FACTORY presence), or an interface
      *> the object implements. The class method raises PB972NBE; the
      *> interface's method has NO RAISING phrase at all (the measured
      *> repro of the note: it compiled and ran clean).
      *> Expected: COBOLNET0841 naming rule 9 b)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972NBI.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION.
       END METHOD WORK.
       END INTERFACE PB972NBI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NBE.
       END CLASS PB972NBE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972NBC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972NBE
           INTERFACE PB972NBI.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS PB972NBI.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION RAISING PB972NBE.
           DISPLAY "IN WORK".
       END METHOD WORK.
       END OBJECT.
       END CLASS PB972NBC.
