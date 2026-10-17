*>--- conformance-case
*> id: pb1551-super-receiving
*> rules: [SR-8.4.3.8.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - ISO 8.4.3.8.3 SR2 (cite.py: OK 8.4.3.8.3 2)): SUPER,
      *> the same identifier format as SELF, written as the receiving operand
      *> of SET ... TO NULL. SR2, not SR3 (SUPER's two homes), is the rule the
      *> receiving position breaks first; the chokepoint reports COBOLNET3978.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551UM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551UM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551UC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           MOVE 1 TO N
           SET SUPER TO NULL.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551UC.
