*>--- conformance-case
*> id: pb443-search-identifier-1-reference-modified
*> rules: [SR-14.9.37.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 14.9.37.3 SR1: "Identifier-1 shall not be reference-modified." identifier-1
*> names a TABLE and a character slice of one is not a table. Before PB443 the binder read only the
*> reference's BASE WORD, so the modifier was discarded unread and this compiled and ran as though
*> `SEARCH E` had been written - byte-identical output for a program the standard does not admit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443SR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E OCCURS 4 TIMES INDEXED BY IX.
               10  K PIC 99.
       01  PLAIN PIC X(4).
       01  NEST.
           05  OUTER OCCURS 2 TIMES INDEXED BY OX.
               10  INNER OCCURS 4 TIMES INDEXED BY IX3.
                   15  NK PIC 99.
       01  NOIX.
           05  NE OCCURS 4 TIMES.
               10  NKK PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1.
           SEARCH E(1:3)
               AT END CONTINUE
               WHEN K (IX) = 03 CONTINUE
           END-SEARCH.
           STOP RUN.
