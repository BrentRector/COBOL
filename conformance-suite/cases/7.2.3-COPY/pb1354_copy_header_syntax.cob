      *>--- conformance-case
      *> id: pb1354_copy_header_syntax
      *> rules: [FMT-7.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1354 - the COPY statement is parsed in its 7.2.3.2
      *> general-format order: after text-name-1 only the OF/IN phrase,
      *> SUPPRESS [PRINTING], the REPLACING phrase and the separator
      *> period may follow. GARBAGE is none of them, so the statement is
      *> COBOLNET2449 at every edition. Before the fix a hand scan
      *> skipped every character after the name to the next '.', and
      *> the program compiled silently.
      *> cite.py --check 7.2.3.4 "logically replacing the entire COPY
      *>   statement beginning with the reserved word COPY and ending
      *>   with the separator period, inclusive"            -> OK 6)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1354NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY pb1354nb GARBAGE MORE.
       01 W PIC X VALUE "W".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY W.
           STOP RUN.
