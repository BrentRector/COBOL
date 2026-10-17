*>--- conformance-case
*> id: pb443-search-identifier-1-not-a-table
*> rules: [SR-14.9.37.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 14.9.37.3 SR2, first sentence: "The data description of identifier-1 shall
*> contain an OCCURS clause with an INDEXED phrase". PLAIN has no OCCURS clause. This verdict was
*> DECIDED before PB443 and then delivered as a BoundUnsupported, i.e. compiled into the program as a
*> run-time abort blaming COBOL.NET for an unimplemented feature (4.2.2 paragraph 2 puts it here).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443SR2B.
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
           SEARCH PLAIN
               AT END CONTINUE
               WHEN K (IX) = 03 CONTINUE
           END-SEARCH.
           STOP RUN.
