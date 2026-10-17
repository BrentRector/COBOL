*>--- conformance-case
*> id: pb129_remainder_no_phrase
*> rules: [GR-8.8.1.2-1, GR-14.9.12.4-6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB129 — the NO-PHRASE dispositions. 14.9.12.4 GR6c + 14.7.5 no-phrase rule 4 (CONFORMANCE.md
      *> item 70): the over-wide quotient stores its LOW-ORDER digit (Q=3) and the remainder is formed from
      *> the quotient AS STORED — the subsidiary quotient capped at identifier-3's digit count, so
      *> R2 = 1000 - 3*3 = 991 (the uncapped back-multiply gave 1000 - 333*3 = 1, stored 0001). And the CA5
      *> sibling: (1/3) ** 0 under ROUNDED MODE IS PROHIBITED is exactly 1.00 — the nested division computes
      *> at the D2 guard scale with truncation, never at the receiver's PROHIBITED mode (the leak raised a
      *> spurious EC-SIZE-TRUNCATION here before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB129NP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9(4) VALUE 1000.
       01 V PIC 9 VALUE 3.
       01 Q PIC 9.
       01 R2 PIC 9(4).
       01 X PIC 99V99.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE V INTO D GIVING Q REMAINDER R2
           DISPLAY "Q=" Q " R2=" R2
           COMPUTE X ROUNDED MODE IS PROHIBITED = (1 / 3) ** 0
           DISPLAY "X=" X
           STOP RUN.
