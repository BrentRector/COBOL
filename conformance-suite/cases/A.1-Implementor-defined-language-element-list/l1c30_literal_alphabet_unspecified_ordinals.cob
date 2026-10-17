      *>--- conformance-case
      *> id: l1c30_literal_alphabet_unspecified_ordinals
      *> rules: [DOC-A.1-186]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.4 GR7 k) 4. — ordinals of unspecified characters
      *> THE RULE (Annex A.1 item 186, documented scheme):
      *>   cite.py --check 12.3.7.4 "the implementor defines the ordinal
      *>     number within the character code set being specified for
      *>     each character within the native character set that is not
      *>     specified by the literal-1 phrase" -> OK §12.3.7.4 7) 2.
      *>     (cite.py mislabels the list item, PB1554: it is GR7 k) 4.)
      *>   cite.py --check 12.3.7.4 "the value of figurative constant
      *>     symbolic-character-1 is the representation of the coded
      *>     character at ordinal position integer-1"
      *>     -> OK §12.3.7.4 11)  (GR11 b), read with and without IN)
      *> DOCUMENTED SCHEME (docs/CONFORMANCE.md DOC-A.1-186): a
      *> character's ordinal is its collating position counting from 1;
      *> the n specified positions come first (an ALSO group is ONE
      *> position named by its first character), then the unspecified
      *> characters take n+1, n+2, ... in ascending native code order,
      *> skipping every specified character. Native ordinals: code + 1
      *> (DOC-A.1-8).
      *> AL IS "Z" "Y" ALSO "W" "X": n = 3 (Z; Y ALSO W; X).
      *> DERIVATION of every .out line:
      *>   ordinals 1 2 3 = Z Y X                          -> [ZYX...
      *>   70: the 67th unspecified character; codes 0..65 (66 chars)
      *>       precede "B" (U+0042) and none is specified -> "B"
      *>   90: codes 0..85 precede "V" (U+0056), none specified, so V
      *>       is the 87th unspecified character, 3+87 = 90 -> "V"
      *>   97: codes 0..96 hold 97 characters of which W X Y Z are
      *>       specified: 93 precede "a", so a is 94th, 3+94 -> "a"
      *>                                        -> SPEC=[ZYXBVa]
      *>   4 and 5: the 1st and 2nd unspecified characters, U+0000 and
      *>   U+0001 = native ordinals 1 and 2 (NUL/SOH below, no IN):
      *>                                        -> S4-IS-U0000=Y
      *>                                        -> S5-IS-U0001=Y
      *> Other plausible schemes print differently: keeping native
      *> ordinals gives 70 -> "E" and 4 -> U+0003; giving the ALSO
      *> character W its own ordinal gives 3 -> "W".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "Z" "Y" ALSO "W" "X"
           SYMBOLIC CHARACTERS S1 S2 S3 S4 S5 S70 S90 S97
               ARE 1 2 3 4 5 70 90 97 IN AL
           SYMBOLIC CHARACTERS NUL SOH ARE 1 2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SPEC=[" S1 S2 S3 S70 S90 S97 "]".
           IF S4 = NUL
               DISPLAY "S4-IS-U0000=Y"
           ELSE
               DISPLAY "S4-IS-U0000=N".
           IF S5 = SOH
               DISPLAY "S5-IS-U0001=Y"
           ELSE
               DISPLAY "S5-IS-U0001=N".
           STOP RUN.
