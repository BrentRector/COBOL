*>--- conformance-case
*> id: pb415-initialize-category-repeated-word
*> rules: [SR-14.9.20.3-6, FMT-14.9.20.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB415 — ISO 5.2.6.4 (choice indicators): "one or more of the alternatives contained within the
*> choice indicators shall be specified, but any single alternative shall be specified only once", and
*> 14.9.20.3 SR6: "The same category shall not be repeated in a REPLACING phrase." The two rules are the
*> same ban at two scopes — within ONE category-name, and across the phrase's items — and this program
*> breaks the first, which only became expressible when category-name became a SET.
*> The compiler evaluates both against ONE accumulator, so `NUMERIC NUMERIC` here and `NUMERIC … NUMERIC`
*> in two separate REPLACING items are diagnosed identically (COBOLNET0834).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NREP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N1 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING NUMERIC NUMERIC DATA BY 7.
           STOP RUN.
