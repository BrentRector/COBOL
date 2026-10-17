*>--- conformance-case
*> id: pb2991_function_address_data_item_edition_spelled
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2991 - the FUNCTION-ADDRESS arm of pb2991_data_item_names_edition_spelled. Before COBOL 2023,
      *> Annex C maps FINAL SIGMA U+03C2 to SIGMA U+03C3 (the mapping Annex E.2 item 14 deleted at 2023), so
      *> Fς and Fσ are ONE word (ISO 8.1.3.2 GR3: "Equivalence of uppercase and lowercase basic letters is
      *> achieved by folding from uppercase to lowercase in accordance with the case mapping described in
      *> Annex C"; GR4 for the extended letters).
      *> ADDRESS OF FUNCTION identifier-1 (8.4.3.12.4 GR1 a: "the content of the data item referenced by
      *> identifier-1") must therefore locate FUNCTION-ID. Fσ when WS-NAME holds Fς, and the pointer it makes
      *> equals the pointer of the prototype arm ADDRESS OF FUNCTION Fσ (8.8.4.2.16: "The operands are equal if
      *> they reference the same address"). Expected: FUNC-OK (a comparison made by the 2023 table finds no
      *> function and leaves the pointer NULL, so FUNC-BAD).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. Fσ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION Fσ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2991F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION Fσ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO Fσ.
       01 FPREF USAGE FUNCTION-POINTER TO Fσ.
       01 WS-NAME PIC X(8) VALUE "Fς".
       PROCEDURE DIVISION.
       MAIN-P.
           SET FPREF TO ADDRESS OF FUNCTION Fσ
           SET FP TO ADDRESS OF FUNCTION WS-NAME
           IF FP = FPREF
               DISPLAY "FUNC-OK"
           ELSE
               DISPLAY "FUNC-BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB2991F.
