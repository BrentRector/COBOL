*>--- conformance-case
*> id: pb530-picture-floating-left-of-point
*> rules: [SR-13.18.40.3-29]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB530 - ISO 1989:2023 13.18.40.3 SR29: "For floating insertion, at least one insertion
      *> symbol shall be specified to the left of the decimal point position." 13.18.40.5 rule 6 is why:
      *> it defines the IMAGE of a floating string only for one anchored left of the point - a) "represent
      *> any or all of the leading numeric character positions to the left of the decimal point position by
      *> the same insertion symbol", b) "represent all of the numeric character positions by the same
      *> insertion symbol", whose result "is the same as if the floating insertion editing were defined only
      *> to the left of the decimal point position". A string lying wholly to the RIGHT matches neither, so
      *> the standard derives no image for it, and every one of these used to bind and render one anyway:
      *> PIC .$$ turned MOVE 0.5 into ".00" and PIC V-- into "  ". Each entry is COBOLNET1934, and SR29 is a
      *> syntax rule of every edition from 1985 on, so all four reject.
      *>
      *> PF01 .$$   - the floating currency string with NOTHING to the left of the point. This is the
      *>     residue Table 10 cannot see: its right-of-point floating rows admit the decimal separator, so
      *>     the matrix says yes and only SR29 says no.
      *> PF02 V$$   - the same shape with the IMPLIED point (13.18.40.3 SR20 makes 'V' and '.' exclusive, so
      *>     the two spellings are one rule, not two).
      *> PF03 .++   - the floating-SIGN leg of the same rule.
      *> PF04 V--   - and its implied-point spelling.
      *> PF05 B.$$  - !! THE READING: "for floating insertion ... at least one insertion symbol" is the
      *>     FLOATING insertion symbol, not any insertion symbol. A simple insertion 'B' left of the point
      *>     leaves the floating string with no digit position of its own to the left of it, which is exactly
      *>     what rule 6 cannot edit.
      *> PF06 9.$$  - a shape that ALSO breaks Table 10 (no '9' may precede a right-of-point floating
      *>     currency symbol). The composition rules run before the precedence walk, so the message names
      *>     the specific rule rather than the table - this entry pins that order.
      *> PF07 999.++ - the same, on the floating-sign row.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530FLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PF01 PIC .$$.
       01 PF02 PIC V$$.
       01 PF03 PIC .++.
       01 PF04 PIC V--.
       01 PF05 PIC B.$$.
       01 PF06 PIC 9.$$.
       01 PF07 PIC 999.++.
       PROCEDURE DIVISION.
           STOP RUN.
