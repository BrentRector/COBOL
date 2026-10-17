*>--- conformance-case
*> id: pb396-if-empty-consequent
*> rules: [SR-14.9.19.3-1, FMT-14.9.13.2, FMT-14.9.19.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB396. ISO 14.9.19.2 Format 1 prints `IF condition-1 THEN statement-1 [ ELSE statement-2 ] END-IF`
*> and Format 2 stacks `{ statement-1 | NEXT SENTENCE }` inside BRACES. MEASURED off the PDF (page 665, printed
*> folio 635): statement-1 carries NO bracket in Format 1 and is a brace alternative in Format 2. ISO 5.2.6.2
*> gives the omission licence to BRACKETED portions only and 5.2.6.3 requires one brace alternative to be
*> explicitly specified, so statement-1 is REQUIRED in both formats; 14.9.19.3 SR1 states the same cardinality a
*> second way - "Statement-1 and statement-2 represent either one or more imperative statements or a conditional
*> statement optionally preceded by one or more imperative statements".
*> The rule is edition-INDEPENDENT: every edition since 1985 prints the same unbracketed operand, so every
*> edition rejects. Until PB396 the grammar wrote `statementBlock*` at this position and an IF with an empty
*> consequent compiled to an empty C# block in SILENCE at every --std.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB396IFEMPTYCONSEQUENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           IF X = 1
           END-IF
           DISPLAY "AFTER"
           STOP RUN.
