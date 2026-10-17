*>--- conformance-case
*> id: pb2845-flag-option-word-folded-by-host-case-mapping
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2845 - the option words of >>FLAG-14 are COBOL words, so
      *> they match by the Annex C fold (ISO 8.1.3.2 GR3 b and GR4 b), not
      *> by the host's upper-casing: U+017F LATIN SMALL LETTER LONG S has
      *> no Annex C mapping, so "I-O-ſTATUS-07" is NOT the option
      *> I-O-STATUS-07 (7.3.15 format: the option is one of the words the
      *> directive lists), though .NET's ToUpperInvariant raises it to S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2845N.
       PROCEDURE DIVISION.
       >>FLAG-14 I-O-ſTATUS-07 ON
           STOP RUN.
