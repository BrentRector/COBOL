*>--- conformance-case
*> id: pb814-raising-interface-not-conforming
*> rules: [SR-14.9.14.3-5, SR-14.9.18.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB814 -- ISO 14.9.18.3 SR4 b): "the interface referenced by that interface-name shall conform
      *> to an interface specified in the RAISING phrase".  9.3.8.2.3: interface-1 conforms to interface-2 iff
      *> "for every method in interface-2 there is a method in interface-1 with the same name" satisfying the
      *> numbered conditions.  IN814B (identifier-1's interface) has no method PING, which IN814A (the listed
      *> interface) requires, so IN814B does not conform -> COBOLNET0849 (SR4b).  The header itself is legal
      *> (14.2.2 SR9) -- before PB814 it was the header that was refused (COBOLNET0858).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PN814A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE IN814A
           INTERFACE IN814B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE IN814B.
       PROCEDURE DIVISION RAISING IN814A.
       MAIN-P.
           GOBACK RAISING F.
       END PROGRAM PN814A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IN814A.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE IN814A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IN814B.
       END INTERFACE IN814B.
