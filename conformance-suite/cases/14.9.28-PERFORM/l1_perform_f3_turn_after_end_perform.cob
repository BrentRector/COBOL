      *>--- conformance-case
      *> id: l1_perform_f3_turn_after_end_perform
      *> rules: [GR-14.9.28.4-22]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.4 GR22 — what checking survives an
      *> exception-checking (Format 3) PERFORM. Three sentences, and
      *> each is measured on the statement AFTER END-PERFORM:
      *>   (1) "If WHEN is specified and an exception condition was
      *>       raised and checking for that exception condition was
      *>       enabled by a TURN directive before the execution of the
      *>       PERFORM statement, that checking remains enabled."
      *>   (2) "If there is a TURN directive within the range of the
      *>       PERFORM statement, the checking for that TURN directive
      *>       is retained."
      *>   (3) "Otherwise, any checking for an exception condition
      *>       specified in a WHEN phrase is not enabled."
      *>
      *> ⛔ SENTENCE (2) IS DELIBERATELY NOT EXERCISED, AND CANNOT BE.
      *> §7.3.25.3 SR5: "A TURN directive shall not be specified
      *> within an exception processing PERFORM statement." That ban
      *> is whole-statement — imperative-statement-1 INCLUDED — which
      *> is owner decision D20; the sibling bans read the same way
      *> (§7.3.20.3 SR4 for POP, §7.3.22.3 SR4 for PUSH). So a >>TURN
      *> written lexically inside this statement is illegal source and
      *> has no place in a golden. Nor can a directive in a paragraph
      *> performed FROM imperative-statement-1 stand in: TURN is
      *> scoped over the sequence of source lines (§7.3.25.4 GR6/GR8),
      *> so one written after this PERFORM never reaches the statement
      *> following END-PERFORM, and one written before it is sentence
      *> (1)'s antecedent rather than sentence (2)'s.
      *>
      *> ⛔ BOTH ARMS USE THE SAME PROBE, so they differ only in their
      *> directive context. The probe is a STRING whose pointer passes
      *> the receiver (§14.9.43.4 GR8 b) sets EC-OVERFLOW-STRING,
      *> which Table 13 marks NF) written with NO ON OVERFLOW phrase,
      *> so §14.6.13.1.4 falls through items 1-3 to item 4 and
      *> execution simply continues — no declarative and no abnormal
      *> termination, whatever the answer is.
      *>
      *> The observable is FUNCTION EXCEPTION-STATUS, "a 31-character,
      *> left-justified, alphanumeric character string that is the
      *> exception-name … associated with the last exception status"
      *> (§15.33.3 r1). Immediately before each probe a MARKER PERFORM
      *> raises a distinct EC-USER- name, which sets the last exception
      *> status (§14.6.13.1.1); the probe then either OVERWRITES it —
      *> checking was enabled, the condition was raised — or leaves it
      *> standing, because §14.6.13.1.1 says "if checking for an
      *> exception that occurs is not enabled, no exception condition
      *> is raised". So the printed name IS the answer, and neither
      *> outcome is the absence of output.
      *>
      *> ARM ORDER IS LOAD-BEARING: a TURN directive governs the source
      *> lines that FOLLOW it (§7.3.25.4 GR6), so arm C must precede
      *> the one and only >>TURN in this file.
      *>   C — no directive anywhere yet. §14.9.28.4 GR14 supplies the
      *>       implicit enable over imperative-statement-1 (C-WHEN
      *>       proves it fired) and the matching implicit TURN … OFF
      *>       immediately preceding END-PERFORM; sentence (3) then
      *>       governs, so the probe raises nothing and the marker
      *>       name stands.
      *>   A — a pre-PERFORM >>TURN … ON. Arm C left checking OFF, so
      *>       this is the genuine first enable, and the exception
      *>       raised inside is that very one — sentence (1)'s exact
      *>       antecedent: the probe raises.
      *>
      *> EDITION: the exception-checking PERFORM is new in COBOL-2023
      *> (Annex E.3.3 item 36), so 2023 is the whole edition window.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFG22.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
      *> ---- ARM C — sentence (3). No TURN directive precedes this.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               DISPLAY "C-WHEN"
           END-PERFORM.
           PERFORM
               RAISE EXCEPTION EC-USER-MARKC
           WHEN EC-USER-MARKC
               CONTINUE
           END-PERFORM.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D.
           DISPLAY "C-AFTER=[" FUNCTION EXCEPTION-STATUS "]".
      *> ---- ARM A — sentence (1).
       >>TURN EC-OVERFLOW-STRING CHECKING ON
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               DISPLAY "A-WHEN"
           END-PERFORM.
           PERFORM
               RAISE EXCEPTION EC-USER-MARKA
           WHEN EC-USER-MARKA
               CONTINUE
           END-PERFORM.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D.
           DISPLAY "A-AFTER=[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
