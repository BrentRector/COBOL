      *> ISO §8.4.3.13.4 GR3 — ADDRESS OF PROGRAM program-prototype-name-1 has the characteristics of a
      *> program-pointer RESTRICTED to that prototype, so it may be SET into a restricted program-pointer.
      *> GR3: "When program-prototype-name-1 is specified, the program-address-identifier has the
      *>   characteristics of a program-pointer restricted to program-prototype-name-1."
      *>   cite.py: OK  §8.4.3.13.4 3)  (General rules)
      *> §14.9.39.3 SR22: "If identifier-7 references a restricted program-pointer, identifier-8 shall be the
      *>   predefined address NULL or shall reference a program-pointer and the program-prototypes associated
      *>   with identifier-7 and identifier-8 shall have the same signature."   cite.py: OK  §14.9.39.3 22)
      *> §13.18.60.3 SR19: a restricted program-pointer is declared only in a TYPEDEF (hence RPT / TYPE RPT).
      *>   cite.py: OK  §13.18.60.3 19)
      *> §12.3.8.3 SR14: the REPOSITORY PROGRAM name is "the name of a program prototype specified in this
      *>   compilation group" — L1M7PPA, the IS PROTOTYPE unit that opens the group.  cite.py: OK  §12.3.8.3 14)
      *> §8.4.3.13.4 GR2: "For a COBOL program, the address is that of the outermost program identified by the
      *>   externalized program-name in its PROGRAM-ID paragraph."  cite.py: OK  §8.4.3.13.4 2)
      *>   The prototype's externalized name is "L1M7PPA"; the only program DEFINITION with that externalized
      *>   name is L1M7PPD AS "L1M7PPA", whose LINKAGE/USING matches the prototype's (same signature).
      *> EXPECTED OUTPUT, derived line by line:
      *>   RP-NULL   RP is set to the predefined NULL (SR22's first alternative) and compares equal to NULL.
      *>   RP-SET    GR3 makes the sender restricted to L1M7PPA — the very prototype RP is restricted to — so
      *>             SR22 is met and §14.9.39.4 GR16 stores the address: RP is no longer NULL.
      *>   PA OK     CALL through RP activates the program GR2 identifies (L1M7PPD), which displays "PA " and
      *>             its argument X1 ("OK").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PPA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM L1M7PPA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PPM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1M7PPA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO L1M7PPA.
       01 RP TYPE RPT.
       01 X1 PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
       MAIN-P.
           SET RP TO NULL
           IF RP = NULL
               DISPLAY "RP-NULL"
           ELSE
               DISPLAY "RP-NOTNULL"
           END-IF
           SET RP TO ADDRESS OF PROGRAM L1M7PPA
           IF RP NOT = NULL
               DISPLAY "RP-SET"
           ELSE
               DISPLAY "RP-UNSET"
           END-IF
           CALL RP USING X1
           STOP RUN.
       END PROGRAM L1M7PPM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PPD AS "L1M7PPA".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       P-MAIN.
           DISPLAY "PA " L-X
           GOBACK.
       END PROGRAM L1M7PPD.
