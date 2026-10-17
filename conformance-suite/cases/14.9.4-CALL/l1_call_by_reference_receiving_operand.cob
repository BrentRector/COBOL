      *>--- conformance-case
      *> id: l1_call_by_reference_receiving_operand
      *> rules: [SR-14.9.4.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.4.3 SR5 — "If the BY REFERENCE phrase is specified or implied for an identifier-2 and
      *> identifier-2 is not an address-identifier, it is a receiving operand."
      *> (cite.py --check 14.9.4.3 "If the BY REFERENCE phrase is specified or implied for an identifier-2
      *> and identifier-2 is not an address-identifier, it is a receiving operand." -> OK, §14.9.4.3
      *> Syntax rules 5.)
      *>
      *> DERIVED BEFORE MEASURING. "Receiving operand" is an observable property, not a label: the callee
      *> stores into the formal parameter and the store reaches the CALLER's storage, because a BY
      *> REFERENCE argument's formal "occupies the same storage area as the argument" (§14.2.3 GR8). SR5's condition has
      *> two halves and both are pinned here:
      *>   · "specified"  — CALL … USING BY REFERENCE WS-REF ;
      *>   · "or IMPLIED" — CALL … USING WS-BARE, a bare argument, whose mode is BY REFERENCE by default.
      *> The contrast arm is SR4's, which classifies a BY CONTENT identifier-2 as a SENDING operand: the
      *> callee's store must NOT reach the caller.
      *>
      *> DERIVED VALUES. The callee adds 5 to its formal on every activation.
      *>   REF=0015   10 + 5 — the BY REFERENCE store reached the caller's item.
      *>   BARE=0025  20 + 5 — the IMPLIED BY REFERENCE mode did the same.
      *>   CON=0030   30 unchanged — BY CONTENT is a sending operand (SR4), so the callee wrote a
      *>              detached copy and the caller's item is untouched. Were the mode threaded wrongly
      *>              this line would read CON=0035 and the whole classification would be unwitnessed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CBRMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REF  PIC 9(4) VALUE 10.
       01 WS-BARE PIC 9(4) VALUE 20.
       01 WS-CON  PIC 9(4) VALUE 30.
       PROCEDURE DIVISION.
       MAIN.
           CALL "L1CBRSUB" USING BY REFERENCE WS-REF
           CALL "L1CBRSUB" USING WS-BARE
           CALL "L1CBRSUB" USING BY CONTENT WS-CON
           DISPLAY "REF=" WS-REF
           DISPLAY "BARE=" WS-BARE
           DISPLAY "CON=" WS-CON
           STOP RUN.
       END PROGRAM L1CBRMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CBRSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
       P.
           ADD 5 TO LK-N
           EXIT PROGRAM.
       END PROGRAM L1CBRSUB.
