*>--- conformance-case
*> id: pb987-property-per-evaluation-below-2002
*> rules: [GR-8.8.4.13-2]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/8.4.3.1-Identifier/pb987_property_per_evaluation
      *> (kb/Work PB987). An object property reference in a repeated
      *> condition (ISO 8.8.4.13 2)) needs the REPOSITORY paragraph's
      *> PROPERTY specifier, a class definition and object references -
      *> ISO/IEC 1989:2002 introductions. At COBOL-85 this source does not
      *> describe a program, so the compiler refuses the first construct
      *> the edition does not have with COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB987N85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB987N85C
           PROPERTY P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE PB987N85C.
       01 K PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM UNTIL P OF S > 3
               ADD 1 TO K
           END-PERFORM.
           STOP RUN.
       END PROGRAM PB987N85.
