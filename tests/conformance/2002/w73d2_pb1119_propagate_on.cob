      *> kb/Work PB1119 - >>PROPAGATE ON makes an unhandled FATAL
      *> exception condition propagate to the activating element.
      *> ISO 7.3.21.4 GR1: "automatic propagation of exception
      *> conditions becomes enabled for functions, methods, and
      *> programs that follow in the compilation group".
      *>   cite.py --check 7.3.21.4 -> OK  7.3.21.4 1)
      *> ISO 14.6.13.1.3 6): checking enabled, not EC-FLOW-GLOBAL-*,
      *> "and there is an applicable PROPAGATE ON directive, the
      *> exception condition is propagated as if a GOBACK statement
      *> with the RAISING LAST EXCEPTION phrase were executed".
      *>   cite.py --check 14.6.13.1.3 -> OK  14.6.13.1.3 6)
      *> ISO 14.9.18.4 GR1 b) 3.: the condition "is set to exist in
      *> the activating runtime element" (raised there when checking
      *> is enabled there) - so W73D2PA's USE declarative runs and
      *> RESUMEs, and the statement after each CALL runs.
      *> Expected, derived from the rules above:
      *>  W73D2PB: STRING overflows (EC-OVERFLOW-STRING is NONFATAL;
      *>   14.6.13.1.4 4) - execution continues; not propagated),
      *>   then E(I) with I = 7 on OCCURS 3 raises the FATAL
      *>   EC-BOUND-SUBSCRIPT with no handler in PB -> propagated;
      *>   "B AFTER SUBSCRIPT" is never displayed.
      *>  W73D2PC calls W73D2PD, whose DIVIDE by zero raises the
      *>   FATAL EC-SIZE-ZERO-DIVIDE with no SIZE ERROR phrase and no
      *>   handler -> propagated to PC, raised there (checking on),
      *>   no handler in PC and PC is under PROPAGATE ON -> propagated
      *>   again to PA, whose declarative catches it.
      *>  W73D2PE follows >>PROPAGATE OFF (7.3.21.4 GR3) and handles
      *>   its own condition with its own declarative + RESUME.
       >>TURN EC-BOUND-SUBSCRIPT EC-SIZE-ZERO-DIVIDE CHECKING ON
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2PA.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-ALL.
           DISPLAY "A CAUGHT " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
           CALL "W73D2PB".
           DISPLAY "A AFTER B".
           CALL "W73D2PC".
           DISPLAY "A AFTER C".
           CALL "W73D2PE".
           DISPLAY "A AFTER E".
           STOP RUN.
       END PROGRAM W73D2PA.
       >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2PB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3.
       01 I PIC 99 VALUE 7.
       01 S PIC X(3).
       PROCEDURE DIVISION.
           STRING "ABCDEF" DELIMITED BY SIZE INTO S.
           DISPLAY "B CONTINUES S=" S.
           MOVE "1" TO E(I).
           DISPLAY "B AFTER SUBSCRIPT".
           GOBACK.
       END PROGRAM W73D2PB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2PC.
       PROCEDURE DIVISION.
           CALL "W73D2PD".
           DISPLAY "C AFTER D".
           GOBACK.
       END PROGRAM W73D2PC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2PD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC 9 VALUE 0.
       01 X PIC 9(4) VALUE 10.
       PROCEDURE DIVISION.
           DIVIDE Z INTO X.
           DISPLAY "D AFTER DIVIDE".
           GOBACK.
       END PROGRAM W73D2PD.
       >>PROPAGATE OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2PE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3.
       01 I PIC 99 VALUE 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DX SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
           DISPLAY "E CAUGHT " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
           MOVE "1" TO E(I).
           DISPLAY "E AFTER SUBSCRIPT".
           GOBACK.
       END PROGRAM W73D2PE.
