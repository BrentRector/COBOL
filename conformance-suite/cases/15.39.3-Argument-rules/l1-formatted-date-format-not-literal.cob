*>--- conformance-case
*> id: l1-formatted-date-format-not-literal
*> rules: [AR-15.39.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.39.3 r1 — "Argument-1 shall be a national or alphanumeric LITERAL." W-F is a data item, not a
      *> literal. Its CONTENT is a legal basic calendar date format (§15.3.1.2), which §15.39.3 r2 requires, so
      *> nothing but literal-ness is wrong here and a rejection cannot be attributed to the format-kind screen.
      *> ⚠ THE CONVERSE MUST STAY LEGAL: §8.3.3.6.3 SR1 admits a figurative constant wherever 'literal' appears,
      *> and §13.10 CONSTANT AS names a literal — both are exercised green elsewhere in the corpus, so this file
      *> is the reject side only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFDTLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-F PIC X(8) VALUE "YYYYMMDD".
       01 W-S PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-DATE(W-F 143951) TO W-S
           STOP RUN.
       END PROGRAM L1NFDTLIT.
