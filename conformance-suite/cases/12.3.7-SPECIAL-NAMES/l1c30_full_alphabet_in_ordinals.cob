*>--- conformance-case
*> id: l1c30_full_alphabet_in_ordinals
*> rules: [SR-12.3.7.3-14, SR-12.3.7.3-17]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR17 c5 / SR16 f1 IN arm - the ordinals of an
      *> alphabet that names ALL 65,536 native national characters
      *> (kb/Work PB1557; golden-lane-2 misc-p30; SR17 c) 5., in row
      *> SR-12.3.7.3-17 since kb/Work PB55).
      *>
      *> An ALPHABET whose literal phrase names every native character
      *> is legal: SR14 c4 says the count "shall not exceed" the native
      *> count, so EQUAL is legal (cite.py --check 12.3.7.3 "The number
      *> of characters specified shall not exceed the number of
      *> characters in the native national character set" -> OK
      *> 12.3.7.3 14)). Its coded character set then has 65,536
      *> ordinals, each naming the character at that collating position
      *> (GR7 k2; DOC-A.1-186):
      *>   cite.py --check 12.3.7.3 "the number of characters in the
      *>     character set referenced by alphabet-name-4."
      *>     -> OK 12.3.7.3 17)
      *>   cite.py --check 12.3.7.3 "the ordinal position specified by
      *>     integer-1 shall exist in that character set"
      *>     -> OK 12.3.7.3 16)
      *>   cite.py --check 12.3.7.4 "The order in which the literals
      *>     appear in the ALPHABET clause specifies, in ascending
      *>     sequence, the ordinal number of the character within the
      *>     collating sequence being specified." -> OK 12.3.7.4 7) 2.
      *>
      *> WHY IT CAN FAIL: the binder counted the alphabet's positions in
      *> a 16-bit counter that wrapped 65,536 to 0, so every ordinal of
      *> NREV "did not exist" - the CLASS clause drew COBOLNET1671 and the
      *> SYMBOLIC CHARACTERS clause COBOLNET1670 on legal source.
      *>
      *> DERIVATION: NREV = 65536 THRU 1, so ordinal 1 of NREV is U+FFFF
      *> and ordinal 65536 is U+0000; GR12's THROUGH takes the contiguous
      *> native characters between them = every character, so N"(EURO)A"
      *> IS NIN -> EURO-A-IN-NIN=Y. SYMBOLIC CHARACTERS S66 IS 66 IN NREV
      *> names the character at position 66 = native ordinal 65537-66 =
      *> 65471 = U+FFBE; FUNCTION ORD of it under the NREV national PCS
      *> is its position (15.70.1) -> ORD-S66=00066. N"B" < N"A" under
      *> NREV (8.8.4.2.9; native says N) -> NB-LT-NA=Y.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS NREV.
       SPECIAL-NAMES.
           ALPHABET NREV FOR NATIONAL IS 65536 THRU 1
           CLASS NIN FOR NATIONAL IS 1 THRU 65536 IN NREV
           SYMBOLIC CHARACTERS FOR NATIONAL S66 IS 66 IN NREV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NE PIC N(2) VALUE N"€A".
       01 W-N  PIC N.
       01 W-NA PIC N VALUE N"A".
       01 W-NB PIC N VALUE N"B".
       01 R    PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           IF W-NE IS NIN
               DISPLAY "EURO-A-IN-NIN=Y"
           ELSE
               DISPLAY "EURO-A-IN-NIN=N"
           END-IF
           MOVE S66 TO W-N
           COMPUTE R = FUNCTION ORD(W-N)
           DISPLAY "ORD-S66=" R
           IF W-NB < W-NA
               DISPLAY "NB-LT-NA=Y"
           ELSE
               DISPLAY "NB-LT-NA=N"
           END-IF
           STOP RUN.
