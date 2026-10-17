*>--- conformance-case
*> id: pb917-data-usage-twice
*> rules: [FMT-13.16.2, FMT-13.18.52.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB917 - ISO 5.2.6.2 (cite.py OK): brackets "indicate that the syntax element contained within the brackets ... may be explicitly specified or that portion of the
      *> general format may be omitted"; 5.2.7 (cite.py OK): "the ellipsis represents the position at which the user elects repetition of a portion of a format".  13.16.2 Format 1
      *> prints [ usage-clause ] once with no ellipsis, so USAGE written twice is non-conforming (the last USAGE used to win silently).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917USAGETWICE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N USAGE DISPLAY USAGE BINARY PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO N.
           DISPLAY N.
           STOP RUN.
