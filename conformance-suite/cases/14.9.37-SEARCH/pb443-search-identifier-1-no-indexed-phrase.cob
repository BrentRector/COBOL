*>--- conformance-case
*> id: pb443-search-identifier-1-no-indexed-phrase
*> rules: [SR-14.9.37.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 14.9.37.3 SR2, first sentence, the OTHER half: the OCCURS clause shall contain
*> an INDEXED phrase. NE is a table with no index, and 14.9.37.4 GR1 has the statement vary "the first
*> or only index associated with identifier-1", so there is nothing to vary. The old message miscited
*> itself as SR1 - the reference-modification rule - in BOTH binder arms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443SR2C.
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
           SEARCH NE
               AT END CONTINUE
               WHEN NKK (1) = 03 CONTINUE
           END-SEARCH.
           STOP RUN.
