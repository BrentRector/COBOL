*> reject-at: 2002 2014 2023
*> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details of a program prototype come from a program definition "specified
*> previously in the same compilation group" whose externalized name is the prototype's. PB989PD is such a definition
*> (it is ABOVE the program that names it, with no program prototype in the group), so CALL PB989PD is checked against
*> its one USING formal (14.9.4.3 SR25 -> 14.8.2.1: the argument count equals the formal count, except trailing OPTIONAL
*> formals omitted). The call passes two arguments: COBOLNET1684. The mirror shape - the definition BELOW the program
*> - takes its details from a prototype definition ahead of both (GR10 b), pb237_program_prototype) or from the external
*> repository (c).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB989PD.
DATA DIVISION.
LINKAGE SECTION.
01 L-A PIC 9(4).
PROCEDURE DIVISION USING L-A.
P.
    GOBACK.
END PROGRAM PB989PD.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB989N2.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM PB989PD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-A PIC 9(4) VALUE 1.
01 WS-B PIC 9(4) VALUE 2.
PROCEDURE DIVISION.
MAIN.
    CALL PB989PD USING WS-A WS-B.
    STOP RUN.
END PROGRAM PB989N2.
