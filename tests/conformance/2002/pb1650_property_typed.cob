      *> kb/Work PB1650 - ISO 13.16.3 SR14 (cite.py OK) admits PROPERTY beside TYPE,
      *> and SR21 (cite.py OK) refuses it only beside a BASED or TYPEDEF clause. T
      *> is not BASED, so P TYPE T PROPERTY is legal: P is described as T's
      *> PIC X(4) VALUE "ABCD" (13.18.57.4 GR1) and its GET and SET accessors
      *> read and write it through the object property P OF O.
      *> Expected: ABCD / WXYZ.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1650PT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF PIC X(4) VALUE "ABCD".
       01 P TYPE T PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1650PT.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1650M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1650PT
           PROPERTY P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1650PT.
       PROCEDURE DIVISION.
           INVOKE PB1650PT "NEW" RETURNING O.
           DISPLAY P OF O.
           MOVE "WXYZ" TO P OF O.
           DISPLAY P OF O.
           STOP RUN.
       END PROGRAM PB1650M.
