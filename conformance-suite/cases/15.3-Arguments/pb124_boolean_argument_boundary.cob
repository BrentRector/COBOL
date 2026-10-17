*>--- conformance-case
*> id: pb124_boolean_argument_boundary
*> rules: [AR-15.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124 (AR-15.3-3 b) — the boolean-expression ARGUMENT predicate's space-separated boundary.
      *> ISO 8.4.3.2.3 SR8 admits a boolean expression as a function argument; the scan that recognizes one
      *> used to run to the argument LIST's end, so on a multi-argument call a B-operator ANYWHERE later
      *> predicated EVERY earlier argument into the boolean alternative — CONCAT(WS-A B1 B-AND B0) routed the
      *> alphanumeric WS-A down the boolean channel. A boolean expression connects every term with an operator
      *> (8.8.2 r1), so two ADJACENT operand terms are an argument boundary. Hand-derived: B"1" B-AND B"0" =
      *> B"0" -> CONCAT("ab" B"0") = "ab0"; the subscripted pair BT(1) B-AND BT(2) = B"1" -> ORD-position
      *> arithmetic avoided, INTEGER-OF-BOOLEAN = 1; a literal before a boolean pair keeps its own argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124BB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC XX VALUE "ab".
       01 B1 PIC 1 VALUE B"1".
       01 B0 PIC 1 VALUE B"0".
       01 BT-TAB.
          05 BT PIC 1 OCCURS 2 TIMES VALUE B"1".
       01 R PIC 9(4).
       01 RS PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B1 B-AND B1)
           IF R = 1 DISPLAY "IOB OK" ELSE DISPLAY "IOB BAD " R END-IF
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(BT(1) B-AND BT(2))
           IF R = 1 DISPLAY "SUB OK" ELSE DISPLAY "SUB BAD " R END-IF
           MOVE FUNCTION CONCAT(WS-A B1 B-AND B0) TO RS
           IF RS = "ab0   " DISPLAY "CAT OK" ELSE DISPLAY "CAT BAD [" RS "]"
           END-IF
           MOVE FUNCTION CONCAT("x" B1 B-AND B1) TO RS
           IF RS = "x1    " DISPLAY "LIT OK" ELSE DISPLAY "LIT BAD [" RS "]"
           END-IF
           STOP RUN.
