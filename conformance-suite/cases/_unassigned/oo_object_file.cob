*>--- conformance-case
*> id: oo_object_file
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOOF1.
      *> M2-OO-1i inc 4: an OBJECT-paragraph FILE-CONTROL + FILE SECTION.
      *> An object owns one per-instance file connector (ISO §9.1.4),
      *> registered per object in the emitted ctor; a method round-trips it.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS OFCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE OFCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE OFCLS "NEW" RETURNING T.
           INVOKE T "DOIT".
           STOP RUN.
       END PROGRAM OOOF1.

       IDENTIFICATION DIVISION.
       CLASS-ID. OFCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OFILE ASSIGN TO "oo-obj.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OFILE.
       01 OFILE-REC PIC X(12).
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT OFILE.
           MOVE "HELLO OBJECT" TO OFILE-REC.
           WRITE OFILE-REC.
           CLOSE OFILE.
           OPEN INPUT OFILE.
           READ OFILE AT END DISPLAY "EOF".
           DISPLAY "GOT=" OFILE-REC.
           CLOSE OFILE.
       END METHOD DOIT.
       END OBJECT.
       END CLASS OFCLS.
