*>--- conformance-case
*> id: pb405_exit_paragraph_section_in_inline_perform
*> rules: [GR-14.9.14.4-6, GR-14.9.14.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB405 - EXIT PARAGRAPH and EXIT SECTION written INSIDE an inline PERFORM, at --std 2002
      *> (the edition that introduced the structured-exit statements; the 85 witness is the negative case
      *> conformance-suite/cases/14.9.14-EXIT/pb405-exit-paragraph-in-inline-perform-85.cob, COBOLNET0900).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.14.4 GR6 - "The execution of an EXIT PARAGRAPH statement causes control to be passed to an
      *>                  implicit CONTINUE statement immediately following the last explicit statement of the
      *>                  current paragraph, preceding any return mechanisms for that paragraph."
      *> 14.9.14.4 GR7 - "The execution of an EXIT SECTION statement causes control to be passed to an unnamed
      *>                  empty paragraph immediately following the last paragraph of the current section,
      *>                  preceding any return mechanisms for that section."
      *> GR6/GR7 NOTE  - "The return mechanisms mentioned in the rules for EXIT PARAGRAPH and EXIT SECTION are
      *>                  those associated with language elements such as PERFORM, SORT, and USE." An INLINE
      *>                  PERFORM is NOT one of them - it is a loop written inside the paragraph, not a return
      *>                  mechanism - so an enclosing inline PERFORM has NO standing to intercept either
      *>                  transfer. Each EXIT below therefore leaves the paragraph (GR6) or the section (GR7)
      *>                  outright, and everything after END-PERFORM in that paragraph is skipped.
      *> 14.9.28.4 GR5 - the out-of-line PERFORMs of S3 and P4 return where GR6/GR7 place the return mechanism:
      *>                  after the paragraph's implicit CONTINUE (P4) and after the section's unnamed empty
      *>                  paragraph (S3), so both PERFORMs resume at the statement after them.
      *>
      *> S1-A: I=1 prints, I=2 prints then EXITs the PARAGRAPH => A-TAIL skipped, control falls into S1-B.
      *> S1-B: prints B, I=1/I=2 print, then EXIT SECTION => B-TAIL AND the whole of S1-C skipped, control
      *>       passes to the end of S1 and thence into S2.
      *> S3-A: same EXIT SECTION, but the section is under an out-of-line PERFORM, so the section's return
      *>       mechanism fires: S3-TAIL and S3-B are skipped and control returns to AFTER-S3.
      *> P4  : EXIT PARAGRAPH under an out-of-line PERFORM: P4-TAIL is skipped and the PERFORM returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB405XFER02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1 SECTION.
       S1-A.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "A-IT " I
               IF I = 2
                   EXIT PARAGRAPH
               END-IF
           END-PERFORM
           DISPLAY "A-TAIL".
       S1-B.
           DISPLAY "B"
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "B-IT " I
               IF I = 2
                   EXIT SECTION
               END-IF
           END-PERFORM
           DISPLAY "B-TAIL".
       S1-C.
           DISPLAY "C".
       S2 SECTION.
       S2-A.
           PERFORM S3
           DISPLAY "AFTER-S3"
           PERFORM P4
           DISPLAY "AFTER-P4"
           STOP RUN.
       S3 SECTION.
       S3-A.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "S3-IT " I
               IF I = 2
                   EXIT SECTION
               END-IF
           END-PERFORM
           DISPLAY "S3-TAIL".
       S3-B.
           DISPLAY "S3-B".
       S4 SECTION.
       P4.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "P4-IT " I
               IF I = 2
                   EXIT PARAGRAPH
               END-IF
           END-PERFORM
           DISPLAY "P4-TAIL".
