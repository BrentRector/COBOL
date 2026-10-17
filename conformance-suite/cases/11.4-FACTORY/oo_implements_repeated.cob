*>--- conformance-case
*> id: oo_implements_repeated
*> rules: [GR-11.4.4-1, FMT-11.4.2, FMT-11.8.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB946 -- an interface-name written twice in an IMPLEMENTS clause is legal and changes nothing.
      *> ISO 11.8.2 / 11.4.2 print `IMPLEMENTS { interface-name-1 } ...` (a repetition), and 11.8.3 / 11.4.3
      *> each carry exactly two syntax rules: SR1, interface-name-1 is specified in the class's REPOSITORY
      *> paragraph (it is), and SR2, conformance to every implemented interface (a repeat adds no prototype).
      *> Where the standard forbids a repeated name it says so -- 11.3.3 SR7 and 11.6.3 SR6, the two INHERITS
      *> clauses -- and it does not for IMPLEMENTS. The OBJECT paragraph and the FACTORY paragraph are the two
      *> arms of the one clause, so both repeat here. 11.8.4 GR2 a): the object implements PB946I; the output is
      *> the two SPEAK methods reached through the interface-typed reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB946IMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB946C
           INTERFACE PB946I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE PB946I.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB946C "NEW" RETURNING S.
           INVOKE S "SPEAK".
           SET S TO PB946C.
           INVOKE S "SPEAK".
           STOP RUN.
       END PROGRAM PB946IMP.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB946I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE PB946I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB946C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB946I.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB946I PB946I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "FACTORY SPEAKS".
       END METHOD SPEAK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB946I PB946I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "OBJECT SPEAKS".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB946C.
