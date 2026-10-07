      *> kb/Work PB609 -- ISO 14.9.4.3 SR14, the CALL side of the restricted program-pointer.
      *> 13.18.60.4 GR25: "A restricted program-pointer shall contain only the predefined address NULL or the
      *> address of a program with the same signature as that identified by the specified program-prototype-name."
      *> 14.9.4.3 SR14 (Format 2, CALL identifier-1 AS program-prototype-name-1): "If identifier-1 references a
      *> restricted program-pointer, the signature of the program-prototype specified in the definition of that
      *> pointer shall be the same as the signature of program-prototype-name-1."
      *> PPT is restricted to PBT609A (one BY REFERENCE numeric PIC 9(4) formal). PBT609B has the same signature
      *> (its formal differs only in name), so `CALL PP AS PBT609B` is legal and 14.9.4.4 GR3 b) locates the program
      *> by the pointer's content -- PBT609A -- while 14.9.4.4 GR7 takes the characteristics from the prototype. The
      *> argument is checked against that signature (14.9.4.3 SR25 -> 14.8.2). A pointer restricted to one
      *> signature and a CALL naming another is the negative pb609-call-restricted-program-pointer-signature.
      *> EXPECTED OUTPUT: "A 0042" twice -- once through AS PBT609A and once through AS PBT609B, both reaching the
      *> program PP holds.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609A IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PBT609A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609B IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM PBT609B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB609OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PBT609A
           PROGRAM PBT609B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT609A.
       01 PP TYPE PPT.
       01 WS-N PIC 9(4) VALUE 0042.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ENTRY "PBT609A"
           CALL PP AS PBT609A USING WS-N
           CALL PP AS PBT609B USING WS-N
           STOP RUN.
       END PROGRAM PB609OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "A " L-X
           GOBACK.
       END PROGRAM PBT609A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
           DISPLAY "B " L-Y
           GOBACK.
       END PROGRAM PBT609B.
