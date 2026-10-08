      *> docs/CONFORMANCE.md DOC-A.1-132 - ISO 15.65.4 1): MODULE-NAME
      *> returns a dynamic-length elementary item, and the
      *> determination names its type: ALPHANUMERIC with the
      *> implementor-defined internal structure, so no PREFIXED
      *> length-field bound (65535 / 32767) applies. Under a PREFIXED
      *> structure a value longer than 65535 would not fit, and
      *> 15.65.4 2) would place "as much information as fits" and set
      *> EC-BOUND-FUNC-RET-VALUE.
      *> 15.65.4 9): "If the STACK keyword is specified, then the
      *> returned value is a list of module names separated by
      *> semi-colons": the CURRENT name, the ACTIVATING chain down to
      *> the TOP-LEVEL name, and a final single space for the
      *> operating environment.
      *> This RECURSIVE program (a 30-character name) CALLs itself
      *> until 2200 activations are active, so the list holds 2200
      *> names of 30 characters, each followed by ";", then " ":
      *> 2200 * 31 + 1 = 68201 characters, more than 65535. Expected:
      *> L=68201; the first 40 characters are the name, ";" and the
      *> name's first 9; no exception condition exists (31 spaces).
      *> Needs kb/Work PB2659: before it the recursion died of a stack
      *> overflow at a depth of about 250.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DOCA1132MODULENAMESTACKDEEPRCS RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DEPTH PIC 9(5) VALUE 0.
       01 LIM   PIC 9(5) VALUE 2200.
       01 L     PIC 9(9).
       01 S     PIC X(40).
       PROCEDURE DIVISION.
           ADD 1 TO DEPTH
           IF DEPTH < LIM
               CALL "DOCA1132MODULENAMESTACKDEEPRCS"
           ELSE
               MOVE FUNCTION MODULE-NAME(STACK) TO S
               MOVE FUNCTION LENGTH(FUNCTION MODULE-NAME(STACK)) TO L
               DISPLAY "S=[" S "]"
               DISPLAY "L=" L
               DISPLAY "EC=[" FUNCTION EXCEPTION-STATUS "]"
           END-IF
           GOBACK.
