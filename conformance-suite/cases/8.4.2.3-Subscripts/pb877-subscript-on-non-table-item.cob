*>--- conformance-case
*> id: pb877-subscript-on-non-table-item
*> rules: [SR-8.4.2.3.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB877 - ISO 8.4.2.3.3 SR2, the NEGATIVE half of the rule whose POSITIVE half
*> conformance-suite/cases/8.4.2.3-Subscripts/pb877_subscripted_subscript.cob witnesses.
*>
*> SR2: "If a subscript is specified, the data description entry describing qualified-data-name-1 or the
*> conditional variable associated with qualified-condition-name-1 shall contain an OCCURS clause or shall
*> be subordinate to a data description entry that contains an OCCURS clause." PLAIN below satisfies
*> NEITHER half - it carries no OCCURS clause and no ancestor of it does - so no subscript may be written
*> on it, at any edition. Nothing in 8.4.2.3.3 SR5's seven exceptions is about writing a subscript; SR5 is
*> about OMITTING one.
*>
*> WHY THE CODE MATTERS AND NOT MERELY THE REJECTION. Before PB877 this drew COBOLNET0899, "a reference
*> shape COBOL.NET does not yet implement as a receiver" - a PROMISE, about source no edition of the
*> standard will ever admit - and the identical reference on the SENDING side compiled clean and aborted at
*> run time, where 4.2.2 requires a compile-time mechanism. COBOLNET2096 names the rule on both sides.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB877NONTABLE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
         02 G2          OCCURS 3.
           03 X         PIC 9.
       01 PLAIN         PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           MOVE 1 TO PLAIN (1)
           STOP RUN.
