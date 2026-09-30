      *> PB1215 - ISO 13.6.4 GR2 sends LOCAL-STORAGE to 11.9.10, whose
      *>   GR2 applies the OPTIONS INITIALIZE clause of the source element
      *>   to the storage allocated for the sections it names; 11.9.4 GR1
      *>   carries a class-level clause into the methods it contains
      *>   unless a method's own OPTIONS paragraph overrides it.
      *> cite.py --check 13.6.4 "Data items in the local-storage section
      *>   are initialized as indicated in 11.9.10" -> OK  13.6.4 2)
      *> Derivation (VALUE-less PIC X(3) LOCAL-STORAGE items):
      *>   LSICLS.M1 has its own INITIALIZE LOCAL-STORAGE TO X"41": every
      *>     activation starts AAA (invoked twice: AAA, AAA).
      *>   LSICLS.M2 has none and the class has none: no background, so the
      *>     alphanumeric item is spaces (no fill leaks between methods).
      *>   LSJCLS carries INITIALIZE LOCAL-STORAGE TO X"42" at class level:
      *>     its method M1 inherits it: BBB. Its method M3 has its own
      *>     clause TO X"43", which overrides: CCC.
       IDENTIFICATION DIVISION.
       CLASS-ID. LSICLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"41".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "F=[" F "]".
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "G=[" F "]".
       END METHOD M2.
       END OBJECT.
       END CLASS LSICLS.
       IDENTIFICATION DIVISION.
       CLASS-ID. LSJCLS INHERITS FROM BASE.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"42".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "J=[" F "]".
       END METHOD M1.
       METHOD-ID. M3.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"43".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "K=[" F "]".
       END METHOD M3.
       END OBJECT.
       END CLASS LSJCLS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1215.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS LSICLS
           CLASS LSJCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE LSICLS.
       01 O2 USAGE OBJECT REFERENCE LSJCLS.
       PROCEDURE DIVISION.
           INVOKE LSICLS "NEW" RETURNING O1
           INVOKE O1 "M1"
           INVOKE O1 "M1"
           INVOKE O1 "M2"
           INVOKE LSJCLS "NEW" RETURNING O2
           INVOKE O2 "M1"
           INVOKE O2 "M3"
           STOP RUN.
