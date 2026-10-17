*>--- conformance-case
*> id: l1c23_program_address_outermost
*> rules: [GR-8.4.3.13.4-2, DOC-A.1-217]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.13.4 2) — ADDRESS OF PROGRAM names the OUTERMOST program by its EXTERNALIZED name
      *> Rule: "For a COBOL program, the address is that of the outermost program identified by the
      *>   externalized program-name in its PROGRAM-ID paragraph."
      *>   cite.py: OK  §8.4.3.13.4 2)  (General rules)
      *> Supporting rules:
      *>   §8.4.3.13.4 4) "If the runtime system cannot locate the program, the EC-PROGRAM-NOT-FOUND
      *>   exception condition is set to exist and the value of the address-identifier is the predefined
      *>   address NULL."            cite.py: OK  §8.4.3.13.4 4)  (General rules)
      *>   §8.3.2.2 "For any externalized user-defined words for which the AS phrase is specified, the
      *>   content of the literal specified in that AS phrase is a name that is externalized to the
      *>   operating environment."   cite.py: OK  §8.3.2.2 2)  (User-defined words)
      *>   §8.3.2.2 2) a) (program-address-identifier naming a program) "the program-name is treated as a
      *>   COBOL word that maps to the externalized name of the program"
      *>                             cite.py: OK  §8.3.2.2 2) a)  (User-defined words)
      *> EC-PROGRAM-NOT-FOUND is not enabled (no >>TURN), so a not-located program only yields NULL.
      *> EXPECTED OUTPUT, derived:
      *>   OUTER-S   "L1C23S" names an outermost program (externalized name = its program-name, no AS):
      *>             located, PP is its address, CALL PP runs it.
      *>   NESTED    a direct CALL "L1C23N" from the containing program reaches the contained program,
      *>             so the name is real and in scope here.
      *>   N-NULL    L1C23N is CONTAINED, not outermost; GR2 takes only an outermost program, so no
      *>             program is located and GR4 makes the value NULL.
      *>   EXT-P     "L1C23X" is the externalized name (AS phrase) of outermost L1C23P: located.
      *>   P-NULL    "L1C23P" is the SOURCE word of that program, not its externalized name; no outermost
      *>             program is externalized as L1C23P, so GR4 gives NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM "L1C23S"
           IF PP = NULL
               DISPLAY "S-NULL"
           ELSE
               CALL PP
           END-IF
           CALL "L1C23N"
           SET PP TO ADDRESS OF PROGRAM "L1C23N"
           IF PP = NULL
               DISPLAY "N-NULL"
           ELSE
               CALL PP
           END-IF
           SET PP TO ADDRESS OF PROGRAM "L1C23X"
           IF PP = NULL
               DISPLAY "X-NULL"
           ELSE
               CALL PP
           END-IF
           SET PP TO ADDRESS OF PROGRAM "L1C23P"
           IF PP = NULL
               DISPLAY "P-NULL"
           ELSE
               CALL PP
           END-IF
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23N.
       PROCEDURE DIVISION.
           DISPLAY "NESTED"
           GOBACK.
       END PROGRAM L1C23N.
       END PROGRAM L1C23A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23S.
       PROCEDURE DIVISION.
           DISPLAY "OUTER-S"
           GOBACK.
       END PROGRAM L1C23S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23P AS "L1C23X".
       PROCEDURE DIVISION.
           DISPLAY "EXT-P"
           GOBACK.
       END PROGRAM L1C23P.
