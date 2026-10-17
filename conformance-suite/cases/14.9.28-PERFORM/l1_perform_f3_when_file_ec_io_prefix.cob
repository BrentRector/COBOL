      *>--- conformance-case
      *> id: l1_perform_f3_when_file_ec_io_prefix
      *> rules: [SR-14.9.28.3-16]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR16 — "If file-name-2 is specified,
      *> exception-name-2 shall begin with the COBOL characters
      *> 'EC-I-O'."
      *>
      *> The rule is a PREFIX test on the written name, not a
      *> "belongs to the I-O category" test and not a "is a catalogued
      *> I-O name" test — either of which would be a plausible but
      *> DIFFERENT rule. This golden pins the ACCEPT half on the two
      *> shapes that separate the three readings:
      *>   WHEN EC-I-O FILE F1        — the level-2 name itself, which
      *>     begins with the characters and is not a level-3 I-O
      *>     condition, so a level-3-only reading would reject it;
      *>   WHEN EC-I-O-AT-END FILE F2 — an ordinary level-3 I-O name.
      *> The REJECT half is
      *> tests/conformance/negative/l1-perform-f3-sr16-*, which pairs
      *> FILE with a non-I-O catalogued name AND with an open-family
      *> EC-USER- name; the latter is the case a catalog-membership
      *> implementation cannot decide, because §14.6.13.1.1's
      *> EC-USER- family is open and cannot be enumerated.
      *>
      *> The antecedent is read correctly too: it is conditioned on
      *> file-name-2, so the file-name-1 arm (WHEN EXCEPTION
      *> {file-name-1}…) carries no exception-name and is not subject
      *> to it — that arm is exercised by
      *> conformance:2023/l1_perform_f3_when_file_repeat_licensed's
      *> negative siblings.
      *>
      *> SR14 is separately satisfied — F1 and F2 each appear once.
      *> SR15 is separately satisfied — each name appears once.
      *>
      *> The observable is a third WHEN on a user-defined exception
      *> raised by imperative-statement-1, so no file is ever opened
      *> and the run is deterministic (§14.6.13.1.1 makes user-defined
      *> exceptions nonfatal; §14.9.28.4 GR20 then reaches SR16-AFTER).
      *>
      *> EDITION: the exception-checking PERFORM is new in COBOL-2023
      *> (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFS16.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfs16a.txt".
           SELECT F2 ASSIGN TO "l1pfs16b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       FD F2.
       01 R-F2 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               RAISE EXCEPTION EC-USER-SRONESIX
           WHEN EC-I-O FILE F1
               DISPLAY "SR16-IO"
           WHEN EC-I-O-AT-END FILE F2
               DISPLAY "SR16-ATEND"
           WHEN EC-USER-SRONESIX
               DISPLAY "SR16-PREFIX-OK"
           END-PERFORM.
           DISPLAY "SR16-AFTER".
           STOP RUN.
