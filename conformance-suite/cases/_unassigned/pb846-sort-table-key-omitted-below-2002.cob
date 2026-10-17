*>--- conformance-case
*> id: pb846-sort-table-key-omitted-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.40 Format 2 - the table SORT is a COBOL-2002 introduction (ANSI X3.23-1985's
      *> SORT operates on sort-merge files only). kb/Work PB846 made the KEY-phrase-omitted table form legal
      *> at 2002+ (§14.9.40.3 SR15 / §14.9.40.4 GR21); this pins that the relaxation does not leak below the
      *> edition that introduced the format - the introduction gate still names the construct.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB846N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE OCCURS 5 ASCENDING KEY IS TK INDEXED BY IX.
             10 TK PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE
           STOP RUN.
