*>--- conformance-case
*> id: pb451-classname-sender-superclass-name
*> rules: [SR-14.9.39.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 13 b): with object-class-name-1 as the sender and
      *> identifier-3 described with an object-class-name but WITHOUT the ONLY phrase,
      *> "otherwise, object-class-name-1 shall reference the same class or a subclass of the
      *> class specified in the description of the data item referenced by identifier-3."
      *> The receiver here is described FACTORY OF …D and the sender names …C, its SUPERCLASS
      *> — the direction §13.18.60.4 GR22 d)1.a. forbids ("the factory object of the specified
      *> class OR OF A SUBCLASS" widens downward only).  The legal direction, the same
      *> statement with the names swapped, is the `SET F1 F2 TO PB451D` line of
      *> conformance:cases/13.18.60-USAGE/pb451_set_format5_class_name_sender, which prints FTAG-D — so the
      *> INHERITANCE DIRECTION is what decides here, not the FACTORY or ONLY axes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451NB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451NBC
           CLASS PB451NBD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF PB451NBD.
       PROCEDURE DIVISION.
       MAIN.
           SET F TO PB451NBC.
           STOP RUN.
       END PROGRAM PB451NB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451NBC.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-C".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB451NBC.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451NBD INHERITS FROM PB451NBC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451NBC.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-D".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB451NBD.
