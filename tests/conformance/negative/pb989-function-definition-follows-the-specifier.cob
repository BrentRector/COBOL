*> reject-at: 2002 2014 2023
*> kb/Work PB989 - ISO 12.3.8.4 GR11 a): the details of a function prototype come from a function definition "specified
*> previously in the same compilation group", else from a function prototype definition in it (b), else from the
*> external repository (c). The definition PB989LATE FOLLOWS the program whose REPOSITORY names it, so it is not a);
*> no function prototype definition of that name is in the group (b), and this implementation has no external
*> repository to consult (c) - nothing can say what the call returns, which is COBOLNET1505 at the reference
*> (12.3.8.3 SR10 allows the name only as a prototype in the group, a definition specified PREVIOUSLY, or a function
*> with information in the external repository). Its only defect is the order: moving the definition above the
*> program, or giving the group a prototype of it ahead of every other unit (10.6.2 SR1), makes it conforming.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB989N1.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB989LATE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-N PIC 9(4) VALUE 7.
01 WS-R PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    COMPUTE WS-R = FUNCTION PB989LATE(WS-N).
    DISPLAY WS-R.
    STOP RUN.
END PROGRAM PB989N1.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB989LATE.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC 9(4).
01 L-R PIC 9(4).
PROCEDURE DIVISION USING L-X RETURNING L-R.
P.
    COMPUTE L-R = L-X * 2.
    GOBACK.
END FUNCTION PB989LATE.
