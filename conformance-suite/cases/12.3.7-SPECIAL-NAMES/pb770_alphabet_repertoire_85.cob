      *>--- conformance-case
      *> id: pb770_alphabet_repertoire_85
      *> rules: [GR-12.3.7.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB770 - the ALPHABET literal phrase's repertoire rule holds at COBOL-85 too, and the new
      *> 12.3.7.3 SR14 a duplicate check does not reject a phrase whose characters are all distinct.
      *>
      *> ALF  IS 305 THRU 300 - 12.3.7.4 GR7 k1a makes those ordinals U+0130 and U+012B, k5 positions the
      *>      descending run at 0..5, and k3 leaves every ASCII character in native relative order six
      *>      positions higher. So "0" < "+" is FALSE (8.8.4.2.7). The pre-PB770 builder masked the ordinals
      *>      to 8 bits and reversed '+' and '0', printing ORDER=REVERSED here.
      *> FINE IS "A" ALSO "B" ALSO "C", "1" THRU "9", SPACE - A/B/C share position 0 (k6), '1'..'9' take
      *>      1..9, the space takes 10. No character is specified twice, so SR14 a is satisfied: this clause is
      *>      the POSITIVE CONTROL that the duplicate diagnostic is not rejecting every literal phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770R85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS ALF.
       SPECIAL-NAMES.
           ALPHABET ALF IS 305 THRU 300
           ALPHABET FINE IS "A" ALSO "B" ALSO "C", "1" THRU "9", SPACE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-PLUS     PIC X VALUE "+".
       01 C-ZERO     PIC X VALUE "0".
       01 C-A        PIC X VALUE "A".
       PROCEDURE DIVISION.
           IF C-ZERO < C-PLUS
               DISPLAY "ORDER=REVERSED"
           ELSE
               DISPLAY "ORDER=NATIVE"
           END-IF
           IF C-A < C-ZERO
               DISPLAY "A-BEFORE-ZERO"
           ELSE
               DISPLAY "ZERO-BEFORE-A"
           END-IF
           STOP RUN.
