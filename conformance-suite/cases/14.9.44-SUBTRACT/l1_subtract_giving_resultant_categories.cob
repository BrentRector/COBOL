      *>--- conformance-case
      *> id: l1_subtract_giving_resultant_categories
      *> rules: [SR-14.9.44.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR4 (FORMAT 2) — "Identifier-3 shall reference
      *> a numeric data item or a numeric-edited data item."  BOTH
      *> categories the rule admits are stored into by ONE statement,
      *> so the golden pins the admission itself and not merely that
      *> GIVING works: `SUBTRACT 1 FROM 20 GIVING G NE` writes the one
      *> result through a numeric identifier-3 (G, PIC 9(4)) and a
      *> numeric-edited identifier-3 (NE, PIC ZZZ9), and the two
      *> printed images differ, which is what proves the second
      *> category was taken through the editing path rather than
      *> stored as raw digits.
      *>
      *> EVERY EXPECTED CHARACTER IS DERIVED FROM THE RULE TEXT.
      *> §14.9.44.4 GR2 (format 2): the initial evaluation is
      *> literal-1 = 1; that value is subtracted from literal-2 = 20;
      *> "The result is stored as the new value of the data item
      *> referenced by identifier-3", i.e. 19.  §14.7.7 rule 4b: the
      *> one intermediate is stored into each resulting data item in
      *> the left-to-right order written, so BOTH receivers get 19.
      *>   G  — §14.6.8.2 rule 4: a fixed-point numeric receiver takes
      *>        the value aligned by decimal point "with zero fill or
      *>        truncation on either end as required" -> 0019.
      *>   NE — §14.6.8.2 rule 5 sends a fixed-point numeric-edited
      *>        receiver to §13.18.40's editing rules; §13.18.40.5
      *>        rule 7 makes 'Z' the zero-suppression-with-replacement
      *>        symbol whose replacement character is the space, and
      *>        7 a) puts that replacement into every position
      *>        preceding "the first nonzero numeric character in the
      *>        item".  The four digit positions hold 0 0 1 9 and the
      *>        first three are 'Z', so positions 1 and 2 become
      *>        spaces and the result is the four characters
      *>        space-space-1-9.
      *> The brackets round NE exist so its LEADING spaces survive the
      *> corpus runner's per-line trailing-space trim.
      *>
      *> Its rejecting twin is
      *> negative/l1-subtract-sr4-giving-alphanumeric (PIC X(4), in
      *> neither category), and its distinguishing sibling is
      *> negative/l1-subtract-sr2-receiver-numeric-edited: the SAME
      *> PIC ZZZ9 item that SR4 admits HERE is barred by SR2 at the
      *> in-place FROM receiver.  That contrast is the whole content
      *> of SR4's second alternative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBGIV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G  PIC 9(4).
       01 NE PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT 1 FROM 20 GIVING G NE.
           DISPLAY "G=" G
           DISPLAY "NE=[" NE "]"
           STOP RUN.
