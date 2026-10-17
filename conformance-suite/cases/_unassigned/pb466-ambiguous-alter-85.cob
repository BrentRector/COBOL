*>--- conformance-case
*> id: pb466-ambiguous-alter-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - the ALTER operand positions, at the only edition that still has them (ALTER and the
*> target-less GO TO are deleted by ISO 2002, and the construct row refuses them at 2002 and above, so 85 is
*> where this arm can be measured at all). ISO 14.9.4.2 prints procedure-name-1 and procedure-name-2, and
*> 8.4.2.2.1's uniqueness requirement is a rule about REFERENCES, not about one statement - "uniqueness shall
*> be established through qualification for each user-defined name explicitly referenced" - so both operands
*> owe it and neither has it here: S-REF declares neither DUP-GO nor DUP-DEST, and each is declared in S-ONE
*> and again in S-TWO.
*> This is the arm where the silence cost the most: ALTER rewrites the destination of a GO TO that some later
*> statement will execute, so an arbitrary pick for procedure-name-1 alters the wrong paragraph and an
*> arbitrary pick for procedure-name-2 sends it to the wrong place - two unrelated wrong answers from one
*> unreported reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466ALT85.
       PROCEDURE DIVISION.
       S-REF SECTION.
       P-REF.
           DISPLAY "START".
           ALTER DUP-GO TO PROCEED TO DUP-DEST.
           STOP RUN.
       S-ONE SECTION.
       DUP-GO.
           GO TO DUP-DEST.
       DUP-DEST.
           DISPLAY "ONE-DEST".
           STOP RUN.
       S-TWO SECTION.
       DUP-GO.
           GO TO DUP-DEST.
       DUP-DEST.
           DISPLAY "TWO-DEST".
           STOP RUN.
