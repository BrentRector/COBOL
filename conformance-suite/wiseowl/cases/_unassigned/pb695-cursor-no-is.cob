*>--- conformance-case
*> id: pb695-cursor-no-is
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORD MUST REACH THE DOCUMENTED REFUSAL, NOT A PARSE ERROR (PB695).
      *> ISO 12.3.7.2 prints the clause as `[ CURSOR IS data-name-1 ]` and printed folio 290 rules
      *> CURSOR alone; IS is absent from that page's whole underline roster, so 5.2.3 makes it an
      *> optional word and `CURSOR CURS-IT` is a conforming spelling. The grammar demanded IS until
      *> PB695 and the spelling was a parse error.
      *> The CURSOR clause is the DECLINED screen module (Annex A.4.2 item 25); 4.2.7 makes non-support
      *> conformant only when DIAGNOSED, and COBOLNET1560 is that diagnosis - the same one the fully
      *> written spelling draws, which is exactly what 8.3.2.4.3 requires of an optional word ("with no
      *> effect on the semantics of the format"). A COBOL0001 here would mean the clause was never
      *> recognized. 8.9 reserves CURSOR from 2002 only, so at COBOL-85 this is an implementor-switch
      *> entry naming a mnemonic and not this clause - MEASURED: the same source compiles clean at
      *> --std 85, which is itself a second witness for the relaxed `switch-name-1 [IS] mnemonic-name-1`
      *> spelling, since the entry is written with its IS omitted too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695CURNOIS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURSOR CURS-IT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CURS-IT PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
