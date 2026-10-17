*>--- conformance-case
*> id: l1_alphabet_ucs4_utf8_utf16_correspondence
*> rules: [GR-12.3.7.4-7, DOC-A.1-188]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.3.7.4 GR7 f/g/h — SPECIAL-NAMES ALPHABET clause, UCS-4 / UTF-8 / UTF-16
      *> phrases: the correspondence with the native character set (Annex A.1 item 188;
      *> docs/CONFORMANCE.md DOC-A.1-188).
      *>
      *> THE RULES.
      *>   f) "When the UCS-4 phrase is specified, the coded character set referenced shall be
      *>      as specified in ISO/IEC 10646 as UTF-32. Each character of UTF-32 is associated
      *>      with a corresponding character of the native national character set. The
      *>      implementor shall specify the correspondence between the characters of UTF-32
      *>      and the characters of the native national character set. ..."
      *>   g) "When the UTF-8 phrase is specified ... The association is the same as the
      *>      association for the UCS-32 character from which the UTF-8 character was
      *>      transformed."
      *>   h) "When the UTF-16 phrase is specified ... The association is the same as the
      *>      association for the UTF-32 character from which the UTF-16 character was
      *>      transformed."
      *> Table 6 makes all three references to a CODED CHARACTER SET, so the observation
      *> point is the coded-set surface, not a collating surface. §12.3.7.4 GR11 c is the
      *> surface used here: "When NATIONAL is specified, the value of figurative constant
      *> symbolic-character-1 is the internal representation of the character at ordinal
      *> position integer-1 in the native national character set or, if the IN phrase is
      *> specified, in the character set referenced by alphabet-name-3." §12.3.7.3 SR16 f1
      *> requires that alphabet to define a NATIONAL character set, which all three phrases
      *> do. So `SYMBOLIC CHARACTERS FOR NATIONAL s IS n IN a` NAMES the character at ordinal
      *> n of a, and FUNCTION ORD then reports which NATIVE national character that is —
      *> §15.70.4 r2 reads the national program collating sequence, which is left NATIVE here,
      *> and §12.3.7.4 GR6 makes the native ordinal association the implementor's ("The
      *> implementor shall define the order of characters within the native alphanumeric
      *> coded character set and the native national coded character set, associating each
      *> character with an ordinal position within the character set"). COBOL.NET's
      *> determination is UTF-16 code units in code-unit order, so the native ordinal of a
      *> character is its code unit + 1 (§15.70.1 "The lowest ordinal position is 1").
      *> Each printed number is therefore THE CORRESPONDENCE GR7 asks the implementor to
      *> document, read out of the compiler.
      *>
      *> A / S - the correspondence on the shared BMP range, at two ordinals, in all three
      *>     sets. Ordinal 66 is the 66th character of each set = U+0041, native ordinal 66;
      *>     ordinal 353 is U+0160, native ordinal 353. The determination is the IDENTITY
      *>     over U+0000..U+D7FF, and the non-ASCII probe is what makes that a measurement
      *>     rather than an accident of the ASCII range.
      *> E - THE DISCRIMINATOR, and it is pure rule text. The characters of UTF-32 (and hence
      *>     of UTF-8, by g's "same association") are the ISO/IEC 10646 SCALAR VALUES; the
      *>     surrogate code points U+D800..U+DFFF are not characters of those sets. Ordinals
      *>     1..55296 are therefore U+0000..U+D7FF and ordinal 55297 is the NEXT scalar value,
      *>     U+E000 — whose native ordinal is 57344 + 1 = 57345. A UCS-4 or UTF-8 coded set
      *>     built as a plain 0-origin table over code units would answer 55297 instead. The
      *>     UTF-16 phrase is deliberately NOT probed at this ordinal: the native national set
      *>     is code units and the standard's own transformation wording leaves that shape a
      *>     separate question, so this golden claims only what f and g settle.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ALPH01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET W16 FOR NATIONAL IS UTF-16
           ALPHABET U32 FOR NATIONAL IS UCS-4
           ALPHABET U08 FOR NATIONAL IS UTF-8
           SYMBOLIC CHARACTERS FOR NATIONAL A16 IS 66 IN W16
           SYMBOLIC CHARACTERS FOR NATIONAL A32 IS 66 IN U32
           SYMBOLIC CHARACTERS FOR NATIONAL A08 IS 66 IN U08
           SYMBOLIC CHARACTERS FOR NATIONAL S16 IS 353 IN W16
           SYMBOLIC CHARACTERS FOR NATIONAL S32 IS 353 IN U32
           SYMBOLIC CHARACTERS FOR NATIONAL S08 IS 353 IN U08
           SYMBOLIC CHARACTERS FOR NATIONAL E32 IS 55297 IN U32
           SYMBOLIC CHARACTERS FOR NATIONAL E08 IS 55297 IN U08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC PIC N(1).
       01 R  PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE A16 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "A16=" R
           MOVE A32 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "A32=" R
           MOVE A08 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "A08=" R
           MOVE S16 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "S16=" R
           MOVE S32 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "S32=" R
           MOVE S08 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "S08=" R
           MOVE E32 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "E32=" R
           MOVE E08 TO NC
           MOVE FUNCTION ORD(NC) TO R
           DISPLAY "E08=" R
           STOP RUN.
