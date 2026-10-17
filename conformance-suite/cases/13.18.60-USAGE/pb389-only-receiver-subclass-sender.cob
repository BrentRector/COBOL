*>--- conformance-case
*> id: pb389-only-receiver-subclass-sender
*> rules: [GR-13.18.60.4-22, SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 a)1.: "if the data item referenced by identifier-3 is
      *> described with an ONLY phrase, the data item referenced by identifier-4 shall be
      *> described with the ONLY phrase, and the object-class-name specified in the
      *> description of the data item referenced by identifier-4 shall be the same as the
      *> object-class-name specified in the description of the data item referenced by
      *> identifier-3."  §13.18.60.4 GR22 d)2.b. is the reason: an ONLY reference "shall
      *> contain … an instance object of the specified class", never a subclass instance —
      *> so the SUBCLASS assignment a)2. permits without ONLY is exactly what a)1. forbids
      *> with it.  The permitted twin (the same SET without ONLY) runs in
      *> tests/conformance/2002/l1_set_sr12a2_class_conformance.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB389N4B
           CLASS PB389N4D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BO USAGE OBJECT REFERENCE PB389N4B ONLY.
       01 D  USAGE OBJECT REFERENCE PB389N4D.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB389N4D "NEW" RETURNING D.
           SET BO TO D.
           STOP RUN.
       END PROGRAM PB389N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N4B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB389N4B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N4D INHERITS FROM PB389N4B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB389N4B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB389N4D.
