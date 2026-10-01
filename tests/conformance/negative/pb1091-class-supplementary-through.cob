      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7.4 GR12 (cite.py --check 12.3.7.4 "the contiguous characters in
      *> the native character set beginning with the character specified by the
      *> value of literal-5, and ending with the character specified by the value
      *> of literal-6, are included in the set of characters identified by
      *> class-name-1" -> OK 12.3.7.4 12)).
      *>
      *> kb/Work PB1091 (row SR-12.3.7.3-L7.4). The ordinals 65536 and 65537 of
      *> UCS-4 name SUPPLEMENTARY characters (two UTF-16 code units each). They
      *> are LEGAL ordinals (12.3.7.3 SR17 c2: each exists in the IN set), and
      *> SR17 c4 - "Each national literal, when a THROUGH phrase is specified,
      *> shall be one character in length" - is a rule about national LITERALS,
      *> not numeric ones, so it is not the rule to name. What a THROUGH phrase
      *> needs is a POSITION in the NATIVE national set (GR12), which a character
      *> of two code units has none in; the standard is silent on the pair, so the
      *> implementor's choice (docs/CONFORMANCE.md DOC-A.1-188) is to refuse it
      *> under GR12's own message. The .err pins that message, and the positive
      *> golden pb1091_class_national_ordinal_in_ucs4 pins the legal neighbours.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1091SUPTHRU.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET U4 FOR NATIONAL IS UCS-4
           CLASS SUP FOR NATIONAL IS 65536 THRU 65537 IN U4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
