      *> kb/Work PB1820. ISO/IEC 1989:2023 7.3.4 GR5: a compiler directive applies to all of the source text and library text that follows;
      *> the LEAP-SECOND ON directive below precedes the class definition CLS, so the seconds subfield 60 is admissible inside its method
      *> (15.3.3.3; 7.3.17.4 GR4) and SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960") is 86400 (8640000 in PIC 9(6)V99, 15.79.4). Before this
      *> fix only PROGRAM and FUNCTION units carried the state: the DataBinders of a class, its factory and an interface did not, so the
      *> method answered the OFF default, 0. The directive is outside both units (7.3.17.3 SR1), between PD5 and the CLASS-ID.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1820.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CLS1820.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CLS1820.
       PROCEDURE DIVISION.
           INVOKE CLS1820 "NEW" RETURNING O
           INVOKE O "SHOW".
           STOP RUN.
       END PROGRAM PB1820.
       >>LEAP-SECOND ON
       IDENTIFICATION DIVISION.
       CLASS-ID. CLS1820 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R PIC 9(6)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME
               ("hhmmss", "235960").
           DISPLAY "M " R.
       END METHOD SHOW.
       END OBJECT.
       END CLASS CLS1820.
