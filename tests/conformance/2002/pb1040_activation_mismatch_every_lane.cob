      *> kb/Work PB1040's sibling sweep - ISO 14.9.4.4 GR3 d): the conformance rules of 14.8.2 and 14.8.3 are
      *> applied when the program is called, and a violation sets EC-PROGRAM-ARG-MISMATCH "if checking for it is
      *> enabled in both the activated program and activating runtime element", after which "the program call is
      *> not successful" and GR3 h) 1. sends the condition to the ON EXCEPTION phrase. 14.8.2.1: "The number of
      *> arguments in the activating element shall be equal to the number of formal parameters in the activated
      *> element, with the exception of trailing formal parameters that are specified with an OPTIONAL phrase".
      *> Two arms of the ONE rule were silent before PB1040 (measured on the pre-fix build: both calls ran and
      *> took the not-on-exception branch):
      *>   - a program with NO formal parameters registered no count facts at all, so `CALL "NOFORMALS" USING A`
      *>     ran, although zero formals is a count like any other;
      *>   - a CALL through a PROGRAM-POINTER dropped the activating half of the gate, so it never raised what
      *>     the same CALL by name raised.
      *> The RETURNING arm (14.8.3.3) rides the same pointer CALL.
      *>
      *> EXPECTED OUTPUT, derived line by line (every unit has >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON):
      *>   NAME-MISMATCH   one argument against a formal-less program, called by name.
      *>   PTR-MISMATCH    the same call through a program-pointer.
      *>   OK-NAME / IN-ONE / OK-PTR / IN-ONE   one argument against one formal: conforming, so it runs.
      *>   RET-MISMATCH    an X(3) result into an X(5) receiver, called through the pointer.
      *>   R=[#####]       the receiver is untouched: the call was not successful.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(5) VALUE "HELLO".
       01 R PIC X(5) VALUE "#####".
       01 PN USAGE PROGRAM-POINTER.
       01 P1 USAGE PROGRAM-POINTER.
       01 PR USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET PN TO ENTRY "PB1040NF"
           SET P1 TO ENTRY "PB1040ONE"
           SET PR TO ENTRY "PB1040RET"
           CALL "PB1040NF" USING A
               ON EXCEPTION DISPLAY "NAME-MISMATCH"
               NOT ON EXCEPTION DISPLAY "NAME-NO-EXCEPTION"
           END-CALL
           CALL PN USING A
               ON EXCEPTION DISPLAY "PTR-MISMATCH"
               NOT ON EXCEPTION DISPLAY "PTR-NO-EXCEPTION"
           END-CALL
           CALL "PB1040ONE" USING A
               ON EXCEPTION DISPLAY "ONE-MISMATCH"
               NOT ON EXCEPTION DISPLAY "OK-NAME"
           END-CALL
           CALL P1 USING A
               ON EXCEPTION DISPLAY "ONE-MISMATCH"
               NOT ON EXCEPTION DISPLAY "OK-PTR"
           END-CALL
           CALL PR RETURNING R
               ON EXCEPTION DISPLAY "RET-MISMATCH"
               NOT ON EXCEPTION DISPLAY "RET-NO-EXCEPTION"
           END-CALL
           DISPLAY "R=[" R "]"
           STOP RUN.
       END PROGRAM PB1040B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040NF.
       PROCEDURE DIVISION.
           DISPLAY "IN-NOFORMALS"
           GOBACK.
       END PROGRAM PB1040NF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040ONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(5).
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-ONE"
           GOBACK.
       END PROGRAM PB1040ONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1040RET.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE "ABC" TO L
           GOBACK.
       END PROGRAM PB1040RET.
