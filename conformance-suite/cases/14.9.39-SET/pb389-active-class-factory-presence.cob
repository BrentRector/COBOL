*>--- conformance-case
*> id: pb389-active-class-factory-presence
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 b)3.: with an ACTIVE-CLASS sender into a receiver
      *> described with an object-class-name, "the presence or absence of the FACTORY phrase
      *> shall be the same as in the description of the data item referenced by
      *> identifier-3."  The receiver here is described FACTORY OF and so holds a class's
      *> FACTORY object (§13.18.60.4 GR22 d)1.a.); the ACTIVE-CLASS sender carries no FACTORY
      *> phrase and so holds an INSTANCE object (GR22 e)2.).  The axis is invariant in both
      *> directions — the twin without ACTIVE-CLASS is pb389-factory-presence-mismatch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N7.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB389N7.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N7C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 RCVF USAGE OBJECT REFERENCE FACTORY OF PB389N7C.
       01 A  USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
           SET RCVF TO A.
       END METHOD MK.
       END OBJECT.
       END CLASS PB389N7C.
