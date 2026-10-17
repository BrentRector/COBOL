*>--- conformance-case
*> id: pb830-object-computer-unrecognized-clause
*> rules: [FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB830 - the CLOSED ISO 12.3.6.2 general format (RENDERED, PDF p315 / folio 285): the
      *> OBJECT-COMPUTER paragraph is [computer-name-1] followed by the CHARACTER CLASSIFICATION and
      *> PROGRAM COLLATING SEQUENCE clauses and nothing else.  The token SINK that followed the name
      *> (kept for the deleted '85 MEMORY SIZE and SEGMENT-LIMIT clauses, never gated) swallowed
      *> WIBBLE WOBBLE at every edition; ISO 4.2.2 makes it a violation to be NAMED: COBOLNET1970.
      *> Rejected at 85 as well: X3.23-1985's clause list is MEMORY SIZE, PROGRAM COLLATING SEQUENCE
      *> and SEGMENT-LIMIT, and neither word is reserved at any edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB830OC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. IBM-370 WIBBLE WOBBLE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB830OC".
           STOP RUN.
