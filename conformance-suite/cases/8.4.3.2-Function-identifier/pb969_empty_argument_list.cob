*>--- conformance-case
*> id: pb969_empty_argument_list
*> rules: [SR-8.4.3.2.3-2, FMT-8.4.3.2.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.2 prints the function-identifier as
      *>     [ FUNCTION ] { function-pointer-name-1 | function-prototype-name-1 | intrinsic-function-name-1 }
      *>         [ ( [ { argument-1 | OMITTED } ] ... ) ]
      *> - the arguments are bracketed INSIDE the parentheses (PDF p157 / folio 127), so `F()` is the
      *> zero-argument spelling. 8.4.3.2.3 SR2: "If intrinsic-function-name-1 or the ALL phrase is specified
      *> in the REPOSITORY paragraph or if function-prototype-name-1 or function-pointer-name-1 is specified,
      *> the word FUNCTION may be omitted from the function-identifier". kb/Work PB969: with FUNCTION omitted,
      *> `W51P969Z()` died COBOL0001 "cannot parse construct near ')'" while `W51P969Z( )` compiled.
      *>
      *>   Z1  `W51P969Z()` - the user function returns 77 (its PROCEDURE DIVISION RETURNING item) -> Z1=77
      *>   Z2  `FUNCTION W51P969Z ()` - the keyword form, the same function                        -> Z2=77
      *>   Z3  `W51P969Z` - SR2's bare form, the parentheses being optional for a prototype      -> Z3=77
      *>   C   `CURRENT-DATE()(1:2)` - an intrinsic, keyword omitted under FUNCTION ALL INTRINSIC, with
      *>       empty arguments and then a reference modifier (8.4.3.1.4 GR1 f) before g)); 15.21.3 rule 1
      *>       makes positions 1-4 "Four numeric digits of the year in the Gregorian calendar", so the
      *>       first two are the century digits of a date in 2000-2099                          -> C=20
      *>   M   `MAX(RANDOM() 5 3)` - 8.4.3.2.3 SR6's NOTE writes exactly this nesting,
      *>       `FUNCTION MAX (FUNCTION RANDOM () A B)`: the empty group is RANDOM's argument list, so
      *>       MAX has three arguments; 15.75.4 rule 1: RANDOM's value "is greater than or equal to zero
      *>       and less than one", so the maximum is 5 -> M=5
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W51P969Z.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 77 TO L-RES
           GOBACK.
       END FUNCTION W51P969Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W51P969.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W51P969Z
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(9).
       01 N PIC 9.
       01 C PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R = W51P969Z()
           DISPLAY "Z1=" R
           COMPUTE R = FUNCTION W51P969Z ()
           DISPLAY "Z2=" R
           COMPUTE R = W51P969Z
           DISPLAY "Z3=" R
           MOVE CURRENT-DATE()(1:2) TO C
           DISPLAY "C=" C
           COMPUTE N = MAX(RANDOM() 5 3)
           DISPLAY "M=" N
           STOP RUN.
       END PROGRAM W51P969.
