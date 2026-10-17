*>--- conformance-case
*> id: pb428-inline-invocation-null-receiver
*> rules: [SR-8.4.3.4.3-2, SR-14.9.25.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.4.3 SR2: "Identifier-1 shall be of class object; neither
      *> the predefined object reference NULL nor a universal object reference
      *> shall be specified."  BOTH excluded receivers are written here, and
      *> both are syntactically REACHABLE on purpose: the inline form reuses
      *> INVOKE's own `objectReference` receiver rule so that §8.4.3.4.4 GR1's
      *> equivalence cannot be broken by the two forms disagreeing about what
      *> a receiver is (the superset parse).  SR2 is therefore enforced by a
      *> NAMED bind-time diagnostic, COBOLNET2138, never by the construct's
      *> absence from the grammar.  kb/Work PB428.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB428N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB428N2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 UNI USAGE OBJECT REFERENCE.
       01 W   PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE NULL :: "GETNAME" TO W.
           MOVE UNI :: "GETNAME" TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB428N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428N2C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB428N2C.
