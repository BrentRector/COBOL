*>--- conformance-case
*> id: pb489-linage-counter-receiving
*> rules: [GR-13.18.34.4-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO §8.4.3.14.3 SR2 — "The LINAGE-COUNTER identifier shall not be referenced as a receiving operand."
*> §13.18.34.4 GR7 b) gives the reason: "only the input-output control system may change the value of
*> LINAGE-COUNTER". This is a permanent property of the language, so every edition rejects it.
*> ⛔ THE OUTCOME WAS ALREADY RIGHT AND THE SENTENCE WAS WRONG (kb/Work PB489). The receiving chokepoint
*> screened LINE-COUNTER with a rule-citing rejection and PAGE-COUNTER with a correctly-labelled
*> not-yet-implemented, while LINAGE-COUNTER — the only one of the three that is flatly illegal as a receiver —
*> had no arm and inherited the catch-all, so this source was refused as "a reference shape COBOL.NET does not
*> yet implement as a receiver" (COBOLNET0899). A user reads that as a promise and files a request; a future
*> implementer reads it as a gap to close and would make illegal source compile. The rule that forbids it was
*> named nowhere in the compiler.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n5.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 9 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE 3 TO LINAGE-COUNTER.
           CLOSE LPF.
           STOP RUN.
