      *>--- conformance-case
      *> id: pb415_initialize_multi_category_85
      *> rules: [GR-14.9.20.4-2, GR-14.9.20.4-5, FMT-14.9.20.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB415 — ISO §14.9.20.2 category-name at COBOL-85, where the CHOICE INDICATORS matter most.
      *>
      *> The printed general format (licensed PDF p667 / folio 637) encloses the thirteen category names in a
      *> BRACE carrying choice indicators, and §5.2.6.4 reads them: "one or more of the alternatives contained
      *> within the choice indicators shall be specified, but any single alternative shall be specified only
      *> once". FIVE of the thirteen — ALPHABETIC, ALPHANUMERIC, ALPHANUMERIC-EDITED, NUMERIC, NUMERIC-EDITED —
      *> are COBOL-85 words (tests/version-matrix/reserved-words.json r85=true for each), and the REPLACING
      *> phrase itself is COBOL-85. So a MULTI-CATEGORY category-name is conforming COBOL-85 source, and
      *> `INITIALIZE G REPLACING NUMERIC ALPHANUMERIC DATA BY …` was a COBOL0001 parse error at EVERY edition
      *> before PB415 — this file is the '85 witness that the fix is not a 2002+ feature.
      *>
      *> The eight post-85 words are refused here by the initialize-category-2002 / -2014 / -2023 gates
      *> (COBOLNET0900); the negative witness for that is
      *> conformance-suite/cases/14.9.20-INITIALIZE/pb415-initialize-category-national-85.
      *>
      *> EXPECTED, DERIVED BEFORE THE RUN:
      *>   T1  REPLACING ALPHABETIC ALPHANUMERIC DATA BY "Q" — ONE category-name naming TWO categories.
      *>       §14.9.20.4 GR5c2 qualifies AB (§8.5.2.2) and AN (§8.5.2.3); GR6b's sender is literal-1 "Q";
      *>       GR4's implicit MOVE left-justifies and space-fills each to 3 → "Q  ". NU and NE name no
      *>       category in the phrase, so GR5c leaves them unchanged (GR5c4's premise is false — a REPLACING
      *>       phrase IS specified).
      *>   T2  REPLACING NUMERIC NUMERIC-EDITED DATA BY 7 — a second two-category category-name, this one
      *>       pairing a plain and an edited '85 category. NU → 007; NE takes the numeric literal through the
      *>       ordinary MOVE editing path, PIC ZZZ9 suppressing the leading zeros → "   7".
      *>       §14.9.20.3 SR6 is satisfied: no category is repeated across the two statements' phrases, and
      *>       none is repeated within either category-name (§5.2.6.4's "only once").
      *>   T3  the bare COBOL-85 form (GR5c4 + GR6c's fill table): "Figurative constant alphanumeric SPACES"
      *>       for alphabetic and alphanumeric, ZEROES for numeric, and the numeric-edited receiver takes the
      *>       EDITED zero through the same MOVE path → "   0" for PIC ZZZ9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415MC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(3) VALUE "abc".
          05 AN PIC X(3) VALUE "xyz".
          05 NU PIC 9(3) VALUE 123.
          05 NE PIC ZZZ9 VALUE "  42".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC ALPHANUMERIC DATA BY "Q".
           DISPLAY "T1=[" AB "][" AN "][" NU "][" NE "]".
           INITIALIZE G REPLACING NUMERIC NUMERIC-EDITED DATA BY 7.
           DISPLAY "T2=[" AB "][" AN "][" NU "][" NE "]".
           INITIALIZE G.
           DISPLAY "T3=[" AB "][" AN "][" NU "][" NE "]".
           STOP RUN.
