      *>--- conformance-case
      *> id: l1_read_at_end_ec
      *> rules: [GR-14.9.30.4-24]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR24 a) — "The I-O status of the file connector
      *> associated with file-name-1 is set to '10' to indicate the at
      *> end condition, and, if enabled, the EC-I-O-AT-END exception
      *> condition is set to exist."
      *> The two conjuncts are independent, and the SECOND is the one
      *> only this golden reaches: GR24 c) makes a written AT END phrase
      *> suppress "any other applicable exception processing
      *> statements", but a) sets the exception condition BEFORE c) is
      *> reached ("the following occurs IN THE ORDER SPECIFIED"), so the
      *> condition exists even though nothing handles it.  §9.1.13.1
      *> supplies the status-to-condition correspondence for the '1x'
      *> family.  §15.33.1: "The EXCEPTION-STATUS function returns an
      *> alphanumeric value that is the exception-name associated with
      *> the last exception status" — the observable for "set to exist".
      *> An implementation that set the EC only on the declarative path
      *> — i.e. after the GR24 c) short-circuit — would print two blank
      *> or stale EC lines here while still passing every status check.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD24B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rd24e.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AAAA" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT F.
      *> A successful read: status '00' raises no exception condition
      *> (§9.1.13.1 - only a non-zero second digit is EC-I-O-WARNING).
           READ F AT END DISPLAY "R1-ATEND" END-READ.
           DISPLAY "R1=" F-ST.
      *> The at end read WITH the AT END phrase: GR24 a) sets the
      *> condition, GR24 c) transfers to imperative-statement-1.
           READ F
               AT END
                   DISPLAY "R2=" F-ST
                   DISPLAY "EC=" FUNCTION EXCEPTION-STATUS
           END-READ.
      *> The condition is still the last one set after the statement.
           DISPLAY "AFTER=" FUNCTION EXCEPTION-STATUS.
           CLOSE F.
           STOP RUN.
