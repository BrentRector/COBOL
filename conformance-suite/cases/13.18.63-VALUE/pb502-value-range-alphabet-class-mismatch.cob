*>--- conformance-case
*> id: pb502-value-range-alphabet-class-mismatch
*> rules: [GR-13.18.63.4-18]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR31 sentence 2: "If literal-2 and literal-3 or literal-5 and literal-6 are of class
      *> national, alphabet-name-1 shall reference an alphabet that defines a national collating sequence" -
      *> and, read with 12.3.6, its converse: the two alphabet domains are disjoint, so an ALPHANUMERIC
      *> THROUGH range may not name an alphabet declared FOR NATIONAL either. That is the direction the
      *> sibling negative pb398-range-alphabet-class-mismatch does NOT cover: it puts a NATIONAL range under
      *> an alphanumeric alphabet, on the EVALUATE arm. Here an ALPHANUMERIC range names NREV, which
      *> 12.3.7 declares FOR NATIONAL, so it defines no alphanumeric collating sequence for 14.7.8 rule 2 to
      *> use (kb/Work PB502).
      *> AT 85 THE PROGRAM IS REFUSED TWICE OVER, WHICH IS WHY 85 IS OFF THE reject-at LINE. The
      *> ALPHABET ... FOR NATIONAL clause is itself a COBOL-2002 introduction (the national class; the
      *> compiler's own gate cites ISO 12.3.7, the SPECIAL-NAMES paragraph), so --std 85 emits
      *> COBOLNET0900 for the clause IN ADDITION TO the COBOLNET1999 below - measured, both. A case whose
      *> rejection at an edition has two independent causes witnesses neither cleanly, so the edition is
      *> left unclaimed rather than counted for this rule; the 85 leg of the phrase itself is carried by the
      *> sibling negatives pb502-value-range-alphabet-undeclared and -numeric-operands, which reject at 85
      *> on SR31 alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB502NEG3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET NREV FOR NATIONAL IS N"CBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "B".
           88 C-RANGE VALUE "A" THRU "C" IN NREV.
       PROCEDURE DIVISION.
       MAIN-P.
           IF C-RANGE DISPLAY "IN" ELSE DISPLAY "OUT" END-IF
           STOP RUN.
