      *>--- conformance-case
      *> id: l1c01_native_ordinal_positions
      *> rules: [GR-12.3.7.4-6, DOC-A.1-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.4 r6 (SPECIAL-NAMES) — native ordinal positions
      *> THE RULE (A.1 item 8; documented choice: docs/CONFORMANCE.md
      *> row DOC-A.1-8, "ordinal = UTF-16 code unit + 1"):
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
      *> No PROGRAM COLLATING SEQUENCE is written, so ORD and CHAR see
      *> the native order directly (§12.3.6.4 10).
      *> DERIVATION of every .out line:
      *>   ORD(" ")  U+0020 = 32  -> 32 + 1   -> ORD-SPACE=00033
      *>   ORD("0")  U+0030 = 48              -> ORD-ZERO=00049
      *>   ORD("A")  U+0041 = 65              -> ORD-A=00066
      *>   ORD("a")  U+0061 = 97              -> ORD-LC-A=00098
      *>   ORD("~")  U+007E = 126             -> ORD-TILDE=00127
      *>   CHAR(66)  ordinal 66 = U+0041      -> C-66=[A]
      *>   CHAR(49)  ordinal 49 = U+0030      -> C-49=[0]
      *>   ORD(CHAR(98)) round trip           -> ROUND-98=00098
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(5).
       01 C  PIC X.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ORD(" ")
           DISPLAY "ORD-SPACE=" R
           COMPUTE R = FUNCTION ORD("0")
           DISPLAY "ORD-ZERO=" R
           COMPUTE R = FUNCTION ORD("A")
           DISPLAY "ORD-A=" R
           COMPUTE R = FUNCTION ORD("a")
           DISPLAY "ORD-LC-A=" R
           COMPUTE R = FUNCTION ORD("~")
           DISPLAY "ORD-TILDE=" R
           MOVE FUNCTION CHAR(66) TO C
           DISPLAY "C-66=[" C "]"
           MOVE FUNCTION CHAR(49) TO C
           DISPLAY "C-49=[" C "]"
           COMPUTE R = FUNCTION ORD(FUNCTION CHAR(98))
           DISPLAY "ROUND-98=" R
           STOP RUN.
