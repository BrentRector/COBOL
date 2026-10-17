*>--- conformance-case
*> id: pb549-program-address-operand-shape
*> rules: [SR-8.4.3.13.3-1, GR-13.18.60.4-24]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB549 — §8.4.3.13.3 syntax rule 1: "Identifier-1 shall be of category alphanumeric or
      *> national." The braced operand list of a program-address-identifier is three alternatives with three
      *> rules over them (SR1 identifier-1, SR2 "Literal-1 shall be an alphanumeric or national literal whose
      *> length is not zero", SR3 "Program-prototype-name-1 shall be a program prototype specified in the
      *> REPOSITORY paragraph"), and the grammar cannot tell identifier-1 from program-prototype-name-1 — both
      *> are a bare word — so all three are screened at BIND, by name, on COBOLNET2157. NN is a PIC 9 item:
      *> it is neither a prototype declared in the REPOSITORY paragraph nor an alphanumeric or national
      *> identifier, and §8.4.3.13.4 GR1 a) routes its CONTENT through §8.3.2.2 as a program-name, which a
      *> numeric item cannot supply.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549OPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       01  NN PIC 9(4) VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM NN
           STOP RUN.
