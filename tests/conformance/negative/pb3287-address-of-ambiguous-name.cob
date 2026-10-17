      *> reject-at: 2002 2014 2023
      *> kb/Work PB3287 - ISO 8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly referenced,
      *> uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference."
      *> A below is declared under both G1 and G2 and ADDRESS OF A writes no qualifier, so the operand is ambiguous:
      *> COBOLNET1639 "does not uniquely identify a data item", the verdict `MOVE A TO WS-X` draws.  The ADDRESS OF
      *> entry used to resolve the name by its own sequence and, finding no ONE item, answer COBOLNET0869 that the
      *> operand "names none (an undeclared name ...)" - false about a name declared twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  A PIC X.
       01  G2.
           05  A PIC X.
       01  WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-P TO ADDRESS OF A.
           STOP RUN.
