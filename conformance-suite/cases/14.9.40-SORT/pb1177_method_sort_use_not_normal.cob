      *>--- conformance-case
      *> id: pb1177_method_sort_use_not_normal
      *> rules: [GR-14.9.40.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1177 - 14.9.40.4 GR17 in a METHOD: a USE procedure of
      *> the method, invoked by an OPEN in the SORT's input procedure,
      *> ends in RESUME AT NEXT STATEMENT, so it does not complete
      *> normally (14.6.13.1.2 1)) and the SORT is terminated: the input
      *> procedure stops at the OPEN, the output procedure never runs,
      *> and control goes to the end of the SORT (=> M-AFTER). The
      *> second INVOKE proves the sort file was released.
      *> EXPECTED (computed from the rules, not from a run):
      *>    M-USE=35 / M-AFTER, twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1177MC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE PB1177MC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1177MC "NEW" RETURNING T.
           INVOKE T "DOIT".
           INVOKE T "DOIT".
           STOP RUN.
       END PROGRAM PB1177M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1177MC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb1177m-absent.dat"
               FILE STATUS IS FS.
           SELECT SWK ASSIGN TO "pb1177m-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       FD NF.
       01 NF-REC PIC X.
       SD SWK.
       01 SW-REC PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 EOFF PIC X.
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NF-P.
           DISPLAY "M-USE=" FS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           SORT SWK ON ASCENDING KEY SW-REC
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "M-AFTER".
           GO TO M-END.
       IN-P.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF.
           DISPLAY "M-IN-AFTER-OPEN".
       OUT-P.
           MOVE "N" TO EOFF.
           PERFORM UNTIL EOFF = "Y"
               RETURN SWK AT END MOVE "Y" TO EOFF
                   NOT AT END DISPLAY "M-OUT=" SW-REC
               END-RETURN
           END-PERFORM.
       M-END.
           EXIT METHOD.
       END METHOD DOIT.
       END OBJECT.
       END CLASS PB1177MC.
