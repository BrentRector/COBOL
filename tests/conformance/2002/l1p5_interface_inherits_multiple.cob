      *> ISO §11.6.4 GR2 — INHERITS names several interfaces and the
      *> inheriting interface has every method specification of each,
      *> transitively (9.3.10).
      *> 11.6.4 GR2: "The INHERITS clause specifies the names of
      *>   interfaces that are inherited by interface-name-1 according
      *>   to 9.3.10, Interface inheritance."
      *> 9.3.10: "The inheriting interface has all the method
      *>   specifications defined for the inherited interface
      *>   definition or definitions, including any method
      *>   specifications that the inherited definition or definitions
      *>   inherited."
      *> Shape: L1P5IC INHERITS FROM L1P5IA L1P5IB (two bases, no own
      *> method); L1P5ID INHERITS FROM L1P5IC and adds JUMP. OD is
      *> typed L1P5ID, so 14.9.23.3 SR4e requires each INVOKEd name to
      *> be "a method contained in the interface referenced by that
      *> interface-name": SPEAK (from L1P5IA, two levels up) and STEPS
      *> (from L1P5IB, whose RETURNING prototype is inherited whole)
      *> are contained only through GR2. Expected trace:
      *>   SPEAK          (L1P5K's SPEAK via the L1P5IA specification)
      *>   STEPS=42       (L1P5K's STEPS returns 42 into N PIC 99)
      *>   JUMP           (L1P5ID's own method)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1P5IFM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1P5K
           INTERFACE L1P5ID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OD USAGE OBJECT REFERENCE L1P5ID.
       01 N  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1P5K "NEW" RETURNING OD.
           INVOKE OD "SPEAK".
           MOVE 0 TO N.
           INVOKE OD "STEPS" RETURNING N.
           DISPLAY "STEPS=" N.
           INVOKE OD "JUMP".
           STOP RUN.
       END PROGRAM L1P5IFM.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1P5IA.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1P5IA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1P5IB.
       PROCEDURE DIVISION.
       METHOD-ID. STEPS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-N PIC 99.
       PROCEDURE DIVISION RETURNING LS-N.
       END METHOD STEPS.
       END INTERFACE L1P5IB.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1P5IC INHERITS FROM L1P5IA L1P5IB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1P5IA
           INTERFACE L1P5IB.
       END INTERFACE L1P5IC.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1P5ID INHERITS FROM L1P5IC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1P5IC.
       PROCEDURE DIVISION.
       METHOD-ID. JUMP.
       PROCEDURE DIVISION.
       END METHOD JUMP.
       END INTERFACE L1P5ID.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1P5K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE L1P5ID.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1P5ID.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SPEAK".
       END METHOD SPEAK.
       METHOD-ID. STEPS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-N PIC 99.
       PROCEDURE DIVISION RETURNING LS-N.
       MAIN.
           MOVE 42 TO LS-N.
       END METHOD STEPS.
       METHOD-ID. JUMP.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "JUMP".
       END METHOD JUMP.
       END OBJECT.
       END CLASS L1P5K.
