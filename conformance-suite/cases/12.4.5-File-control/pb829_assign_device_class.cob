      *>--- conformance-case
      *> id: pb829_assign_device_class
      *> rules: [SR-12.4.5.2-5, FMT-12.4.5.1, DOC-A.1-71]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB829 (finisher) - ISO 12.4.5.1 prints the ASSIGN clause's TO phrase as a LIST,
      *>   ASSIGN [TO] {device-name-1 | literal-1} ...
      *> with the ellipsis on the inner brace pair, and ISO 12.4.5.2 SR5 leaves "the meaning and rules
      *> for the allowable specification of device-name-1 and the value of literal-1" to the implementor.
      *> COBOL.NET's determination (docs/CONFORMANCE.md section 7, DOC-A.1-71, following GnuCOBOL): ONE
      *> operand names the file, or a device class (DISK or PRINTER) is followed by ONE operand naming
      *> the file.  The second operand was a COBOL0308 parse error before.
      *> The witness: F1 (DISK + literal) and F3 (the literal alone) are the SAME physical file, and so
      *> are F2 (PRINTER + word) and F4 (the word alone) - the device class selects nothing further.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829AS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK "pb829as1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN PRINTER PB829AS2
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "pb829as1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F4 ASSIGN TO PB829AS2
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(8).
       FD  F2.
       01  R2 PIC X(8).
       FD  F3.
       01  R3 PIC X(8).
       FD  F4.
       01  R4 PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1 F2.
           MOVE "DISKREC1" TO R1.
           WRITE R1.
           MOVE "PRINTREC" TO R2.
           WRITE R2.
           CLOSE F1 F2.
           OPEN INPUT F3 F4.
           READ F3 AT END MOVE "EOF" TO R3.
           READ F4 AT END MOVE "EOF" TO R4.
           DISPLAY "F3=[" R3 "] F4=[" R4 "]".
           CLOSE F3 F4.
           STOP RUN.
