*>--- conformance-case
*> id: pb695-crt-status-no-is
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORD MUST REACH THE DOCUMENTED REFUSAL, NOT A PARSE ERROR (PB695).
      *> ISO 12.3.7.2 prints the clause as `[ CRT STATUS IS data-name-2 ]` and printed folio 290 carries
      *> underline rectangles under CRT and STATUS only - IS appears nowhere in that page's underline
      *> roster - so 5.2.3 makes IS an optional word here and `CRT STATUS CRT-ST` is a conforming
      *> spelling of the clause. The grammar demanded IS until PB695, so this program used to die as a
      *> parse error at CRT-ST.
      *> WHY THIS IS A NEGATIVE CASE AND NOT A POSITIVE ONE. The CRT STATUS clause belongs to the
      *> DECLINED screen module (Annex A.4.2 item 25), and 4.2.7 makes non-support conformant only when
      *> it is DIAGNOSED - COBOLNET1560 is that diagnosis. So the correct behaviour for the IS-less
      *> spelling is the SAME refusal the fully-written spelling draws, and the diagnostic code is what
      *> proves the clause was recognized rather than mis-parsed: a program the parser could not read
      *> would report COBOL0001, not a screen-facility refusal by name.
      *> 8.9 reserves CRT from 2002 only, so below 2002 this is not the CRT STATUS clause at all: CRT
      *> falls through as an ordinary user word to the implementor-switch entry, whose mnemonic-name
      *> slot the reserved word STATUS cannot fill, and COBOL-85 rejects the line as a syntax error
      *> because that edition has no such clause. reject-at therefore names 2002 and later, where the
      *> refusal is the DIAGNOSED non-support of an optional module rather than a syntax error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695CRTNOIS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CRT STATUS CRT-ST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CRT-ST PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
