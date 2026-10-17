*>--- conformance-case
*> id: pb803_sign_default_representation
*> rules: [GR-13.18.52.4-4, DOC-A.1-177]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.52.4 GR4 — the operational sign of a signed numeric item whose PICTURE
      *> contains 'S' and to which NO SIGN clause applies (Annex A.1 item 177, §8.5.1.5;
      *> kb/Work PB803, owner decision 2026-09-09).
      *>
      *> THE RULE. §13.18.52.4 GR4: "A numeric item whose picture character-string contains the
      *> symbol 'S', and to which no SIGN clause applies, has an operational sign. Neither the
      *> representation nor the position of that operational sign is specified by the symbol 'S'.
      *> The implementor shall specify the position and representation of the operational sign.
      *> General rules 5 and 6 do not apply to such signed numeric items."  §8.5.1.5 states the
      *> obligation from the other end: "This clause is optional; if it is not used, operational
      *> signs will be represented as defined by the implementor."
      *>
      *> THE DETERMINATION PINNED HERE (docs/CONFORMANCE.md DOC-A.1-177). The sign is fused onto
      *> the TRAILING digit position, and WHICH character the digit becomes is the compile option
      *> --sign-encoding, whose default `ibm` is in force for this program: positive 0-9 fuse to
      *> "{ABCDEFGHI" and negative 0-9 to "}JKLMNOPQR".  So -123 is `12L` (negative table position
      *> 3) and +123 is `12C` (positive table position 3), and the fused sign occupies an EXISTING
      *> digit position: the item is 3 character positions, never 4.  The ALTERNATIVE convention is
      *> pinned by conformance:2023/pb803_sign_encoding_ascii, which compiles under
      *> `*> options: sign-encoding=ascii`.
      *>
      *> WHY EACH LEG CAN FAIL — expected values derived from the determination above, not observed.
      *> NEG / POS read the two table halves as RAW CHARACTERS through an alphanumeric GROUP move
      *>   (§14.9.25.4 GR4: a group move transfers character positions "without consideration for
      *>   the individual elementary or group items contained within"), so what is displayed is the
      *>   STORAGE and not a re-rendering of the value.
      *> The `|` sentinel immediately after the signed item inside the same group is what makes the
      *>   WIDTH claim falsifiable: were the fused sign to take a character position of its own, the
      *>   sentinel would sit at position 5 and the 4-character window would show the digits shifted
      *>   with no `|` at all.  FUNCTION BYTE-LENGTH states the same two widths independently (3 for
      *>   the item, 4 for the group), so the leg fails if either reader alone drifts.
      *> TRAIL / LEAD are GR4's OTHER half, the POSITION, and they are the reference points that
      *>   make the claim refutable: an item that WRITES `SIGN IS TRAILING` must produce the SAME
      *>   image as the no-clause item (`12L`) and one that writes `SIGN IS LEADING` a DIFFERENT one
      *>   (`J23`).  A determination of LEADING would invert both comparisons.
      *> VNEG / VPOS close the round trip: the character that carried the sign carries it back, so
      *>   the representation and the algebraic value are pinned to agree rather than separately.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB803A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN.
          05 GN-V   PIC S9(3) VALUE -123.
          05 GN-S   PIC X     VALUE "|".
       01 GP.
          05 GP-V   PIC S9(3) VALUE +123.
          05 GP-S   PIC X     VALUE "|".
       01 GT.
          05 GT-V   PIC S9(3) SIGN IS TRAILING VALUE -123.
       01 GL.
          05 GL-V   PIC S9(3) SIGN IS LEADING  VALUE -123.
       01 W4   PIC X(4).
       01 W3   PIC X(3).
       01 SHOW PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE GN TO W4
           DISPLAY "NEG=[" W4 "]"
           MOVE GP TO W4
           DISPLAY "POS=[" W4 "]"
           DISPLAY "LEN=" FUNCTION BYTE-LENGTH(GN-V)
               " " FUNCTION BYTE-LENGTH(GN)
           MOVE GT TO W3
           DISPLAY "TRAIL=[" W3 "]"
           MOVE GL TO W3
           DISPLAY "LEAD=[" W3 "]"
           MOVE GN-V TO SHOW
           DISPLAY "VNEG=[" SHOW "]"
           MOVE GP-V TO SHOW
           DISPLAY "VPOS=[" SHOW "]"
           STOP RUN.
