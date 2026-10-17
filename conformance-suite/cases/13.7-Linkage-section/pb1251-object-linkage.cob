*>--- conformance-case
*> id: pb1251-object-linkage
*> rules: [SR-13.7.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1251 - ISO §13.7.3 SR1 (cite.py --check 13.7.3 "The linkage section may be specified in a program definition" -> OK §13.7.3 1)):
      *> "The linkage section may be specified in a program definition, function definition, method definition, program prototype definition, or function prototype definition."
      *> A instance definition is none of those, so a LINKAGE SECTION written in the OBJECT
      *> paragraph's data division is a syntax-rule violation at EVERY edition with the OO facility.
      *> Before PB1251 it compiled clean and bound as persistent instance data. One table now judges it
      *> (OoDefinitionRules); the positive halves are the instance FILE / WORKING-STORAGE goldens
      *> (conformance:cases/_unassigned/oo_factory_file, oo_object_file).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1251OL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           DISPLAY "HI".
       END METHOD M1.
       END OBJECT.
       END CLASS PB1251OL.
