*>--- conformance-case
*> id: pb412-go-to-names-without-depending
*> rules: [FMT-14.9.17.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB412 — MORE THAN ONE PROCEDURE-NAME AND NO DEPENDING PHRASE. §14.9.17.2 prints exactly two general
*> formats (canonical PDF page 660 / printed folio 630, rendered at 190 dpi because the diagram is
*> load-bearing): Format 1 is `GO TO procedure-name-1` — ONE procedure-name, UNBRACKETED, and no DEPENDING —
*> and Format 2 is `GO TO { procedure-name-1 } … DEPENDING ON identifier-1`, whose DEPENDING is underlined and
*> therefore required (5.2.2). 5.2.6.2 gives the omission licence to BRACKETED portions only, so neither format
*> admits this program. Neither format changed shape across 1985/2002/2014/2023, so it is refused at all four.
*> WHAT THIS WITNESS PINS: until this fix `goToStatement` was the single union
*> `GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?`, this program COMPILED CLEAN at every
*> edition, and the binder's Format-1 arm read names[0] and DISCARDED PARA-B without a word — a program that
*> means nothing under the standard was given a specific behaviour instead of a diagnostic.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB412NEG1.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "START".
    GO TO PARA-A PARA-B.
PARA-A.
    DISPLAY "A".
    STOP RUN.
PARA-B.
    DISPLAY "B".
    STOP RUN.
