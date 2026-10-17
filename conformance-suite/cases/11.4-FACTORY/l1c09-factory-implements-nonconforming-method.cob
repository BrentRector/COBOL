      *>--- conformance-case
      *> id: l1c09-factory-implements-nonconforming-method
      *> rules: [SR-11.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.4.3 SR2 — the factory's method for an implemented
      *> interface's prototype does not conform to it.
      *> Rule: "Each method prototype in each implemented interface
      *> shall be such that the factory interface of this class
      *> conforms to all implemented interfaces."
      *> cite.py: OK  §11.4.3 2)  (Syntax rules)
      *> The prototype SPEAK of L1C09O takes a PIC 9(4) parameter; the
      *> factory's SPEAK takes PIC 9(8). Interface conformance
      *> (§9.3.8.2.3) requires the formal parameters to match, so the
      *> factory interface does not conform to L1C09O. SR1 is
      *> satisfied (L1C09O is in the class REPOSITORY).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C09W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(8) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE L1C09W "SPEAK" USING N.
           STOP RUN.
       END PROGRAM L1C09R.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C09O.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(4).
       PROCEDURE DIVISION USING P.
       END METHOD SPEAK.
       END INTERFACE L1C09O.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C09W.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C09O.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS L1C09O.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(8).
       PROCEDURE DIVISION USING P.
           DISPLAY P.
       END METHOD SPEAK.
       END FACTORY.
       END CLASS L1C09W.
