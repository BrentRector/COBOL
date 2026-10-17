*>--- conformance-case
*> id: pb881-search-varying-constant-record
*> rules: [SR-13.18.15.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB881 — ISO 13.18.15.3 SR2 over SEARCH VARYING: 14.9.37.4 GR1 — "any data item referenced by
*> identifier-2 ... is modified by the execution of the SEARCH statement" — so identifier-2 is a receiving data
*> item and a CONSTANT RECORD shall not be it. The
*> SEARCH binder resolved it with the plain reference resolver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB881NSV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3 INDEXED BY IX.
       01 CK CONSTANT RECORD PIC 9(2) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO 1
           SEARCH E VARYING CK AT END CONTINUE
             WHEN E(IX) = "c" CONTINUE
           END-SEARCH
           STOP RUN.
