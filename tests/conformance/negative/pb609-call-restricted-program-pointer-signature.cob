      *> reject-at: 2002 2014 2023
      *> kb/Work PB609. ISO 14.9.4.3 SR14: "If identifier-1 references a restricted program-pointer, the signature of
      *> the program-prototype specified in the definition of that pointer shall be the same as the signature of
      *> program-prototype-name-1." PP is restricted to PBT609A, whose one formal is PIC 9(4), and the CALL's AS phrase
      *> names PBT609C, whose formal is PIC X(4): the two signatures differ (8.13: a signature includes "the
      *> description of the parameters of the source unit, if any, and the manner of receiving parameters"), so the
      *> CALL is refused COBOLNET2936. The same-signature spelling is the positive golden
      *> 2002/pb609_call_restricted_program_pointer.
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
       PROGRAM-ID. PBT609C IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Z PIC X(4).
       PROCEDURE DIVISION USING L-Z.
       END PROGRAM PBT609C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB609N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PBT609A
           PROGRAM PBT609C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT609A.
       01 PP TYPE PPT.
       01 WS-X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ENTRY "PBT609A"
           CALL PP AS PBT609C USING WS-X
           STOP RUN.
       END PROGRAM PB609N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           GOBACK.
       END PROGRAM PBT609A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT609C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Z PIC X(4).
       PROCEDURE DIVISION USING L-Z.
           GOBACK.
       END PROGRAM PBT609C.
