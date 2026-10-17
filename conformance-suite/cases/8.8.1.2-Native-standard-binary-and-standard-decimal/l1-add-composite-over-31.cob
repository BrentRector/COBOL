*>--- conformance-case
*> id: l1-add-composite-over-31
*> rules: [GR-8.8.1.2-7, SR-14.7.7-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.7.7 rule 2 a) - for "ADD, DIVIDE, MULTIPLY, and SUBTRACT statements when native arithmetic is in
*> effect ... the composite of operands shall not contain more than 31 digits", the composite being
*> (rule 2) "a hypothetical data item resulting from the superimposition of specified operands in a
*> statement aligned on their decimal points" - that sentence is rule 2's OWN trailing paragraph, indented
*> level with a) and b) and defining the term for both, NOT part of b); cite.py --check answers "2) b)"
*> only because its ordinal tracker carries the last seen letter into an unlettered trailing paragraph.
*> 14.9.2.3 SR1 a) determines ADD format 1's composite "by using all of the operands in the statement" -
*> identifier-2 is both an operand and the receiver, so the receiving item is superimposed too.
*> L1DA is 18 integer digits; L1DB is 1 integer + 17 fraction digits; aligned on
*> the decimal point that is 18 + 17 = 35 digits, over the 31-digit cap.  Native arithmetic is in effect (no
*> OPTIONS paragraph, 8.8.1.3), which is the condition rule 2 states.
*> THIS IS THE ARMED COMPLEMENT of conformance:2023/l1_compute_composite_exempt: the SAME two items are legal
*> in a COMPUTE, because 8.8.1.2 rule 7 exempts arithmetic expressions from exactly this restriction.  Without
*> this fixture the positive golden would only show that some COMPUTE compiles, not that the restriction it is
*> exempt from is real and enforced.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1CMP02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 L1DA PIC 9(18)   VALUE 7.
01 L1DB PIC 9V9(17) VALUE 0.5.
PROCEDURE DIVISION.
MAIN.
    ADD L1DA TO L1DB.
    STOP RUN.
