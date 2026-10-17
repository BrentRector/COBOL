      *>--- conformance-case
      *> id: l1c16_justified_spellings
      *> rules: [SR-13.18.32.3-2, GR-13.18.32.4-3, FMT-13.18.32.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.32.2 / §13.18.32.3 SR2 / §13.18.32.4 GR3 — every
      *>   spelling
      *> of the JUSTIFIED clause, and its omission, on
      *>   alphanumeric/alphabetic
      *> General format: "{ JUSTIFIED | JUST } RIGHT" (RIGHT not
      *>   underlined:
      *>   optional; the braces require one of JUSTIFIED / JUST)
      *>   cite.py: OK  §13.18.32.2   (General format)
      *> SR2 "JUST is an abbreviation for JUSTIFIED."
      *>   cite.py: OK  §13.18.32.3 2)  (Syntax rules)
      *> GR3 "When the JUSTIFIED clause is omitted, the standard rules
      *>   for
      *>   aligning data within an elementary item apply (see 14.6.8,
      *>     ...)"
      *>   cite.py: OK  §13.18.32.4 3)  (General rules)
      *> §14.6.8.5: "aligned at the leftmost character position in the
      *>   data item
      *>   with space fill or truncation to the right"
      *>   cite.py: OK  §14.6.8.5
      *> GR2 (JUSTIFIED, receiver larger): "aligned at the rightmost
      *>   character
      *>   position ... space fill for the leftmost character positions"
      *>   cite.py: OK  §13.18.32.4 2)  (General rules)
      *> GR1 (JUSTIFIED, sender larger): "the leftmost character
      *>   positions ...
      *>   of the sending operand shall be truncated"
      *>   cite.py: OK  §13.18.32.4 1)  (General rules)
      *> DERIVATION. J1 JUST, J2 JUSTIFIED, J3 JUSTIFIED RIGHT, J4 JUST
      *>   RIGHT
      *> are the four arms of the format; by SR2 all four mean the one
      *>   clause,
      *> so each behaves per GR1/GR2. N5, N6 omit the clause, so GR3
      *>   sends them
      *> to §14.6.8.5 (left alignment, right fill/truncation).
      *>   MOVE "AB" (2 chars) to 5-char items:
      *>     J1..J4 -> GR2 right-aligned, 3 leading spaces  -> [   AB]
      *>     N5, N6 -> §14.6.8.5 left-aligned, 3 trailing   -> [AB   ]
      *>   MOVE "ABCDEFG" (7 chars) to 5-char items:
      *>     J1..J3 -> GR1 leftmost 2 truncated             -> [CDEFG]
      *>     N5     -> §14.6.8.5 truncation to the right    -> [ABCDE]
      *>   MOVE "ABCDEFG" to J4/N6 (alphabetic): J4 [CDEFG], N6 [ABCDE].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 J1 PIC X(5) JUST.
       01 J2 PIC X(5) JUSTIFIED.
       01 J3 PIC X(5) JUSTIFIED RIGHT.
       01 J4 PIC A(5) JUST RIGHT.
       01 N5 PIC X(5).
       01 N6 PIC A(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO J1 J2 J3 J4 N5 N6.
           DISPLAY "SHORT J1 [" J1 "] J2 [" J2 "] J3 [" J3 "]".
           DISPLAY "SHORT J4 [" J4 "] N5 [" N5 "] N6 [" N6 "]".
           MOVE "ABCDEFG" TO J1 J2 J3 J4 N5 N6.
           DISPLAY "LONG  J1 [" J1 "] J2 [" J2 "] J3 [" J3 "]".
           DISPLAY "LONG  J4 [" J4 "] N5 [" N5 "] N6 [" N6 "]".
           STOP RUN.
