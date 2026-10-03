      *> kb/Work PB1399 — SET Format 1's sending operand identifier-2 is "a data item of class index"
      *> (ISO/IEC 1989:2023 §14.9.39.3 SR2), and ISO §8.4.3.1.3 SR1 lets an identifier be ANY of the identifier
      *> formats, so a function-identifier of class index is identifier-2 exactly as an index data item is.
      *>   §15.2 item 6      "Index functions. These are of the class and category index." MAX and MIN over index
      *>                     arguments are index functions (§15.59.1 / §15.63.1: the Index result row).
      *>   §8.4.3.2.1        "A function-identifier references the unique data item that results from the
      *>                     evaluation of a function" — that item is a data item of class index (§8.5.2.8 item 2).
      *>   §14.9.39.3 SR3    "If identifier-1 references a data item of class index, arithmetic-expression-1 shall
      *>                     not be specified" — so an index data item RECEIVES an index function only as identifier-2.
      *>   §14.9.39.4 GR2    the receiver takes the occurrence number the sender's value corresponds to: an index-name
      *>                     (TX) is positioned at it (a), an index data item (IC, IE) takes the value unchanged (b).
      *> The classifier used to take only a BARE data reference as identifier-2 and send every function down the
      *> arithmetic-expression-1 lane, where the §8.8.1.1 numeric screen refused it (COBOLNET0844) and §14.9.39.3
      *> SR3 refused an index data item receiver (COBOLNET2326).
      *> EXPECTED OUTPUT, derived line by line (T holds "ABCDEFGHI", so occurrence n is the n-th letter):
      *>   A  IA = 3, IB = 7 (set through TX); MAX(IA IB) = 7 positions TX at occurrence 7  -> G
      *>   B  MIN(IA IB) = 3 positions TX at occurrence 3                                    -> C
      *>   C  IC receives MAX(IA IB) unchanged (an index data item receiver); TX is then set from IC -> 7 -> G
      *>   D  the SUBSCRIPTED index items IXE(1) = 3, IXE(2) = 5; MIN over them = 3, held in IE; TX from IE -> C
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1399SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IA USAGE INDEX.
       01 IB USAGE INDEX.
       01 IC USAGE INDEX.
       01 IE USAGE INDEX.
       01 TIX.
          05 IXE USAGE INDEX OCCURS 2 TIMES.
       01 T.
          05 TE PIC X OCCURS 9 TIMES INDEXED BY TX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDEFGHI" TO T
           SET TX TO 3
           SET IA TO TX
           SET TX TO 7
           SET IB TO TX
           SET TX TO FUNCTION MAX(IA IB)
           DISPLAY "A=" TE(TX)
           SET TX TO FUNCTION MIN(IA IB)
           DISPLAY "B=" TE(TX)
           SET IC TO FUNCTION MAX(IA IB)
           SET TX TO IC
           DISPLAY "C=" TE(TX)
           SET TX TO 3
           SET IXE(1) TO TX
           SET TX TO 5
           SET IXE(2) TO TX
           SET IE TO FUNCTION MIN(IXE(1) IXE(2))
           SET TX TO IE
           DISPLAY "D=" TE(TX)
           STOP RUN.
