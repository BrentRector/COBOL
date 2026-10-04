       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOR1.
      *> M2-OO-1i review: a REPORT SECTION in an OBJECT paragraph. The
      *> Report Writer subsystem is complete; the class emit path now calls
      *> RwEmitReportMembers + RwEmitReportConstruction (the same class-emit
      *> gap as inc 3's `using` and inc 5's external backing). A method
      *> INITIATE/GENERATE/TERMINATE round-trips through a per-object report FD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS RCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE RCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE RCLS "NEW" RETURNING T.
           INVOKE T "DOIT".
           STOP RUN.
       END PROGRAM OOR1.

       IDENTIFICATION DIVISION.
       CLASS-ID. RCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "oo-rpt.dat".
           SELECT RBACK ASSIGN TO "oo-rpt.dat" ORGANIZATION LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       FD RBACK.
       01 RB-REC PIC X(40).
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 10 LINES.
       01 DET-1 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC X(6) VALUE "REPORT".
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           OPEN INPUT RBACK.
           MOVE SPACES TO RB-REC.
           PERFORM UNTIL RB-REC NOT = SPACES
               READ RBACK AT END MOVE "NONE" TO RB-REC END-READ
           END-PERFORM.
           DISPLAY "R=" RB-REC(1:6).
           CLOSE RBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS RCLS.
