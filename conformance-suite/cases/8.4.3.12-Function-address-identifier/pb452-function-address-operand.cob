*>--- conformance-case
*> id: pb452-function-address-operand
*> rules: [SR-8.4.3.12.3-1, SR-8.4.3.12.3-2, SR-14.9.39.3-20]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.4.3.12.3 SR1 - "Identifier-1 shall be of category alphanumeric or national." The braced
*> operand of ADDRESS OF FUNCTION is `function-prototype-name-1 | identifier-1` and nothing else (measured on
*> the printed folio 141: unlike 8.4.3.13's PROGRAM twin there is no literal-1 arm), so a NUMERIC identifier
*> names no function. COBOLNET1960. kb/Work PB452.
IDENTIFICATION DIVISION.
FUNCTION-ID. NAODBL.
DATA DIVISION.
LINKAGE SECTION.
01 L-ARG PIC S9(4).
01 L-RES PIC S9(9).
PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
    COMPUTE L-RES = L-ARG * 2
    GOBACK.
END FUNCTION NAODBL.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGFP05.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION NAODBL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FPD USAGE FUNCTION-POINTER TO NAODBL.
01 WS-N PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    SET FPD TO ADDRESS OF FUNCTION WS-N
    STOP RUN.
END PROGRAM NEGFP05.
