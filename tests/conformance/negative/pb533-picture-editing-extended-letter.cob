      *> reject-at: 2023
      *> kb/Work PB533 - SR8's "basic letter". ISO 1989:2023 13.18.40.3 SR8: "Character-1 shall be any basic
      *> letter in the COBOL character set except those specified in a CURRENCY-SIGN clause or a basic letter
      *> character A, B, C, D, E, N, P, R, S, V, X, Z or their lowercase equivalents." 8.1.3.1 Table 1 fixes the
      *> COBOL character repertoire as basic letters, basic digits, basic special characters and EXTENDED
      *> letters - four distinct rows - and the basic letters are A-Z and a-z. An extended letter (Annex B)
      *> is therefore NOT a basic letter, so a PICTURE EDITING phrase that names one as character-1 is illegal
      *> source. The validator asked char.IsLetter, which admits every Unicode letter, so each of these
      *> compiled and rendered exactly like the legal `PIC 99T99 EDITING T IS ":"`; it now asks the ONE
      *> basic-letter predicate (CobolCharacterRepertoire.IsBasicLetter) and the diagnostic is COBOLNET1591.
      *> The EDITING phrase is a COBOL-2023 introduction (the edition gate refuses it below), so only 2023 is
      *> asked.
      *>
      *> NC1  LATIN CAPITAL LETTER E WITH ACUTE (U+00C9) - the adjudicator's own probe.
      *> NC2  GREEK SMALL LETTER ALPHA (U+03B1) - a lowercase extended letter outside Latin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB533NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC 99É99 EDITING É IS ":".
       01 NC2 PIC 99α99 EDITING α IS ":".
       PROCEDURE DIVISION.
           STOP RUN.
