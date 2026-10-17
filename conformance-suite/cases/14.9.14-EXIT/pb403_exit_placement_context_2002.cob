*>--- conformance-case
*> id: pb403_exit_placement_context_2002
*> rules: [SR-14.9.14.3-7, SR-14.9.14.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> THE EARLIEST EDITION AT WHICH THIS PROGRAM CAN BE WRITTEN (kb/Work PB403). EXIT PERFORM (§14.9.14.2
      *> Format 3) is a COBOL-2002 introduction, so COBOL-2002 is the oldest edition at which §14.9.14.3 SR8 --
      *> "The EXIT PERFORM statement may be specified only in an inline or exception-checking PERFORM statement.
      *> The CYCLE phrase shall not be specified within an exception-checking PERFORM statement." -- has an
      *> admitted position to pin at all. §14.9.14.3 SR7 ("An EXIT PROGRAM statement may be specified only in a
      *> program procedure division") is edition-stable, but its NON-program arms are reachable only from 2002
      *> too, when user-defined functions arrive; the negative fixtures carry those.
      *>
      *> WHAT IT PROVES. Before PB403, SR8's FIRST sentence was enforced nowhere, and the accepted statement did
      *> not quietly do nothing: the emitter wrote a bare C# `break;` into the pc dispatcher's switch without
      *> advancing the pc, so the paragraph ran forever. The fix screens the statement at bind time against the
      *> enclosing-construct stack -- which means the ADMITTED positions have to keep working, and that is what
      *> this program measures: EXIT PERFORM and EXIT PERFORM CYCLE in every inline-PERFORM shape, plus SR7's
      *> admitted position (a CALLed program's EXIT PROGRAM).
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, from §14.9.14.4 only.
      *>  1) GR5 a): EXIT PERFORM without CYCLE passes control "to an implicit CONTINUE statement immediately
      *>     FOLLOWING the END-PERFORM phrase". Loop A varies I from 1 by 1 until I > 5 and exits when I = 3, so
      *>     the DISPLAY runs for I = 1 and I = 2 only:      A=01, A=02.
      *>  2) GR5 b): EXIT PERFORM CYCLE passes control "to an implicit CONTINUE statement immediately PRECEDING
      *>     the END-PERFORM phrase", so the VARYING augment and the UNTIL re-test still run. Loop B varies I
      *>     from 1 by 1 until I > 4 and cycles when I = 2, so only I = 2's DISPLAY is skipped:
      *>                                                     B=01, B=03, B=04.
      *>  3) GR5 a) again, over a PERFORM VARYING ... AFTER: the statement it leaves is the whole inline PERFORM
      *>     ("the most closely preceding, and as yet unterminated, inline PERFORM statement"), not one level of
      *>     it. Loop C varies I 1..3 with J 1..3 inside and exits at I = 2, J = 2, so:
      *>                                       C=0101, C=0102, C=0103, C=0201.
      *>  4) §14.9.14.4 GR3: an EXIT PROGRAM executed in a program under the control of a calling runtime element
      *>     proceeds as GOBACK's GR3/GR4 -- control returns to the caller, so the statement after it in the
      *>     called program never executes:                  SUB-IN, SUB-BACK (never SUB-NEVER).
      *>  5) The final DISPLAY:                              DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB403XP02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I    PIC 9(2) VALUE 0.
       01  W-J    PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 5
               IF W-I = 3
                   EXIT PERFORM
               END-IF
               DISPLAY "A=" W-I
           END-PERFORM.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 4
               IF W-I = 2
                   EXIT PERFORM CYCLE
               END-IF
               DISPLAY "B=" W-I
           END-PERFORM.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
                   AFTER W-J FROM 1 BY 1 UNTIL W-J > 3
               IF W-I = 2 AND W-J = 2
                   EXIT PERFORM
               END-IF
               DISPLAY "C=" W-I W-J
           END-PERFORM.
           CALL "PB403XS02".
           DISPLAY "SUB-BACK".
           DISPLAY "DONE".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB403XS02.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-IN".
           EXIT PROGRAM.
           DISPLAY "SUB-NEVER".
       END PROGRAM PB403XS02.
       END PROGRAM PB403XP02.
