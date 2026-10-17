*>--- conformance-case
*> id: pb2727-special-names-reserve-entry
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - RESERVE integer [CHANNELS] is a vendor SPECIAL-NAMES entry that no ISO
      *> format prints: RESERVE is the FILE-CONTROL entry's RESERVE clause (12.4.5.14), and the
      *> 12.3.7.2 general format of the SPECIAL-NAMES paragraph is a closed list that has no RESERVE.
      *> It used to be parsed into an arm nothing bound, so it compiled clean and did nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727RS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           RESERVE 2 CHANNELS.
       PROCEDURE DIVISION.
           STOP RUN.
