*> reject-at: 2002 2014 2023
*> ISO 1989:2023 14.9.39.3 SR22 - "If identifier-7 references a restricted program-pointer, identifier-8
*> shall be the predefined address NULL or shall reference a program-pointer and the program-prototypes
*> associated with identifier-7 and identifier-8 shall have the same signature." PPU is an UNRESTRICTED
*> program-pointer: it is associated with no program-prototype at all, so no same-signature reading can be
*> satisfied. (14.8.2.3.2 states the same requirement explicitly in the argument-passing direction: "if
*> either is a restricted pointer, both shall be restricted and of the same type.") COBOLNET1959 - the one
*> code SR20 and SR22 share, because they are one rule over two carriers. kb/Work PB817.
*> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
*> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
*> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
*> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPPTGT IS PROTOTYPE.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC 9(4).
PROCEDURE DIVISION USING L-X.
END PROGRAM NEGPPTGT.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP06.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM NEGPPTGT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO NEGPPTGT.
01 PPR TYPE PPT.
01 PPU USAGE PROGRAM-POINTER.
PROCEDURE DIVISION.
MAIN.
    SET PPR TO PPU
    STOP RUN.
END PROGRAM NEGPP06.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPPTGT.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC 9(4).
PROCEDURE DIVISION USING L-X.
    GOBACK.
END PROGRAM NEGPPTGT.
