      *>--- conformance-case
      *> id: l1-present-when-level-01
      *> rules: [SR-13.16.3-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.16.3 SR20 data description entry — "The PRESENT WHEN
      *> clause shall not be specified in a data description entry that
      *> has a level number of 1 or 77."
      *>   python scripts/spec/cite.py --check 13.16.3 "The PRESENT
      *>   WHEN clause shall not be specified in a data description
      *>   entry that has a level number of 1 or 77."
      *>   -> OK  §13.16.3 20)  (Syntax rules)
      *>
      *> ⛔ THIS FIXTURE WRITES THE RULE'S OWN FORBIDDEN CONSTRUCT — a
      *> LEVEL 1 entry carrying PRESENT WHEN — and that is the whole
      *> point of it. The pre-existing witness
      *> negative/declined-validate-present-when.cob writes
      *> `05 WS-A PIC X(4) PRESENT WHEN WS-F = 1.`, a level SR20
      *> PERMITS; it pins the Annex A.4.14 decline, never SR20's
      *> prohibition. Its twin for level 77 is
      *> negative/l1-present-when-level-77.cob.
      *>
      *> WHY THE ROW IS CONFORMS AND WHY A NEGATIVE IS ITS WITNESS.
      *> §13.16.2 Format 1's `[ validation-clauses ]` group is the only
      *> place a data description entry can carry PRESENT WHEN, and
      *> that clause (§13.18.41 format 2) is item 5 of Annex A.4.14
      *> (VALIDATE), an optional element whose support this
      *> implementation does not claim (docs/CONFORMANCE.md §5). Annex
      *> A.4.1 admits the syntax of an optional element only when
      *> support is claimed, so the clause is refused BY NAME with
      *> COBOLNET1708 in EVERY data description entry — level 1 and 77
      *> included. The antecedent SR20 forbids can therefore never be
      *> constructed and the prohibition cannot be violated: kb/Work
      *> PB371's landed vacuously-satisfied CONFORMS shape.
      *>
      *> ⛔ AND IT STOPS PASSING THE DAY THE ROW NEEDS RE-ADJUDICATING.
      *> kb/Work PB283 (the A.4.14 posture) is OPEN. If VALIDATE is
      *> ever claimed, the clause is admitted and this level-1 entry
      *> must then be diagnosed on SR20's own ground rather than by the
      *> decline; COBOLNET1708 stops being the answer and this fixture
      *> goes red — which is exactly what PB371 requires of a vacuity
      *> witness and exactly what the level-05 fixture cannot do.
      *>
      *> EDITIONS. The validation-clauses group enters at COBOL-2002
      *> (grammar arm `{is2002()}? validationClause`), so at --std 85
      *> the same source is an ordinary syntax error rather than this
      *> decline and the rule has no 85 leg; the reject-at header names
      *> 2002, 2014 and 2023 only.
      *>
      *> POSITIVE CONTROL, WHICH MUST KEEP COMPILING:
      *> tests/conformance/2023/declined_rw_present_varying_control.cob
      *> — report-writer PRESENT WHEN (§13.18.41 format 1, Annex A.4.11
      *> item 14) is a DIFFERENT clause with the same spelling, it IS
      *> supported, and SR20 does not govern it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PW01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F PIC 9.
       01 WS-A PIC X(4) PRESENT WHEN WS-F = 1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "UNREACHABLE".
           STOP RUN.
