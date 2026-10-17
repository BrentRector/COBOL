*>--- conformance-case
*> id: pb331-read-two-from-one-lock-bracket
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 5.2.6.2: brackets "enclosing a portion of a general format indicate that
      *> the syntax element contained within the brackets or ONE OF THE ALTERNATIVES
      *> contained within the brackets may be explicitly specified". 14.9.30.2 Format 1
      *> puts ADVANCING ON LOCK, IGNORING LOCK and retry-phrase in ONE such bracket
      *> (figure_geometry.py 722: y=280.90 h=48.79 over three stacked alternatives, and
      *> the stems are PLAIN - no 5.2.6.4 choice indicators), so at most one of the
      *> three may be written. The READ below writes all three.
      *> Until kb/Work PB331 this compiled: the grammar offered the three as free
      *> independent optionals, which is the same defect as the over-rejection of the
      *> legal pair in 2002/pb331_read_lock_brackets, read from the other side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331TFB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb331tfb.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT SQF.
           READ SQF NEXT RECORD ADVANCING ON LOCK RETRY 3 TIMES
               IGNORING LOCK
               AT END CONTINUE
           END-READ.
           CLOSE SQF.
           STOP RUN.
