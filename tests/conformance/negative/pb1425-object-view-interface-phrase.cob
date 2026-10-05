      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.5.2: the object-view's AS phrase is ONE brace of three alternatives,
      *>   `[FACTORY OF] object-class-name-1 [ONLY]`, `interface-name-1` and `UNIVERSAL`; FACTORY OF and ONLY
      *>   belong to the class alternative alone, and GR6 states the interface reading with neither.
      *>   PB1425W3I is an interface, so `OU AS PB1425W3I ONLY` is refused (COBOLNET2872).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425W3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB1425W3I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OU USAGE OBJECT REFERENCE.
       01 OI USAGE OBJECT REFERENCE PB1425W3I.
       PROCEDURE DIVISION.
           SET OI TO OU AS PB1425W3I ONLY.
           STOP RUN.
       END PROGRAM PB1425W3.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1425W3I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE PB1425W3I.
