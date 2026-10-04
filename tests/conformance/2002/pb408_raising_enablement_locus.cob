      *> kb/Work PB408 - ISO 14.9.18.4 GR1 b): "If the RAISING phrase is specified, an
      *> exception condition is raised in the activating runtime element IF CHECKING FOR
      *> THAT EXCEPTION CONDITION IS ENABLED IN THE ACTIVATING RUNTIME ELEMENT". One
      *> element, named twice - and it is the ACTIVATOR, never the element that wrote the
      *> RAISING phrase. 7.3.25.4 GR6/GR8 scope a >>TURN directive to the statements "that
      *> follow in the compilation group", so the four called elements below sit in a
      *> checking state the main program does not share, inside this one file.
      *>
      *> SUB1  activator ON  / callee OFF -> raised HERE: the activator's declarative runs.
      *> SUB2  activator OFF / callee ON  -> not raised at all (14.6.13.1.1: "if checking
      *>       for an exception that occurs is not enabled, no exception condition is
      *>       raised"), so the activator's matching declarative shall NOT run.
      *> SUB3  RAISING LAST, the name LISTED in the callee's procedure division header ->
      *>       GR1b3a first sentence: that condition is set to exist in the activator.
      *> SUB4  RAISING LAST, the name NOT listed -> GR1b3a third sentence: the
      *>       EC-RAISING-NOT-SPECIFIED condition is set to exist in the activator INSTEAD
      *>       of the EC-USER condition, so the EC-USER-PBON declarative shall NOT run.
      *>       Checking for EC-RAISING is not enabled here, so GR1 b) raises nothing and
      *>       execution continues (Table 13 would otherwise make it fatal).
      *> SUB5  the EXIT PROGRAM twin of SUB1, and SUB6 the twin of SUB2: 14.9.14.4 GR3
      *>       sends a called-program EXIT PROGRAM into the GOBACK rules, so the locus is
      *>       the same and both verbs stage through one site (kb/Work PB406).
      *> SUB3 and SUB4 differ ONLY in that header phrase; before PB408 they produced
      *> byte-identical output.
       >>TURN EC-USER-PBON CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408M.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-ON SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBON.
       H-ON-P.
           DISPLAY "ACTIVATOR-CAUGHT-PBON".
           RESUME AT NEXT STATEMENT.
       H-OFF SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBOFF.
       H-OFF-P.
           DISPLAY "ACTIVATOR-CAUGHT-PBOFF".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB408S1".
           DISPLAY "AFTER-1".
           CALL "PB408S2".
           DISPLAY "AFTER-2".
           CALL "PB408S3".
           DISPLAY "AFTER-3".
           CALL "PB408S4".
           DISPLAY "AFTER-4".
           CALL "PB408S5".
           DISPLAY "AFTER-5".
           CALL "PB408S6".
           DISPLAY "AFTER-6".
           STOP RUN.
       END PROGRAM PB408M.

       >>TURN EC-USER-PBON CHECKING OFF
       >>TURN EC-USER-PBOFF CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S1.
       PROCEDURE DIVISION RAISING EC-USER-PBON.
       S1-P.
           DISPLAY "IN-SUB1".
           GOBACK RAISING EXCEPTION EC-USER-PBON.
       END PROGRAM PB408S1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S2.
       PROCEDURE DIVISION RAISING EC-USER-PBOFF.
       S2-P.
           DISPLAY "IN-SUB2".
           GOBACK RAISING EXCEPTION EC-USER-PBOFF.
       END PROGRAM PB408S2.

       >>TURN EC-USER-PBON CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S3.
       PROCEDURE DIVISION RAISING EC-USER-PBON.
       DECLARATIVES.
       S3-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBON.
       S3-H-P.
           DISPLAY "IN-SUB3-DECL".
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       S3 SECTION.
       S3-P.
           RAISE EXCEPTION EC-USER-PBON.
           DISPLAY "NEVER-3".
       END PROGRAM PB408S3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S4.
       PROCEDURE DIVISION.
       DECLARATIVES.
       S4-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBON.
       S4-H-P.
           DISPLAY "IN-SUB4-DECL".
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       S4 SECTION.
       S4-P.
           RAISE EXCEPTION EC-USER-PBON.
           DISPLAY "NEVER-4".
       END PROGRAM PB408S4.

       >>TURN EC-USER-PBON CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S5.
       PROCEDURE DIVISION RAISING EC-USER-PBON.
       S5-P.
           DISPLAY "IN-SUB5".
           EXIT PROGRAM RAISING EXCEPTION EC-USER-PBON.
       END PROGRAM PB408S5.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB408S6.
       PROCEDURE DIVISION RAISING EC-USER-PBOFF.
       S6-P.
           DISPLAY "IN-SUB6".
           EXIT PROGRAM RAISING EXCEPTION EC-USER-PBOFF.
