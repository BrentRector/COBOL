*>--- conformance-case
*> id: pb1251-factory-local-storage
*> rules: [SR-13.6.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1251 - ISO §13.6.3 SR1 (cite.py --check 13.6.3 "The local-storage section may be specified in a program defi" -> OK §13.6.3 1)):
      *> "The local-storage section may be specified in a program definition or function definition or in a method definition contained in a class definition."
      *> A factory definition is none of those, so a LOCAL-STORAGE SECTION written in the FACTORY
      *> paragraph's data division is a syntax-rule violation at EVERY edition with the OO facility.
      *> Before PB1251 it compiled clean and bound as persistent factory data. One table now judges it
      *> (OoDefinitionRules); the positive halves are the factory FILE / WORKING-STORAGE goldens
      *> (conformance:cases/_unassigned/oo_factory_file, oo_object_file).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1251FS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 L1 PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           DISPLAY "HI".
       END METHOD M1.
       END FACTORY.
       END CLASS PB1251FS.
