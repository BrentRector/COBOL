*>--- conformance-case
*> id: pb660-duplicate-contained-program-name
*> rules: [SR-11.10.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.4.6.3 - two programs contained within ONE outermost program share a program-name: "The
*> names assigned to programs that are contained directly or indirectly within the same outermost program
*> shall be unique within that outermost program."
*> This is the CONTAINED half of the pair whose OUTERMOST half is
*> pb660-duplicate-outermost-externalized-name.cob, and it is a SEPARATE clause and a separate code because a
*> containee's name is not externalized at all - 8.3.2.2's list item 1 externalizes "program-names of
*> OUTERMOST programs" - so its uniqueness is scoped to its outermost program, never to the compilation group.
*> kb/Work PB660 recorded this half as "appears enforced"; RE-MEASURED on 2026-09-22 it was not: the program
*> below compiled, exited 0 and ran the FIRST containee. The positive control for the same scope is
*> conformance-suite/cases/_unassigned/pb660_definition_name_scopes.cob, where two DIFFERENT outermost programs each
*> contain a program of one name and both run - which 8.4.6.3 rules 1 and 2 keep apart at every reference.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660CTM.
PROCEDURE DIVISION.
MAIN-M.
    CALL "PB660CTD".
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660CTD.
PROCEDURE DIVISION.
MAIN-1.
    DISPLAY "C-FIRST".
    EXIT PROGRAM.
END PROGRAM PB660CTD.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660CTD.
PROCEDURE DIVISION.
MAIN-2.
    DISPLAY "C-SECOND".
    EXIT PROGRAM.
END PROGRAM PB660CTD.
END PROGRAM PB660CTM.
