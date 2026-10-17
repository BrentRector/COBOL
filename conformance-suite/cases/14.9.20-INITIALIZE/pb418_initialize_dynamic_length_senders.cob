*>--- conformance-case
*> id: pb418_initialize_dynamic_length_senders
*> rules: [SR-14.9.20.3-8, GR-14.9.20.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB418 / PB415 — ⚖ DETERMINATION D-DL1 (docs/CONFORMANCE.md §3): ISO 14.9.20.4 GR7, "When a
      *> dynamic-length elementary item is initialized, its length is set to zero", is the GR6c arm's rule, not
      *> a post-pass over all three sending-operand arms. The sibling golden initialize_dynamic_length pins the
      *> GR6c pole (a bare INITIALIZE); this one pins the two poles GR7 must NOT reach, and the ordering between
      *> them. Every expected value below is derived from the rules, not measured:
      *>   L1 GR6a3 — "The actual sending-operand is a literal that, when moved to the receiving-operand with a
      *>      MOVE statement, produces the same result as the initial value of the data item as produced by the
      *>      application of the VALUE clause" — and 8.6.4, "When the data description entry of a dynamic-length
      *>      elementary item contains a VALUE clause, the rules of the VALUE clause define the length of that
      *>      item in its initial state". DV's initial state is 5 characters "hello", so TO VALUE restores 05.
      *>   L2 14.9.20.3 SR8 — "If the REPLACING phrase is specified, literal-1 or the data item referenced by
      *>      identifier-2 is the sending operand." The sender is the 3-character "ABC", so DN takes 03.
      *>   L3 GR6c gives an alphanumeric receiver "Figurative constant alphanumeric SPACES", whose length is one
      *>      character by 8.3.3.6.4 GR3b; GR7 is what makes it 00 instead — 13.18.19.4 GR1's minimum.
      *>   L4 8.3.3.6.4 GR3b again, from the VALUE side: a figurative VALUE other than ALL literal-1 is ONE
      *>      character, so DF's initial state is one space and GR6a3 restores 01 — NOT 00. This is the line
      *>      that separates D-DL1 from the unconditional reading most sharply.
      *>   L5 GR8 - elementary items of a group are initialized in definition order; DG takes its own VALUE
      *>      (GR6a3) and the fixed-length sibling FX takes its VALUE padded by the MOVE rules.
      *>   L6 GR5c is a disjunction tested in order: DV qualifies through GR5c1 (VALUE), so GR6a governs and the
      *>      DEFAULT phrase cannot overwrite it - 05 again.
      *>   L7 DN carries no VALUE clause, so GR5c1b and GR5c1c are both false; it qualifies through GR5c3
      *>      (DEFAULT) and GR6c + GR7 give 00.
      *> DYNAMIC LENGTH is COBOL-2014 (8.5.1.10 / 13.18.19); the negative below it is
      *> tests/conformance/negative/pb418-initialize-dynamic-length-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DV PIC X DYNAMIC LENGTH LIMIT IS 30 VALUE "hello".
       01 DN PIC X DYNAMIC LENGTH LIMIT IS 30.
       01 DF PIC X DYNAMIC LENGTH LIMIT IS 30 VALUE SPACE.
       01 G.
          05 DG PIC X DYNAMIC LENGTH LIMIT IS 30 VALUE "world".
          05 FX PIC X(4) VALUE "QQ".
       01 WN PIC 9(2).
       PROCEDURE DIVISION.
           MOVE "ZZZZZZZ" TO DV
           INITIALIZE DV ALL TO VALUE
           MOVE FUNCTION LENGTH(DV) TO WN
           DISPLAY "L1 VALUE LEN=" WN " [" DV "]"
           MOVE "ZZZZZZZ" TO DN
           INITIALIZE DN REPLACING ALPHANUMERIC DATA BY "ABC"
           MOVE FUNCTION LENGTH(DN) TO WN
           DISPLAY "L2 REPL  LEN=" WN " [" DN "]"
           INITIALIZE DV
           MOVE FUNCTION LENGTH(DV) TO WN
           DISPLAY "L3 DFLT  LEN=" WN " [" DV "]"
           MOVE "ZZZZZZZ" TO DF
           INITIALIZE DF ALL TO VALUE
           MOVE FUNCTION LENGTH(DF) TO WN
           DISPLAY "L4 FIGVL LEN=" WN " [" DF "]"
           MOVE "ZZZZZZZ" TO DG
           MOVE "ZZZZ" TO FX
           INITIALIZE G ALL TO VALUE
           MOVE FUNCTION LENGTH(DG) TO WN
           DISPLAY "L5 GROUP LEN=" WN " [" DG "] FX=[" FX "]"
           MOVE "ZZZZZZZ" TO DV
           INITIALIZE DV ALL TO VALUE THEN TO DEFAULT
           MOVE FUNCTION LENGTH(DV) TO WN
           DISPLAY "L6 VDEF  LEN=" WN " [" DV "]"
           MOVE "ZZZZZZZ" TO DN
           INITIALIZE DN ALL TO VALUE THEN TO DEFAULT
           MOVE FUNCTION LENGTH(DN) TO WN
           DISPLAY "L7 VDEFN LEN=" WN " [" DN "]"
           STOP RUN.
