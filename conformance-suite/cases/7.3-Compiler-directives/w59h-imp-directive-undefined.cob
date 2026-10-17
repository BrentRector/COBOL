*>--- conformance-case
*> id: w59h-imp-directive-undefined
*> rules: [SR-7.3.3-9, DOC-A.1-30]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1536 Q1 / R43 item 3 - Annex A.1 item 30 (the IMP
      *> directive's syntax and general rules) is conditioned on the
      *> implementor defining an IMP directive, and COBOL.NET defines none:
      *> docs/CONFORMANCE.md section 7 records item 30 as "Condition
      *> absent.".
      *>   cite.py --check 7.3.3 "The compiler-directive word 'IMP' is
      *>     reserved for use by the implementor." -> OK 7.3.3 9)
      *> THIS PROGRAM IS THE WITNESS: a >>IMP line is not a compiler
      *> directive this implementation recognizes (CompilerDirectiveCatalog
      *> has no IMP row), so it is left in the text and refused, at every
      *> edition, as the syntax error it is. Directive at COLUMN 8.
       >>IMP W59H-OPTION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HIMP.
       PROCEDURE DIVISION.
           DISPLAY "IMP-ACCEPTED".
           STOP RUN.
