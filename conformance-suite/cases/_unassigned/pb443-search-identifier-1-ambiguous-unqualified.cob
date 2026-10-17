*>--- conformance-case
*> id: pb443-search-identifier-1-ambiguous-unqualified
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 8.4.2.2.1: "Qualification of a user-defined name is required unless ... no other
*> name has the identical spelling." Two groups each declare a table E, and identifier-1 is written
*> unqualified, so the reference identifies no single resource (8.4.2.1). Before PB443 the binder read
*> only the base word and took candidates.FirstOrDefault(IsTable), so this compiled clean and searched
*> whichever E was declared FIRST - a silent wrong answer decided by declaration order. The qualified
*> spellings, which are the ones the standard admits, are in conformance-suite/cases/14.9.37-SEARCH/pb443_search_identifier_1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443AMB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  E OCCURS 4 TIMES INDEXED BY IX1.
               10  K PIC 99.
       01  G2.
           05  E OCCURS 4 TIMES INDEXED BY IX2.
               10  K PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX1 TO 1.
           SET IX2 TO 1.
           SEARCH E
               AT END CONTINUE
               WHEN K IN G2 (IX2) = 03 CONTINUE
           END-SEARCH.
           STOP RUN.
