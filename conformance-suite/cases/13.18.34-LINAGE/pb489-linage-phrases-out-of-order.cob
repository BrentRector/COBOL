*>--- conformance-case
*> id: pb489-linage-phrases-out-of-order
*> rules: [FMT-13.18.34.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO §13.18.34.2 prints the LINAGE clause's three optional phrases in ONE order — WITH FOOTING AT, then
*> LINES AT TOP, then LINES AT BOTTOM — as three separate brackets on the page, and the order of a general
*> format's elements is what the format states. Writing `LINES AT TOP 2 WITH FOOTING AT 4` is therefore not a
*> spelling of this clause in any edition. The figure was rendered from the canonical PDF (printed folio 417)
*> when this was adjudicated, so the printed order is the standard's own and not a transcription artifact.
*> ⛔ THE POSITIVE TWIN IS tests/conformance/85/pb489_linage_qualified_operands.cob, which writes all three
*> phrases in the printed order with every optional word OMITTED from one file and every optional word WRITTEN
*> on the other. Together they pin the format from both sides: the words are optional (§8.3.2.4.3) and the
*> PHRASES are not reorderable — which is the distinction a grammar can silently lose in either direction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n6.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 5 LINES LINES AT TOP 2 WITH FOOTING AT 4.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
