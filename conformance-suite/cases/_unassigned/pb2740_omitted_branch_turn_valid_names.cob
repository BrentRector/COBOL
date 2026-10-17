*>--- conformance-case
*> id: pb2740_omitted_branch_turn_valid_names
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 7.3.25.3 SR2 + 7.2.1 (cite.py --check 7.3.25.3 "Exception-name-1 shall be one of the exception names listed in 14.6.13.1" -> OK;
      *> cite.py --check 7.2.1 "syntactically correct in the initial source text and library text" -> OK): the TURN directive in the
      *> OMITTED branch of >>IF 1 = 0 (7.3.16.4 GR3, cite.py OK: text-1 is omitted when the expression is FALSE) names only
      *> exception-names that exist in COBOL-2002 (a level-1 name, a level-3 name, an EC-USER- name, an EC-I-O name with a
      *> file-name), so the omitted-branch check (kb/Work PB2740) must NOT reject it.
      *> DERIVATION. Omitted text is not part of the resultant text, so no checking is enabled: the ADD below overflows PIC 9
      *> (9 + 1), its ON SIZE ERROR phrase runs (it runs whether or not checking is on, 14.7.5) and no exception is raised, so
      *> FUNCTION EXCEPTION-STATUS (15.33.3 r1, cite.py OK) is 31 spaces -> "P1 [<31 spaces>]".
       >>IF 1 = 0
       >>TURN EC-SIZE EC-ALL EC-USER-MINE EC-I-O-AT-END F1 CHECKING ON
       >>END-IF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2740P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       M1.
           ADD 1 TO A
               ON SIZE ERROR
                 DISPLAY "P1 [" FUNCTION EXCEPTION-STATUS "]"
           END-ADD.
           STOP RUN.
