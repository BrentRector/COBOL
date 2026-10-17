*>--- conformance-case
*> id: pb431-format3-control-phrase
*> rules: [FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB431 — ISO §14.9.28.2 Format 3 (exception-checking PERFORM) prints NO loop-control phrase:
      *> its head is [ WITH LOCATION ] and nothing else, followed by imperative-statement-1 and the WHEN
      *> phrases (rendered from the printed page 682 / PDF 712). Formats 2 and 3 share one grammar
      *> alternative here, so a times-phrase can be written before the WHEN — and the Format-3 binder never
      *> asked for a control phrase, so EVERY one written was dropped and the body ran exactly ONCE.
      *> reject-at names 2023 alone because Format 3 itself is COBOL-2023: below it the WHEN phrase is
      *> refused first by the introduction gate (COBOLNET0900), which is a different diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB431NEGF3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM 3 TIMES
               ADD 1 TO X
             WHEN EC-ALL
               DISPLAY "CAUGHT"
           END-PERFORM
           STOP RUN.
