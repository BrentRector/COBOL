*>--- conformance-case
*> id: pb2502-record-key-alias-of-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2502 - the negative twin of positive/85/pb2502_renames_record_key: making a RENAMES alias a
      *> legal RECORD KEY (12.4.5.12.3 SR2, checked: "category alphanumeric or category national within a record
      *> description entry") must not make EVERY alias one. The alias without THROUGH takes ALL the attributes of
      *> data-name-2 (13.18.45.4 GR1), so KN, which renames the numeric item N1, is a category-numeric item and
      *> is refused as a record key exactly as N1 itself would be. The key screen reads the alias's category
      *> from the renamed item; the registration fix places the alias in the record, it does not admit it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2502NEGNUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2502-neg-num.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS KN.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 N1 PIC 9(3).
          05 D PIC X(7).
       66 KN RENAMES N1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           CLOSE F.
           STOP RUN.
