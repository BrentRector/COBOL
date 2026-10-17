*>--- conformance-case
*> id: pb389-factory-presence-mismatch
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 a)3.: "the presence or absence of the FACTORY phrase
      *> shall be the same as in the description of the data item referenced by
      *> identifier-3."  A FACTORY-described reference holds a class's FACTORY object
      *> (§13.18.60.4 GR22 d)1.a.) and one described without it holds an INSTANCE object
      *> (d)1.b.) — two different objects of two different method interfaces (§9.3.6), so the
      *> axis is INVARIANT in both directions, never widening.  Until kb/Work PB389 the
      *> compiler carried no FACTORY field to compare: the rule was "enforced" only by the
      *> FACTORY OF description being refused outright.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB389N5C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF PB389N5C.
       01 I USAGE OBJECT REFERENCE PB389N5C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB389N5C "NEW" RETURNING I.
           SET F TO I.
           STOP RUN.
       END PROGRAM PB389N5.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N5C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB389N5C.
