      *>--- conformance-case
      *> id: pb228_add_corresponding_values
      *> rules: [GR-14.9.2.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.2.4 GR3 and GR5 - ADD CORRESPONDING under native
      *> arithmetic, at COBOL-2002. GR3: "data items in identifier-4
      *> are added to and stored in corresponding items in
      *> identifier-5"; GR5: the pairs are chosen by §14.7.6 and "The
      *> results are the same as if the user had referred to each
      *> pair of corresponding identifiers in separate ADD
      *> statements". The rule has no edition delta; the xunit
      *> AddCorresponding cases run at 85 and 2014/
      *> corresponding_rounded_mode at 2014, so this golden is the
      *> value evidence at 2002 (kb/Work PB228).
      *>
      *> A    12 + 100 = 112 into PIC 999.
      *> B    1.25 + 2.1 = 3.35; no ROUNDED phrase means TRUNCATION
      *>      (§14.7.4.3 GR2), so PIC 9V9 holds 3.3 and DISPLAYs 33.
      *> C    -5 + 3 = -2; SIGN IS LEADING SEPARATE shows -02.
      *> K    a pair one level down: GRP OF SRC and GRP OF DST give
      *>      K the same qualifiers up to SRC and DST (§14.7.6
      *>      rule 1), so 2 + 40 = 42.
      *> N    SRC's N is alphanumeric, and an ADD pair needs both
      *>      items numeric (§14.7.6 rule 3): no pair, N keeps 4.
      *> T    DST's T has no partner in SRC (rule 1): T keeps 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB228ACN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 A PIC 99 VALUE 12.
          05 B PIC 9V99 VALUE 1.25.
          05 C PIC S99 VALUE -5.
          05 GRP.
             10 K PIC 9 VALUE 2.
          05 N PIC X VALUE "Z".
          05 S PIC 9 VALUE 7.
       01 DST.
          05 A PIC 999 VALUE 100.
          05 B PIC 9V9 VALUE 2.1.
          05 C PIC S99 SIGN IS LEADING SEPARATE VALUE 3.
          05 GRP.
             10 K PIC 99 VALUE 40.
          05 N PIC 9 VALUE 4.
          05 T PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           ADD CORRESPONDING SRC TO DST
           DISPLAY "A=" A OF DST " B=" B OF DST " C=" C OF DST
           DISPLAY "K=" K OF DST " N=" N OF DST " T=" T
           STOP RUN.
