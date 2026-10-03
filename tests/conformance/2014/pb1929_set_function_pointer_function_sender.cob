      *> kb/Work PB1929 — Format 8's leg of the SET sender classifier (ISO/IEC 1989:2023 §14.9.39.2 Format 8,
      *> function-pointer-assignment; see 2002/pb1929_set_identifier_senders for Formats 1, 5, 7 and 9). §14.9.39.3 SR20:
      *> identifier-13 shall be of category function-pointer, and §8.4.3.1.3 SR1 lets an identifier be any identifier
      *> format — so a function-identifier whose RETURNING item is a FUNCTION-POINTER references a data item of that
      *> category (§8.4.3.2.1: "A function-identifier references the unique data item that results from the evaluation
      *> of a function") and is identifier-13. Two defects stood in front of it: the sender sniff took only a bare data
      *> reference (COBOLNET0869), and a FUNCTION-ID RETURNING USAGE FUNCTION-POINTER did not compile at all — the
      *> generated StoreReturn call bound only to the object-reference generic (CS0315) because no overload took the
      *> FunctionPointer carrier, the program-pointer / data-pointer shape already fixed one category earlier.
      *> EXPECTED OUTPUT: PB1929FP returns the address of PB1929T (SET … TO ADDRESS OF FUNCTION, §8.4.3.12.4 GR1 b));
      *> SET FP TO FUNCTION PB1929FP(1) stores that address, so FP equals FPREF, which was set from the same
      *> function-address-identifier: FP-SAME. (§14.9.39.3 SR20's same-signature rule holds: both prototypes are PB1929T.)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION PB1929T.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1929T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES USAGE FUNCTION-POINTER TO PB1929T.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           SET L-RES TO ADDRESS OF FUNCTION PB1929T
           GOBACK.
       END FUNCTION PB1929FP.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1929C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1929T
           FUNCTION PB1929FP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PB1929T.
       01 FPREF USAGE FUNCTION-POINTER TO PB1929T.
       PROCEDURE DIVISION.
       MAIN.
           SET FPREF TO ADDRESS OF FUNCTION PB1929T
           SET FP TO FUNCTION PB1929FP(1)
           IF FP = FPREF
               DISPLAY "FP-SAME"
           ELSE
               DISPLAY "FP-DIFF"
           END-IF
           STOP RUN.
       END PROGRAM PB1929C.
