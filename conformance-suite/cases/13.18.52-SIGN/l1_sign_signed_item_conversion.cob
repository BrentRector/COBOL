*>--- conformance-case
*> id: l1_sign_signed_item_conversion
*> rules: [GR-13.18.52.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.52.4 GR7 — a picture containing 'S' makes the item a
      *> SIGNED item, and where a SIGN clause applies, conversion needed for
      *> computation or comparison happens AUTOMATICALLY.
      *>
      *> THE RULE. §13.18.52.4 GR7: "Each numeric item whose picture
      *> character-string contains the symbol 'S' is a signed item. If a SIGN
      *> clause applies to such an item and conversion is necessary for
      *> purposes of computation or comparisons, conversion takes place
      *> automatically."
      *> Two obligations: SIGNEDNESS (the item can carry and be compared as a
      *> negative value), and AUTOMATIC CONVERSION (no source-level step is
      *> required to make two DIFFERENT sign layouts compare or combine).
      *>
      *> HOW THE SECOND OBLIGATION IS MADE OBSERVABLE. A conversion claim is
      *> vacuous unless the two operands really are stored differently, so A1
      *> and B1 carry the SAME value in two layouts that §13.18.52.4 GR6
      *> defines completely and differently:
      *>   GR6 a) "The operational sign is presumed to be the leading (or,
      *>          respectively, trailing) character position of the data item
      *>          to which it applies; this character position is not a digit
      *>          position."
      *>   GR6 b) "The operational signs for positive and negative are the
      *>          basic special characters '+' and '-', respectively." (The
      *>          printed standard sets that second character as an en dash;
      *>          the basic special character it names is Table 1's "minus
      *>          sign (hyphen)", §8.1.3.1 — U+002D HYPHEN-MINUS.)
      *> so A1 (LEADING SEPARATE) is [-012] and B1 (TRAILING SEPARATE) is
      *> [012-] — the sign character sits at opposite ends. Both are 4 bytes:
      *> 3 digit positions plus the separate sign character position, at one
      *> byte per character position (docs/CONFORMANCE.md DOC-A.1-209). The
      *> IMG leg prints both images, so the later legs cannot be satisfied by
      *> two items that happened to be stored identically.
      *> The unseparate (over-punch) form is deliberately avoided: its
      *> representation is the §13.18.52.4 GR4 implementor determination and
      *> has no docs/CONFORMANCE.md A.1 row yet.
      *>
      *> THE LEGS.
      *> IMG — the two layouts, derived above. This leg alone would pass with
      *>       no conversion machinery at all; it exists to make EQ/NEG/SUM
      *>       non-vacuous.
      *> EQ  — GR7's COMPARISON half, and §8.8.4.2.4 is the clause that makes
      *>       the conversion NECESSARY: "For operands whose class is numeric,
      *>       a comparison is made with respect to the algebraic value of the
      *>       operands regardless of the manner in which their usage is
      *>       described." Both hold -12, so YES. An implementation comparing
      *>       the stored characters answers NO — [-012] and [012-] agree in
      *>       no character position.
      *> NEG — GR7's first sentence. 'S' makes each item signed, so each holds
      *>       a genuinely negative value and A1 < 0 AND B1 < 0 is true. An
      *>       implementation storing the magnitude only answers NO.
      *> SUM — GR7's COMPUTATION half, into a THIRD layout. -12 + -12 = -24;
      *>       C1 is PIC S9(4) SIGN IS TRAILING SEPARATE, so by GR6 a) it is
      *>       4 digit positions plus a trailing sign character position and
      *>       by GR6 b) the negative sign is '-': [0024-]. Three different
      *>       sign layouts take part in one statement with no source-level
      *>       conversion written anywhere.
      *> Every image is read through a GROUP move to an alphanumeric item,
      *> which §14.9.25.4 GR4 makes a byte transfer rather than a numeric
      *> conversion, so the printed characters are the stored ones.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SGN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA.
          05 A1 PIC S9(3) SIGN IS LEADING SEPARATE VALUE -12.
       01 GB.
          05 B1 PIC S9(3) SIGN IS TRAILING SEPARATE VALUE -12.
       01 GC.
          05 C1 PIC S9(4) SIGN IS TRAILING SEPARATE.
       01 XA PIC X(4).
       01 XB PIC X(4).
       01 XC PIC X(5).
       01 EQF PIC X(3).
       01 NGF PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE GA TO XA
           MOVE GB TO XB
           DISPLAY "IMG=[" XA "][" XB "]"
           MOVE "NO" TO EQF
           IF A1 = B1
               MOVE "YES" TO EQF
           END-IF
           DISPLAY "EQ=" EQF
           MOVE "NO" TO NGF
           IF A1 < 0 AND B1 < 0
               MOVE "YES" TO NGF
           END-IF
           DISPLAY "NEG=" NGF
           COMPUTE C1 = A1 + B1
           MOVE GC TO XC
           DISPLAY "SUM=[" XC "]"
           STOP RUN.
