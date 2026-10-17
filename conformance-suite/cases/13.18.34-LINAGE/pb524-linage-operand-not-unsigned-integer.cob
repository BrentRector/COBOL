*>--- conformance-case
*> id: pb524-linage-operand-not-unsigned-integer
*> rules: [SR-13.18.34.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB524 - ISO 13.18.34.3 SR2: "Data-name-1, data-name-2, data-name-3, and data-name-4 shall reference
*> elementary unsigned numeric integer data items." PG-SIZE is ALPHANUMERIC, so this is not a LINAGE clause of
*> any edition. It used to compile clean and run: the logical page was built from the characters "007" read as
*> the number 7, a silent reinterpretation of illegal source (the emitter ASSUMED SR2 and nothing enforced it).
*> The same screen names the other failing conjuncts - a group, a signed, a fractional, a floating-point or an
*> index operand - each by the word of SR2 it breaks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB524N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb524n1.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS PG-SIZE LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 PG-SIZE PIC X(3) VALUE "007".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
