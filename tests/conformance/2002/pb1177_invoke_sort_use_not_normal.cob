      *> kb/Work PB1177 - 14.9.40.4 GR17 for a USE procedure of a METHOD
      *> INVOKEd from the range of a SORT's input procedure: "If a USE
      *> procedure invoked while a format 1 SORT statement is active
      *> does not complete normally, the SORT statement is terminated."
      *> GR10 puts the method's statements, and the USE procedures they
      *> invoke, in the input procedure's range; 14.6.13.1.2 1) makes
      *> a RESUME or a GOBACK in the USE procedure a completion that is
      *> not normal. The SORT program has no declaratives of its own.
      *> 14.2.3 GR8: the BY REFERENCE formal "occupies the same storage
      *> area as the argument" (=> In-ARG=Y however the SORT ended).
      *> EXPECTED (computed from the rules, not from a run):
      *>  I0  control - the method's USE procedure completes normally:
      *>      I0-M-USE=35 I0-M-AFTER-OPEN I0-IN-AFTER-INVOKE I0-OUT=A
      *>      I0-OUT=B I0-ARG=Y I0-AFTER.
      *>  I1  RESUME AT NEXT STATEMENT => I1-M-USE=35 I1-ARG=Y I1-AFTER.
      *>  I2  GOBACK - the method returns, and the SORT is terminated
      *>      in its invoker => I2-M-USE=35 I2-ARG=Y I2-AFTER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1177IC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1177i-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  SW-REC PIC X.
       WORKING-STORAGE SECTION.
       01  T USAGE OBJECT REFERENCE PB1177IC.
       01  W-EOF PIC X.
       01  W-LEG PIC 9.
       01  W-ARG.
           05 W-ARG-1 PIC X.
           05 W-ARG-N PIC 9(3) COMP.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB1177IC "NEW" RETURNING T.
           PERFORM RUN-LEG VARYING W-LEG FROM 0 BY 1 UNTIL W-LEG > 2.
           STOP RUN.
       RUN-LEG.
           MOVE "N" TO W-ARG-1.
           SORT SW ON ASCENDING KEY SW-REC
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "I" W-LEG "-ARG=" W-ARG-1.
           DISPLAY "I" W-LEG "-AFTER".
       IN-P.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           INVOKE T "FAIL" USING W-LEG W-ARG.
           DISPLAY "I" W-LEG "-IN-AFTER-INVOKE".
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       OUT-P.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SW AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "I" W-LEG "-OUT=" SW-REC
               END-RETURN
           END-PERFORM.
       END PROGRAM PB1177I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1177IC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb1177i-absent.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  NF.
       01  NF-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       METHOD-ID. FAIL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-LEG PIC 9.
       01  L-ARG.
           05 L-ARG-1 PIC X.
           05 L-ARG-N PIC 9(3) COMP.
       PROCEDURE DIVISION USING L-LEG L-ARG.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NF-P.
           DISPLAY "I" L-LEG "-M-USE=" FS.
           EVALUATE L-LEG
             WHEN 1
               RESUME AT NEXT STATEMENT
             WHEN 2
               GOBACK
           END-EVALUATE.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-1.
           MOVE "Y" TO L-ARG-1.
           OPEN INPUT NF.
           DISPLAY "I" L-LEG "-M-AFTER-OPEN".
           EXIT METHOD.
       END METHOD FAIL.
       END OBJECT.
       END CLASS PB1177IC.