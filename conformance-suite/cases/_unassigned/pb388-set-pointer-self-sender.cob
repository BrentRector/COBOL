      *>--- conformance-case
      *> id: pb388-set-pointer-self-sender
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.39.2 Format 7 (data-pointer-assignment) — its sending
      *> brace is { NULL | ADDRESS OF identifier-5 | identifier-6 }, and
      *> §14.9.39.3 SR17 makes identifier-6 "of category data-pointer".
      *> SELF is a predefined object reference (§8.4.3.3), admissible as
      *> a sending operand of Format 5 alone, so this statement is
      *> refused whatever the receivers are.
      *>
      *> ⛔ WHAT THIS PINS IS THE MESSAGE, NOT THE REFUSAL (kb/Work
      *> PB388's elision half). The refusal was already right; the text
      *> read `SET … TO SELF/SUPER`, and the diagnostic renderer
      *> transliterates U+2026 to ASCII, so the programmer was shown
      *> `SET . TO SELF/SUPER` — neither the two receivers they wrote
      *> nor the sender they wrote. Both were in the binder's hand:
      *> targetRefs is the receiving list and objRef.GetText() is the
      *> word. The .err beside this file is the whole message, so a
      *> future arm that re-elides either one fails by name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB388-SET-PTR-SELF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P1 USAGE POINTER.
       01 WS-P2 USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-P1 WS-P2 TO SELF.
           GOBACK.
