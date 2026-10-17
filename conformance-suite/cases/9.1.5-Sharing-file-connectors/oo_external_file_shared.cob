*>--- conformance-case
*> id: oo_external_file_shared
*> rules: [GR-9.1.5-1, GR-13.18.22.4-4, GR-14.9.27.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOEF1.
      *> M2-OO-1i inc 5: an EXTERNAL FD shared between a PROGRAM and an
      *> OBJECT (§13.18.22.4 GR4a/GR4b) — ONE run-unit connector + ONE
      *> record area, keyed ::EXT:: by the FD name. The program OPENs the
      *> connector; the object WRITEs through it (no open of its own) and
      *> through the SHARED record area.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS EFCLS.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "oo-ext.dat".
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 P-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE EFCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE EFCLS "NEW" RETURNING T.
           OPEN OUTPUT EF.
           MOVE "SHAREDABCD" TO P-REC.
           WRITE P-REC.
           INVOKE T "PUTREC".
           CLOSE EF.
           OPEN INPUT EF.
           READ EF AT END DISPLAY "EOF1".
           DISPLAY "R1=" P-REC.
           READ EF AT END DISPLAY "EOF2".
           DISPLAY "R2=" P-REC.
           CLOSE EF.
           STOP RUN.
       END PROGRAM OOEF1.

       IDENTIFICATION DIVISION.
       CLASS-ID. EFCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "oo-ext.dat".
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 O-REC PIC X(10).
       PROCEDURE DIVISION.
       METHOD-ID. PUTREC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "OBJWROTE12" TO O-REC.
           WRITE O-REC.
       END METHOD PUTREC.
       END OBJECT.
       END CLASS EFCLS.
