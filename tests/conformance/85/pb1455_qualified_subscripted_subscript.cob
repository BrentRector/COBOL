      *> kb/Work PB1455 (+PB1426) - the subscripts of a qualified identifier follow the WHOLE qualified name.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.2.3.2 Format 1 prints `qualified-data-name-1 [ ( subscript ... ) ]`
      *> (Format 2 the condition-name twin), and 8.4.3.1.2 Format 3 prints `identifier-1 reference-modifier-1`, so
      *> every qualifier comes first, then the subscripts, then the reference modifier. The misordered spelling
      *> `E (1) OF T` is refused (negative pb1455-subscript-before-qualifier). This positive pins the standard
      *> order in the one position the compiler got WRONG: a qualified, subscripted identifier used AS A SUBSCRIPT.
      *> The splitter that decides whether a '(' belongs to the name before it asked only the LAST word - `T`, a
      *> group with no OCCURS - so `X (E OF T OF G (2))` became two subscripts of X and was refused COBOLNET2270.
      *> 8.4.2.3.3 SR2 asks it of qualified-data-name-1, which here is `E OF T OF G`, a table element.
      *>
      *> EXPECTED OUTPUT, computed from the rules (8.4.2.3.4 GR1b: the subscript is the value of the expression):
      *>   BBBB    E OF T OF G (2) = 2, so X (2)
      *>   CCCC    E OF H (1) = 3 (T omitted - 8.4.2.2.1: "All available qualifiers need not be specified so long
      *>           as uniqueness is established"), so X (3)
      *>   AAAA    E IN T IN H (2) = 1, so X (1)
      *>   BC      S OF G (2:2): two characters of "ABCD" from position 2
      *>   E-TWO   E OF T OF G (2) is 2, the value of condition-name E-TWO
      *>   WXYZ    MOVE "WXYZ" TO X (E OF T OF G (3)) is a move to X (3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1455P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T.
             10 E PIC 9 OCCURS 3.
                88 E-TWO VALUE 2.
          05 S PIC X(4) VALUE "ABCD".
       01 H.
          05 T.
             10 E PIC 9 OCCURS 3.
       01 XS.
          05 X PIC X(4) OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO E OF T OF G (1)
           MOVE 2 TO E OF T OF G (2)
           MOVE 3 TO E OF T OF G (3)
           MOVE 3 TO E OF T OF H (1)
           MOVE 1 TO E OF H (2)
           MOVE "AAAA" TO X (1)
           MOVE "BBBB" TO X (2)
           MOVE "CCCC" TO X (3)
           DISPLAY X (E OF T OF G (2))
           DISPLAY X (E OF H (1))
           DISPLAY X (E IN T IN H (2))
           DISPLAY S OF G (2:2)
           IF E-TWO OF T OF G (2)
               DISPLAY "E-TWO"
           END-IF
           MOVE "WXYZ" TO X (E OF T OF G (3))
           DISPLAY X (3)
           STOP RUN.
