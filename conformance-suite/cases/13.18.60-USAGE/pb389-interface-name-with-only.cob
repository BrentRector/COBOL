*>--- conformance-case
*> id: pb389-interface-name-with-only
*> rules: [GR-13.18.60.4-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.2: the USAGE OBJECT REFERENCE general format stacks THREE alternatives
      *> in one bracket pair — interface-name-1 alone, [FACTORY OF] ACTIVE-CLASS, and
      *> [FACTORY OF] object-class-name-1 [ONLY].  ONLY belongs to the THIRD alternative and
      *> to no other, and §13.18.60.4 GR22 c) states the interface reading with no subordinate
      *> rules at all ("If interface-name-1 is specified, the object referenced by this data
      *> item shall implement interface-1") while the ONLY reading, GR22 d)2., is stated only
      *> for an object-class-name.  The grammar cannot tell an interface-name from an
      *> object-class-name — both are one user-defined word — so it parses the superset and
      *> the binder makes this rejection once the name resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB389N3I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB389N3I ONLY.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB389N3.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB389N3I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB389N3I.
