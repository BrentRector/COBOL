*>--- conformance-case
*> id: pb1263-occurs-key-under-nested-table
*> rules: [SR-13.18.38.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1263 - ISO 13.18.38.3 SR4: "If data-name-2 is subordinate to an alphanumeric group
      *> item ... that is subordinate to the entry containing the OCCURS clause, that group item shall
      *> not contain an OCCURS clause." K sits under G, a group of T that OCCURS 2, so K has two
      *> values per element of T and no single order. Contrast pb1263_occurs_key_items_admitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T OCCURS 3 ASCENDING KEY K INDEXED BY IX.
               10 G OCCURS 2.
                   15 K PIC 9.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
