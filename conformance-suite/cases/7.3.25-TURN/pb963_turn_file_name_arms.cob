      *>--- conformance-case
      *> id: pb963_turn_file_name_arms
      *> rules: [GR-7.3.25.4-3, GR-7.3.25.4-5, GR-7.3.25.4-6, GR-7.3.25.4-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB963 - >>TURN file-name-1, both arms (ISO 7.3.25.4)
      *> GR6: "if file-name-1 is specified, checking is enabled only for
      *> exception conditions associated with that file-name"
      *>   cite.py: OK  §7.3.25.4 6)  (General rules)
      *> GR8: "if file-name-1 is specified, checking is disabled only
      *> for exception conditions associated with that file-name"
      *>   cite.py: OK  §7.3.25.4 8)  (General rules)
      *> GR3: "If file-name-1 is specified, the effect is as if
      *> file-name-1 were specified for each of these exception-names"
      *>   cite.py: OK  §7.3.25.4 3)  (General rules)
      *> GR5: "If specified within a statement, the TURN directive does
      *> not apply to any phrase of that statement"
      *>   cite.py: OK  §7.3.25.4 5)  (General rules)
      *> Supporting: §14.9.30.4 24) a) READ at end sets status '10'
      *> "and, if enabled, the EC-I-O-AT-END exception condition is set
      *> to exist" (cite.py: OK  §14.9.30.4 24)); §14.9.39.4 28) SET
      *> LAST EXCEPTION TO OFF: "the last exception status is set to
      *> indicate no exception" (cite.py: OK  §14.9.39.4 28));
      *> §15.33.3 1) "If the last exception status indicates no
      *> exception, alphanumeric spaces are returned"
      *> (cite.py: OK  §15.33.3 1)).
      *> DERIVATION. F1 and F2 are empty; each phase reopens both,
      *> clears the last exception status, and READs each once to its
      *> at end condition. The AT END phrase shows EXCEPTION-STATUS: the
      *> name when checking was on for that READ (the condition is
      *> raised), spaces when it was off. Enablement is the fold of the
      *> TURNs that precede the READ in source order.
      *>   1 'EC-I-O-AT-END F1 ON' (GR6): F1 only -> F1 [AT-END] F2 [ ].
      *>   2 'EC-I-O-AT-END F2 ON' (GR6): adds F2; F1 keeps its prior
      *>     state (GR6 "remains enabled") -> both [AT-END].
      *>   3 'EC-I-O-AT-END F1 OFF' (GR8): F1 only
      *>     -> F1 [ ] F2 [AT-END].
      *>   4 'EC-I-O F2 OFF' (GR8 + GR3: the level-2 name with F2 is
      *>     every level-3 EC-I-O name with F2) -> both [ ].
      *>   5 'EC-I-O-AT-END ON', no file-name (GR6 for every file; GR8
      *>     "until another TURN directive ... with the ON phrase")
      *>     -> both [AT-END].
      *>   6 'EC-I-O-AT-END OFF', then 'EC-I-O-AT-END F1 F2 ON', then
      *>     'EC-I-O-AT-END F2 OFF' -> F1 [AT-END] F2 [ ].
      *>   7 'EC-I-O-AT-END OFF', then a READ F1 whose AT END phrase is
      *>     preceded by 'EC-I-O-AT-END F1 ON' (GR5): the TURN does not
      *>     apply to that READ or its phrase -> "7 F1 [ ]"; the READ
      *>     nested in the phrase is a succeeding statement -> "7 F1
      *>     INNER [AT-END]".
      *>   8 the statements after that READ are succeeding statements
      *>     too, and the TURN named only F1 -> F1 [AT-END] F2 [ ].
      *> An implementation that ignored file-name-1 would show F2
      *> enabled in 1 and 8, F2 disabled in 3 and F1 disabled in 6;
      *> one that let a file-scoped OFF outlive a later file-less ON
      *> would show spaces for both files in 5.
      *> EDITION: >>TURN, SET LAST EXCEPTION and FUNCTION
      *> EXCEPTION-STATUS are COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB963A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB963A1.DAT".
           SELECT F2 ASSIGN TO "PB963A2.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F1 F2.
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END F1 CHECKING ON
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "1 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "1 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END F2 CHECKING ON
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "2 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "2 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END F1 CHECKING OFF
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "3 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "3 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O F2 CHECKING OFF
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "4 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "4 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END CHECKING ON
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "5 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "5 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END CHECKING OFF
       >>TURN EC-I-O-AT-END F1 F2 CHECKING ON
       >>TURN EC-I-O-AT-END F2 CHECKING OFF
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "6 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "6 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
       >>TURN EC-I-O-AT-END CHECKING OFF
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1
       >>TURN EC-I-O-AT-END F1 CHECKING ON
             AT END
               DISPLAY "7 F1 [" FUNCTION EXCEPTION-STATUS "]"
               CLOSE F1
               OPEN INPUT F1
               READ F1 AT END
                 DISPLAY "7 F1 INNER [" FUNCTION EXCEPTION-STATUS "]"
               END-READ
           END-READ.
           CLOSE F1 F2.
           OPEN INPUT F1 F2.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "8 F1 [" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           READ F2 AT END
               DISPLAY "8 F2 [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F1 F2.
           STOP RUN.
