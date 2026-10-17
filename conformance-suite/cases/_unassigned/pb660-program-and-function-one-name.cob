*>--- conformance-case
*> id: pb660-program-and-function-one-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.3.2.2, FIRST sentence - an outermost PROGRAM definition and a FUNCTION definition in one
*> compilation group externalize the same name: "Within a run unit, all instances of a given name that is
*> externalized to the operating environment shall identify the same kind of entity or item." Its list item 1
*> puts BOTH kinds in the externalized population ("program-names of outermost programs, object-class-names,
*> function-prototype-names, interface-names, method-names, program-prototype-names, property-names, and
*> user-function-names"), so a program and a function under one name are two KINDS under one name.
*> The SAME-kind sentence of the same clause governs two programs or two functions and is pinned by
*> pb660-duplicate-outermost-externalized-name.cob - one clause, one check (COBOLNET2213), two messages.
*> Measured before kb/Work PB660: both registered and both ran (the run-unit table discriminates on a
*> Node.IsFunction flag), so FUNCTION PBSAME returned 0007 and CALL "PBSAME" printed PROGRAM-RAN in one run.
*> reject-at begins at 2002 because a user-defined FUNCTION-ID definition is a COBOL-2002 introduction.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660KM.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB660SAME.
PROCEDURE DIVISION.
MAIN-M.
    DISPLAY "F=" FUNCTION PB660SAME.
    CALL "PB660SAME".
    GOBACK.
END PROGRAM PB660KM.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660SAME.
PROCEDURE DIVISION.
MAIN-P.
    DISPLAY "PROGRAM-RAN".
    GOBACK.
END PROGRAM PB660SAME.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB660SAME.
DATA DIVISION.
LINKAGE SECTION.
01 R PIC 9(4).
PROCEDURE DIVISION RETURNING R.
MAIN-F.
    MOVE 7 TO R.
    GOBACK.
END FUNCTION PB660SAME.
