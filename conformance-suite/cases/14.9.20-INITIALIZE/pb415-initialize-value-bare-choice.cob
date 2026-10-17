*>--- conformance-case
*> id: pb415-initialize-value-bare-choice
*> rules: [FMT-14.9.20.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB415 — ISO 14.9.20.2 draws the VALUE phrase as `{ ALL | category-name } TO VALUE`: a BRACE, and
*> 5.2.6.3 says "the syntax element contained within the braces or one of the alternatives contained within
*> the braces shall be explicitly specified or is implicitly selected". Nothing is implicitly selected here,
*> so ONE of ALL and a category-name shall be written and the bare `TO VALUE` below is NOT conforming source.
*> WHAT THIS WITNESS PINS: until PB415 this exact program COMPILED CLEAN and ran, the omission silently read
*> as ALL, defended by a code comment citing a "14.9.20.2 note 2" that subclause does not carry — it has no
*> notes at all, and the general rule that does mention ALL (14.9.20.4 GR2) answers what ALL MEANS, not
*> whether the choice may be omitted. Run this fixture against the pre-fix compiler and Assert.False(ok) reds.
*> The conforming spelling is `INITIALIZE G ALL TO VALUE` (or `ALL VALUE` — TO is not underlined).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NBARE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A1 PIC X(3) VALUE "xyz".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G TO VALUE.
           STOP RUN.
