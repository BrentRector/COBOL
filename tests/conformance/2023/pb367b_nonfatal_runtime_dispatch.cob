      *> kb/Work PB367b - AN ENABLED NONFATAL CONDITION RAISED AT A RUNTIME SITE SELECTS ITS USE
      *> DECLARATIVE. Every condition below is detected inside the runtime, not by a guard the emitter
      *> could place around a statement: an untranslatable code unit deep inside an expression, a
      *> dynamic-capacity table growing under a receiving subscript, an inverted THROUGH range in a
      *> condition, a dynamic-length resize. Each used to set the last exception status and RETURN, so
      *> FUNCTION EXCEPTION-STATUS saw the raise and the declarative never ran.
      *>
      *> THE RULE, AND WHERE EACH EXPECTED LINE COMES FROM:
      *>  . 14.6.13.1.4 - "If checking for that exception condition is enabled, processing of the
      *>    statement is interrupted and one of the following occurs in the order specified: ...
      *>    3) If there is an applicable USE statement in the source unit that specifies the
      *>    exception-name associated with the exception condition or an exception-name of a higher
      *>    level in the same hierarchy, the associated declarative is executed. If execution of the
      *>    declarative completes normally, execution continues as specified in the rules for normal
      *>    execution." So each declarative below RUNS (the *-DECL line), and then the statement
      *>    finishes under its own rules - which each condition's own clause names outright:
      *>  . 8.5.1.9.6 GR1 - the implicit growth past the expected capacity raises EC-BOUND-OVERFLOW on
      *>    the FIRST crossing only; the growth itself proceeds (8.5.1.9.5), so WS-E (5) holds 22.
      *>  . 14.7.8 rule 2 - "the EC-RANGE-INVALID exception condition is set to exist, and, upon
      *>    completion of any exception processing, execution proceeds as if the range of values were
      *>    empty", so the IF takes its ELSE branch AFTER the declarative has run.
      *>  . 14.9.39 Format 16 GR37 - a value that is not nonnegative sets the length to 0 and raises
      *>    EC-STORAGE-NOT-AVAIL, so FUNCTION LENGTH answers 0 after the declarative.
      *>  . 15.19.4 rule 3 / CONFORMANCE.md item 209 - a code unit with no one-byte image in the
      *>    8-bit usage-DISPLAY serialization is replaced by the substitution character and
      *>    EC-DATA-CONVERSION is raised, so CONVERT ... HEX answers "3F" (the hexadecimal image of
      *>    '?') after the declarative.
      *>  . 14.6.13.1.1 - "All exception status indicators are cleared at the beginning of the
      *>    execution of any statement", but the LAST EXCEPTION STATUS persists until the next raise
      *>    or SET LAST EXCEPTION TO OFF (14.9.39 Format 13), which is why each EC[...] line below
      *>    shows the name just raised.
      *>  . The four declaratives are selected by 14.9.49.4 GR3 e) - format 3, no file-name-2, a
      *>    level-3 exception-name - and each completes normally (14.6.13.1.2: no EXIT PROGRAM,
      *>    GOBACK, RESUME or STOP, and no fatal exception).
      *> Every value here is COMPUTED FROM THOSE RULES, not from a run.
      >>TURN EC-BOUND-OVERFLOW CHECKING ON
      >>TURN EC-RANGE-INVALID CHECKING ON
      >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
      >>TURN EC-DATA-CONVERSION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367BNFD.
      *> INV-RANGE is ordered IN a LOCALE alphabet: §13.18.63.3 SR26 refuses an inverted range whose runtime
      *> collating sequence is known (COBOLNET2961, kb/Work PB552), and a locale's is not.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       01 WS-C  PIC X VALUE "M".
          88 INV-RANGE VALUE "Z" THRU "A" IN LOC.
       01 WS-D  PIC X DYNAMIC LENGTH LIMIT IS 5.
       01 WS-NEG PIC S9 VALUE -1.
       01 WS-N  PIC 9(2).
       01 WS-X  PIC X(1).
       01 WS-H  PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BO SECTION.
           USE AFTER EC EC-BOUND-OVERFLOW.
       D-BO-P.
           DISPLAY "BO-DECL".
       D-RI SECTION.
           USE AFTER EC EC-RANGE-INVALID.
       D-RI-P.
           DISPLAY "RI-DECL".
       D-SNA SECTION.
           USE AFTER EC EC-STORAGE-NOT-AVAIL.
       D-SNA-P.
           DISPLAY "SNA-DECL".
       D-DC SECTION.
           USE AFTER EC EC-DATA-CONVERSION.
       D-DC-P.
           DISPLAY "DC-DECL".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> 8.5.1.9.6 GR1: 5 is the first implicit crossing of the expected capacity 4.
           MOVE 22 TO WS-E (5).
           DISPLAY "BO-VAL=" WS-E (5) " EC[" FUNCTION EXCEPTION-STATUS "]".
      *> 14.7.8 rule 2: "Z" collates after "A", so the range is empty - the ELSE branch runs.
           IF INV-RANGE DISPLAY "RI-TRUE" ELSE DISPLAY "RI-FALSE" END-IF.
           DISPLAY "RI-EC[" FUNCTION EXCEPTION-STATUS "]".
      *> 14.9.39 Format 16 GR37: -1 is not nonnegative - the length becomes 0.
           MOVE "ABCDE" TO WS-D.
           SET SIZE OF WS-D TO WS-NEG.
           MOVE FUNCTION LENGTH (WS-D) TO WS-N.
           DISPLAY "SNA-LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
      *> 15.19.4 rule 3: U+0160 has no one-byte image - '?' (X"3F") is substituted.
           MOVE FUNCTION DISPLAY-OF (FUNCTION CHAR-NATIONAL (353)) TO WS-X.
           MOVE FUNCTION CONVERT (WS-X ANUM ANUM HEX) TO WS-H.
           DISPLAY "DC-HEX=" WS-H " EC[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
