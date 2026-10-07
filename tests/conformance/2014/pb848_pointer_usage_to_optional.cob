      *> ISO/IEC 1989:2023 §13.18.60.2 — TO is an OPTIONAL word in all three pointer usages. kb/Work PB848.
      *>
      *> The printed general format (folio 503, RENDERED) reads `POINTER [ TO type-name-1 ]`,
      *> `FUNCTION-POINTER TO function-prototype-name-1` and `PROGRAM-POINTER [ TO program-prototype-name-1 ]`
      *> with the underline rule under POINTER / FUNCTION-POINTER / PROGRAM-POINTER only. §5.2.3 — an optional
      *> word "may be written to add clarity" and its omission changes nothing, so each TO-less entry below
      *> declares EXACTLY the restriction its TO-full twin declares:
      *> §13.18.60.4 GR23 — "If type-name-1 is specified, this data item is a restricted data-pointer."
      *> §13.18.60.4 GR25 — "If program-prototype-name-1 is specified, this data item is a restricted
      *>   program-pointer."  §13.18.60.3 SR18/SR19 — the TYPEDEF clause is specified for those subjects.
      *> §13.18.60.4 GR26 — a function-pointer is restricted to the prototype its operand names.
      *> The observable proof of "the same restriction" is assignment between the twins: §14.9.39.3 SR19
      *> (data-pointers restricted to the same type), SR22 (program-pointers whose prototypes have the same
      *> signature) and SR20 (function-pointers likewise) each ADMIT the copy only because the TO-less and
      *> TO-full declarations name the same type / prototype; a TO-less operand that parsed as nothing would
      *> make the receiver unrestricted-vs-restricted and the copy a compile-time error.
      *> This is a 2014 program because FUNCTION-POINTER is a COBOL-2014 usage; POINTER and PROGRAM-POINTER are
      *> 2002 (the negative pb848-function-pointer-to-less-below-2014 pins that the relaxation does not leak).
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   DP-SAME      DP1 (POINTER REC-T) took W's address; DP2 (POINTER TO REC-T) copied it (SR19), so the
      *>                two are equal (§8.8.4.2.16 — pointers are equal when they address the same item).
      *>   FP-SAME      FP1 (FUNCTION-POINTER PBF848) holds PBF848's address; FP2 (… TO PBF848) copied it.
      *>   FP-CALL 42   FUNCTION PBF848(21) doubles its argument (the prototype both pointers are restricted to).
      *>   PP-SAME      PP1 (PROGRAM-POINTER PBT848) took the program's address; PP2 (… TO PBT848) copied it.
      *>   CALLED 0042  CALL through PP2 activates PBT848, which displays its argument.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT848 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PBT848.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBF848.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBF848.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB848PU.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBF848
           PROGRAM PBT848.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT1 IS TYPEDEF USAGE POINTER REC-T.
       01 DPT2 IS TYPEDEF USAGE POINTER TO REC-T.
       01 PPT1 IS TYPEDEF USAGE PROGRAM-POINTER PBT848.
       01 PPT2 IS TYPEDEF USAGE PROGRAM-POINTER TO PBT848.
       01 DP1 TYPE DPT1.
       01 DP2 TYPE DPT2.
       01 FP1 USAGE FUNCTION-POINTER PBF848.
       01 FP2 USAGE FUNCTION-POINTER TO PBF848.
       01 PP1 TYPE PPT1.
       01 PP2 TYPE PPT2.
       01 W TYPE REC-T.
       01 WS-N PIC 9(4) VALUE 0042.
       01 WS-R PIC 9(9).
       PROCEDURE DIVISION.
       MAIN.
           SET DP1 TO ADDRESS OF W
           SET DP2 TO DP1
           IF DP2 = DP1
               DISPLAY "DP-SAME"
           ELSE
               DISPLAY "DP-DIFF"
           END-IF
           SET FP1 TO ADDRESS OF FUNCTION PBF848
           SET FP2 TO FP1
           IF FP2 = FP1
               DISPLAY "FP-SAME"
           ELSE
               DISPLAY "FP-DIFF"
           END-IF
           COMPUTE WS-R = FUNCTION PBF848(21)
           DISPLAY "FP-CALL " WS-R
           SET PP1 TO ENTRY "PBT848"
           SET PP2 TO PP1
           IF PP2 = PP1
               DISPLAY "PP-SAME"
           ELSE
               DISPLAY "PP-DIFF"
           END-IF
           CALL PP2 USING WS-N
           STOP RUN.
       END PROGRAM PB848PU.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT848.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "CALLED " L-X
           GOBACK.
       END PROGRAM PBT848.
