*>--- conformance-case
*> id: pb396-evaluate-when-other-repeated
*> rules: [FMT-14.9.13.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB396. ISO 14.9.13.2's general format (MEASURED off the PDF, page 648, printed folio 618) is
*>     EVALUATE selection-subject [ ALSO selection-subject ] ...
*>     { { WHEN selection-object [ ALSO selection-object ] ... } ... imperative-statement-1 } ...
*>     [ WHEN OTHER imperative-statement-2 ]
*>     [ END-EVALUATE ]
*> `[ WHEN OTHER imperative-statement-2 ]` is ONE bracketed phrase that FOLLOWS the repetition: ISO 5.2.7 scopes
*> an ellipsis to "the portion of the format between the determined pair of delimiters" - the brace group
*> immediately to its left - so the OTHER phrase is outside it and is admitted at most once, and last.
*> 14.9.13.4 GR5 b) is written for exactly one such phrase ("If no WHEN phrase is selected and a WHEN OTHER
*> phrase is specified, execution continues with imperative-statement-2") and says nothing about two.
*> The rule is edition-INDEPENDENT. Until PB396 WHEN OTHER was a second ALTERNATIVE of the repeated clause, so
*> it was admissible any number of times at any position and EvaluateBinder's `other = body` overwrote the
*> earlier one: SECOND-OTHER below silently replaced FIRST-OTHER, whose DISPLAY was never emitted and never
*> reported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB396EVALWHENOTHERREPEATED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE X
               WHEN 3
                   DISPLAY "THREE"
               WHEN OTHER
                   DISPLAY "FIRST-OTHER"
               WHEN OTHER
                   DISPLAY "SECOND-OTHER"
           END-EVALUATE
           STOP RUN.
