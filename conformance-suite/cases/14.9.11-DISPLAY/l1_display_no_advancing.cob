      *>--- conformance-case
      *> id: l1_display_no_advancing
      *> rules: [GR-14.9.11.4-9]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.11.4 GR9 - "If the WITH NO ADVANCING phrase is
      *> specified, then the positioning of the device shall not be
      *> reset to the next line or changed in any other way following
      *> the display of the last operand. If the device is capable of
      *> positioning to a specific character position, it will remain
      *> positioned at the character position immediately following the
      *> last character of the last operand displayed."
      *> The standard display device here (GR8) is a character stream,
      *> so "the character position immediately following the last
      *> character" is observable as WHERE THE NEXT DISPLAY LANDS.
      *> Line 1 chains three displays, the first two WITH NO ADVANCING:
      *> no reset happens between them, so ABCDEF is one line; the
      *> third carries no phrase and therefore resets to the leftmost
      *> position of the next line (GR10), which is what makes GH a
      *> separate line and the ABSENCE of a reset in GR9 visible.
      *> Line 3 pins "the LAST character of the LAST operand": with two
      *> operands (GR6 - transferred in sequence "without modifying the
      *> positioning of the device between the successive operands")
      *> the resting position is after the last character of the
      *> four-character field WS-X, pad spaces included, so ">" lands
      *> in column 6, not column 4.
      *> Line 4 writes the phrase without the optional word WITH (the
      *> Format 1 diagram underlines NO and ADVANCING but not WITH), so
      *> it is the same phrase and the same GR9 behaviour.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "PQ".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "AB" WITH NO ADVANCING
           DISPLAY "CD" WITH NO ADVANCING
           DISPLAY "EF"
           DISPLAY "GH"
           DISPLAY "<" WS-X WITH NO ADVANCING
           DISPLAY ">"
           DISPLAY "IJ" NO ADVANCING
           DISPLAY "KL"
           STOP RUN.
