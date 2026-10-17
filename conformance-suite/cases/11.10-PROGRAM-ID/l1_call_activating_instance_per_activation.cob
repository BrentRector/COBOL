*>--- conformance-case
*> id: l1_call_activating_instance_per_activation
*> rules: [GR-11.10.4-4, GR-14.9.4.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.4.4 GR1 — "The instance of the program, function, or method that executes the CALL
      *> statement is the activating runtime element."
      *> (cite.py --check 14.9.4.4 "The instance of the program, function, or method that executes the CALL
      *> statement is the activating runtime element." -> OK, §14.9.4.4 General rules 1.)
      *>
      *> WHAT THE RULE ASSERTS, DERIVED BEFORE MEASURING. The activating runtime element is an INSTANCE,
      *> not a compilation unit. Three consequences follow, and this file pins all three in one run:
      *>   (a) the called program is located in the ACTIVATOR's §8.4.6.3 scope — a contained program is
      *>       reachable from its container's own activation;
      *>   (b) on return, control resumes IN THAT INSTANCE, so the activation's own automatic data
      *>       (LOCAL-STORAGE, §13.6.4 GR1 / §14.6.2.3.2 — allocated per activation) is intact;
      *>   (c) anything that reaches the activating element answers PER ACTIVATION — including the
      *>       container handle through which a CONTAINED callee reads a GLOBAL item of its container
      *>       (§13.18.27 GR2). §13.18.27.3 SR1b permits IS GLOBAL on a level-1 entry of the LOCAL-STORAGE
      *>       section, which is the ONE composition that makes (c) observable: the item is per-activation
      *>       AND visible to the containee.
      *>
      *> DERIVED TRACE. L1ACTR is RECURSIVE, so each activation gets its own instance and its own
      *> LOCAL-STORAGE; §11.10.4 GR4 ("the program and any programs contained within it are recursive")
      *> gives L1ACTC the attribute too, so it is likewise per-activation and binds to the container
      *> instance that activated it.
      *>   D=1 tags its frame 11, activates the containee     -> R-ENTER D=1 TAG=11 / C-SEES=11
      *>   D=1 re-enters itself; D=2 tags ITS frame 22        -> R-ENTER D=2 TAG=22 / C-SEES=22
      *>   D=2 resumes after its (skipped) recursion and calls the containee again
      *>                                                      -> R-RESUME D=2 TAG=22 / C-SEES=22
      *>   D=2 returns; control resumes in the D=1 INSTANCE   -> R-RESUME D=1 TAG=11   (leg b)
      *>   D=1 activates the containee once more              -> C-SEES=11             (leg c)
      *> The two failure shapes this discriminates: a registry slot left naming the DEAD depth-2 instance
      *> prints C-SEES=22 on the last line, and a containee whose container handle is bound ONCE at
      *> construction prints C-SEES=11 on the second and third.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CALLACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL "L1ACTR" USING WS-D
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACTR RECURSIVE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-TAG GLOBAL PIC 9(2) VALUE 0.
       01 LS-NEXT PIC 9.
       LINKAGE SECTION.
       01 LK-D PIC 9.
       PROCEDURE DIVISION USING LK-D.
       P.
           COMPUTE LS-TAG = LK-D * 11
           DISPLAY "R-ENTER D=" LK-D " TAG=" LS-TAG
           CALL "L1ACTC"
           IF LK-D < 2
               COMPUTE LS-NEXT = LK-D + 1
               CALL "L1ACTR" USING LS-NEXT
           END-IF
           DISPLAY "R-RESUME D=" LK-D " TAG=" LS-TAG
           CALL "L1ACTC"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACTC.
       PROCEDURE DIVISION.
       Q.
           DISPLAY "C-SEES=" LS-TAG
           GOBACK.
       END PROGRAM L1ACTC.
       END PROGRAM L1ACTR.
       END PROGRAM L1CALLACT.
