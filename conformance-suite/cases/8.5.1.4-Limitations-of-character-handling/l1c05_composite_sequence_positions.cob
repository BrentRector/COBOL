      *>--- conformance-case
      *> id: l1c05_composite_sequence_positions
      *> rules: [GR-8.5.1.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.4 rule 1 — a base character and its combining
      *>   character are each one character position
      *> Rule: "The non-combining character and the following combining
      *>   characters of a composite sequence defined in ISO/IEC 10646
      *>   are each treated as a single character position."
      *> cite.py --check 8.5.1.4 "The non-combining character and the
      *>   following combining characters of a composite sequence
      *>   defined in ISO/IEC 10646 are each treated as a single
      *>   character position." -> OK  §8.5.1.4 1)  (Limitations of
      *>   character handling)
      *> cite.py --check 8.5.1.4 "The following processing limitations
      *>   shall apply when ISO/IEC 10646 is chosen as a computer's
      *>   coded character set" -> OK  §8.5.1.4   (Limitations of
      *>   character handling)
      *> The national set is UTF-16 of ISO/IEC 10646
      *>   (docs/CONFORMANCE.md DOC-A.1-188), so the rule applies.
      *>   NX"00650301" is U+0065 e followed by U+0301 COMBINING ACUTE
      *>   ACCENT: one composite sequence, TWO character positions.
      *> Derivation:
      *>   FUNCTION LENGTH of the literal = 2 (not 1)      -> "LEN=2"
      *>   PIC N(3) VALUE it: position 1 is U+0065        -> "P1=BASE"
      *>     position 2 is U+0301 alone                   -> "P2=MARK"
      *>     position 3 is the space fill                 -> "P3=SPACE"
      *>   INSPECT FOR CHARACTERS BEFORE INITIAL space = 2 -> "CNT=2"
      *>   MOVE it to PIC N(1): right truncation keeps the base only ->
      *>     "TRUNC=BASE"
      *>   FUNCTION REVERSE of it = U+0301 then U+0065    -> "REV=SPLIT"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N3  PIC N(3) VALUE NX"00650301".
       01 W-N1  PIC N(1).
       01 W-N2  PIC N(2).
       01 W-CNT PIC 9 VALUE 0.
       01 W-LEN PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(NX"00650301") TO W-LEN.
           DISPLAY "LEN=" W-LEN.
           IF W-N3(1:1) = NX"0065"
               DISPLAY "P1=BASE"
           ELSE
               DISPLAY "P1=OTHER"
           END-IF.
           IF W-N3(2:1) = NX"0301"
               DISPLAY "P2=MARK"
           ELSE
               DISPLAY "P2=OTHER"
           END-IF.
           IF W-N3(3:1) = NX"0020"
               DISPLAY "P3=SPACE"
           ELSE
               DISPLAY "P3=OTHER"
           END-IF.
           INSPECT W-N3 TALLYING W-CNT
               FOR CHARACTERS BEFORE INITIAL NX"0020".
           DISPLAY "CNT=" W-CNT.
           MOVE NX"00650301" TO W-N1.
           IF W-N1 = NX"0065"
               DISPLAY "TRUNC=BASE"
           ELSE
               DISPLAY "TRUNC=OTHER"
           END-IF.
           MOVE FUNCTION REVERSE(NX"00650301") TO W-N2.
           IF W-N2 = NX"03010065"
               DISPLAY "REV=SPLIT"
           ELSE
               DISPLAY "REV=OTHER"
           END-IF.
           STOP RUN.
