*>--- conformance-case
*> id: oo_override_final
*> rules: [GR-11.7.4-2, FMT-11.7.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.7 SR3/SR4a/GR3 + §11.3 (OVERRIDE / IS FINAL — the attribute wave, DEVLOG 605):
      *> DOG overrides SPEAK with OVERRIDE IS FINAL (emits C# `sealed override`; virtual dispatch through an
      *> ANIMAL-typed reference still reaches it); KEEPER IS FINAL holds a fresh method GREET (a sealed C#
      *> class with a NON-virtual member — the CS0549 emitted-code regression trap).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOVRFIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS OFANI
           CLASS OFDOG
           CLASS OFKEEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE OFANI.
       01 K USAGE OBJECT REFERENCE OFKEEP.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE OFDOG "NEW" RETURNING A.
           INVOKE A "SPEAK".
           INVOKE OFKEEP "NEW" RETURNING K.
           INVOKE K "GREET".
           STOP RUN.
       END PROGRAM OOVRFIN.

       IDENTIFICATION DIVISION.
       CLASS-ID. OFANI INHERITS FROM BASE.
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
           DISPLAY "GENERIC".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS OFANI.

       IDENTIFICATION DIVISION.
       CLASS-ID. OFDOG INHERITS FROM OFANI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS OFANI.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE IS FINAL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WOOF".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS OFDOG.

       IDENTIFICATION DIVISION.
       CLASS-ID. OFKEEP IS FINAL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HELLO-KEEPER".
       END METHOD GREET.
       END OBJECT.
       END CLASS OFKEEP.
