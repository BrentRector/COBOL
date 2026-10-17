*>--- conformance-case
*> id: pb846_sort_table_occurs_keys
*> rules: [GR-13.18.38.4-3, SR-14.9.40.3-15, GR-14.9.40.4-18,
*>    GR-14.9.40.4-19, GR-14.9.40.4-21, GR-14.9.40.4-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.40 SORT Format 2 (table) with the KEY phrase OMITTED. kb/Work PB846.
      *>
      *> §14.9.40.2 — the printed Format 2 (folio 746, RENDERED) BRACKETS the KEY phrase, where Format 1 braces it.
      *> §14.9.40.3 SR15 — "The KEY phrase may be omitted only if the description of the table referenced by
      *>   data-name-2 contains a KEY phrase."
      *> §14.9.40.4 GR21 — "If the KEY phrase is not specified, the sequence is determined by the KEY phrase in the
      *>   data description entry of the table referenced by data-name-2."
      *> §13.18.38.4 GR3 — the OCCURS KEY data-names are "specified in descending order of significance", and each
      *>   carries its own ASCENDING/DESCENDING (§14.9.40.4 GR1's transitivity is the same shape).
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   S=12345         TA (one ASCENDING key TK) loaded 3 1 5 2 4; GR21 sorts on TK ascending.
      *>   M=B1B2B3A1A2A3  TB's OCCURS KEY is DESCENDING KA then ASCENDING KB: KA is the MAJOR key (listed first,
      *>                   GR3), so every B precedes every A; within equal KA, KB ascends. Elements are loaded
      *>                   (KB,KA) = 1A 2B 3A 1B 2A 3B and displayed KA then KB.
      *>   X=A3A2A1B3B2B1  The same table sorted with the statement's OWN key phrase (GR2 — the arm GR21 does not
      *>                   replace): ASCENDING KA, DESCENDING KB. The table's KEY phrase plays no part.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB846ST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA.
          05 TAE OCCURS 5 ASCENDING KEY IS TK INDEXED BY IX.
             10 TK PIC 9.
       01 TB.
          05 TBE OCCURS 6 DESCENDING KEY IS KA ASCENDING KEY IS KB.
             10 KB PIC 9.
             10 KA PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "31524" TO TA
           SORT TAE
           DISPLAY "S=" TK(1) TK(2) TK(3) TK(4) TK(5)
           MOVE "1A2B3A1B2A3B" TO TB
           SORT TBE
           DISPLAY "M=" KA(1) KB(1) KA(2) KB(2) KA(3) KB(3)
                   KA(4) KB(4) KA(5) KB(5) KA(6) KB(6)
           SORT TBE ON ASCENDING KEY KA DESCENDING KEY KB
           DISPLAY "X=" KA(1) KB(1) KA(2) KB(2) KA(3) KB(3)
                   KA(4) KB(4) KA(5) KB(5) KA(6) KB(6)
           STOP RUN.
