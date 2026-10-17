*>--- conformance-case
*> id: pb466-ambiguous-section-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - a duplicated SECTION-name. ISO 14.4.1: "A procedure-name is a word used to refer to a
*> paragraph or section in the source element in which it occurs", so the 8.4.2.2.1 uniqueness question is
*> asked over BOTH kinds and a section-name collides exactly as a paragraph-name does: rule 1 ("No other name
*> has the identical spelling") is false for S-DUP, and rule 6 is about paragraph-names, so nothing excuses
*> the reference. This arm is worth its own witness because the REPAIR differs: 8.4.2.2.2 format 4 qualifies a
*> paragraph-name by its section-name and offers a section-name no qualifier at all, so the only conforming
*> repair here is a rename - a compiler that answered with "add a qualifier" would be sending the user to
*> write something the standard has no format for.
*> Silently picking the first S-DUP is what makes this worth rejecting rather than tolerating: PERFORM of a
*> section runs "the first statement of its first paragraph through the last statement of its last"
*> (14.9.28.4), so the arbitrary pick decides WHICH BLOCK OF THE PROGRAM RUNS. Rejected at all four editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466SECT.
       PROCEDURE DIVISION.
       S-MAIN SECTION.
       P0.
           DISPLAY "START".
           PERFORM S-DUP.
           STOP RUN.
       S-DUP SECTION.
       P1.
           DISPLAY "FIRST-SECTION".
       S-DUP SECTION.
       P2.
           DISPLAY "SECOND-SECTION".
