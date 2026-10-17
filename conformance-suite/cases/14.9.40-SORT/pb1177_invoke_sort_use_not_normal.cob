      *>--- conformance-case
      *> id: pb1177_invoke_sort_use_not_normal
      *> rules: [GR-14.9.40.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
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
      *> A GOBACK in the method's USE procedure is the method's return
      *> (docs/CONFORMANCE.md section 3): the return completes, so
      *> 14.9.23.4 GR8 places the result into identifier-4, and THEN
      *> the SORT is terminated in the invoker - for every INVOKE form,
      *> as a CALL's RETURNING item is delivered (14.9.4.4 GR4).
      *> EXPECTED (computed from the rules, not from a run):
      *>  I0  control - the method's USE procedure completes normally:
      *>      I0-M-USE=35 I0-M-AFTER-OPEN I0-IN-AFTER-INVOKE I0-OUT=A
      *>      I0-OUT=B I0-ARG=Y I0-RET=N I0-AFTER.
      *>  I1  RESUME AT NEXT STATEMENT => I1-M-USE=35 I1-ARG=Y I1-RET=N
      *>      I1-AFTER.
      *>  I2  GOBACK - the method returns, and the SORT is terminated
      *>      in its invoker => I2-M-USE=35 I2-ARG=Y I2-RET=N I2-AFTER.
      *>  I3  GOBACK in a method with RETURNING, INVOKEd through a
      *>      typed reference: GR8 delivers first => I3-R-USE=35
      *>      I3-ARG=Y I3-RET=Y I3-AFTER.
      *>  I4  the same through a universal object reference (14.9.23.4
      *>      GR7 c)) => I4-R-USE=35 I4-ARG=Y I4-RET=Y I4-AFTER.
      *>  I5  the same as an inline method invocation (8.4.3.4) in a
      *>      MOVE: the method returns its result to the MOVE, and the
      *>      SORT, whose range the MOVE is in, is terminated before the
      *>      MOVE stores it => I5-R-USE=35 I5-ARG=Y I5-RET=N I5-AFTER.
      *>  I6  GOBACK in a factory NEW OVERRIDE's USE procedure (a user
      *>      method, 11.7.3 SR3): the object it already created is
      *>      delivered => I6-NEW-USE=35 I6-ARG=N I6-NEW=SET I6-AFTER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1177IC
           CLASS PB1177ID.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1177i-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  SW-REC PIC X.
       WORKING-STORAGE SECTION.
       01  T USAGE OBJECT REFERENCE PB1177IC.
       01  U USAGE OBJECT REFERENCE.
       01  T2 USAGE OBJECT REFERENCE PB1177ID.
       01  W-EOF PIC X.
       01  W-LEG PIC 9.
       01  W-RI PIC X.
       01  W-ARG.
           05 W-ARG-1 PIC X.
           05 W-ARG-N PIC 9(3) COMP.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB1177IC "NEW" RETURNING T.
           SET U TO T.
           PERFORM RUN-LEG VARYING W-LEG FROM 0 BY 1 UNTIL W-LEG > 6.
           STOP RUN.
       RUN-LEG.
           MOVE "N" TO W-ARG-1.
           MOVE "N" TO W-RI.
           SET T2 TO NULL.
           SORT SW ON ASCENDING KEY SW-REC
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "I" W-LEG "-ARG=" W-ARG-1.
           IF W-LEG < 6
               DISPLAY "I" W-LEG "-RET=" W-RI
           ELSE
               IF T2 = NULL
                   DISPLAY "I" W-LEG "-NEW=NULL"
               ELSE
                   DISPLAY "I" W-LEG "-NEW=SET"
               END-IF
           END-IF.
           DISPLAY "I" W-LEG "-AFTER".
       IN-P.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           EVALUATE W-LEG
             WHEN 3
               INVOKE T "FAILR" USING W-LEG W-ARG RETURNING W-RI
             WHEN 4
               INVOKE U "FAILR" USING W-LEG W-ARG RETURNING W-RI
             WHEN 5
               MOVE T :: "FAILR" (W-LEG W-ARG) TO W-RI
             WHEN 6
               INVOKE PB1177ID "NEW" RETURNING T2
             WHEN OTHER
               INVOKE T "FAIL" USING W-LEG W-ARG
           END-EVALUATE.
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

       METHOD-ID. FAILR.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-LEG PIC 9.
       01  L-ARG.
           05 L-ARG-1 PIC X.
           05 L-ARG-N PIC 9(3) COMP.
       01  L-RET PIC X.
       PROCEDURE DIVISION USING L-LEG L-ARG RETURNING L-RET.
       DECLARATIVES.
       D-NR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NR-P.
           DISPLAY "I" L-LEG "-R-USE=" FS.
           GOBACK.
       END DECLARATIVES.
       R-MAIN SECTION.
       R-1.
           MOVE "Y" TO L-ARG-1.
           MOVE "Y" TO L-RET.
           OPEN INPUT NF.
           DISPLAY "I" L-LEG "-R-AFTER-OPEN".
           EXIT METHOD.
       END METHOD FAILR.
       END OBJECT.
       END CLASS PB1177IC.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1177ID INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb1177i-absent3.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  NF.
       01  NF-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       METHOD-ID. NEW OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK.
       DECLARATIVES.
       D-NN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NN-P.
           DISPLAY "I6-NEW-USE=" FS.
           GOBACK.
       END DECLARATIVES.
       N-MAIN SECTION.
       N-1.
           INVOKE SUPER "NEW" RETURNING LK.
           OPEN INPUT NF.
           DISPLAY "I6-NEW-AFTER-OPEN".
       END METHOD NEW.
       END FACTORY.
       END CLASS PB1177ID.
