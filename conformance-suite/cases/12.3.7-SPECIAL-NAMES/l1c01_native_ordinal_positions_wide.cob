      *>--- conformance-case
      *> id: l1c01_native_ordinal_positions_wide
      *> rules: [GR-12.3.7.4-6, DOC-A.1-8, DOC-A.1-34, DOC-A.1-36, DOC-A.1-37, DOC-A.1-38]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.4 r6 (SPECIAL-NAMES) — ordinals, both native sets
      *> THE RULE (A.1 item 8; documented choice: docs/CONFORMANCE.md
      *> row DOC-A.1-8, "ordinal = UTF-16 code unit + 1" in BOTH the
      *> alphanumeric and the national native sets):
      *>   cite.py --check 12.3.7.4 "The implementor shall define the
      *>     order of characters within the native alphanumeric coded
      *>     character set and the native national coded character set,
      *>     associating each character with an ordinal position"
      *>     -> OK §12.3.7.4 6)
      *>   cite.py --check 12.3.6.4 "When the PROGRAM COLLATING SEQUENCE
      *>     clause is not specified and the source unit is not
      *>     contained within a source unit for which a PROGRAM
      *>     COLLATING SEQUENCE clause is specified, the initial program
      *>     collating sequences are the native alphanumeric collating
      *>     sequence" -> OK §12.3.6.4 10)
      *>   cite.py --check 15.70.1 "The ORD function returns an integer
      *>     value that is the ordinal position of argument-1 in the
      *>     program collating sequence. The lowest ordinal position is
      *>     1." -> OK §15.70.1
      *>   cite.py --check 15.15.4 "The returned value shall be the
      *>     character in the alphanumeric program collating sequence
      *>     having the ordinal position specified by argument-1"
      *>     -> OK §15.15.4 1)
      *>   cite.py --check 15.16.4 "The returned value is the character
      *>     in the national program collating sequence having the
      *>     ordinal position specified by argument-1" -> OK §15.16.4 1)
      *> 2002 dir: hex (X"") and national (N"") literals and
      *> CHAR-NATIONAL are 2002 constructs.
      *> DERIVATION of every .out line:
      *>   ORD(X"00")  U+0000, the lowest ordinal     -> ORD-X00=00001
      *>   ORD(X"FF")  the byte 0xFF is U+FFFF, the
      *>               highest native character (the
      *>               storage-byte law; kb/Work PB2919
      *>               reversed the former U+00FF)     -> ORD-XFF=65536
      *>   ORD("ÿ")    U+00FF = 255 (a character, not
      *>               a hexadecimal byte)             -> ORD-YUML=00256
      *>   ORD("€")    U+20AC = 8364 (the set is all
      *>               65,536 code units, not 256)   -> ORD-EURO=08365
      *>   ORD(N"A")   national U+0041, same numbering -> ORD-NA=00066
      *>   ORD(N"€")   national U+20AC               -> ORD-NEURO=08365
      *>   CHAR(8365) = "€"                            -> CHAR-8365=EQ
      *>   CHAR-NATIONAL(66) = N"A"                    -> CHARN-66=EQ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ORD(X"00")
           DISPLAY "ORD-X00=" R
           COMPUTE R = FUNCTION ORD(X"FF")
           DISPLAY "ORD-XFF=" R
           COMPUTE R = FUNCTION ORD("ÿ")
           DISPLAY "ORD-YUML=" R
           COMPUTE R = FUNCTION ORD("€")
           DISPLAY "ORD-EURO=" R
           COMPUTE R = FUNCTION ORD(N"A")
           DISPLAY "ORD-NA=" R
           COMPUTE R = FUNCTION ORD(N"€")
           DISPLAY "ORD-NEURO=" R
           IF FUNCTION CHAR(8365) = "€"
               DISPLAY "CHAR-8365=EQ"
           ELSE
               DISPLAY "CHAR-8365=NE"
           END-IF
           IF FUNCTION CHAR-NATIONAL(66) = N"A"
               DISPLAY "CHARN-66=EQ"
           ELSE
               DISPLAY "CHARN-66=NE"
           END-IF
           STOP RUN.
