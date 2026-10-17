      *>--- conformance-case
      *> id: l1_perform_f3_when_file_repeat_licensed
      *> rules: [SR-14.9.28.3-14]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR14 — "If file-name-1 or file-name-2 is
      *> specified in a WHEN phrase, it shall not be specified more
      *> than once in any of the WHEN phrases within the scope of a
      *> format 3 PERFORM statement UNLESS all such instances are
      *> specified in conjunction with an exception-name."
      *>
      *> The rule has TWO halves and a check that only counts would
      *> pass one and break the other. This golden pins the LICENCE
      *> half — the file F1 appears in TWO WHEN phrases and EVERY
      *> instance is paired with an exception-name, so the statement is
      *> legal and must COMPILE AND RUN. The prohibiting half is
      *> tests/conformance/negative/l1-perform-f3-sr14-* (three
      *> rejects: bare in both phrases, the cross-form pairing, and
      *> two occurrences inside one phrase).
      *>
      *> SR15 is separately satisfied — each of the three
      *> exception-names appears exactly once.
      *>
      *> The observable is a THIRD WHEN, on a user-defined exception
      *> raised by imperative-statement-1, so the program's output does
      *> not depend on any file ever being opened: F1 is SELECTed and
      *> never touched, which is what keeps the run deterministic.
      *> §14.6.13.1.1 makes every user-defined exception nonfatal, so
      *> §14.9.28.4 GR20 returns execution to the end of the PERFORM
      *> and SR14-AFTER is reached.
      *>
      *> EDITION: the exception-checking PERFORM is new in COBOL-2023
      *> (Annex E.3.3 item 36 — "An exception checking variant of
      *> this statement has been added"), so 2023 is the whole
      *> edition window for this rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFS14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfs14.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               RAISE EXCEPTION EC-USER-SRONEFOUR
           WHEN EC-I-O-AT-END FILE F1
               DISPLAY "SR14-ATEND"
           WHEN EC-I-O-PERMANENT-ERROR FILE F1
               DISPLAY "SR14-PERM"
           WHEN EC-USER-SRONEFOUR
               DISPLAY "SR14-LICENSED"
           END-PERFORM.
           DISPLAY "SR14-AFTER".
           STOP RUN.
