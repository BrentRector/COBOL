      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1455 (+PB1426). ISO 8.4.2.3.2 Format 1 is `qualified-data-name-1 [ ( subscript ... ) ]`: every
      *> qualifier precedes the subscripts. 8.4.3.1.2 Format 3 is `identifier-1 reference-modifier-1`: the reference
      *> modifier follows the whole identifier. No edition prints a subscript or reference modifier BETWEEN
      *> qualifiers, and no dialect this compiler declares owns it, so each line below is refused COBOLNET2776 at
      *> every edition and strictness: a data reference (E (2) OF T, S (2:2) OF G) and a subscript's own identifier
      *> inside a SUBSCRIPT-mode capture (X (E (1) OF T)). The conforming spellings are E OF T (2), S OF G (2:2)
      *> and X (E OF T (1)) (positive 85/pb1455_qualified_subscripted_subscript).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1455N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T.
             10 E PIC 9 OCCURS 3.
          05 S PIC X(4) VALUE "ABCD".
       01 XS.
          05 X PIC X(4) OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO E (2) OF T
           DISPLAY S (2:2) OF G
           DISPLAY X (E (1) OF T)
           STOP RUN.
