      *>--- conformance-case
      *> id: pb206_value_size_categories
      *> rules: [SR-13.18.63.3-5, SR-13.18.63.3-10]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206P2.
      *> ISO 13.18.63.3 SR5 and SR10 - the national and boolean twins of
      *> SR4's size sentence pair, plus the SUBJECT the pair does not
      *> measure at all.  kb/Work PB206.
      *>
      *> GN - SR5 sentence 3: "National literals in the VALUE clause of
      *>   a national group item shall not exceed the size of the group
      *>   item."  8.5.2.1 - "a national group item has class and
      *>   category national"; 13.18.29.4 GR2b treats it as though it
      *>   were PICTURE N(m) with m the group's national positions.
      *>   Exactly at m is conforming (negative twin:
      *>   pb206-national-group-value-oversize).
      *> EN / EB - the ELEMENTARY sentence of each (SR5 s.2, SR10 s.2),
      *>   at their boundary.
      *> UD / UN - THE SUBJECT WITH NO SIZE.  13.18.19.3 SR1: a DYNAMIC
      *>   LENGTH item's "character-string specified in that PICTURE
      *>   clause shall be one instance of the picture symbol 'N', or
      *>   'X'", and 13.18.19.4 GR1: "The picture symbol determines the
      *>   class."  The one symbol indicates a CLASS, never a size - the
      *>   maximum is the LIMIT phrase's or implementor-defined (GR2) -
      *>   so SR4/SR5's "size indicated by an explicit PICTURE clause"
      *>   has no value here and the screen must not measure.
      *>   ⛔ MEASURED ON 1d949007: UN was REJECTED (COBOLNET0898, "the
      *>   VALUE national literal exceeds the item's 1 national
      *>   positions") while UD, the byte-for-byte same shape in the
      *>   other category, compiled and ran - legal source refused by
      *>   one arm of a two-arm rule.  Both now bind, and FUNCTION
      *>   LENGTH proves the whole literal survived rather than a
      *>   one-position truncation.
      *>   15.50.4 r6 - "If argument-1 is a dynamic-length elementary
      *>   item, the current length of argument-1 in BYTES is returned"
      *>   - so UD's eight characters are 8 and UN's FOUR national
      *>   positions are also 8 (two bytes per national position,
      *>   D-N1/D-N3).  r6 governs, not r2's national-position count,
      *>   because the item is dynamic-length.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN GROUP-USAGE NATIONAL VALUE N"ABCD".
          05 GN1 PIC N(2).
          05 GN2 PIC N(2).
       01 EN PIC N(4) VALUE N"ABCD".
       01 EB PIC 1(4) VALUE B"1010".
       01 UD PIC X DYNAMIC LENGTH VALUE "SEEDLING".
       01 UN PIC N DYNAMIC LENGTH VALUE N"SEED".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "GN=[" GN1 "][" GN2 "]"
           DISPLAY "EN=[" EN "]"
           DISPLAY "EB=[" EB "]"
           DISPLAY "UD=[" UD "] " FUNCTION LENGTH(UD)
           DISPLAY "UN=[" UN "] " FUNCTION LENGTH(UN)
           STOP RUN.
