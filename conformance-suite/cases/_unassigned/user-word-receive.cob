*>--- conformance-case
*> id: user-word-receive
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> RECEIVE (the STATEMENT is §14.9.31; this fixture is about the WORD) is NON-MONOTONIC across the
*> editions' reserved-word lists: reserved at 85 and 2002 (the communication facility, obsolete at 2002:
*> the GnuCOBOL 2002 list, kb/Work PB656), a USER WORD at 2014, re-reserved at 2023 (Annex E.2 item 25,
*> which compares 2023 with 2014 only). Wave H gave it a hard lexer token, so the fixture pins that the
*> reservation gate answers COBOLNET0901 by name at every reserving edition; the version-matrix row
*> receive-as-user-word pins that the cobolWord nameSlot funnel lets it be a data-name at 2014.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUWRCV.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 RECEIVE PIC X(4) VALUE "ABCD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY RECEIVE.
    STOP RUN.
