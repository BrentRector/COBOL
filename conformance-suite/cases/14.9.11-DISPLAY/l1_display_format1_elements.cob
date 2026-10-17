*>--- conformance-case
*> id: l1_display_format1_elements
*> rules: [FMT-14.9.11.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.2 — the DISPLAY statement's GENERAL FORMATS. Format 1 (device) prints:
      *>     DISPLAY { identifier-1 | literal-1 } … [ UPON mnemonic-name-1 ] [ WITH NO ADVANCING ]
      *>              [ END-DISPLAY ]
      *> with DISPLAY, NO, ADVANCING and END-DISPLAY underlined and WITH and UPON's bracket optional.
      *>
      *> DERIVED BEFORE MEASURING, element for element:
      *>   · the brace repeats with "…", so ONE statement takes a LIST of operands mixing identifier-1 and
      *>     literal-1 freely, and §14.9.11.4 GR1 transfers "the content of each operand … in the order
      *>     listed" — one concatenated line, no separators of the compiler's own.
      *>   · an identifier includes a FUNCTION-IDENTIFIER (§8.4.4.1), so FUNCTION UPPER-CASE(x) is a legal
      *>     identifier-1.
      *>   · a figurative constant is a literal-1, and §14.9.11.4 GR3 says "only a single occurrence of
      *>     the figurative constant is displayed" — SPACE contributes exactly one space, not the width of
      *>     any neighbouring item.
      *>   · WITH is NOT underlined, so `NO ADVANCING` without it is the same statement (§5.2.3).
      *>   · END-DISPLAY is optional, so the same list is legal with and without it.
      *> The UPON phrase's own element is pinned by conformance:2023/l1_display_standard_device, which
      *> discriminates SYSERR from the standard display device; it is not repeated here.
      *>
      *> FORMAT 2 (screen) IS NOT EXERCISED, AND ITS ABSENCE IS DOCUMENTED, NOT MISSING. Annex A.4.2
      *> (ACCEPT and DISPLAY screen handling) is recorded "Not claimed" in docs/CONFORMANCE.md, and Annex
      *> A.4.1 admits an optional element's syntax "only when support for that language element is claimed
      *> by the implementor" — so screen-name-1, the AT LINE/COLUMN phrase and the [NOT] ON EXCEPTION pair
      *> are refused BY NAME (COBOLNET1707), witnessed by conformance:negative/a42-display-screen-minimal
      *> and conformance:negative/a42-display-screen-positioned.
      *>
      *> DERIVED OUTPUT:
      *>   A=0042|abc   four operands: literal, PIC 9(4) item (stored digits), literal, PIC X(3) item.
      *>   S=[ ]        the figurative constant contributes ONE space between the two literals.
      *>   F=ABC        the function-identifier operand.
      *>   N1N2!        two NO ADVANCING statements (one with WITH, one without) and a terminating one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPF1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 42.
       01 WS-B PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A=" WS-A "|" WS-B END-DISPLAY
           DISPLAY "S=[" SPACE "]"
           DISPLAY "F=" FUNCTION UPPER-CASE(WS-B)
           DISPLAY "N1" WITH NO ADVANCING
           DISPLAY "N2" NO ADVANCING
           DISPLAY "!" END-DISPLAY
           STOP RUN.
