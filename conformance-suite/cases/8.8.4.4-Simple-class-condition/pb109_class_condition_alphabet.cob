*>--- conformance-case
*> id: pb109_class_condition_alphabet
*> rules: [GR-8.8.4.4.4-3, DOC-A.1-187]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.4 GR3 a (kb/Work PB109 - it was a loud staged value): "If alphabet-name-1 is specified, the
      *> condition is true if the content of the data item referenced by identifier-1 consists entirely of characters
      *> in the coded character set identified by alphabet-name-1 in the SPECIAL-NAMES paragraph." Which SET each
      *> phrase identifies is 12.3.7.4 GR7 + Table 6 (the CodedCharacterSet determinations, CONFORMANCE.md):
      *> STANDARD-1 = the 128 ISO/IEC 646 IRV characters (the identity on U+0000-U+007F); a literal-phrase alphabet's
      *> set contains EVERY native character (GR7 k4 - the unspecified characters are IN the set at remapped
      *> positions), so membership of it is true for any content; GR1 makes a zero-length operand false; GR2 NOT.
      *>
      *> What each line proves:
      *>   STD-ASCII=yes  - "Hi!" is all ISO 646.
      *>   STD-ACCENT=no  - an accented letter (U+00E9) is outside STANDARD-1's 128.
      *>   NOT-STD=yes    - the NOT form reverses (GR2).
      *>   REV-ANY=yes    - a literal alphabet's coded character set is total (GR7 k4), whatever its sequence.
      *>   STD-SPACE=yes  - space IS an ISO 646 character (membership, not the ALPHABETIC letter rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB109CC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET STD IS STANDARD-1
           ALPHABET REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(3) VALUE "Hi!".
       01  E               PIC X VALUE "é".
       01  SP3             PIC X(3) VALUE "a b".
       PROCEDURE DIVISION.
           IF A IS STD DISPLAY "STD-ASCII=yes" ELSE DISPLAY "STD-ASCII=no" END-IF
           IF E IS STD DISPLAY "STD-ACCENT=yes" ELSE DISPLAY "STD-ACCENT=no" END-IF
           IF E IS NOT STD DISPLAY "NOT-STD=yes" ELSE DISPLAY "NOT-STD=no" END-IF
           IF E IS REV DISPLAY "REV-ANY=yes" ELSE DISPLAY "REV-ANY=no" END-IF
           IF SP3 IS STD DISPLAY "STD-SPACE=yes" ELSE DISPLAY "STD-SPACE=no" END-IF
           STOP RUN.
