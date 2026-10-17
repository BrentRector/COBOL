*>--- conformance-case
*> id: pb1353-copy-operand-content
*> rules: [SR-7.2.3.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO §7.2.3.3 SR6 "Pseudo-text-1 shall contain one or more text-words,
*> at least one of which shall be neither a separator comma nor a
*> separator semicolon" (cite.py --check: OK §7.2.3.3 6)). ==;== names
*> no text-word: COBOLNET2572 (kb/Work PB1353), checked on the statement
*> itself, before the library text is looked for. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1353CC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY PB1353BK REPLACING ==;== BY ==AAA==.
       PROCEDURE DIVISION.
           STOP RUN.
