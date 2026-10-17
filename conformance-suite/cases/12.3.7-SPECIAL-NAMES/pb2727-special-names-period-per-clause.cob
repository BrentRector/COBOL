*>--- conformance-case
*> id: pb2727-special-names-period-per-clause
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - ISO 12.3.7.2 prints ONE separator period after the SPECIAL-NAMES clauses
      *> (12.3.7.3 SR31 lets one of the paragraph's two periods go only when no clause is written), and
      *> 5.2.1 binds the punctuation a general format prints.  A period after the CLASS clause compiled
      *> clean at every edition; it is refused by name at that inner period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727SP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXD IS "0" THRU "9".
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
           STOP RUN.
