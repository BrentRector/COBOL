*>--- conformance-case
*> id: pb466-ambiguous-procedure-name
*> rules: [SR-8.4.2.2.1-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - an unqualified paragraph-name that more than one section declares. ISO 8.4.2.2.1:
*> "Identical user-defined names may be specified in a source unit; however, uniqueness shall be established
*> through qualification for each user-defined name explicitly referenced, except as specified in rules 2
*> through 6." Rule 1 ("No other name has the identical spelling") is false for DUP-A and DUP-B, and rule 6
*> ("The name is a paragraph-name and the section containing the reference also contains the named paragraph")
*> does not reach S-REF, which declares neither. 8.4.2.2.3 SR1 then requires "a sequence of qualifiers that
*> precludes any ambiguity of reference", and none is written - so every reference below is non-conforming and
*> owes a compile-time diagnostic (4.2.2). Rules 2 through 5 concern REDEFINES, VARYING, type declarations and
*> data-names subordinate to a common group; none reaches a paragraph-name.
*> The three statements are three distinct operand positions of the one resolution step: PERFORM
*> procedure-name-1, PERFORM ... THRU procedure-name-2 (written with an UNAMBIGUOUS first operand so the THRU
*> END is what is measured - an ambiguous range end would otherwise let an arbitrary SPAN of the program run),
*> and GO TO format 1. Rejected at all four editions: 8.4.2.2.1 is unchanged across them.
*> The legal counterparts - rule 6 resolution and an explicit IN qualifier - are the positive witness
*> tests/conformance/85/pb466_procedure_name_uniqueness.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466AMBG.
       PROCEDURE DIVISION.
       S-REF SECTION.
       P-REF.
           DISPLAY "START".
           PERFORM DUP-A.
           PERFORM UNIQ-A THRU DUP-B.
           GO TO DUP-B.
       S-ONE SECTION.
       DUP-A.
           DISPLAY "ONE-A".
       UNIQ-A.
           DISPLAY "ONE-UNIQ".
       DUP-B.
           DISPLAY "ONE-B".
           STOP RUN.
       S-TWO SECTION.
       DUP-A.
           DISPLAY "TWO-A".
       DUP-B.
           DISPLAY "TWO-B".
           STOP RUN.
