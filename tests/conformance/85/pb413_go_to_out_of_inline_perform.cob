      *> kb/Work PB413 - a format-1 GO TO (and the format-2 DEPENDING form) written INSIDE an inline PERFORM.
      *> The inline PERFORM lowers to a REAL C# loop, so the transfer has to leave that loop AND re-enter the
      *> paragraph dispatcher; every shape EmitPerformLoop produces is exercised here at --std 85, the earliest
      *> supported edition, because GO TO format 1 and the inline PERFORM are present unchanged at all four
      *> (no per-edition copy - the behaviour does not differ; the version matrix covers the gating).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.17.4 GR1 - "When a GO TO statement represented by format 1 is executed, control is transferred to
      *>                  procedure-name-1." Unconditional: no phrase of the rule exempts a GO TO that happens
      *>                  to be written inside the specified set of statements of an inline PERFORM.
      *> 14.9.17.4 GR2 - format 2: the value 1 ... n selects procedure-name-1 ... procedure-name-n and control
      *>                  is transferred there (K = 2 selects D-TWO).
      *> 14.9.28.4 GR4 - an inline PERFORM and an out-of-line PERFORM "function identically"; the inline form's
      *>                  specified set of statements is simply written in place.
      *> 14.9.28.4 GR6 NOTE 2 - "Statements such as the GO TO statement ... can occur in the flow of execution
      *>                  of the specified set of statements", which is the standard contemplating exactly this
      *>                  program. GR2's undefined case is OVERLAPPING PERFORMs and does not reach here: every
      *>                  PERFORM below is entered and left once, and none is executed inside another's range.
      *> 14.9.28.4 GR11/GR13 - the VARYING augmentation order: with TWO levels, identifier-4 (J) is re-set from
      *>                  its FROM value each time identifier-2 (I) is augmented, so the body runs at
      *>                  (1,1) (1,2) (1,3) (2,1) and the GO TO fires on (2,2) BEFORE that body displays.
      *>
      *> The trace, statement by statement:
      *>   MAIN-P      GO TO with no enclosing lowered container (the control that always worked).
      *>   T1-TIMES    PERFORM n TIMES        -> C# for      : leaves on N = 2, T1-TAIL is never reached.
      *>   T2-BARE     bare inline PERFORM    -> C# do/while : leaves before T2-MID, T2-TAIL never reached.
      *>               (The GO TO is the whole THEN phrase of an IF: written beside T2-MID in one sequence it would
      *>               violate 14.9.17.3 SR2, "it shall appear as the last statement in that sequence" - kb/Work PB397.)
      *>   T3-VARYING  PERFORM VARYING/AFTER  -> nested while: leaves EVERY level from the inner one.
      *>   T4-UNTIL    PERFORM UNTIL + an EVALUATE inside it -> the WHEN arm's GO TO leaves both.
      *>   T5-AFTER    PERFORM WITH TEST AFTER-> C# do/while : leaves on the first body execution.
      *>   BACK-P      a BACKWARD, non-adjacent target reached from inside T5's loop.
      *>   T6-DEP      GO TO ... DEPENDING inside PERFORM n TIMES -> the selector switch's arm must transfer.
      *> Every "MUST-NOT" paragraph tail below is unreachable under GR1; if one prints, the transfer was
      *> swallowed by the lowered loop and execution fell out of the PERFORM instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB413XFER85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       01 I PIC 9(4) VALUE 0.
       01 J PIC 9(4) VALUE 0.
       01 K PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START".
           GO TO T1-TIMES.
       BACK-P.
           DISPLAY "BACK " N.
           GO TO T6-DEP.
       T1-TIMES.
           DISPLAY "T1".
           PERFORM 5 TIMES
               ADD 1 TO N
               IF N = 2
                   GO TO T2-BARE
               END-IF
               DISPLAY "T1-IT " N
           END-PERFORM.
           DISPLAY "T1-TAIL-MUST-NOT".
       T2-BARE.
           DISPLAY "T2 " N.
           PERFORM
               ADD 1 TO N
               IF N > 0
                   GO TO T3-VARYING
               END-IF
               DISPLAY "T2-MID-MUST-NOT"
           END-PERFORM.
           DISPLAY "T2-TAIL-MUST-NOT".
       T3-VARYING.
           DISPLAY "T3 " N.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                     AFTER J FROM 1 BY 1 UNTIL J > 3
               IF I = 2 AND J = 2
                   GO TO T4-UNTIL
               END-IF
               DISPLAY "T3-IT " I " " J
           END-PERFORM.
           DISPLAY "T3-TAIL-MUST-NOT".
       T4-UNTIL.
           DISPLAY "T4 " I " " J.
           PERFORM UNTIL N > 99
               ADD 1 TO N
               EVALUATE N
                   WHEN 5
                       GO TO T5-AFTER
                   WHEN OTHER
                       DISPLAY "T4-IT " N
               END-EVALUATE
           END-PERFORM.
           DISPLAY "T4-TAIL-MUST-NOT".
       T5-AFTER.
           DISPLAY "T5 " N.
           PERFORM WITH TEST AFTER UNTIL N > 99
               ADD 1 TO N
               GO TO BACK-P
           END-PERFORM.
           DISPLAY "T5-TAIL-MUST-NOT".
       T6-DEP.
           DISPLAY "T6 " N.
           MOVE 2 TO K.
           PERFORM 3 TIMES
               GO TO D-ONE D-TWO DEPENDING ON K
               DISPLAY "T6-MID-MUST-NOT"
           END-PERFORM.
           DISPLAY "T6-TAIL-MUST-NOT".
       D-ONE.
           DISPLAY "D-ONE-MUST-NOT".
           STOP RUN.
       D-TWO.
           DISPLAY "D-TWO".
           STOP RUN.
