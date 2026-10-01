      *> ISO 8.5.1.11.3 5) - a fatal exception that is RESUMEd leaves
      *> the program's variable-length items as they were
      *> Resources "may be freed automatically when ... 5) the program
      *> containing the data item encounters a fatal exception", but
      *> "The persistence of a variable-length data item is the same as
      *> that of a non-variable-length data item": when execution goes
      *> on after the exception, the dynamic table and the
      *> dynamic-length item still hold their values.
      *>
      *> THE RULES.
      *> 14.7.5 (no SIZE ERROR phrase): "if the divisor in a divide
      *>   operation or the DIVIDE statement is zero, the
      *>   EC-SIZE-ZERO-DIVIDE exception condition is set to exist" and
      *>   processing proceeds per 14.6.13.1.3.        OK  14.7.5 2)
      *> Table 13: EC-SIZE-ZERO-DIVIDE is Fatal.       OK  14.6.13.1.6
      *> 14.6.13.1.3 5): checking is enabled (>>TURN) and a USE
      *>   declarative names the exception, so the declarative runs;
      *>   only if it "completes normally" is the run unit terminated.
      *>   NOTE 2: "The user is able to continue by using a RESUME
      *>   statement".                                 OK  14.6.13.1.3 5)
      *> 14.6.13.1.2 1): executing RESUME means the declarative does
      *>   not complete normally.                      OK  14.6.13.1.2 1)
      *> 14.9.33.4 GR2 a): RESUME AT NEXT STATEMENT continues after the
      *>   DIVIDE.                                     OK  14.9.33.4 2) a)
      *> 8.6.4: a non-initial program's working storage is static.
      *> 8.5.1.9.1: no FROM, no VALUE - initial capacity 0; the MOVEs
      *>   to TE (1) and TE (2) raise it to 2 (8.5.1.9.3).
      *> 8.5.1.10.4 / 15.50.4 GR6: MOVE "ABCD" makes the length 4.
      *> Do not use ON SIZE ERROR: with that phrase 14.7.5 handles the
      *> size error condition and no fatal EC-SIZE-ZERO-DIVIDE exists.
      *> Q, the DIVIDE's receiving operand, is never displayed.
      *>
      *> DERIVATION (N is PIC 99).
      *> FATAL            the declarative runs for the zero divisor.
      *> CAP=02 111222    the table is unchanged after the exception.
      *> D=[ABCD] 04      the dynamic-length item is unchanged.
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL5F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 TE PIC 9(3) OCCURS DYNAMIC CAPACITY IN TCAP.
       01 D PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 Z PIC 9 VALUE 0.
       01 Q PIC 9 VALUE 7.
       01 N PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
       H-P.
           DISPLAY "FATAL".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 111 TO TE (1).
           MOVE 222 TO TE (2).
           MOVE "ABCD" TO D.
           DIVIDE Z INTO Q.
           MOVE TCAP TO N.
           DISPLAY "CAP=" N " " TE (1) TE (2).
           MOVE FUNCTION LENGTH (D) TO N.
           DISPLAY "D=[" D "] " N.
           STOP RUN.
