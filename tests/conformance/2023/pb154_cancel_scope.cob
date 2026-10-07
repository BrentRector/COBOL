       IDENTIFICATION DIVISION.
       FUNCTION-ID. FCTR154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9(4) VALUE 0.
       LINKAGE SECTION.
       01 RES PIC 9(4).
       PROCEDURE DIVISION RETURNING RES.
       MAIN.
           ADD 1 TO C
           MOVE C TO RES
           GOBACK.
       END FUNCTION FCTR154.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB154SC.
      *> kb/Work PB154 - 8.4.6.3 first paragraph: a program-name is
      *> referenced only by CALL/CANCEL/program-address/end-marker, and a
      *> FUNCTION-ID's name is a FUNCTION-name - so CANCEL "FCTR154" is
      *> the GR7 no-op and the UDF's static counter SURVIVES (it answered
      *> 1 again before, its statics wrongly re-initialized). GR12: a
      *> zero-length DYNAMIC LENGTH target is no effect. GR7: a name that
      *> does not locate a program is no effect. (The GR4 containee
      *> cascade and GR7's never-called/already-canceled arms are
      *> pb154_cancel_cascade's subject, plus NIST IC203A.)
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION FCTR154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       01 DL PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION FCTR154
           DISPLAY "F1=" R
           COMPUTE R = FUNCTION FCTR154
           DISPLAY "F2=" R
           CANCEL "FCTR154"
           COMPUTE R = FUNCTION FCTR154
           DISPLAY "F3=" R
           CALL "HLP154"
           CANCEL DL
           CALL "HLP154"
           CANCEL "NOPE154"
           CALL "HLP154"
           STOP RUN.
       END PROGRAM PB154SC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HLP154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N
           DISPLAY "H=" N
           GOBACK.
       END PROGRAM HLP154.
