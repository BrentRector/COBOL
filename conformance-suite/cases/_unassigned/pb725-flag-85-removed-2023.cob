*>--- conformance-case
*> id: pb725-flag-85-removed-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> Annex E.2 item 21 ("Obsolete elements"): "The following features that were
      *> classified as obsolete in the previous COBOL standard, have been removed from this
      *> Working Draft International Standard: - FLAG-85 - FLAG-NATIVE-ARITHMETIC -
      *> Standard Arithmetic - Move of the figurative constant, QUOTE, to numeric or
      *> numeric-edited items". So >>FLAG-85 and >>FLAG-NATIVE-ARITHMETIC are REMOVED at
      *> 2023 (COBOLNET0902) and, by the same sentence's "classified as obsolete in the
      *> previous COBOL standard", OBSOLETE at 2014 (the COBOLNET0903 warning). Both were
      *> silently consumed at EVERY edition until kb/Work PB725.
      *>
      *> The introduction edge rides the same derivation as VCR row 28's other landed leg,
      *> arithmetic-standard-2002: the 7.3 facility is a COBOL-2002 introduction.
      *>
      *> Their OPERAND syntax is NOT derivable in-repo - the 2023 text removed the clauses,
      *> and the repo holds no 2002 or 2014 standard - so these rows gate the directive
      *> WORD, which is exactly what the removal record supports.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725NF8.
       PROCEDURE DIVISION.
       MAIN.
       >>FLAG-85 ALL ON
           DISPLAY "A".
       >>FLAG-NATIVE-ARITHMETIC STANDARD-BINARY
           STOP RUN.
