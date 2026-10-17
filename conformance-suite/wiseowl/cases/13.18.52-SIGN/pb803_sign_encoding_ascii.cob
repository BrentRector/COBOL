*>--- conformance-case
*> id: pb803_sign_encoding_ascii
*> rules: [GR-13.18.52.4-4, GR-13.18.52.4-5, DOC-A.1-177, DOC-A.1-178]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *>
      *> ISO §13.18.52.4 GR4 / GR5 b) under the NON-DEFAULT over-punch convention — Annex A.1 items
      *> 177 and 178 (kb/Work PB803, owner decision 2026-09-09: "Keep both behind an option with the
      *> default being IBM and Micro Focus compatibility").
      *>
      *> ⛔ THIS GOLDEN COMPILES WITH AN OPTION, and the `*> options:` header on its first line is
      *> how it says so — the same place the negative corpus declares `*> reject-at:`, read by
      *> ConformanceCorpus.ApplySourceOptions.  Without the header this program compiles under the
      *> DEFAULT convention and every leg below is wrong, so the header is not decoration: it is the
      *> only thing that makes the option OBSERVABLE from the corpus, and its own failure mode is a
      *> loud one (an unknown key or value throws rather than falling back to the default).
      *>
      *> THE DETERMINATION PINNED HERE (docs/CONFORMANCE.md DOC-A.1-177 / -178, the `ascii` row).
      *> A POSITIVE digit is left alone and a NEGATIVE digit becomes digit + 0x40, so 0-9 fuse to
      *> "0123456789" positive and "pqrstuvwxy" negative.  This is GnuCOBOL's `-fsign=ascii`, its
      *> default on a PC host; the default `ibm` row of the same tables is pinned by
      *> conformance:2023/pb803_sign_default_representation.
      *>
      *> WHY EACH LEG CAN FAIL — expected values derived from the determination, not observed.
      *> NEG / LEAD / POS are the three images the two conventions DISAGREE on: under `ibm` they
      *>   would read `123M|`, `J234|` and `123D|`.  POS is the leg that is easy to get wrong in the
      *>   other direction -- under `ascii` a POSITIVE value is byte-identical to an unsigned one,
      *>   so an encoder that punched every sign would answer `1234|` only by accident and a decoder
      *>   that did not treat a plain digit as positive would lose the value.
      *> The `|` sentinel pins the width exactly as it does in the default golden: the fused sign
      *>   occupies a DIGIT position under every convention (§13.18.52.4 GR5 a), which is normative
      *>   and not part of the latitude), so PIC S9(4) is 4 characters here too.
      *> The class legs pin GR5 b)'s valid set for THIS convention, and the last two are the point:
      *>   `12C` and `12{` are valid signs under `ibm` and are NOT valid signs here.  A predicate
      *>   that answered from a fixed table would report them NUMERIC and the option would be a
      *>   half-measure that changed the encoder and not the class condition.
      *> `12p` / `12y` are the two ends of the negative run (digit 0 and digit 9), so the leg fails
      *>   if the table is off by one at either end.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB803C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA.
          05 GA-V   PIC S9(4) VALUE -1234.
          05 GA-S   PIC X     VALUE "|".
       01 GB.
          05 GB-V   PIC S9(4) SIGN IS LEADING VALUE -1234.
          05 GB-S   PIC X     VALUE "|".
       01 GC.
          05 GC-V   PIC S9(4) VALUE +1234.
          05 GC-S   PIC X     VALUE "|".
       01 W5   PIC X(5).
       01 RAW.
          05 R3    PIC X(3).
          05 RT    REDEFINES R3 PIC S9(3) SIGN IS TRAILING.
       01 SHOW PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE GA TO W5
           DISPLAY "NEG=[" W5 "]"
           MOVE GB TO W5
           DISPLAY "LEAD=[" W5 "]"
           MOVE GC TO W5
           DISPLAY "POS=[" W5 "]"
           MOVE "123" TO R3
           PERFORM SHOW-T
           MOVE "12t" TO R3
           PERFORM SHOW-T
           MOVE "12p" TO R3
           PERFORM SHOW-T
           MOVE "12y" TO R3
           PERFORM SHOW-T
           MOVE "12C" TO R3
           PERFORM SHOW-T
           MOVE "12{" TO R3
           PERFORM SHOW-T
           STOP RUN.
       SHOW-T.
           IF RT IS NUMERIC
              MOVE RT TO SHOW
              DISPLAY "T[" R3 "]=Y " SHOW
           ELSE
              DISPLAY "T[" R3 "]=N"
           END-IF.
