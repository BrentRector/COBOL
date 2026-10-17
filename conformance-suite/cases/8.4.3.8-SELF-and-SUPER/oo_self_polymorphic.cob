*>--- conformance-case
*> id: oo_self_polymorphic
*> rules: [GR-8.4.3.8.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §8.4.3.8 / §11.7 — POLYMORPHIC INVOKE SELF across inheritance. ANIMAL.DESCRIBE (inherited by
      *> DOG) calls INVOKE SELF "SOUND". On a DOG instance, SELF SOUND dispatches virtually (callvirt) to DOG's
      *> override → "WOOF", NOT the base ANIMAL.SOUND "GENERIC". Proves INVOKE SELF is virtual + composes with
      *> INHERITS + multi-method.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. POLYSELF.
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
           INVOKE D "DESCRIBE".
           STOP RUN.
       END PROGRAM POLYSELF.

       IDENTIFICATION DIVISION.
       CLASS-ID. ANIMAL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DESCRIBE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DESCRIBING:".
           INVOKE SELF "SOUND".
       END METHOD DESCRIBE.
       METHOD-ID. SOUND.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "GENERIC".
       END METHOD SOUND.
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
       METHOD-ID. SOUND OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WOOF".
       END METHOD SOUND.
       END OBJECT.
       END CLASS DOG.
