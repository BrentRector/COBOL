      *>--- conformance-case
      *> id: pb337_into_receiver_admissible_85
      *> rules: [SR-14.9.30.3-1, SR-14.9.34.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 14.9.30.3 syntax rule 1 (READ) and 14.9.34.3 syntax rule 2 (RETURN) admit the
      *> INTO phrase on two grounds: a) "If no record description entry or only one record
      *> description is subordinate to the file description entry" (RETURN: "If only one record
      *> description is subordinate to the sort-merge file description entry"), or b) "If the data
      *> item referenced by identifier-1 and all record-names associated with file-name-1 describe
      *> an alphanumeric group item or an elementary item of category alphanumeric or category
      *> national."  14.9.30.3 SR2 and 14.9.34.3 SR3 add the strongly-typed-receiver rule.
      *> THIS PROGRAM IS THE OVER-REJECT GUARD for kb/Work PB337, which gave those four rules their
      *> first implementation: every case below is source the rules ADMIT, and each is written so a
      *> screen that read only one arm would reject it.
      *> DERIVATION -- every expected line follows from the rules named, nothing from the compiler.
      *>  . A: ONE record description, so arm a) admits the phrase OUTRIGHT and the receiver's
      *>    category is never consulted.  WS-NUM is category numeric, which arm b) does NOT admit --
      *>    a screen that tested only arm b) would reject this legal program.  14.9.30.4 GR4 b)
      *>    moves the record area to WS-NUM "according to the rules for the MOVE statement", and
      *>    Table 16 admits an alphanumeric sender into an integer numeric receiver, so the four
      *>    characters "0042" written into the record arrive as the value 42: A=0042.
      *>  . B: TWO record descriptions, so arm a) FAILS and arm b) carries it -- both record-names
      *>    and identifier-1 are alphanumeric group items (13.18.29.4 GR3: no GROUP-USAGE clause,
      *>    not strongly typed, not a variable-length group).  The move is a group move, 14.9.25.4
      *>    GR4, "the receiving area is filled without consideration for the individual elementary
      *>    or group items", so the record's four characters arrive unchanged: B=[WXYZ].
      *>  . R: the RETURN twin of B -- an SD with TWO record descriptions, both alphanumeric groups,
      *>    into an alphanumeric group.  14.9.34.3 SR2 arm b), and 14.9.34.4 GR5 b) for the move.
      *>    One record is released, so it is the one returned: R=[WXYZ].
      *> The whole receiving area is displayed inside [ ] so a trailing-blank difference cannot be
      *> trimmed away by the corpus runner's per-line trailing-space normalisation.
      *> The 85 leg: TYPEDEF/TYPE and USAGE NATIONAL are COBOL-2002 introductions, so cases C and D
      *> are absent here -- 14.9.30.3 SR2 is unreachable at 1985 by construction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB337P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb337p1f1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb337p1f2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF ASSIGN TO "pb337p1s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(4).
       FD F2.
       01 REC-A.
          05 A-1 PIC X(4).
       01 REC-B.
          05 B-1 PIC X(4).
       SD SF.
       01 S-REC-A.
          05 SA-1 PIC X(4).
       01 S-REC-B.
          05 SB-1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(4).
       01 WS-SEND PIC X(4) VALUE "WXYZ".
       01 WS-GRP.
          05 WS-A PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "0042" TO F1-REC
           WRITE F1-REC
           CLOSE F1
           OPEN INPUT F1
           READ F1 INTO WS-NUM AT END CONTINUE END-READ
           DISPLAY "A=" WS-NUM
           CLOSE F1
           OPEN OUTPUT F2
           MOVE WS-SEND TO A-1
           WRITE REC-A
           CLOSE F2
           OPEN INPUT F2
           READ F2 INTO WS-GRP AT END CONTINUE END-READ
           DISPLAY "B=[" WS-A "]"
           CLOSE F2
           SORT SF ASCENDING SA-1
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           DISPLAY "DONE"
           STOP RUN.
       FEED.
           MOVE WS-SEND TO SA-1
           RELEASE S-REC-A.
       DRAIN.
           RETURN SF INTO WS-GRP AT END CONTINUE END-RETURN
           DISPLAY "R=[" WS-A "]".
