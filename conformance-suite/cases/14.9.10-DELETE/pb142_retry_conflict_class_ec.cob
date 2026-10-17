*>--- conformance-case
*> id: pb142_retry_conflict_class_ec
*> rules: [GR-14.9.10.4-15, GR-14.9.27.4-24, DOC-A.1-109]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB142RCE.
      *> kb/Work PB142 - the SECOND-ORDER harm of the manufactured retry
      *> status, made observable. ISO 9.1.13.1 keys the exception-name on
      *> the I-O status FIRST DIGIT: '5' -> EC-I-O-RECORD-OPERATION,
      *> '6' -> EC-I-O-FILE-SHARING. A DELETE FILE blocked by another file
      *> connector is a FILE SHARING conflict (9.1.13.9 item 2 = '62'), so
      *> under EVERY RETRY form -- 14.7.9.3 GR4a and that clause's closing
      *> paragraph both land "the appropriate value ... according to the
      *> rules for 9.1.13", and 9.1.13.9 defines NO deadlock value -- the
      *> condition raised is EC-I-O-FILE-SHARING. The compiler used to
      *> manufacture '52' on the SECONDS/FOREVER arm, which raised
      *> EC-I-O-RECORD-OPERATION instead, so a handler keyed on the file
      *> sharing condition silently did not fire. This fixture is that
      *> handler.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "pb142rce.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb142rce.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-A
           MOVE "HELLO" TO A-REC
           WRITE A-REC
      *> No RETRY: the baseline that was always right.
           DELETE FILE F-B
               ON EXCEPTION
                   DISPLAY "NONE=" B-ST " " FUNCTION EXCEPTION-STATUS
           END-DELETE
      *> GR1 -- the arm that was always right.
           DELETE FILE F-B RETRY 2 TIMES
               ON EXCEPTION
                   DISPLAY "TIMES=" B-ST " " FUNCTION EXCEPTION-STATUS
           END-DELETE
      *> GR3 -- FOREVER. Was '52'/EC-I-O-RECORD-OPERATION.
           DELETE FILE F-B RETRY FOREVER
               ON EXCEPTION
                   DISPLAY "FOREVER=" B-ST " " FUNCTION EXCEPTION-STATUS
           END-DELETE
      *> GR4a -- a zero arithmetic-expression-2 makes no attempt at all.
           DELETE FILE F-B RETRY FOR 0 SECONDS
               ON EXCEPTION
                   DISPLAY "SEC0=" B-ST " " FUNCTION EXCEPTION-STATUS
           END-DELETE
      *> GR2 -- a positive one is clamped to this implementation's maximum
      *> meaningful value (0), so it MUST agree with the line above. The
      *> pair is the observable form of the A.1 item 166 determination.
           DELETE FILE F-B RETRY FOR 30 SECONDS
               ON EXCEPTION
                   DISPLAY "SEC30=" B-ST " " FUNCTION EXCEPTION-STATUS
           END-DELETE
           CLOSE F-A
           DELETE FILE F-B
           DISPLAY "CLEANUP=" B-ST
           STOP RUN.
