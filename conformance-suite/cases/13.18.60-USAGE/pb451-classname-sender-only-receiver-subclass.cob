*>--- conformance-case
*> id: pb451-classname-sender-only-receiver-subclass
*> rules: [GR-13.18.60.4-22, SR-14.9.39.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 13 a): "if the data item referenced by identifier-3 is
      *> described with the ONLY phrase, object-class-name-1 shall be the object-class-name
      *> specified in the description of the data item referenced by identifier-3."
      *> §13.18.60.4 GR22 d)2.a. is why: with ONLY, "the object referenced by this data item
      *> shall be the factory object of the specified class" — that class exactly, where
      *> d)1.a. (no ONLY) admits "or of a subclass".  The receiver here is FACTORY OF …C ONLY
      *> and the sender names …D, a subclass.  The twin WITHOUT the ONLY phrase is the
      *> `SET F1 F2 TO PB451D` line of conformance:cases/13.18.60-USAGE/pb451_set_format5_class_name_sender,
      *> which is legal and prints FTAG-D — so the ONLY axis is what decides, not the names.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N2C
           CLASS PB451N2D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF PB451N2C ONLY.
       PROCEDURE DIVISION.
       MAIN.
           SET F TO PB451N2D.
           STOP RUN.
       END PROGRAM PB451N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N2C.
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
       END CLASS PB451N2C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N2D INHERITS FROM PB451N2C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N2C.
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
       END CLASS PB451N2D.
