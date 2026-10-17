      *>--- conformance-case
      *> id: l1c34_typedef_format_spellings
      *> rules: [FMT-13.18.58.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.58.2 format — IS TYPEDEF [STRONG]: every spelling
      *> General format: "IS TYPEDEF [ STRONG ]" (TYPEDEF and STRONG
      *>   underlined,
      *> IS not underlined: IS is optional, STRONG is optional).
      *> cite.py --check 13.18.58.2 "TYPEDEF" -> OK §13.18.58.2
      *>   (General format)
      *>   IS <u>TYPEDEF</u> [ <u>STRONG</u> ]
      *> cite.py --check 13.18.58.3 "If the subject of the entry is an
      *>   elementary item, the STRONG phrase shall not be specified"
      *>   -> OK §13.18.58.3 1) (Syntax rules)
      *> cite.py --check 13.18.58.4 "If the TYPEDEF clause is
      *>   specified, the
      *>   data description entry is a type declaration" -> OK
      *>     §13.18.58.4 1)
      *> The four spellings the format admits -- TYPEDEF, IS TYPEDEF,
      *> TYPEDEF STRONG, IS TYPEDEF STRONG -- each declare a type
      *>   (STRONG only
      *> on groups, SR1), and each type is then used by a TYPE clause.
      *>   Every
      *> spelling must compile, and each declaration must be a TYPE
      *>   (not a
      *> data item): V1..V4 take their descriptions from T1..T4.
      *> DERIVATION:
      *>  V1 TYPE T1 (TYPEDEF, PIC 9)         MOVE 7        -> V1=7
      *>  V2 TYPE T2 (IS TYPEDEF, PIC X(3))   MOVE "ABC"    -> V2=ABC
      *>  V3 TYPE T3 (TYPEDEF STRONG group F 9(2), H X)
      *>     F=42, H="Z"                                    -> V3=42Z
      *>  V4 TYPE T4 (IS TYPEDEF STRONG group G X(2))
      *>     G="QR"                                         -> V4=QR
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF PIC 9.
       01  T2 IS TYPEDEF PIC X(3).
       01  T3 TYPEDEF STRONG.
           05  F           PIC 9(2).
           05  H           PIC X.
       01  T4 IS TYPEDEF STRONG.
           05  G           PIC X(2).
       01  V1 TYPE T1.
       01  V2 TYPE T2.
       01  V3 TYPE T3.
       01  V4 TYPE T4.
       PROCEDURE DIVISION.
           MOVE 7 TO V1.
           MOVE "ABC" TO V2.
           MOVE 42 TO F OF V3.
           MOVE "Z" TO H OF V3.
           MOVE "QR" TO G OF V4.
           DISPLAY "V1=" V1.
           DISPLAY "V2=" V2.
           DISPLAY "V3=" V3.
           DISPLAY "V4=" V4.
           STOP RUN.
