      *> reject-at: 85 2002 2014 2023
      *> ISO §12.3.8.3 SR10 / §12.3.8.4 GR11 - a CLASS definition's
      *> REPOSITORY names a user-defined function that the compilation
      *> group neither defines nor prototypes (kb/Work PB1100). The
      *> class REPOSITORY now reaches the method (§12.3.4 GR1), so the
      *> method's reference is resolved - and there is nothing to
      *> resolve it to: COBOLNET1505, the program unit's own verdict.
      *> RULE §12.3.8.3 SR10: literal-5 or function-prototype-name-1
      *>   "shall be one of the following" - a function prototype of
      *>   this group, a function definition specified previously in
      *>   it, or a function for which information exists in the
      *>   external repository (this implementation holds none).
      *>   cite.py --check 12.3.8.3 "or function-prototype-name-1, if
      *>   literal-5 is not specified, shall be one of the following"
      *>   -> OK §12.3.8.3 10) (positive twin:
      *>   2002/pb1100_class_repository_function_program).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1100NP.
       PROCEDURE DIVISION.
       P-MAIN.
           STOP RUN.
       END PROGRAM PB1100NP.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1100NC INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           FUNCTION PB1100NU.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FUSE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION RETURNING R.
       P-MAIN.
           MOVE FUNCTION PB1100NU(1) TO R.
       END METHOD FUSE.
       END FACTORY.
       END CLASS PB1100NC.
