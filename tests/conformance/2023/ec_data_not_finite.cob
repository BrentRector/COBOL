      *> EC-DATA-NOT-FINITE (ISO §14.6.13.2 item 3, spec :24571; Table 13 Fatal): a standard-float SENDING operand
      *> whose content is NaN or ±Infinity is referenced. Raised at BOTH float read chokepoints — the numeric-value
      *> read (relation/arithmetic) and the string-image read (DISPLAY, or a
      *> different-usage float MOVE source). The four exemptions do NOT raise: class condition, sign condition, a
      *> ⛔ THE STRING AND MOVE-TO-ALPHANUMERIC ARMS ARE RUN-TIME CHOKEPOINTS THAT NO CONFORMING PROGRAM CAN
      *> REACH WITH A FLOAT, and this file used to assert one of them over ILLEGAL SOURCE (kb/Work PB664,
      *> 2026-09-22). `STRING WS-INF DELIMITED BY SIZE INTO WS-TXT` is refused twice over: 14.9.43.3 SR1 -
      *> "all identifiers, except identifier-4, shall be described implicitly or explicitly as usage display
      *> or national" - and SR8 - "Where identifier-1 or identifier-2 is an elementary numeric data item, it
      *> shall be described as an integer". `MOVE WS-INF TO WS-TXT` is refused by 14.9.25.3 SR10 / Table 16
      *> ("a noninteger numeric sending operand does not move to an ... alphanumeric receiver"). The leg was
      *> green only while SR1 was unenforced at the sending position; it is REMOVED rather than re-spelled,
      *> because the DISPLAY leg below reaches the same string-image read from source the standard admits.
      *> same-usage MOVE (FLOAT-BINARY-64 -> FLOAT-BINARY-64), and VALIDATE (not yet emitted). Under >>TURN … CHECKING ON the USE
      *> declarative reports the condition via FUNCTION EXCEPTION-STATUS (§15.33) and RESUME AT NEXT STATEMENT
      *> (§14.9.33) continues, so every leg is observable in one run. With checking OFF nothing raises.
      >>TURN EC-DATA-NOT-FINITE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-DNF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-INF USAGE FLOAT-BINARY-64.
       01 WS-B   USAGE FLOAT-BINARY-64.
       01 WS-S   USAGE FLOAT-BINARY-32.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-NOT-FINITE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE WS-INF = 1.0E300 * 1.0E300.
           IF WS-INF IS POSITIVE CONTINUE END-IF.
           DISPLAY "SIGN-EXEMPT-OK".
           IF WS-INF IS NUMERIC CONTINUE END-IF.
           DISPLAY "CLASS-EXEMPT-OK".
           MOVE WS-INF TO WS-B.
           DISPLAY "SAME-USAGE-EXEMPT-OK".
           DISPLAY "R1".
           DISPLAY WS-INF.
           DISPLAY "R2".
           IF WS-INF > 1.0 CONTINUE END-IF.
           DISPLAY "R3".
           MOVE WS-INF TO WS-S.
           DISPLAY "DONE".
           STOP RUN.
