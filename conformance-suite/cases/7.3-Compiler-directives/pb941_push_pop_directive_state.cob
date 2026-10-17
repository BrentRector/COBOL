*>--- conformance-case
*> id: pb941_push_pop_directive_state
*> rules: [GR-7.3.4-3, SR-7.3.20.3-3, GR-7.3.20.4-1, GR-7.3.20.4-2,
*>    GR-7.3.20.4-3, SR-7.3.22.3-3, GR-7.3.22.4-1, GR-7.3.22.4-2,
*>    GR-7.3.22.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB941 - >>PUSH (7.3.22) and >>POP (7.3.20) SAVE AND RESTORE directive
      *> state. Before PB941 both were parsed, screened and discarded, so every leg below
      *> printed the state written INSIDE the pushed region.
      *>
      *> THE RULES:
      *>   7.3.22.4 GR1 - "If directive-name is specified, the state of the directive
      *>                   is saved."
      *>   7.3.22.4 GR2 - "If ALL is specified, the state of all of the directives other
      *>                   than EVALUATE, IF, PAGE, POP, or PUSH are saved."
      *>   7.3.22.4 GR3 - "The effects of the directive being pushed remain active."
      *>   7.3.20.4 GR1 - a named POP restores that directive's state when a PUSH stored
      *>                   it and no POP has removed it; GR3 - POP ALL restores every
      *>                   directive "previously stored by a PUSH directive and ... not
      *>                   removed by a POP directive".
      *>   7.3.20.4 GR2 - a named POP with nothing stored is UNSUCCESSFUL: it restores
      *>                   nothing (the required warning is COBOLNET2297, pinned by
      *>                   unit:DirectiveStateStackTests).
      *> THE OBSERVATION: EC-OVERFLOW-STRING is nonfatal. 14.6.13.1.4: "If checking is
      *> not enabled, execution continues as if the exception did not occur"; if it is
      *> enabled, the applicable USE declarative runs (item 3) and execution continues.
      *> So OVF counts the STRING overflows that were CHECKED - one per checked STRING.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   L1 IN=0 OUT=1   TURN ON; PUSH ALL; TURN OFF -> unchecked (0). POP ALL
      *>                   restores ON (GR3) -> checked (1).
      *>   L2 IN=2 OUT=2   the MIRROR. TURN OFF; PUSH ALL; TURN ON -> checked (2). POP
      *>                   ALL restores OFF -> unchecked, still 2.
      *>   L3 A=2 B=3      NESTING. TURN ON; PUSH TURN (saves ON); TURN OFF; PUSH ALL
      *>                   (saves OFF); TURN ON; POP TURN restores the MOST RECENT save,
      *>                   OFF -> unchecked (2). POP ALL restores the older ON -> 3.
      *>   L4 C=4          POP TURN with every save already removed is unsuccessful
      *>                   (GR2): ON stays in force -> checked (4).
      *>   L5 D=RESTORED   DEFINE V AS 1; PUSH DEFINE; DEFINE V AS 2 OVERRIDE; POP
      *>                   DEFINE restores V = 1 (7.3.20.4 GR1: "all instances of that
      *>                   directive are restored"), so the >>IF V = 1 branch is kept.
      *>   L6 FREE-SEGMENT then L6 S=FIXED
      *>                   SOURCE FIXED; PUSH SOURCE saves FIXED; SOURCE FREE switches
      *>                   the following text (7.3.24.3 GR1); POP SOURCE restores FIXED,
      *>                   so the line after it - a sequence number in columns 1-6 and
      *>                   PB941ID in the identification area, columns 73-80 - is read
      *>                   in fixed form. Read as free form, PB941ID is an undefined
      *>                   DISPLAY operand and the program does not compile.
      *> Directives sit at COLUMN 8 (column 7 is the fixed-form indicator area). Every
      *> PUSH ALL / POP ALL stands between statements in the procedure division
      *> (7.3.20.3 SR3 / 7.3.22.3 SR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB941PP23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       01 OVF PIC 9 VALUE 0.
       01 T-IN PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OVF SECTION.
           USE AFTER EXCEPTION CONDITION EC-OVERFLOW-STRING.
       D-OVF-P.
           ADD 1 TO OVF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> L1 - an enclosing ON survives a region that turns it OFF.
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       >>PUSH ALL
       >>TURN EC-OVERFLOW-STRING CHECKING OFF
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           MOVE OVF TO T-IN
       >>POP ALL
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           DISPLAY "L1 IN=" T-IN " OUT=" OVF
      *> L2 - the mirror: an enclosing OFF survives a region that turns it ON.
       >>TURN EC-OVERFLOW-STRING CHECKING OFF
       >>PUSH ALL
       >>TURN EC-OVERFLOW-STRING CHECKING ON
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           MOVE OVF TO T-IN
       >>POP ALL
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           DISPLAY "L2 IN=" T-IN " OUT=" OVF
      *> L3 - nesting: each POP removes the most recent save.
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       >>PUSH TURN
       >>TURN EC-OVERFLOW-STRING CHECKING OFF
       >>PUSH ALL
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       >>POP TURN
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           MOVE OVF TO T-IN
       >>POP ALL
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           DISPLAY "L3 A=" T-IN " B=" OVF
      *> L4 - an unsuccessful POP restores nothing.
       >>POP TURN
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           DISPLAY "L4 C=" OVF
      *> L5 - DEFINE: the compilation variable is restored.
       >>DEFINE V AS 1
       >>PUSH DEFINE
       >>DEFINE V AS 2 OVERRIDE
       >>POP DEFINE
       >>IF V = 1
           DISPLAY "L5 D=RESTORED"
       >>ELSE
           DISPLAY "L5 D=LOST"
       >>END-IF
      *> L6 - SOURCE FORMAT: the reference format is restored.
       >>SOURCE FIXED
       >>PUSH SOURCE
       >>SOURCE FREE
    DISPLAY "L6 FREE-SEGMENT"
       >>POP SOURCE
000100     DISPLAY "L6 S=FIXED"                                         PB941ID 
           STOP RUN.
