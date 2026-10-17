      *>--- conformance-case
      *> id: l1c30_alphabet_named_locale_format_order
      *> rules: [SR-12.3.7.3-24, GR-12.3.7.4-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.4 GR7 d)2, e), k) 1.-2. — alphabet operands
      *> CLAUSE ORDER (kb/Work PB1558; lane misc-p30): §5.2.1 - clauses
      *> "shall be written ... in the sequence given in the general
      *> format"; §12.3.7.2 puts alphabet-name-clause BEFORE the
      *> LOCALE clause (rendered PDF p290), so SR24's locale-name-2
      *> is necessarily a FORWARD reference. The compiler bound the
      *> clauses in source order and refused it (COBOLNET1664 'no
      *> LOCALE clause is in scope'); it now resolves locale-names
      *> after the whole paragraph is bound.
      *> THE RULES (GR7 d) 2. and its sibling items, in row
      *> GR-12.3.7.4-7 since kb/Work PB55; its i)
      *> code-name, f)/g)/h) UCS-4/UTF-8/UTF-16 and e) unnamed-locale
      *> legs are pinned by existing goldens - see the lane report):
      *>   cite.py --check 12.3.7.4 "If the NATIONAL phrase is
      *>     specified, the native national coded character set and
      *>     native national collating sequence are referenced"
      *>     -> OK §12.3.7.4 7)  (GR7 d) 2.)
      *>   cite.py --check 12.3.7.4 "the collating sequence identified
      *>     is defined by the locale referenced by locale-name-2 when
      *>     specified" -> OK §12.3.7.4 7)  (GR7 e))
      *>   cite.py --check 12.3.7.4 "when specified in the NATIONAL
      *>     phrase, a national collating sequence is identified"
      *>     -> OK §12.3.7.4 7)  (GR7 e))
      *>   cite.py --check 12.3.7.4 "The ordinal number of a character
      *>     within the native character set, if the literal is
      *>     numeric." -> OK §12.3.7.4 7) 1.  (GR7 k) 1. a.)
      *>   cite.py --check 12.3.7.4 "each character in the literal,
      *>     starting with the leftmost character, is assigned
      *>     successive ascending positions in the collating sequence
      *>     being specified" -> OK §12.3.7.4 7) 1.  (GR7 k) 1. b.)
      *>   cite.py --check 12.3.7.4 "The order in which the literals
      *>     appear in the ALPHABET clause specifies, in ascending
      *>     sequence, the ordinal number of the character within the
      *>     collating sequence being specified." -> OK §12.3.7.4 7) 2.
      *>   cite.py --check 15.70.1 "The ORD function returns an integer
      *>     value that is the ordinal position of argument-1 in the
      *>     program collating sequence." -> OK §15.70.1
      *>   (cite.py labels list items by their parent, PB1554.)
      *> Native ordinals (docs/CONFORMANCE.md DOC-A.1-8, GR6): ordinal
      *> n is UTF-16 code unit n-1, so ordinal 36 is "#" (X"23").
      *> PROGRAM L1C30F: PCS FOR ALPHANUMERIC IS LA, FOR NATIONAL IS LN.
      *>   LA IS "C" "QM" 36 -> k2 order, k1b "QM" = Q then M, k1a 36 =
      *>   "#": positions C=1 Q=2 M=3 #=4 (§15.70.4 1) reads them):
      *>     ORD("C")=1 ORD("Q")=2 ORD("M")=3 ORD("#")=4
      *>       -> ORD-C=00001 ORD-Q=00002 ORD-M=00003 ORD-H=00004
      *>     "Q" < "M" under LA (native: N)        -> Q-LT-M=Y
      *>   LN FOR NATIONAL IS LOCALE US (US IS "en-US"): a NATIONAL
      *>   collating sequence defined by that locale. Every CLDR locale
      *>   orders letters before case, so N"a" < N"B" (native: U+0061
      *>   > U+0042, N)                              -> LOC Na-LT-NB=Y
      *> PROGRAM L1C30G (sibling, own OBJECT-COMPUTER): PCS FOR
      *> NATIONAL IS NN, NN FOR NATIONAL IS NATIVE: code-unit order.
      *>     N"a" < N"B"? U+0061 > U+0042        -> NAT Na-LT-NB=N
      *>     ORD(N"A") = native ordinal 66 (§15.70.4 2))
      *>                                        -> NAT ORD-NA=00066
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR ALPHANUMERIC IS LA
                                      FOR NATIONAL IS LN.
       SPECIAL-NAMES.
           ALPHABET LA IS "C" "QM" 36
           ALPHABET LN FOR NATIONAL IS LOCALE US
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9(5).
       01 W-Q  PIC X VALUE "Q".
       01 W-M  PIC X VALUE "M".
       01 W-NL PIC N VALUE N"a".
       01 W-NU PIC N VALUE N"B".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ORD("C")
           DISPLAY "ORD-C=" R
           COMPUTE R = FUNCTION ORD("Q")
           DISPLAY "ORD-Q=" R
           COMPUTE R = FUNCTION ORD("M")
           DISPLAY "ORD-M=" R
           COMPUTE R = FUNCTION ORD("#")
           DISPLAY "ORD-H=" R
           IF W-Q < W-M
               DISPLAY "Q-LT-M=Y"
           ELSE
               DISPLAY "Q-LT-M=N"
           END-IF
           IF W-NL < W-NU
               DISPLAY "LOC Na-LT-NB=Y"
           ELSE
               DISPLAY "LOC Na-LT-NB=N"
           END-IF
           CALL "L1C30G"
           STOP RUN.
       END PROGRAM L1C30F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS NN.
       SPECIAL-NAMES.
           ALPHABET NN FOR NATIONAL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9(5).
       01 W-NL PIC N VALUE N"a".
       01 W-NU PIC N VALUE N"B".
       PROCEDURE DIVISION.
       MAIN.
           IF W-NL < W-NU
               DISPLAY "NAT Na-LT-NB=Y"
           ELSE
               DISPLAY "NAT Na-LT-NB=N"
           END-IF
           COMPUTE R = FUNCTION ORD(N"A")
           DISPLAY "NAT ORD-NA=" R
           GOBACK.
       END PROGRAM L1C30G.
