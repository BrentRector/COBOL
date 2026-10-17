*>--- conformance-case
*> id: pb3164-method-duplicate-constant-unequal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3164 - ISO 13.10.3 SR9: "If constant-name-1 duplicates
      *>   another constant-name, the specification of
      *>   arithmetic-expression-1, literal-1, data-name-1, data-name-2,
      *>   or compilation-variable-name-1 shall be the same as specified
      *>   in the other constant-name". A method's constant entries are
      *>   its own (a method is a source element of its own, 3.164 and
      *>   8.4.6.1), so a SIBLING method may declare K AS "B" beside this
      *>   method's K AS "A" (golden 2002/pb3164_method_constant_scope);
      *>   but two entries of K inside ONE method are a duplicated
      *>   constant-name of one source element with unequal
      *>   specifications, refused COBOLNET1547 at the second entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3164N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3164T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3164T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3164T "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3164N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3164T INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 K CONSTANT AS "A".
       PROCEDURE DIVISION.
           DISPLAY "A " K.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 K CONSTANT AS "B".
       01 K CONSTANT AS "C".
       PROCEDURE DIVISION.
           DISPLAY "B " K.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3164T.
