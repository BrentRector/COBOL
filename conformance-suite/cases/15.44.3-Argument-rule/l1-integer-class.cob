*>--- conformance-case
*> id: l1-integer-class
*> rules: [AR-15.44.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.44.3 r1 — "Argument-1 shall be of class numeric."
      *>
      *> WS-X is described PIC X(4), which §8.5.2.1 Table 2 (Class and category relationships for elementary
      *> data items) puts in class ALPHANUMERIC; §15.3's argument type 10 admits only "an arithmetic
      *> expression or a numeric data item". ⛔ ITS VALUE IS THE CHARACTERS "12.5" ON PURPOSE — the rule
      *> screens the operand's CLASS, not whether its content happens to look numeric, so a compiler that
      *> reinterpreted the characters would silently answer 12 for source the standard forbids.
      *>
      *> Written at EVERY edition the row spans (85 through 2023): the argument rule's text is unchanged
      *> across them, so an enforcement that reached only the post-85 compilers would be a defect this
      *> fixture catches. The complementary shape — class INDEX, which is also not class numeric — is pinned
      *> by conformance:negative/intrinsic-index-argument, and the ACCEPT side by
      *> conformance:85/l1_integer_floor_85 and conformance:2023/l1_integer_family_notes.
      *> Expected: COBOLNET1627, the intrinsic-argument-class diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INTCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "12.5".
       01 R    PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER(WS-X).
           DISPLAY R.
           STOP RUN.
