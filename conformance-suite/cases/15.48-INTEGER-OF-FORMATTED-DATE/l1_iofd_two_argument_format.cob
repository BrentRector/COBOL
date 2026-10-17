*>--- conformance-case
*> id: l1_iofd_two_argument_format
*> rules: [FMT-15.48.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.2 general format — FUNCTION INTEGER-OF-FORMATTED-DATE
      *> ( argument-1 argument-2 ): exactly two arguments, both required.
      *>
      *> THE FORMAT IS THE RULE. FUNCTION and INTEGER-OF-FORMATTED-DATE are both
      *> underlined, so both are required words; NEITHER argument is bracketed, so
      *> the admitted argument count is exactly two — no optional trailing
      *> argument, no repetition. The reject side covers BOTH halves the format
      *> asserts: negative/l1-iofd-arity1 and l1-iofd-arity3 for the argument count,
      *> and negative/l1-iofd-no-function-word for the underlined required word,
      *> whose one permission (§8.4.3.2.3 SR2, the REPOSITORY paragraph) is
      *> exercised in the accepting direction by
      *> 2014/l1_repository_bare_intrinsic_names.
      *> §8.3.5 r2 makes the comma "a separator that may be used anywhere the
      *> separator space is used", so the format's one space admits a comma too.
      *>
      *> EXPECTED VALUES, from §15.48.4 r1 — "The returned value is the integer
      *> date form equivalent of the date represented by argument-2 when analyzed
      *> according to argument-1" — over §15.5.2's integer date form ("a number of
      *> days succeeding December 31, 1600" on "a starting date of Monday,
      *> January 1, 1601"). 1995-02-15 is 143,950 days after 1601-01-01, hence
      *> integer date 143951; the leap years counted are the §15.5.1 Note's
      *> (÷4, except a century, except a century ÷400).
      *> THE SAME DAY IS SPELLED THREE WAYS, so what is pinned is the analysis, not
      *> one string: the basic calendar format "YYYYMMDD" (§15.3.1.2, eight
      *> characters), the extended calendar format "YYYY-MM-DD" (§15.3.1.2, ten
      *> characters with the two hyphens present in the data), and the basic
      *> ordinal format "YYYYDDD" (§15.3.1.4, seven characters) — 1995-02-15 is
      *> ordinal day 46 of 1995 (31 + 15). All three must return 143951.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A8   PIC X(8)  VALUE "19950215".
       01 A10  PIC X(10) VALUE "1995-02-15".
       01 A7   PIC X(7)  VALUE "1995046".
       01 R7   PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" A8)
           DISPLAY "BASIC=" R7
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD", A8)
           DISPLAY "BASIC-COMMA=" R7
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD" A10)
           DISPLAY "EXTENDED=" R7
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYDDD" A7)
           DISPLAY "ORDINAL=" R7
           STOP RUN.
       END PROGRAM L1IFD01.
