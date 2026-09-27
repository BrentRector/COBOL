      *> reject-at: 2002 2014 2023
      *> kb/Work PB1142 - ISO 8.4.3.4.3 SR1: "Inline method invocation shall not be specified as a
      *> receiving operand." DIVIDE Format 1 (14.9.12.2) prints INTO {identifier-2 [rounded-phrase]}, a
      *> RECEIVER, so an inline invocation there is illegal (COBOLNET1689). The per-verb Format-1 screen
      *> enumerated literal and function-identifier only; the invocation fell through with no receiver
      *> and the emitter CRASHED (InvalidOperationException: Sequence contains no elements). The same
      *> statement with GIVING is Format 2 and legal (2002/pb1142_arithmetic_identifier_operands).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1142N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1142N1C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3)V9 VALUE 2.5.
       01 OB USAGE OBJECT REFERENCE PB1142N1C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1142N1C "NEW" RETURNING OB.
           DIVIDE A INTO OB :: "GETX".
           STOP RUN.
       END PROGRAM PB1142N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1142N1C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC 9(3)V9.
       PROCEDURE DIVISION RETURNING LK-X.
       MAIN.
           MOVE 4 TO LK-X.
       END METHOD GETX.
       END OBJECT.
       END CLASS PB1142N1C.
