*>--- conformance-case
*> id: l1_repository_bare_intrinsic_names
*> rules: [GR-12.3.8.4-13, FMT-15.46.2, FMT-15.48.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.46.2 / §15.48.2 general formats — the ACCEPTING arm of the one
      *> permission that lets the required word FUNCTION be left out.
      *>
      *> ⛔ A DISPATCH WITH TWO ARMS, BOTH WRITTEN DOWN. §8.4.3.2.3 SR2 reads "If
      *> intrinsic-function-name-1 or the ALL phrase is specified in the REPOSITORY
      *> paragraph … the word FUNCTION may be omitted from the function-identifier;
      *> otherwise the word FUNCTION is required." Its REJECTING arm is
      *> negative/l1-integer-of-date-no-function-word and negative/l1-iofd-no-
      *> function-word (no REPOSITORY paragraph, the name written bare). This is the
      *> other arm: the ALL phrase IS specified, so the same two references are
      *> legal written bare — and must return exactly what the FUNCTION-keyword
      *> spelling of the same reference returns. Writing only the negative would
      *> pin one arm of a two-arm rule and leave the permission itself unpinned.
      *>
      *> THE VALUE IS DERIVED ONCE, FROM THE RULES, NOT COPIED FROM A RUN.
      *> §15.46.4 r1 ("The returned value is in integer date form") and §15.48.4 r1
      *> ("The returned value is the integer date form equivalent of the date
      *> represented by argument-2 when analyzed according to argument-1") both
      *> resolve through §15.5.2: an integer date form value is "a positive integer
      *> that represents a number of days succeeding December 31, 1600, in the
      *> Gregorian calendar", on "a starting date of Monday, January 1, 1601", over
      *> the calendar §15.5.1 names and its leap Note (÷4, except a century, except
      *> a century ÷400). 1995-02-15 is 143,950 days after 1601-01-01, so its
      *> integer date form is 143951 — the same number by both routes, which is why
      *> all four lines below carry it. "YYYYMMDD" is §15.3.1.2's basic calendar
      *> date format (eight characters).
      *>
      *> ⚠ 2014, not 2002: SR2's permission arrives with the REPOSITORY FUNCTION
      *> specifier at COBOL-2002 (§12.3.8), but §15.48 INTEGER-OF-FORMATTED-DATE
      *> arrives at COBOL-2014, and one program that writes both bare names is the
      *> honest shape for a rule both rows cite. The permission's own edition floor
      *> is pinned separately by 2002/repository_paragraph.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1REPOBARE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A8 PIC X(8) VALUE "19950215".
       01 R7 PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.46.2 — the word omitted, then the same reference with it written.
           COMPUTE R7 = INTEGER-OF-DATE(19950215)
           DISPLAY "BARE-IOD=" R7
           COMPUTE R7 = FUNCTION INTEGER-OF-DATE(19950215)
           DISPLAY "KW-IOD=" R7
      *> §15.48.2 — the same pair for the two-argument format.
           COMPUTE R7 = INTEGER-OF-FORMATTED-DATE("YYYYMMDD" A8)
           DISPLAY "BARE-IOFD=" R7
           COMPUTE R7 = FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" A8)
           DISPLAY "KW-IOFD=" R7
           STOP RUN.
       END PROGRAM L1REPOBARE.
