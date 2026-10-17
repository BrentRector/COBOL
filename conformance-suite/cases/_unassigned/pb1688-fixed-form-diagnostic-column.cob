      *>--- conformance-case
      *> id: pb1688-fixed-form-diagnostic-column
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1688N.
000300* kb/Work PB1688 / PB1801 - a diagnostic names the PHYSICAL column of a
000400* fixed-form line. The logical conversion keeps only the program-text
000500* area (ISO 6.5 5) "the program text area of that line is copied to the
000600* resultant compilation group"), and 6.5's NOTE: "An implementor does not
000700* have to perform an actual conversion as long as the effect is as though
000800* it were performed" - so the conversion must not move a position the
000900* user wrote. TYPEDEF (a COBOL-2002 clause) stands in column 16 of line
001000* 15; the gate at --std 85 named column 9 before the source map carried
001100* columns.
001200 DATA DIVISION.
001300 WORKING-STORAGE SECTION.
001400 01 MY-T TYPEDEF.
001500    05 F PIC X.
001600 PROCEDURE DIVISION.
001700     STOP RUN.
