*>--- conformance-case
*> id: user-word-end-receive
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> END-RECEIVE is reserved at 85 and 2002 (the communication facility, obsolete at 2002: the
*> GnuCOBOL 2002 list), a user word at 2014 only, and re-reserved at 2023 (Annex E.2 item 25,
*> which compares 2023 with 2014 only - E.1). kb/Work PB656 added the 85/2002 editions.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUW04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 END-RECEIVE PIC 9 VALUE 4.
PROCEDURE DIVISION.
MAIN.
    DISPLAY END-RECEIVE.
    STOP RUN.
