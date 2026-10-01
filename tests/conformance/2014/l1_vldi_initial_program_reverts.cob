      *> ISO §8.5.1.11.3 2) — initial program: VL items reset per CALL
      *> An initial program's variable-length items, whose resources may
      *> be freed at its GOBACK, revert to initial state on every
      *> activation; a non-initial program's keep their last-used state.
      *>
      *> THE RULES.
      *> §8.5.1.11.3: "The resources used by a variable-length data item
      *>   may be freed automatically when: ... 2) the program
      *>     containing
      *>   the data item is an initial program and executes an EXIT
      *>   PROGRAM or GOBACK;" and "The persistence of a variable-length
      *>   data item is the same as that of a non-variable-length data
      *>   item ... this has no effect on the results of the execution
      *>     of
      *>   the program."
      *>   OK  §8.5.1.11.3 2)  /  OK  §8.5.1.11.3 (persistence
      *>     paragraph)
      *> §8.6.4: "A variable-length data item is in its initial state at
      *>   the start of processing and reverts to its initial state
      *>     under
      *>   the same circumstances as defined for other data items."
      *>   "If no VALUE clause is specified, the length of that item in
      *>   its initial state is zero."  Working-storage items of an
      *>   initial program "are set to their initial state each time an
      *>   initial program is activated"; those of a program that is not
      *>   initial are static items.                    OK  §8.6.4
      *> §11.10.4 GR3: "When an initial program is activated, the data
      *>   items and file connectors contained in it ... are set to
      *>     their
      *>   initial states."                             OK  §11.10.4 3)
      *> §8.5.1.9.1: no FROM phrase and no VALUE clause, so "the current
      *>   capacity is initialized to zero".            OK  §8.5.1.9.1
      *> §8.5.1.9.3: MOVE to TE (5) past capacity 0 creates the element
      *>   and raises the capacity to 5.                OK  §8.5.1.9.3
      *> §8.5.1.10.4 / §15.50.4 GR6: MOVE "ABCD" makes the current
      *>     length
      *>   4, which FUNCTION LENGTH returns.            OK  both
      *> No TO phrase, so no expected capacity and no EC-BOUND-OVERFLOW.
      *>
      *> DERIVATION (N and M are PIC 99, so each value shows 2 digits).
      *> L1VL2I is INITIAL: both activations start at CAP=00 LEN=00 and
      *>   grow to CAP=05 LEN=04 before GOBACK.
      *> L1VL2E is INITIAL and returns by EXIT PROGRAM, not GOBACK:
      *>   rule 2 names both statements, and 14.9.14.4 GR3 sends
      *>   EXIT PROGRAM "as specified in 14.9.18, GOBACK statement,
      *>   General rules 3 and 4".  Its two activations print the
      *>   same pair as L1VL2I: E CAP=00 LEN=00, then
      *>   E GROWN CAP=05 LEN=04.                    OK  14.9.14.4 3)
      *> L1VL2S is NOT initial (the control): its first activation
      *>     starts
      *>   at CAP=00 LEN=00; its second finds the last-used state CAP=05
      *>   LEN=04, and MOVE to TE (5) creates nothing (subscript 5 does
      *>     not
      *>   exceed capacity 5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL2M.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1VL2I"
           CALL "L1VL2I"
           CALL "L1VL2E"
           CALL "L1VL2E"
           CALL "L1VL2S"
           CALL "L1VL2S"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL2I IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE PIC X(2) OCCURS DYNAMIC CAPACITY IN TC.
       01 S PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 N PIC 99.
       01 M PIC 99.
       PROCEDURE DIVISION.
       I-PARA.
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "I CAP=" N " LEN=" M
           MOVE "QQ" TO TE (5)
           MOVE "ABCD" TO S
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "I GROWN CAP=" N " LEN=" M
           GOBACK.
       END PROGRAM L1VL2I.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL2E IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE PIC X(2) OCCURS DYNAMIC CAPACITY IN TC.
       01 S PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 N PIC 99.
       01 M PIC 99.
       PROCEDURE DIVISION.
       E-PARA.
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "E CAP=" N " LEN=" M
           MOVE "QQ" TO TE (5)
           MOVE "ABCD" TO S
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "E GROWN CAP=" N " LEN=" M
           EXIT PROGRAM.
       END PROGRAM L1VL2E.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL2S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE PIC X(2) OCCURS DYNAMIC CAPACITY IN TC.
       01 S PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 N PIC 99.
       01 M PIC 99.
       PROCEDURE DIVISION.
       S-PARA.
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "S CAP=" N " LEN=" M
           MOVE "QQ" TO TE (5)
           MOVE "ABCD" TO S
           MOVE TC TO N
           MOVE FUNCTION LENGTH (S) TO M
           DISPLAY "S GROWN CAP=" N " LEN=" M
           GOBACK.
       END PROGRAM L1VL2S.
       END PROGRAM L1VL2M.
