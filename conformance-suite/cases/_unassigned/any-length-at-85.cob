*>--- conformance-case
*> id: any-length-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.2 - the ANY LENGTH clause is a COBOL-2002 introduction: at --std 85 the
*> version-conformance pass rejects it (COBOLNET0900, registry row any-length-clause-2002). The fixture
*> is the SR2-legal contained-program shape whose ONLY 2002+ construct is ANY LENGTH (it compiles at
*> 2002+ - the version-matrix row pins that half).
IDENTIFICATION DIVISION.
PROGRAM-ID. ALN85P9AL.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALN85CP9AL.
DATA DIVISION.
LINKAGE SECTION.
01 L PIC X ANY LENGTH.
PROCEDURE DIVISION USING L.
M.
    EXIT PROGRAM.
END PROGRAM ALN85CP9AL.
END PROGRAM ALN85P9AL.
