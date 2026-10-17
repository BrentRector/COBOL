      *>--- conformance-case
      *> id: l1-subtract-sr4-giving-alphanumeric
      *> rules: [SR-14.9.44.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR4 (FORMAT 2) — "Identifier-3 shall reference
      *> a numeric data item or a numeric-edited data item."
      *> THE REJECTING HALF of a two-category admission.  XG is
      *> described PIC X(4), which §8.5.2.3 makes a data item of
      *> category ALPHANUMERIC; it is on neither §8.5.2.12's list of
      *> numeric data items nor §8.5.2.13's list of numeric-edited
      *> ones, so it is outside both alternatives SR4 offers and
      *> `SUBTRACT 1 FROM 20 GIVING XG` is illegal.
      *> Literal-1 and literal-2 are numeric literals, so SR3 holds
      *> and cannot be the reason for the rejection; the only rule
      *> broken is SR4.
      *> The ADMITTING half is 2023/l1_subtract_giving_resultant_-
      *> categories, which stores the same value through BOTH
      *> categories SR4 does name and must COMPILE AND RUN.  The pair
      *> is what makes this a test of SR4's boundary rather than of a
      *> blanket receiver screen.
      *> Reject-at names every edition: SR4 carries no edition
      *> condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBSR4N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XG PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT 1 FROM 20 GIVING XG.
           STOP RUN.
