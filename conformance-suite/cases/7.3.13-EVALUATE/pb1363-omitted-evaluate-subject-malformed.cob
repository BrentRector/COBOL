*>--- conformance-case
*> id: pb1363-omitted-evaluate-subject-malformed
*> rules: [SR-7.3.13.3-15, FMT-7.3.13.2]
*> source-format: free
*> portable: unverified
*>---
*> 7.2.1 requires compiler directives to be syntactically correct in the initial source text, and the false path of an
*> IF directive is part of that text (7.2.1 Step 1: the lines of a false path "may be omitted from the expanded compilation
*> group", which permits not INCORPORATING them, never accepting a malformed directive among them). kb/Work PB1363. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N16.
       PROCEDURE DIVISION.
       >>IF 1 = 2
       >>EVALUATE 1 +
       >>WHEN 1
           DISPLAY "A".
       >>END-EVALUATE
       >>END-IF
           STOP RUN.
