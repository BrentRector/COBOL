*>--- conformance-case
*> id: pb2845_flag_option_word_annex_c_fold
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2845 - the option words of >>FLAG-14 are COBOL words, so
      *> they match by the Annex C fold (ISO 8.1.3.2 GR3 b and GR4 b): the
      *> pair (0130,0069) folds U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE
      *> to i, so "İ-O-STATUS-07" IS the option I-O-STATUS-07 of the
      *> directive (7.3.15 format) and the directive is accepted. The
      *> negative pb2845-flag-option-word-folded-by-host-case-mapping is
      *> the converse (U+017F has no Annex C mapping).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2845P.
       PROCEDURE DIVISION.
       >>FLAG-14 İ-O-STATUS-07 ON
       MAIN-P.
           DISPLAY "ACCEPTED".
           STOP RUN.
