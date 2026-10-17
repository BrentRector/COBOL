*>--- conformance-case
*> id: pb487-unrecognized-data-clause
*> rules: [FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - the CLOSED 13.16.2 Format-1 clause list.  The grammar used to end the list in
      *> `genericDataClause -> genericClause : IDENTIFIER (IDENTIFIER|literal)*`, a vendor-extension catch-all
      *> matching any run of words at the tail of a data description entry.  This program compiled CLEAN at
      *> every edition and ran, with WIBBLE and WOBBLE simply discarded - so a misspelled clause word, an
      *> undefined COMP-n, and every clause the compiler had not implemented all bound an entry the programmer
      *> did not write.  ISO 13.16.2's general format is a CLOSED list (rendered from the printed page, PDF
      *> p393 / printed folio 363) and 4.2.2 makes a general format the definition of what may be written, so
      *> the word is now NAMED: COBOLNET1941, at every edition and every strictness.  There is no dialect that
      *> admits it - a vendor extension is admitted only under the dialect that owns it, never by a catch-all,
      *> and this implementation declares no vendor dialect.
      *> Rejected at 85 as well as 2023: the general format is a closed list in every edition of the standard,
      *> and neither WIBBLE nor WOBBLE is a reserved word at any of them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487JK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) WIBBLE WOBBLE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
