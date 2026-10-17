*>--- conformance-case
*> id: pb78-object-computer-duplicate-clause
*> rules: [FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.6.2's format encloses the OBJECT-COMPUTER clauses in choice indicators within brackets - 5.2.6.4:
      *> zero or more, EACH AT MOST ONCE, any order. A second PROGRAM COLLATING SEQUENCE clause is COBOLNET2104
      *> choice-alternative-repeated (kb/Work PB3133: counted by the ClauseCardinalities row, reported by the one
      *> choice-indicator reader; formerly COBOLNET1652).
      *> kb/Work PB78.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB78DUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           PROGRAM COLLATING SEQUENCE IS AL
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS STANDARD-1.
       PROCEDURE DIVISION.
           STOP RUN.
