*>--- conformance-case
*> id: pb488-cond-var-after-constant-entry
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "The condition-name entries for a particular conditional variable
*> shall immediately follow the entry describing the item with which the condition-name is associated."
*>
*> A CONSTANT entry (13.10) is a compile-time substitution and describes NO data item, so the 88 written
*> after one follows no entry describing an item. kb/Work PB488: the old `stack.Peek()` reader stepped over
*> the constant entry exactly as it stepped over a level-66 alias and silently associated ORPHAN with A.
*> The CONSTANT entry is a COBOL-2002 addition, so 85 rejects it at its own introduction gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-AFTER-CONSTANT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(2) VALUE "AB".
       01 K CONSTANT AS 3.
       88 ORPHAN VALUE "AB".
       PROCEDURE DIVISION.
           IF ORPHAN DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
