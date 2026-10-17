*>--- conformance-case
*> id: pb889-group-usage-over-type
*> rules: [SR-13.18.57.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB889 - ISO 13.18.57.3 SR5: "No group item to which the subject of the entry is subordinate
      *> shall contain a GROUP-USAGE, SIGN, or USAGE clause." The TYPE twin of 13.18.49.3 SR9, and the same
      *> missing arm: the walk tested SIGN and USAGE only. Written with the standard's own Format 1 spelling
      *> TYPE TO type-name-1 (13.18.57.2; TO is the optional word). Expected: COBOLNET1538 naming
      *> 13.18.57.3 SR5, at every edition that has TYPE (2002+).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB889TYGU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TSRC TYPEDEF.
          05 TN PIC N(4).
       01 OUTER GROUP-USAGE NATIONAL.
          02 INNER TYPE TO TSRC.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
