      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1399 — the negative twin of 85/pb1399_set_index_function_sender. An INDEX function is
      *> identifier-2 (§14.9.39.3 SR2; §15.2 item 6), NOT index-name-2, and §14.9.39.3 SR4 reads "If identifier-1
      *> references a numeric data item, index-name-2 shall be specified" — so a numeric receiver (N, PIC 9(4))
      *> cannot take it. SET N TO FUNCTION MAX(IA IB) is refused by SR4 alone: the index function is classified as
      *> the sender it IS rather than put through the arithmetic screen, which used to answer first with
      *> COBOLNET0844 ("an index function is not a numeric operand") about a statement whose broken rule is SR4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1399N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IA USAGE INDEX.
       01 IB USAGE INDEX.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           SET N TO FUNCTION MAX(IA IB)
           STOP RUN.
