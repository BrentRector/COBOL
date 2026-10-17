*>--- conformance-case
*> id: oo_super
*> rules: [SR-8.4.3.8.3-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §8.4.3.8 (SUPER) — an override calls the base class's version via INVOKE SUPER. DOG.SPEAK
      *> overrides ANIMAL.SPEAK, first INVOKE SUPER "SPEAK" (the base runs, prints ANIMAL) then prints DOG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ANIMAL
           CLASS DOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE DOG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DOG "NEW" RETURNING D.
           INVOKE D "SPEAK".
           STOP RUN.
       END PROGRAM OOSUP.
       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ANIMAL".
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
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SUPER "SPEAK".
           DISPLAY "DOG".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS DOG.
