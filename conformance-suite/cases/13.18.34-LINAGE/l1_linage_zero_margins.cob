      *>--- conformance-case
      *> id: l1_linage_zero_margins
      *> rules: [SR-13.18.34.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.34.3 SR4 LINAGE clause — "Integer-3, integer-4 may
      *> be zero."
      *>   python scripts/spec/cite.py --check 13.18.34.3 "Integer-3,
      *>   integer-4 may be zero."
      *>   -> OK  §13.18.34.3 4)  (Syntax rules)
      *>
      *> WHAT THE RULE OBLIGES. SR4 is a PERMISSION over integer-3 (the
      *> LINES AT TOP operand) and integer-4 (LINES AT BOTTOM), so the
      *> obligation it places on a compiler is that a LINAGE clause
      *> writing zero for either is CONFORMING SOURCE and shall not be
      *> refused. PRTZ below writes both zeros; if either were rejected
      *> this fixture would not compile, which is the first half of the
      *> assertion.
      *>
      *> THE SECOND HALF — WHAT ZERO MEANS. §13.18.34.4 GR1:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If the LINES
      *>   AT TOP or LINES AT BOTTOM phrases are not specified, the
      *>   values of these items are zero."
      *>   -> OK  §13.18.34.4 1)  (General rules)
      *> An omitted phrase HAS the value zero, so a written zero and an
      *> omitted phrase describe the same logical page: PRTZ (explicit
      *> zeros) and PRTN (phrases omitted) both have page size 5 and a
      *> logical page of 5 + 0 + 0 lines. Their LINAGE-COUNTERs must
      *> therefore step identically, and the counter is fully defined
      *> by GR7:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The value of
      *>   LINAGE-COUNTER is automatically set to one at the time an
      *>   OPEN statement with the OUTPUT phrase is executed for the
      *>   associated file."   -> OK  §13.18.34.4 7) a)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "When the
      *>   ADVANCING phrase of the WRITE statement is not specified,
      *>   the LINAGE-COUNTER is incremented by the value one."
      *>   -> OK  §13.18.34.4 7) a)
      *> GR7d gives 1 at OPEN OUTPUT; each WRITE without an ADVANCING
      *> phrase adds one (GR7c3). Hence 1, 2, 3, 4 on BOTH files. Three
      *> writes keep the counter at 4, inside the page body of 5, so no
      *> page overflow condition arises and nothing here depends on how
      *> a logical page is physically positioned. The FOOTING phrase is
      *> deliberately absent: GR1's closing sentence then leaves no
      *> end-of-page condition independent of page overflow, so the
      *> displayed sequence is a pure function of GR7.
      *>
      *> ⛔ WHY EACH WRITE IS PRECEDED BY ITS OWN MOVE — do not fold
      *> them into a single MOVE before the first WRITE. §14.9.51.4 GR4
      *> empties the record area on every successful WRITE:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "The logical
      *>   record released by the successful execution of the WRITE
      *>   statement is no longer available in the record area unless
      *>   the file-name associated with record-name-1 is specified in
      *>   a SAME RECORD AREA clause."
      *>   -> OK  §14.9.51.4 4)  (General rules)
      *> Neither file is named in a SAME RECORD AREA clause, so after
      *> each WRITE the contents of Z-REC and N-REC are no longer
      *> available and a second WRITE of the same record item would
      *> release an area the standard has declared unavailable. The
      *> displayed LINAGE-COUNTER values would not change, but the
      *> bytes reaching the file would not be derivable from the rule
      *> text, and a golden may not contain such a statement.
      *>
      *> DISTINGUISHABILITY, STATED HONESTLY. What SR4 itself asserts
      *> is the PERMISSION, and the permission is tested by the fixture
      *> COMPILING: a compiler that refused either zero never reaches
      *> the first DISPLAY. A compiler that reacted to the zeros by
      *> dropping the LINAGE clause altogether also fails to compile,
      *> because GR7 generates a LINAGE-COUNTER only for a file whose
      *> file description entry contains a LINAGE clause, and
      *> LINAGE-COUNTER OF PRTZ would then have no referent.
      *>   ⛔ WHAT THE Z AND N COLUMNS DO NOT SHOW. They cannot detect
      *> margin mishandling, and no claim here should say they do. By
      *> §13.18.34.4 GR1 the phrases omitted on PRTN ARE zero, so PRTZ
      *> and PRTN have the SAME margins; an implementation that folded
      *> the margins into the page body computes 5 + 0 + 0 = 5 on both
      *> files and the two columns stay identical. A zero margin is the
      *> identity, so equality of the columns is blind to how a margin
      *> is handled. What the columns DO pin is §13.18.34.4 GR7d and
      *> GR7c3 — one at OPEN OUTPUT, plus one per WRITE with no
      *> ADVANCING phrase — and their equality pins GR1's "not
      *> specified ... are zero" against an explicitly written zero.
      *> Telling a nonzero margin from a zero one needs a rule that
      *> makes the margin observable; that is not SR4's assertion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LNZM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTZ ASSIGN TO "l1lnzm-z.prt".
           SELECT PRTN ASSIGN TO "l1lnzm-n.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTZ LINAGE IS 5 LINES LINES AT TOP 0 LINES AT BOTTOM 0.
       01 Z-REC PIC X(4).
       FD PRTN LINAGE IS 5 LINES.
       01 N-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 LZ PIC 9(3).
       01 LN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTZ.
           OPEN OUTPUT PRTN.
           MOVE LINAGE-COUNTER OF PRTZ TO LZ.
           MOVE LINAGE-COUNTER OF PRTN TO LN.
           DISPLAY "OPEN Z=" LZ " N=" LN.
           MOVE "AAAA" TO Z-REC.
           MOVE "AAAA" TO N-REC.
           WRITE Z-REC.
           WRITE N-REC.
           MOVE LINAGE-COUNTER OF PRTZ TO LZ.
           MOVE LINAGE-COUNTER OF PRTN TO LN.
           DISPLAY "W1 Z=" LZ " N=" LN.
           MOVE "AAAA" TO Z-REC.
           MOVE "AAAA" TO N-REC.
           WRITE Z-REC.
           WRITE N-REC.
           MOVE LINAGE-COUNTER OF PRTZ TO LZ.
           MOVE LINAGE-COUNTER OF PRTN TO LN.
           DISPLAY "W2 Z=" LZ " N=" LN.
           MOVE "AAAA" TO Z-REC.
           MOVE "AAAA" TO N-REC.
           WRITE Z-REC.
           WRITE N-REC.
           MOVE LINAGE-COUNTER OF PRTZ TO LZ.
           MOVE LINAGE-COUNTER OF PRTN TO LN.
           DISPLAY "W3 Z=" LZ " N=" LN.
           CLOSE PRTZ.
           CLOSE PRTN.
           STOP RUN.
