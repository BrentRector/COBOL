*>--- conformance-case
*> id: pb889-group-usage-over-same-as
*> rules: [SR-13.18.49.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB889 - ISO 13.18.49.3 SR9: "A group item to which the subject of the entry is subordinate
      *> shall not contain a GROUP-USAGE, SIGN, or USAGE clause." THREE clauses. The binder's ancestor walk
      *> tested SIGN and USAGE only, so a GROUP-USAGE NATIONAL group over a SAME AS entry compiled clean.
      *> The picture of SRC2 is N(4) on purpose: over a PIC X source the 13.18.60.3 SR12 picture/usage rule
      *> (COBOLNET0881) would reject the entry first and MASK the SR9 answer (the note's Correction 1).
      *> Expected: COBOLNET1555 naming 13.18.49.3 SR9, at every edition that has SAME AS (2002+).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB889SAGU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC2 PIC N(4).
       01 OUTER GROUP-USAGE NATIONAL.
          02 INNER SAME AS SRC2.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
