*>--- conformance-case
*> id: pb451-active-class-receiver-in-factory-method
*> rules: [GR-13.18.60.4-22, SR-14.9.39.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 14 b)1.: with identifier-3 described with an ACTIVE-CLASS
      *> phrase and the sender "the predefined object SELF", "if the data item referenced by
      *> identifier-3 is described without a FACTORY phrase, the method containing the SET
      *> statement shall be defined in the instance definition of its containing class."
      *> §13.18.60.4 GR22 e)2. is the semantics being protected: without FACTORY the item
      *> shall hold "an instance object of that class", and SELF in a factory method is not
      *> one.  ⛔ SR14 b)1. is WORD FOR WORD SR12 c)3. — `cite.py --check` on the sentence
      *> returns rule 12, never 14 — which is precisely why the compiler runs ONE placement
      *> predicate over the receiver's FACTORY axis and lets the receiver's KIND choose the
      *> clause it names.  The conforming twin is the MK method of
      *> conformance:2002/pb389_object_reference_descriptor (SET A TO SELF, prints DERIVED).

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N7.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N7C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N7C "MK".
           STOP RUN.
       END PROGRAM PB451N7.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N7C.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG".
       END METHOD TAG.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD MK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO".
       END METHOD WHO.
       END OBJECT.
       END CLASS PB451N7C.
