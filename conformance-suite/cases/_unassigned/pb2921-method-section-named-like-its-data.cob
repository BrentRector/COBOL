      *>--- conformance-case
      *> id: pb2921-method-section-named-like-its-data
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2921 - ISO 8.3.2.2: "Within a source element, a
      *>   given user-defined word may be used as only one type of
      *>   user-defined word" -> OK 8.3.2.2. Method PB2921S is one
      *>   source element, and S1 is both its data-name and its
      *>   section-name: refused COBOLNET2692. The method arm of the
      *>   procedure-name walk declared no section-name, so this
      *>   compiled clean.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2921S INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PB2921S.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 S1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       S1 SECTION.
       S1-P.
           DISPLAY S1.
       END METHOD PB2921S.
       END OBJECT.
       END CLASS PB2921S.
