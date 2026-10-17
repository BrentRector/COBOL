      *>--- conformance-case
      *> id: pb1353-source-format-in-copy-pseudo-text
      *> rules: [SR-7.2.3.3-10, SR-7.3.3-8]
      *> files: [PB1384CB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.2.3.3 SR10: 'Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2'. The >>SOURCE FORMAT line below stands inside
      *> pseudo-text-1; logical conversion discards it before COPY reads
      *> its operands, so only the blank it leaves is seen (kb/Work
      *> PB1353). Fixed form.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1353SC.
000300 PROCEDURE DIVISION.
000400     COPY PB1384CB REPLACING ==AA
000500 >>SOURCE FORMAT FIXED
000600         == BY ==BB==.
000700     STOP RUN.
