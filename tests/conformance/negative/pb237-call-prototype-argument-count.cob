      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 14.9.4.4 general rule 7: "If the NESTED phrase is not specified,
      *> program-prototype-name-1 is used to determine the characteristics of the
      *> called program." 12.3.8.4 GR10 a) takes those characteristics from the
      *> same-externalized-name program definition in this compilation group, and
      *> 14.9.4.3 SR25 makes 14.8.2's conformance rules apply. 14.8.2.1: "The number of
      *> arguments in the activating element shall be equal to the number of formal
      *> parameters in the activated element" (trailing OPTIONAL formals excepted) -
      *> two arguments are supplied where the callee declares one.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NCE IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       PROCEDURE DIVISION USING LK-A.
       END PROGRAM PB237NCE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NARG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM ARG-PROTO AS "PB237NCE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 1.
       01 WS-B PIC 9(4) VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           CALL ARG-PROTO USING WS-A WS-B.
           STOP RUN.
       END PROGRAM PB237NARG.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NCE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       PROCEDURE DIVISION USING LK-A.
       CMAIN.
           GOBACK.
       END PROGRAM PB237NCE.
