       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOOF2.
      *> M2-OO-1i inc 4: per-object connector INDEPENDENCE. Two objects of
      *> one class open the SAME file on their OWN connectors; A holds its
      *> read position across B's open (distinct __fkey#n per object, §9.1.4).
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS OFCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE OFCLS.
       01 B USAGE OBJECT REFERENCE OFCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE OFCLS "NEW" RETURNING A.
           INVOKE OFCLS "NEW" RETURNING B.
           INVOKE A "SETUP".
           INVOKE A "OPENREAD".
           INVOKE B "OPENREAD".
           INVOKE A "NEXTREC".
           STOP RUN.
       END PROGRAM OOOF2.

       IDENTIFICATION DIVISION.
       CLASS-ID. OFCLS.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OFILE ASSIGN TO "oo-two.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OFILE.
       01 OFILE-REC PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. SETUP.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT OFILE.
           MOVE "AAA" TO OFILE-REC.
           WRITE OFILE-REC.
           MOVE "BBB" TO OFILE-REC.
           WRITE OFILE-REC.
           CLOSE OFILE.
       END METHOD SETUP.
       METHOD-ID. OPENREAD.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT OFILE.
           READ OFILE AT END DISPLAY "EOF".
           DISPLAY "R=" OFILE-REC.
       END METHOD OPENREAD.
       METHOD-ID. NEXTREC.
       PROCEDURE DIVISION.
       MAIN.
           READ OFILE AT END DISPLAY "EOF".
           DISPLAY "N=" OFILE-REC.
           CLOSE OFILE.
       END METHOD NEXTREC.
       END OBJECT.
       END CLASS OFCLS.
