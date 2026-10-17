*>--- conformance-case
*> id: pb443-search-identifier-1-subscripted
*> rules: [SR-14.9.37.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 14.9.37.3 SR2, second sentence: identifier-1 "shall not be subscripted at the
*> level for which the SEARCH is applicable". The statement supplies that occurrence itself by varying
*> the table's first index (14.9.37.4 GR1), so a subscript there contradicts the statement. E lies
*> within no enclosing table, so SR3 requires none either.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443SR2A.
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
           SEARCH E(IX)
               AT END CONTINUE
               WHEN K (IX) = 03 CONTINUE
           END-SEARCH.
           STOP RUN.
