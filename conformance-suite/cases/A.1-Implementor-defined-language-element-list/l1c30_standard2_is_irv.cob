      *>--- conformance-case
      *> id: l1c30_standard2_is_irv
      *> rules: [DOC-A.1-182]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.4 GR7 c) — STANDARD-2 references the 646 IRV
      *> THE RULE (Annex A.1 item 182, documented choice):
      *>   cite.py --check 12.3.7.4 "When the STANDARD-2 phrase is
      *>     specified, the referenced coded character set shall be as
      *>     specified in ISO/IEC 646; the implementor shall specify
      *>     whether the International Reference Version or a national
      *>     version is referenced" -> OK §12.3.7.4 7)  (GR7 c))
      *>   cite.py --check 12.3.7.4 "the value of figurative constant
      *>     symbolic-character-1 is the representation of the coded
      *>     character at ordinal position integer-1"
      *>     -> OK §12.3.7.4 11)  (GR11 b), the IN arm)
      *> DOCUMENTED CHOICE (docs/CONFORMANCE.md DOC-A.1-182): STANDARD-2
      *> ALWAYS references the ISO/IEC 646 International Reference
      *> Version, never a national version; its 128 characters are
      *> U+0000..U+007F, ordinal n = U+(n-1).
      *> The observation reads, through SYMBOLIC CHARACTERS ... IN S2,
      *> exactly the twelve code positions ISO/IEC 646 leaves to
      *> national versions (X"23" X"24" X"40" X"5B"-X"5E" X"60"
      *> X"7B"-X"7E"). A national-version choice prints different
      *> characters there (e.g. German DIN 66003 has a section sign at
      *> X"40" and umlauts at X"5B"-X"5D" and X"7B"-X"7D"; the UK
      *> version a pound sign at X"23"), so this line pins the IRV.
      *> ISO/IEC 646 is an UNDATED normative reference (clause 2), so
      *> its latest edition (1991) applies: that IRV has "$" at X"24".
      *> DERIVATION (ordinal = code + 1; IRV characters):
      *>   36 #  37 $  65 @  92 [  93 \  94 ]  95 ^  97 `  124 {
      *>   125 |  126 }  127 ~   -> IRV=[#$@[\]^`{|}~]
      *> Two controls read the invariant positions the same way:
      *>   66 -> "A", 98 -> "a"  -> INV=[Aa]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S2 IS STANDARD-2
           SYMBOLIC CHARACTERS
               V23 V24 V40 V5B V5C V5D V5E V60 V7B V7C V7D V7E
               ARE 36 37 65 92 93 94 95 97 124 125 126 127
               I41 I61 ARE 66 98
               IN S2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IRV=[" V23 V24 V40 V5B V5C V5D V5E V60
                   V7B V7C V7D V7E "]".
           DISPLAY "INV=[" I41 I61 "]".
           STOP RUN.
