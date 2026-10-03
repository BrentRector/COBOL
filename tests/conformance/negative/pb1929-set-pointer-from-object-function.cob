      *> reject-at: 2002 2014 2023
      *> kb/Work PB1929 — the second negative twin of 2002/pb1929_set_identifier_senders. The sender is a
      *> function-identifier of class object (it RETURNS an object reference), so it references a data item of class
      *> object — which §14.9.39.3 SR17 ("Identifier-6 shall be of category data-pointer") does not admit into a
      *> data-pointer receiver. The refusal names the SENDER's category, where it used to name "a literal or an
      *> arithmetic expression" (COBOLNET0869).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929NF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           SET L-RES TO NULL
           GOBACK.
       END FUNCTION PB1929NF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1929N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1929NF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO FUNCTION PB1929NF(1)
           STOP RUN.
       END PROGRAM PB1929N2.
