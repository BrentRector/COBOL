*>--- conformance-case
*> id: oo_instance_data
*> rules: [GR-16.2.1.2-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.2/§11.7 (OO COBOL) — typed-native object data is PER-INSTANCE (ADR §7): a class's
      *> OBJECT WORKING-STORAGE becomes per-instance .NET fields, so two objects hold INDEPENDENT state. BOX has
      *> object datum V (PIC X(3) VALUE "---"); BUMP displays V then mutates it to "XYZ". The sequence
      *> b1.BUMP, b2.BUMP, b1.BUMP must print  ---/---/XYZ  (a single shared/static field would print ---/XYZ/XYZ).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOINST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 USAGE OBJECT REFERENCE BOX.
       01 B2 USAGE OBJECT REFERENCE BOX.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE BOX "NEW" RETURNING B1.
           INVOKE BOX "NEW" RETURNING B2.
           INVOKE B1 "BUMP".
           INVOKE B2 "BUMP".
           INVOKE B1 "BUMP".
           STOP RUN.
       END PROGRAM OOINST.

       IDENTIFICATION DIVISION.
       CLASS-ID. BOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC X(3) VALUE "---".
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY V.
           MOVE "XYZ" TO V.
       END METHOD BUMP.
       END OBJECT.
       END CLASS BOX.
