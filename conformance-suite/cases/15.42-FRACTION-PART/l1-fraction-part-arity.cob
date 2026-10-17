*>--- conformance-case
*> id: l1-fraction-part-arity
*> rules: [FMT-15.42.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.42.2 — the general format is `FUNCTION FRACTION-PART ( argument-1 )`: two underlined words
      *> and ONE parenthesized argument, with no optional element and no repetition. §15.3 makes that format
      *> the authority on the count — "The definition of a function specifies the number of arguments
      *> required, which may be zero, one, or more" — so a two-argument list is not a reference to this
      *> function at all, and there is no other §15.42 format for it to be.
      *>
      *> The ACCEPT side of the same format (the one-argument form, in a MOVE and in a COMPUTE) is pinned by
      *> conformance:2023/l1_integer_family_notes. This is the half that says the format is a CONSTRAINT and
      *> not merely a suggestion; without it, an arity model that silently ignored trailing arguments — or
      *> that treated FRACTION-PART as variadic like MAX — would pass every positive fixture in the corpus.
      *> Expected: COBOLNET1504, the §15.3 argument-count diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FPARITY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9V99 VALUE 1.50.
       01 B PIC S9V99 VALUE 2.25.
       01 R PIC S9V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION FRACTION-PART(A B).
           DISPLAY R.
           STOP RUN.
