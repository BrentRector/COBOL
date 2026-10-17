*>--- conformance-case
*> id: oo_method_sort
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOS1.
      *> M2-OO-1i review: an OBJECT-paragraph SD (sort file) + a method SORT.
      *> The SD is not a per-object host connector — it keeps a static
      *> class-qualified key, so no undeclared __fkey reference is emitted.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS SCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE SCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SCLS "NEW" RETURNING T.
           INVOKE T "DOIT".
           STOP RUN.
       END PROGRAM OOS1.

       IDENTIFICATION DIVISION.
       CLASS-ID. SCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INF ASSIGN TO "oo-sort-in.dat".
           SELECT OUTF ASSIGN TO "oo-sort-out.dat".
           SELECT SWK ASSIGN TO "oo-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       FD INF.
       01 IN-REC PIC X(3).
       FD OUTF.
       01 OUT-REC PIC X(3).
       SD SWK.
       01 SW-REC PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT INF.
           MOVE "CCC" TO IN-REC.
           WRITE IN-REC.
           MOVE "AAA" TO IN-REC.
           WRITE IN-REC.
           MOVE "BBB" TO IN-REC.
           WRITE IN-REC.
           CLOSE INF.
           SORT SWK ON ASCENDING KEY SW-REC
               USING INF GIVING OUTF.
           OPEN INPUT OUTF.
           READ OUTF AT END DISPLAY "EOF".
           DISPLAY "S1=" OUT-REC.
           READ OUTF AT END DISPLAY "EOF".
           DISPLAY "S2=" OUT-REC.
           READ OUTF AT END DISPLAY "EOF".
           DISPLAY "S3=" OUT-REC.
           CLOSE OUTF.
       END METHOD DOIT.
       END OBJECT.
       END CLASS SCLS.
