      *>--- conformance-case
      *> id: l1c16-io-control-before-file-control
      *> rules: [FMT-12.4.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.2 General format — the i-o-control-paragraph follows
      *>   the
      *> file-control-paragraph: "INPUT-OUTPUT SECTION. [
      *>   file-control-paragraph ]
      *> [ i-o-control-paragraph ]"
      *>   cite.py: OK  §12.4.2   (General format)
      *> Here I-O-CONTROL precedes FILE-CONTROL; the source is otherwise
      *>   valid
      *> (the same paragraphs in the format's order compile, see the
      *>   positive
      *> l1c16_io_section_paragraphs, program L1C16D), so the only
      *>   reason to
      *> reject is the general format's order. A pure general-format
      *>   rule: the
      *> diagnostic is the parser's syntax error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           SAME RECORD AREA FOR F2 F3.
       FILE-CONTROL.
           SELECT F2 ASSIGN TO "L1C16N2.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "L1C16N3.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       PROCEDURE DIVISION.
       N-MAIN.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
