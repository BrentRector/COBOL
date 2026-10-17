*>--- conformance-case
*> id: pb830-source-computer-unrecognized-clause
*> rules: [FMT-12.3.5.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB830 - the CLOSED ISO 12.3.5.2 general format, SOURCE-COMPUTER. [computer-name-1] .
      *> (RENDERED from the printed page, PDF p314 / folio 284).  After the computer-name the grammar
      *> had a `~DOT` token SINK, kept for the deleted '85 WITH DEBUGGING MODE clause and never gated,
      *> so this program compiled CLEAN and RAN at every edition with WIBBLE WOBBLE discarded.  ISO
      *> 4.2.2 ("An implementation shall provide a warning mechanism ... to indicate violations of the
      *> general formats and the explicit syntax rules of standard COBOL") makes the word a violation
      *> to be NAMED: COBOLNET1970.  Rejected at 85 as well: X3.23-1985's SOURCE-COMPUTER admits only
      *> WITH DEBUGGING MODE after the name, and neither word is reserved at any edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB830SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-370 WIBBLE WOBBLE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB830SC".
           STOP RUN.
