*>--- conformance-case
*> id: w59_numeric_category_usages
*> rules: [GR-8.5.2.12-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.5.2.12 item 2 (kb/Work R43 / PB579, the LIVE part of GR-8.5.2.12-2): an elementary data item
      *> described with one of the usages "binary-char, binary-short, binary-long, binary-double, float-short,
      *> float-long, float-extended, float-binary-32, float-binary-64, float-binary-128, float-decimal-16, or
      *> float-decimal-34" is of CATEGORY NUMERIC. FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 are not provided
      *> (Annex A.3 items 17 and 19, refused COBOLNET1564 — R43 item 2 records them as the declined part), so
      *> this program holds the nine provided usages to the category, through three consumers that admit ONLY
      *> numeric operands:
      *>  - an ADD with every item as an addend (§14.9.2.3 SR2: "Identifier-1 and identifier-2 shall reference
      *>    numeric data items") — the sum of 1 .. 9 is 45;
      *>  - the NUMERIC class condition, which for a numeric-category item tests the content: each item holds a
      *>    valid value, so each answers true (nine Ys);
      *>  - FUNCTION MAX over all nine, whose type table admits integer and numeric arguments: 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59NUMCAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BC  USAGE BINARY-CHAR   VALUE 1.
       01 BS  USAGE BINARY-SHORT  VALUE 2.
       01 BL  USAGE BINARY-LONG   VALUE 3.
       01 BD  USAGE BINARY-DOUBLE VALUE 4.
       01 FS  USAGE FLOAT-SHORT   VALUE 5.
       01 FL  USAGE FLOAT-LONG    VALUE 6.
       01 FE  USAGE FLOAT-EXTENDED VALUE 7.
       01 F32 USAGE FLOAT-BINARY-32 VALUE 8.
       01 F64 USAGE FLOAT-BINARY-64 VALUE 9.
       01 WS-SUM PIC 9(3) VALUE 0.
       01 WS-MAX PIC 9(3).
       01 WS-FLAGS PIC X(9) VALUE SPACES.
       PROCEDURE DIVISION.
           ADD BC BS BL BD FS FL FE F32 F64 TO WS-SUM.
           IF BC  IS NUMERIC MOVE "Y" TO WS-FLAGS(1:1) END-IF.
           IF BS  IS NUMERIC MOVE "Y" TO WS-FLAGS(2:1) END-IF.
           IF BL  IS NUMERIC MOVE "Y" TO WS-FLAGS(3:1) END-IF.
           IF BD  IS NUMERIC MOVE "Y" TO WS-FLAGS(4:1) END-IF.
           IF FS  IS NUMERIC MOVE "Y" TO WS-FLAGS(5:1) END-IF.
           IF FL  IS NUMERIC MOVE "Y" TO WS-FLAGS(6:1) END-IF.
           IF FE  IS NUMERIC MOVE "Y" TO WS-FLAGS(7:1) END-IF.
           IF F32 IS NUMERIC MOVE "Y" TO WS-FLAGS(8:1) END-IF.
           IF F64 IS NUMERIC MOVE "Y" TO WS-FLAGS(9:1) END-IF.
           COMPUTE WS-MAX = FUNCTION MAX(BC BS BL BD FS FL FE F32 F64).
           DISPLAY "SUM=" WS-SUM " NUMERIC=" WS-FLAGS " MAX=" WS-MAX.
           STOP RUN.
