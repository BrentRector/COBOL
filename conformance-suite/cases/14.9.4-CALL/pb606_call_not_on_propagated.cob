*>--- conformance-case
*> id: pb606_call_not_on_propagated
*> rules: [GR-14.9.4.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB606 - ISO 14.9.4.4 GR3 i): "If an exception condition
      *> is propagated from the called program, execution continues as
      *> specified in 14.6.13.1, Exception conditions; otherwise, control
      *> is transferred to the end of the CALL statement or, if the NOT ON
      *> EXCEPTION phrase is specified, to imperative-statement-2". The
      *> two outcomes are ALTERNATIVES: a propagated condition never also
      *> runs NOT ON EXCEPTION, and 14.6.13.1.4 3) says the same for a
      *> nonfatal condition whose declarative completes normally.
      *> 14.9.18.4 GR1 b) raises the propagated condition in the activator
      *> only if checking for it is enabled THERE, so EC-USER-PB6B (never
      *> turned on) propagates nothing and NOT ON EXCEPTION runs.
      *>
      *> C1 EC-USER-PB6A, declarative RESUMEs AT NEXT STATEMENT -> DECL-A
      *> C2 EC-USER-PB6C, declarative completes normally      -> DECL-C
      *> C3 EC-USER-PB6B, checking not enabled -> NOT ON runs  -> NOT-ON-3
      *> C4 a plain GOBACK, nothing propagated -> NOT ON runs  -> NOT-ON-4
      *> C5 ON and NOT ON both written, C1's callee -> neither phrase runs
      >>TURN EC-USER-PB6A CHECKING ON
      >>TURN EC-USER-PB6C CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606M.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PB6A.
       DA-P.
           DISPLAY "DECL-A".
           RESUME AT NEXT STATEMENT.
       DC SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PB6C.
       DC-P.
           DISPLAY "DECL-C".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB606A"
               NOT ON EXCEPTION DISPLAY "NOT-ON-1"
           END-CALL.
           DISPLAY "AFTER-1".
           CALL "PB606C"
               NOT ON EXCEPTION DISPLAY "NOT-ON-2"
           END-CALL.
           DISPLAY "AFTER-2".
           CALL "PB606B"
               NOT ON EXCEPTION DISPLAY "NOT-ON-3"
           END-CALL.
           DISPLAY "AFTER-3".
           CALL "PB606N"
               NOT ON EXCEPTION DISPLAY "NOT-ON-4"
           END-CALL.
           DISPLAY "AFTER-4".
           CALL "PB606A"
               ON EXCEPTION DISPLAY "ON-5"
               NOT ON EXCEPTION DISPLAY "NOT-ON-5"
           END-CALL.
           DISPLAY "AFTER-5".
           STOP RUN.
       END PROGRAM PB606M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606A.
       PROCEDURE DIVISION RAISING EC-USER-PB6A.
       A-P.
           GOBACK RAISING EXCEPTION EC-USER-PB6A.
       END PROGRAM PB606A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606B.
       PROCEDURE DIVISION RAISING EC-USER-PB6B.
       B-P.
           GOBACK RAISING EXCEPTION EC-USER-PB6B.
       END PROGRAM PB606B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606C.
       PROCEDURE DIVISION RAISING EC-USER-PB6C.
       C-P.
           GOBACK RAISING EXCEPTION EC-USER-PB6C.
       END PROGRAM PB606C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606N.
       PROCEDURE DIVISION.
       N-P.
           GOBACK.
       END PROGRAM PB606N.
