*>--- conformance-case
*> id: pb524-linage-footing-beyond-page
*> rules: [SR-13.18.34.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB524 - ISO 13.18.34.3 SR3: "Integer-2 shall not be greater than integer-1." Both operands are
*> literals, so the relation is known at compile time and is a SYNTAX rule. FOOTING AT 7 on a page of 5 used to
*> compile clean and throw a CLR InvalidOperationException out of the page evaluation at OPEN. The boundary
*> (footing equal to the page size) is legal and is witnessed by conformance-suite/cases/13.18.34-LINAGE/pb524_linage_operand_shapes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB524N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb524n2.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 5 LINES WITH FOOTING AT 7.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
