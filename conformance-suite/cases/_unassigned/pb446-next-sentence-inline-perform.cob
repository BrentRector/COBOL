*>--- conformance-case
*> id: pb446-next-sentence-inline-perform
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.28.2 Format 2 prints PERFORM [ ... ] imperative-statement-1 END-PERFORM, and NEXT SENTENCE
*> is not a statement: it is a phrase of IF Format 2 (§14.9.19.2) and of both SEARCH formats (§14.9.37.2) only.
*> The sibling of the SEARCH AT END case - the grammar parses NEXT SENTENCE as a statement everywhere, and the
*> ONE funnel now admits it only as the whole of one of those phrases. Refused at every edition. COBOLNET2269
*> (kb/Work PB446).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB446NSPF.
       PROCEDURE DIVISION.
           PERFORM 2 TIMES NEXT SENTENCE END-PERFORM
           DISPLAY "TAIL".
           STOP RUN.
