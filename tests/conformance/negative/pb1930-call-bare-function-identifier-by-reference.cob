*> reject-at: 2002 2014 2023
*> kb/Work PB1930 - the refusing half. 14.9.4.2 Format 1's bare argument is identifier-2, and a function-identifier is an
*> identifier (8.4.3.1.2 Format 1), but it takes the PREVAILING mode (14.9.4.4 GR5): BY REFERENCE for the first argument.
*> 14.9.4.3 SR3: "Identifier-2 shall reference an address-identifier or a data item defined in the file, working-storage,
*> local-storage, or linkage section", and the temporary a function returns (8.4.3.2.1) is none of those, so a function
*> can cross only BY CONTENT. The binder reports it by that rule (COBOLNET1677), not as an arithmetic operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930NEGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1930NEGS" USING FUNCTION UPPER-CASE(X)
           STOP RUN.
       END PROGRAM PB1930NEGM.
