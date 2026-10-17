*>--- conformance-case
*> id: pb660-duplicate-outermost-externalized-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.3.2.2 - two OUTERMOST program definitions in one compilation group externalize one name.
*> The clause states the rule twice over one population and both sentences bite here: "Within a run unit, all
*> instances of a given name that is externalized to the operating environment shall identify the same kind of
*> entity or item. Except for method-names and property-names, when two or more source elements identify
*> something with the same externalized name, they refer to the same instance." - two distinct program
*> definitions cannot be ONE instance, and list item 1 of the same clause is what makes an outermost
*> program-name externalized at all ("program-names of outermost programs ... and user-function-names").
*> BEFORE kb/Work PB660 this compiled, exited 0 and ran the SECOND definition, while the AS-phrase twin ran the
*> FIRST: BuildProgramDefinitionTable's TryAdd dropped the loser and the registrar emitted
*> ProgramRegistry.Register("P3DND", ...) TWICE under one path. Order-dependent, and silent.
*> No AS phrase is written, so the externalized name IS the declared word (8.3.2.2: "For any externalized
*> user-defined words for which the AS phrase is not specified, the implementor defines the mapping between the
*> user-defined word and the corresponding name that is externalized"); this implementation maps it to itself,
*> case-insensitively, the same comparison ProgramTable.NameEquals resolves a CALL with.
*> Every edition rejects: 8.3.2.2 is a language-fundamentals clause with no edition gate, and the program
*> bodies here use only COBOL-85 constructs.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660DUPA.
PROCEDURE DIVISION.
MAIN-A.
    DISPLAY "FIRST".
    EXIT PROGRAM.
END PROGRAM PB660DUPA.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660DUPA.
PROCEDURE DIVISION.
MAIN-B.
    DISPLAY "SECOND".
    EXIT PROGRAM.
END PROGRAM PB660DUPA.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660DUPM.
PROCEDURE DIVISION.
MAIN-M.
    CALL "PB660DUPA".
    STOP RUN.
END PROGRAM PB660DUPM.
