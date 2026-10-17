*>--- conformance-case
*> id: pb712-write-two-advancing-operands
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.51.2 Format 1 prints ONE `ADVANCING` and ONE operand. MEASURED off the PDF (page 815, printed
*> folio 785): the choice indicators — the | bars, enclosed by braces — enclose ONLY the words BEFORE and
*> AFTER; ADVANCING and the whole operand group sit OUTSIDE them, inside the single bracket. §5.2.6.4 lets
*> each enclosed alternative appear at most once and in any order, so the format spells BEFORE …, AFTER …,
*> BEFORE AFTER … and AFTER BEFORE … — and nothing else. A SECOND advancing phrase with its OWN operand is
*> not a spelling of this statement in ANY edition, so every edition rejects it.
*> ⛔ This exact statement COMPILED AND RAN until kb/Work PB712, in a private dialect no other implementation
*> accepts and §14.9.51.4 GR25 a)–d) cannot interpret (they define ONE advance from ONE operand), and it
*> silently discarded the end-of-page condition the first advance raised (§14.9.51.4 GR27 b), the kb/Work
*> PB686 observation). The COBOL-2023 edition gate fired correctly on the second phrase the whole time,
*> which is exactly what kept the over-acceptance invisible: the EDITION was checked, so the SPELLING was not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB712NEG1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb712neg1.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 4 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC BEFORE ADVANCING 4 LINES AFTER ADVANCING 0 LINES.
           CLOSE LPF.
           STOP RUN.
