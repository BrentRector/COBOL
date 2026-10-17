*>--- conformance-case
*> id: pb451-classname-sender-receiver-without-factory
*> rules: [SR-14.9.39.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 13, the LEADING requirement: "If object-class-name-1 is
      *> specified and the data item referenced by identifier-3 is described with an
      *> object-class-name, the data item shall be described with the FACTORY phrase."
      *> §14.9.39.4 GR10 sends the class's FACTORY object, and §13.18.60.4 GR22 d)1.b. makes a
      *> receiver WITHOUT the FACTORY phrase a holder of INSTANCE objects — two different
      *> objects with two different method interfaces (§9.3.6), so the axis is invariant.
      *> Before kb/Work PB389 this program was refused because FACTORY OF could not be
      *> DECLARED at all; the rejection is now the rule's, and the conforming spelling
      *> (`01 F … FACTORY OF …C ONLY.` + `SET F TO …C.`) runs — see
      *> conformance:cases/13.18.60-USAGE/pb451_set_format5_class_name_sender.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N3C
           CLASS PB451N3D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB451N3C.
       PROCEDURE DIVISION.
       MAIN.
           SET O TO PB451N3C.
           STOP RUN.
       END PROGRAM PB451N3.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N3C.
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
       END CLASS PB451N3C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N3D INHERITS FROM PB451N3C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N3C.
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
       END CLASS PB451N3D.
