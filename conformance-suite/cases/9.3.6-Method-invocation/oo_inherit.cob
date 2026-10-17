*>--- conformance-case
*> id: oo_inherit
*> rules: [GR-9.3.6-L2.1, GR-11.3.4-2, GR-13.18.60.4-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.3/§11.7 (OO COBOL) — INHERITS FROM + polymorphism. DOG INHERITS FROM ANIMAL and
      *> overrides SPEAK; invoking SPEAK through an ANIMAL-typed reference that holds a DOG dispatches to the
      *> override (virtual dispatch). A DOG with no override would inherit ANIMAL.SPEAK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPOLY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ANIMAL.
       01 D USAGE OBJECT REFERENCE DOG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ANIMAL "NEW" RETURNING A.
           INVOKE A "SPEAK".
           INVOKE DOG "NEW" RETURNING D.
           INVOKE D "SPEAK".
           INVOKE DOG "NEW" RETURNING A.
           INVOKE A "SPEAK".
           STOP RUN.
       END PROGRAM OOPOLY.

       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(8) VALUE "GENERIC".
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY MSG.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS ANIMAL.

       IDENTIFICATION DIVISION.
       CLASS-ID. DOG INHERITS FROM ANIMAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WOOF".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
