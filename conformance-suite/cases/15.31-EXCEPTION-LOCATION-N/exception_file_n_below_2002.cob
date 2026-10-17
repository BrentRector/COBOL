*>--- conformance-case
*> id: exception_file_n_below_2002
*> rules: [FMT-15.31.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.29 / 15.31: the national EC twins EXCEPTION-FILE-N / EXCEPTION-LOCATION-N are COBOL-2002
*> introductions (the EC model + national data are both 2002; the 2023 delta is only the optional
*> file-connector argument, E.3.3 item 26). Below 2002 the D8 catalog window rejects BY NAME AND
*> EDITION with COBOLNET1502 (P10 Step-11 EC-N wave — the audit's missing 85-window witness).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGEFNP10EN.
PROCEDURE DIVISION.
MAIN.
    DISPLAY FUNCTION EXCEPTION-FILE-N.
    DISPLAY FUNCTION EXCEPTION-LOCATION-N.
    STOP RUN.
