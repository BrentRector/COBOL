*>--- conformance-case
*> id: pb64t5-classification-twice
*> rules: [FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.6.2: the OBJECT-COMPUTER paragraph's clauses may each appear at most once (the bracket carries choice
      *> indicators - 5.2.6.4); a second CHARACTER CLASSIFICATION clause is COBOLNET2104 choice-alternative-repeated
      *> (kb/Work PB3133: counted by the ClauseCardinalities row, reported by the one choice-indicator reader;
      *> formerly COBOLNET1652)
      *> (kb/Work PB78 registered the code for the PCS; PB64 T5 gives the classification clause the same arm).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5DUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS LOCALE
           CHARACTER CLASSIFICATION IS USER-DEFAULT.
       PROCEDURE DIVISION.
           STOP RUN.
