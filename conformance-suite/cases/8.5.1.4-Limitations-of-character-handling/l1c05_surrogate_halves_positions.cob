      *>--- conformance-case
      *> id: l1c05_surrogate_halves_positions
      *> rules: [GR-8.5.1.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.4 rule 2 — each UTF-16 surrogate half is one
      *>   character position
      *> Rule: "The high-half 2 octets and the low-half 2 octets of a
      *>   four-octet sequence defined in the UTF-16 format of ISO/IEC
      *>   10646 are each treated as a single character
      *>   position." NOTE: "Each two-octet code element of UTF-16 is
      *>   treated in COBOL as though it were itself a character."
      *> cite.py --check 8.5.1.4 "The high-half 2 octets and the
      *>   low-half 2 octets of a four-octet sequence defined in the
      *>   UTF-16 format of ISO/IEC 10646 are each treated as a single
      *>   character position." -> OK  §8.5.1.4 2)  (Limitations of
      *>   character handling)
      *> cite.py --check 8.5.1.4 "Each two-octet code element of UTF-16
      *>   is treated in COBOL as though it were itself a character" ->
      *>   OK  §8.5.1.4 2)  (Limitations of character handling)
      *> cite.py --check 8.5.1.4 "The following processing limitations
      *>   shall apply when ISO/IEC 10646 is chosen as a computer's
      *>   coded character set" -> OK  §8.5.1.4   (Limitations of
      *>   character handling)
      *> The national set is UTF-16 (docs/CONFORMANCE.md DOC-A.1-188).
      *>   U+1F600 is the four-octet sequence D83D DE00: a high half and
      *>   a low half, TWO positions.
      *> Derivation:
      *>   FUNCTION LENGTH of NX"D83DDE00" = 2 (not 1)    -> "LEN=2"
      *>   PIC N(4) VALUE NX"0061D83DDE000062" (a, pair, b):
      *>     position 2 is the high half alone            -> "P2=HIGH"
      *>     position 3 is the low half alone             -> "P3=LOW"
      *>     position 4 is b (the pair is not 1 position) -> "P4=B"
      *>   INSPECT TALLYING FOR CHARACTERS counts 4       -> "CNT=4"
      *>   MOVE the pair to PIC N(1): right truncation keeps the high
      *>     half only, no special handling -> "TRUNC=HIGH"
      *>   FUNCTION REVERSE of the pair = DE00 then D83D  -> "REV=SPLIT"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N4  PIC N(4) VALUE NX"0061D83DDE000062".
       01 W-N1  PIC N(1).
       01 W-N2  PIC N(2).
       01 W-CNT PIC 9 VALUE 0.
       01 W-LEN PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(NX"D83DDE00") TO W-LEN.
           DISPLAY "LEN=" W-LEN.
           IF W-N4(2:1) = NX"D83D"
               DISPLAY "P2=HIGH"
           ELSE
               DISPLAY "P2=OTHER"
           END-IF.
           IF W-N4(3:1) = NX"DE00"
               DISPLAY "P3=LOW"
           ELSE
               DISPLAY "P3=OTHER"
           END-IF.
           IF W-N4(4:1) = NX"0062"
               DISPLAY "P4=B"
           ELSE
               DISPLAY "P4=OTHER"
           END-IF.
           INSPECT W-N4 TALLYING W-CNT FOR CHARACTERS.
           DISPLAY "CNT=" W-CNT.
           MOVE NX"D83DDE00" TO W-N1.
           IF W-N1 = NX"D83D"
               DISPLAY "TRUNC=HIGH"
           ELSE
               DISPLAY "TRUNC=OTHER"
           END-IF.
           MOVE FUNCTION REVERSE(NX"D83DDE00") TO W-N2.
           IF W-N2 = NX"DE00D83D"
               DISPLAY "REV=SPLIT"
           ELSE
               DISPLAY "REV=OTHER"
           END-IF.
           STOP RUN.
