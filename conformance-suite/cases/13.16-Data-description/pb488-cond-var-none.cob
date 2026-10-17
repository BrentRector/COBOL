*>--- conformance-case
*> id: pb488-cond-var-none
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "The condition-name entries for a particular conditional variable
*> shall immediately follow the entry describing the item with which the condition-name is associated."
*>
*> This level-88 entry is the FIRST entry of working-storage, so it follows no entry at all and has no
*> conditional variable. kb/Work PB488: the binder bound an 88 only `if (stack.Count > 0)` and said nothing
*> otherwise, so this compiled clean and threw NotImplementedCobolFeatureException at RUN TIME on IF ORPHAN -
*> a compile-time syntax rule reported, if at all, as a run-time crash.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-NONE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       88 ORPHAN VALUE "AB".
       01 A PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           IF ORPHAN DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
