*>--- conformance-case
*> id: pb132-call-byref-object-data
*> rules: [SR-14.9.4.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR3 sentence 2: with BY REFERENCE specified or implied, identifier-2 shall not be
      *> defined in the working-storage or file section of a factory or an instance object. The predicate
      *> (DataBinder.OoIsObjectData) had exactly two consumers, both INVOKE (kb/Work PB132).
       IDENTIFICATION DIVISION.
       CLASS-ID. CPB132.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB132.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ-W PIC 9(4).
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       P.
           CALL "SUB" USING OBJ-W
           GOBACK.
       END METHOD M1.
       END OBJECT.
       END CLASS CPB132.
