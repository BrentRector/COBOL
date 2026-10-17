*>--- conformance-case
*> id: pb535-picture-no-category
*> rules: [SR-13.18.40.3-14, GR-13.18.40.4-3, GR-13.18.40.4-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB535 - A CHARACTER-STRING THAT DEFINES NO CATEGORY. ISO 1989:2023 13.18.40.4 GR3 lets a
      *> PICTURE clause define the subject of the entry to fall into ONE OF EIGHT categories, and each of
      *> GR5-GR13 states the character-string that defines its own. A string built only from 'S', 'V' and 'P'
      *> satisfies none of them: GR11, the only rule whose alphabet those three symbols belong to, requires
      *> that character-string-1 "shall include at least one symbol '9'" and admits 'P', 'S' and 'V' only
      *> BESIDE that '9'. 13.18.40.3 SR12 a says the same from the other side - character-string-1 shall
      *> contain at least one of 'A', 'N', 'X', 'Z', '1', '9', '*' or at least two occurrences of one of
      *> character-1, 'x', '+', '-' and the currency symbol - so the diagnostic is COBOLNET1934.
      *>
      *> Each of these bound a ZERO-LENGTH category-numeric item, silently, at every edition, for as long as
      *> nothing read symbol COMBINATION: the pure-numeric arm of the analyzer is the FALL-THROUGH, reached
      *> by exhaustion. A zero-length elementary item inside a group mis-lays every following member.
      *> GR11 and SR12 a are rules of every edition from 1985 on, so all four reject.
      *>
      *> NC1  S     - a lone operational sign: no digit position, no character position.
      *> NC2  PPP   - three scaling positions, none of which GR14 counts in the size of the item.
      *> NC3  SV    - a sign and an assumed decimal point, with no digit between them to align.
      *> NC4  SPPP  - the same with the scaling positions; still not one symbol '9'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB535NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC S.
       01 NC2 PIC PPP.
       01 NC3 PIC SV.
       01 NC4 PIC SPPP.
       PROCEDURE DIVISION.
           STOP RUN.
