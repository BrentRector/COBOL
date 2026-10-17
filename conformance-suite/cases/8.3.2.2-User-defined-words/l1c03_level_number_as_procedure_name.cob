      *>--- conformance-case
      *> id: l1c03_level_number_as_procedure_name
      *> rules: [SR-8.3.2.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.2.2 2) — a level-number spelled as a paragraph-name
      *> and as a section-name
      *>
      *> "Within a source element, a given user-defined word may be
      *> used as only one type of user-defined word with the following
      *> exceptions: ... 2) a level-number may be the same as a
      *> paragraph-name or a section-name"
      *>   cite.py: OK  §8.3.2.2 2)  (User-defined words)
      *> "With the exception of section-names, paragraph-names, and
      *> level-numbers, each user-defined word shall contain at least
      *> one basic letter or extended letter."
      *>   cite.py: OK  §8.3.2.2 3)  (User-defined words)
      *>   (the all-digit procedure-names below are therefore legal)
      *>
      *> The words 01, 05, 77 and 88 are each a LEVEL-NUMBER in the
      *> data division AND a procedure-name: 05 and 88 are
      *> section-names,
      *> 01 and 77 are paragraph-names. Exception 2) makes the program
      *> legal; each word keeps both meanings.
      *>
      *> DERIVATION of each expected line (X=0, N=0 initially).
      *>   PERFORM 05  -> section 05 runs its one paragraph 01:
      *>                  X becomes 1 -> "01 RAN X=1".
      *>   PERFORM 01  -> the paragraph 01 alone: X=2 -> "01 RAN X=2".
      *>   PERFORM 88  -> section 88 runs paragraph 77: N=1; the
      *>                  level-88 condition X-TWO (VALUE 2) is true
      *>                  -> "77 RAN N=1 X-TWO".
      *>   then "END".
      *> A compiler that entered level-numbers into the procedure-name
      *> namespace would reject the program; one that confused the
      *> paragraph 01 with section 05 would change the counts.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  X     PIC 9 VALUE 0.
               88  X-TWO VALUE 2.
       77  N         PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           PERFORM 05.
           PERFORM 01.
           PERFORM 88.
           DISPLAY "END".
           STOP RUN.
       05 SECTION.
       01.
           ADD 1 TO X.
           DISPLAY "01 RAN X=" X.
       88 SECTION.
       77.
           ADD 1 TO N.
           IF X-TWO
               DISPLAY "77 RAN N=" N " X-TWO"
           END-IF.
