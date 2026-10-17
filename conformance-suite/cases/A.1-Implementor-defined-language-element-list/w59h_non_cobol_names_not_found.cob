*>--- conformance-case
*> id: w59h_non_cobol_names_not_found
*> rules: [DOC-A.1-13, DOC-A.1-15, DOC-A.1-88, DOC-A.1-142, DOC-A.1-143, DOC-A.1-217]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1536 Q1 / R43 item 3 - Annex A.1 items 13, 15, 88 and 142
      *> (and the optional item 143) are CONDITIONALLY REQUIRED on support for
      *> non-COBOL programs and functions, and COBOL.NET provides none:
      *> docs/CONFORMANCE.md section 7 records each as "Condition absent.".
      *>   cite.py --check A.1 "Conditionally required: If the associated
      *>     feature or language element is implemented then this element
      *>     is also required." -> OK A.1
      *> THIS PROGRAM IS THE WITNESS that the condition really is absent: a
      *> name that spells a real NON-COBOL module deployed beside every
      *> COBOL.NET program - the runtime assembly Cobol.Net.Runtime.dll - is
      *> never located, never called and never given an address.
      *>   cite.py --check 14.9.4.4 "If the program being called is not
      *>     a COBOL program, the rules for program-name formation and for
      *>     locating the program are defined by the implementor." -> OK
      *>     14.9.4.4 3)
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 14.9.4.4 3)
      *>   cite.py --check 8.4.3.13.4 "If the runtime system cannot locate
      *>     the program, the EC-PROGRAM-NOT-FOUND exception condition is
      *>     set to exist and the value of the address-identifier is the
      *>     predefined address NULL." -> OK 8.4.3.13.4 4)
      *>   cite.py --check 8.4.3.2.4 "If the function is not found, the
      *>     EC-FUNCTION-NOT-FOUND exception condition is set to exist"
      *>     -> OK 8.4.3.2.4 6)
      *> DERIVATION of every .out line:
      *>   CALL-NOT-FOUND  items 13/15: the program cannot be located, so
      *>                   EC-PROGRAM-NOT-FOUND exists and the ON EXCEPTION
      *>                   phrase runs (14.9.4.4 GR3 b), h)).
      *>   PP-NULL         item 142: ADDRESS OF PROGRAM cannot locate it,
      *>                   so the program-pointer is NULL (8.4.3.13.4 GR4).
      *>   FN-NOT-FOUND    item 88: the prototype's externalized name spells
      *>                   the same non-COBOL module; the function is not
      *>                   found, EC-FUNCTION-NOT-FOUND (enabled by the
      *>                   TURN) runs its declarative, which resumes at the
      *>                   statement after the MOVE.
      *>   Y=7             so the MOVE never stores: Y keeps VALUE 7.
      *> 2002 dir: prototypes, >>TURN, RESUME and ADDRESS OF PROGRAM are 2002.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W59HFN AS "Cobol.Net.Runtime" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
       END FUNCTION W59HFN.
       >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HNCOB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W59HFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 Y  PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HF SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
       HF-P.
           DISPLAY "FN-NOT-FOUND".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           CALL "Cobol.Net.Runtime"
               ON EXCEPTION DISPLAY "CALL-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "CALL-FOUND"
           END-CALL
           SET PP TO ADDRESS OF PROGRAM "Cobol.Net.Runtime"
           IF PP = NULL
               DISPLAY "PP-NULL"
           ELSE
               DISPLAY "PP-SET"
           END-IF
           MOVE FUNCTION W59HFN TO Y
           DISPLAY "Y=" Y
           STOP RUN.
       END PROGRAM W59HNCOB.
