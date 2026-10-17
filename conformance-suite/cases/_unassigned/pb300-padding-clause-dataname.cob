*>--- conformance-case
*> id: pb300-padding-clause-dataname
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 12.4.5 - the file control entry's clauses are 12.4.5.4 through 12.4.5.15 and
*> NONE of them is PADDING; the word occurs nowhere in the 2023 text. The clause is the
*> ANSI X3.23-1985 Sequential I-O block-fill character, deleted from the standard, so at
*> 2014 and 2023 it is a removed construct: COBOLNET0902 under strict, a warning with the
*> pre-removal no-op reading under --permissive (registry row padding-character-removed-2014).
*>
*> THIS FIXTURE EXISTS FOR THE OPERAND ARM THE MATRIX ROW DOES NOT WRITE. The matrix row
*> spells the clause with all its optional words - PADDING CHARACTER IS <literal>. This one
*> writes NIST SQ217A's shape instead: the bare verb, no CHARACTER, no IS, and a DATA-NAME
*> operand rather than a literal. Both arms of `PADDING CHARACTER? IS? (literal|dataReference)`
*> therefore have a witness, and this arm additionally measures that admitting PADDING to
*> cobolWord at 2014/2023 (kb/Work PB300's other half) did not let a user-word reading absorb
*> the clause and silence its gate - the exact failure a name-slot-only fix would have caused.
 IDENTIFICATION DIVISION.
 PROGRAM-ID. PB300-PAD-CLAUSE-DN.
 ENVIRONMENT DIVISION.
 INPUT-OUTPUT SECTION.
 FILE-CONTROL.
     SELECT PB300-F ASSIGN TO "pb300pad" ORGANIZATION IS SEQUENTIAL
         PADDING PB300-PAD-CHAR.
 DATA DIVISION.
 FILE SECTION.
 FD PB300-F.
 01 PB300-REC PIC X(10).
 WORKING-STORAGE SECTION.
 01 PB300-PAD-CHAR PIC X VALUE "Z".
 PROCEDURE DIVISION.
 MAIN-PARA.
     DISPLAY "UNREACHABLE"
     STOP RUN.
