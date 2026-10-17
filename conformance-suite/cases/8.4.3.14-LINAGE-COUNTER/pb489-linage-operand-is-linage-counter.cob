*>--- conformance-case
*> id: pb489-linage-operand-is-linage-counter
*> rules: [SR-8.4.3.14.3-1, GR-13.18.34.4-7, FMT-13.18.34.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO §8.4.3.14.3 SR1 — "LINAGE-COUNTER may be referenced only in procedure division statements." A file
*> description entry is not a procedure division statement, so `FD LPG LINAGE IS LINAGE-COUNTER OF LPF LINES`
*> is not a legal reference to the register. §13.18.34.4 GR7 b) says the same thing from the other side, and
*> the general format agrees independently: §13.18.34.2 prints data-name-1, a qualified-data-name
*> (§8.4.2.2.2 Format 1), while LINAGE-COUNTER is an IDENTIFIER — §8.4.3.1 Format 10, qualified-linage-counter.
*> ⛔ THIS COMPILED CLEAN until kb/Work PB489 and died at OPEN with "LINAGE operand 'LPF' is not resolvable to
*> storage": the binder kept the FIRST cobolWord of the written reference, which for the register alternative
*> is the FILE-NAME QUALIFIER, so the diagnostic named a word the programmer never wrote as a data item. The
*> register and its placement rule are present in all four supported editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n2a.prt".
           SELECT LPG ASSIGN TO "pb489n2b.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 9 LINES.
       01 F-REC PIC X(4).
       FD LPG LINAGE IS LINAGE-COUNTER OF LPF LINES.
       01 G-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           OPEN OUTPUT LPG.
           CLOSE LPF.
           CLOSE LPG.
           STOP RUN.
