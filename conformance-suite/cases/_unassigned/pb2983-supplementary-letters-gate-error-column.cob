      *>--- conformance-case
      *> id: pb2983-supplementary-letters-gate-error-column
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2983 - the reference-format gates and the parser name ONE column unit. ISO/IEC 1989:2023
      *> 6.2.3.2 SR2 requires a separator space before the floating comment indicator of an inline comment;
      *> 6.1 1) a) and 6.3.1 count in character positions, so the *> below stands in character position 24
      *> after two supplementary-plane letters (Annex B extended letters, 8.1.3.2 4)), the column the
      *> parser names for the same position on its own diagnostics.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB2983G.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  𐀍𐀎 PIC X(2).*> c
000600 PROCEDURE DIVISION.
000700     STOP RUN.
