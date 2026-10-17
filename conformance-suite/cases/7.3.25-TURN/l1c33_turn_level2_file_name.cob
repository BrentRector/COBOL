      *>--- conformance-case
      *> id: l1c33_turn_level2_file_name
      *> rules: [GR-7.3.25.4-3, GR-7.3.25.4-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.25.4 GR3 — level-2 EC-I-O + file-name-1
      *> "With the exception of EC-I-O-WARNING, if exception-name-1 is
      *> one of the level-2 exception-names, the effect is as if that
      *> TURN directive were specified containing all exception-names
      *> that are subordinate to that level-2 exception-name. If
      *> file-name-1 is specified, the effect is as if file-name-1 were
      *> specified for each of these exception-names."
      *>   cite.py: OK  §7.3.25.4 3)  (General rules)
      *> Supporting: §9.1.13.1 - an I-O status '1x' is associated with
      *> EC-I-O-AT-END; §7.3.25.4 GR6 "if file-name-1 is specified,
      *> checking is enabled only for exception conditions associated
      *> with that file-name"; §14.6.13.1.1 - a raised exception sets
      *> the last exception status; §15.33.3 1) EXCEPTION-STATUS returns
      *> spaces "If the last exception status indicates no exception".
      *> DERIVATION. '>>TURN EC-I-O F1 CHECKING ON' is, by GR3, the same
      *> as naming every level-3 EC-I-O-* (among them EC-I-O-AT-END)
      *> each with F1.
      *>   READ F2 (empty) -> at end, status 10; checking is NOT on for
      *>     F2, so no exception is raised: "F2 AT END []" (EXCEPTION-
      *>     STATUS still spaces). An implementation dropping
      *>     file-name-1 would print EC-I-O-AT-END here.
      *>   READ F1 (empty) -> at end; EC-I-O-AT-END is enabled for F1 by
      *>     the level-2 expansion, so it is raised: "F1 AT END [EC-I-O-
      *>     AT-END]". An implementation not expanding the level-2 name
      *>     (or expanding it without the file-name) prints [] here.
      *> EDITION: >>TURN and FUNCTION EXCEPTION-STATUS are COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C33D1.DAT".
           SELECT F2 ASSIGN TO "L1C33D2.DAT".
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
           OPEN INPUT F1 F2.
       >>TURN EC-I-O F1 CHECKING ON
           READ F2
               AT END
                 DISPLAY "F2 AT END [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           READ F1
               AT END
                 DISPLAY "F1 AT END [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1 F2.
           STOP RUN.
