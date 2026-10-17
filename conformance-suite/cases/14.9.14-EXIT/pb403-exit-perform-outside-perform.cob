*>--- conformance-case
*> id: pb403-exit-perform-outside-perform
*> rules: [SR-14.9.14.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.14.3 SR8, first sentence: "The EXIT PERFORM statement may be specified only in an inline or
*> exception-checking PERFORM statement." This program writes one in an ordinary paragraph with no PERFORM of
*> any kind anywhere in it, so the statement has no PERFORM to exit.
*>
*> WHY THE RULE IS A REJECTION AND NOT A NO-OP. §14.9.14.4 GR5 a) defines the statement's meaning ONLY relative
*> to a matching END-PERFORM -- control passes "to an implicit CONTINUE statement immediately following the
*> END-PERFORM phrase that matches the most closely preceding, and as yet unterminated, inline PERFORM
*> statement" -- so outside one there is nothing for it to mean.
*>
*> WHAT IT COST (kb/Work PB403). Before 2026-09-13 this program compiled CLEAN at every edition from 2002 and
*> HUNG: the emitter's F3Region.None arm wrote a bare C# `break;` into the pc dispatcher's `switch (__pc)`
*> WITHOUT advancing `__pc`, the enclosing `while` re-tested the unchanged pc, and MAIN-PARA ran again forever
*> (measured: `--run` printed "A" until a 10 s timeout killed it). That arm's own comment called itself a
*> "defensive fallback (SR8: never reached for a valid bind)" -- an invariant the binder never established.
*>
*> The 85 arm is its own fixture (pb403-exit-perform-below-2002): EXIT PERFORM does not exist in COBOL-85, so
*> the refusal there is the introduction gate COBOLNET0900 and not this placement rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB403NEG1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           EXIT PERFORM.
           DISPLAY "B".
           STOP RUN.
