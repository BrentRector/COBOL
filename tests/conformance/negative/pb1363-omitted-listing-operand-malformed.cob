*> reject-at: 2002 2014 2023
*> 7.2.1 requires compiler directives to be syntactically correct in the initial source text, and the false path of an
*> IF directive is part of that text (7.2.1 Step 1: the lines of a false path "may be omitted from the expanded compilation
*> group", which permits not INCORPORATING them, never accepting a malformed directive among them). kb/Work PB1363. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N20.
       PROCEDURE DIVISION.
       >>IF 1 = 2
       >>LISTING GARBAGE
       >>END-IF
           STOP RUN.
