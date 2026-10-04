      *> reject-at: 2002 2014 2023
      *> kb/Work PB1062. ISO 8.4.3.11.3 SR1, second sentence: "Identifier-1 shall not be defined in the working-storage or
      *> file section of an object or a factory object." Each ADDRESS OF below names an item of the OBJECT's (OW) or the
      *> FACTORY's (FW) working storage, in the three surfaces the one operand screen serves: the SET sender, the
      *> relation operand and the CALL argument. All are refused COBOLNET2784. A method's own LOCAL-STORAGE (LP, LM) is
      *> legal and is addressed in the same statements.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1062N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FW PIC X(4) VALUE "FACT".
       PROCEDURE DIVISION.
       METHOD-ID. FM.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LP USAGE POINTER.
       01 LM PIC X(4).
       PROCEDURE DIVISION.
           SET LP TO ADDRESS OF LM
           SET LP TO ADDRESS OF FW.
       END METHOD FM.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OW PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LP USAGE POINTER.
       01 LM PIC X(4).
       PROCEDURE DIVISION.
           SET LP TO ADDRESS OF LM
           SET LP TO ADDRESS OF OW
           IF LP = ADDRESS OF OW
               DISPLAY "EQ"
           END-IF
           CALL "SINK" USING BY CONTENT ADDRESS OF OW.
       END METHOD M.
       END OBJECT.
       END CLASS PB1062N.
