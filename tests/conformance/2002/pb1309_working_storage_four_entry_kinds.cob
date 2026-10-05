      *> kb/Work PB1309 - ISO 13.5.2 general format: WORKING-STORAGE SECTION. [ { 77-level-description-entry |
      *> constant-entry | record-description-entry | type-declaration-entry } ... ] - any number, in any order.
      *> cite.py: OK 13.5.2 (General format) "type-declaration-entry". The constant and type-declaration entries
      *> are COBOL-2002 (13.10, 13.18.58), so 2002 is the introducing edition of the four-kind format. The
      *> leading-entry half (a section whose first entry is not level 1 or 77 is none of the four kinds, 13.11.1)
      *> is pinned by negative/pb1246-section-first-entry-not-level-1 (COBOLNET2771).
      *> Expected: A=007 (PIC 9(3) VALUE 7), K=42, the record typed by the TYPEDEF carries the type's VALUE "AB",
      *> B=WXY, and the alphanumeric constant KS=HI; the BYTE-LENGTH OF constant form is accepted here too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1309WS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 A PIC 9(3) VALUE 7.
       01 K CONSTANT AS 42.
       01 T-REC TYPEDEF.
          05 T-X PIC X(2) VALUE "AB".
       01 R TYPE T-REC.
       77 B PIC X(3) VALUE "WXY".
       01 KB CONSTANT AS BYTE-LENGTH OF B.
       01 KS CONSTANT AS "HI".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY A " " K " " T-X OF R " " B " " KS.
           STOP RUN.
