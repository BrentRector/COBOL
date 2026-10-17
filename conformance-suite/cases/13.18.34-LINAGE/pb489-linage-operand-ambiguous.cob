*>--- conformance-case
*> id: pb489-linage-operand-ambiguous
*> rules: [GR-13.18.34.4-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO §8.4.2.2.3 SR1 — "For each non unique user-defined name that is explicitly referenced, uniqueness shall
*> be established through a sequence of qualifiers that precludes any ambiguity of reference." §13.18.34.2
*> prints data-name-1, which is a qualified-data-name (§8.4.2.2.2 Format 1), so the LINAGE clause's operand is
*> an explicit reference to a user-defined name and SR1 is in force over it. SZ is declared twice, under two
*> different groups, and the clause writes it unqualified: no sequence of qualifiers precludes the ambiguity
*> because none was written, and the reference identifies no single data item (§8.4.2.1: "a statement shall
*> contain a reference that uniquely identifies that resource").
*> ⛔ THIS COMPILED AND RAN until kb/Work PB489. The resolution was `ByName[n][0]` — the FIRST declaration of
*> that name in source order — so the program silently built its whole logical page on GRP-A's SZ, and moving
*> the same two VALUE clauses between the two groups changed the output with no other edit. The rules are
*> present in all four supported editions, so every edition rejects it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n1.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS SZ LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 GRP-A.
          05 SZ PIC 99 VALUE 3.
       01 GRP-B.
          05 SZ PIC 99 VALUE 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
