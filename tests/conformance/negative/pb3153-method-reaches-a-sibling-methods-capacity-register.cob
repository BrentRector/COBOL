      *> reject-at: 2014 2023
      *> kb/Work PB3153 (finisher) - a METHOD's named OCCURS DYNAMIC
      *>   CAPACITY register is a data-name of that method alone.
      *> ISO 13.18.38.3 SR30: "Data-name-3 shall not be defined elsewhere
      *>   in the source element" - the register is declared in the
      *>   source element that declares the table: here method SHOWA, a
      *>   contained source unit (10.3), so a source element of its own
      *>   (3.164).
      *> ISO 8.4.6.1: the names a source element declares are its own;
      *>   SHOWB declares no R2 and its containers declare none, so
      *>   SHOWB's MOVE R2 references nothing (8.4.2.1).
      *> Before the fix the class binder's one register index answered
      *>   for every method: SHOWB's R2 reached SHOWA's LOCAL-STORAGE
      *>   register and the generated C# failed (CS0103 'U').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153LP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3153LC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3153LC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3153LC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3153LP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3153LC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN R2 FROM 4 TO 10.
       PROCEDURE DIVISION.
           MOVE R2 TO N
           DISPLAY "A " N.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       PROCEDURE DIVISION.
           MOVE R2 TO N
           DISPLAY "B " N.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3153LC.
