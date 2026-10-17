      *>--- conformance-case
      *> id: pb515_index_item_without_value
      *> rules: [SR-13.16.3-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 13.16.3 SR10 (kb/Work PB515) forbids a VALUE clause on a data item of class index;
      *> an index data item takes its value from SET (14.9.39.4 GR1). This is the legal spelling beside the
      *> negative pb515-value-on-index-item, written (77 I) and inherited (13.18.60.4 GR1: 01 G USAGE INDEX makes
      *> A and B two index data items).
      *> WHY EACH LINE CAN FAIL:
      *>   I=       I saved X at 3, so T(X) after SET X TO I is C.
      *>   AB=      A saved X at 1 and B at 5 - two distinct items: A E. One shared cell would print E E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB515IXPOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT VALUE "ABCDE".
           05 T PIC X OCCURS 5 INDEXED BY X.
       77 I USAGE INDEX.
       01 G USAGE INDEX.
           05 A.
           05 B.
       PROCEDURE DIVISION.
       MAIN.
           SET X TO 3
           SET I TO X
           SET X TO 1
           SET A TO X
           SET X TO 5
           SET B TO X
           SET X TO I
           DISPLAY "I=" T (X)
           SET X TO A
           DISPLAY "AB=" T (X) WITH NO ADVANCING
           SET X TO B
           DISPLAY " " T (X)
           STOP RUN.
