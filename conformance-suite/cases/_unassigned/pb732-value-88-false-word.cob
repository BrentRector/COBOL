*>--- conformance-case
*> id: pb732-value-88-false-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 3's SECOND literal position, `[ WHEN SET TO FALSE IS literal-4 ]`.
*> ISO 13.18.63.2 format 3 prints literal-4, so the operand is a literal position exactly like
*> literal-2, and an undefined word there identifies no resource (8.4.2.1) — COBOLNET1639, the same
*> code and the same verdict the singleton and THRU operands draw.
*> Pre-fix the grammar wrote this operand as `literal`, so the word died as a PARSE error
*> (COBOL0001 + COBOL0309 "A literal value is expected here, not a data-name") — which also refused
*> the CONFORMING constant-name and symbolic-character spellings 13.10.3 SR2 and 8.3.3.6.2 format 7
*> admit there. Widening the operand and screening it are ONE change; this negative pins the screen
*> and conformance-suite/cases/_unassigned/pb732_value_word_substitutions.cob pins the widening.
*> Rejected at 85 as well, and this case pins that the OPERAND SCREEN still fires there: since
*> kb/Work PB555 the phrase also draws the COBOLNET0900 introduction diagnostic at 85 (it is a
*> COBOL-2002 addition, row value-false-phrase-2002), and a gate that SHORT-CIRCUITED the screen
*> would leave the undefined word unreported. The .err pins COBOLNET1639's text, so it fails if the
*> introduction gate ever swallows the screen.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732J.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC 9 VALUE 1.
   88 A-IS-ONE VALUE 1 WHEN SET TO FALSE IS NOSUCHW.
PROCEDURE DIVISION.
    IF A-IS-ONE DISPLAY "Y" ELSE DISPLAY "N" END-IF.
    STOP RUN.
