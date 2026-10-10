      *> reject-at: 2002 2014 2023
      *> kb/Work PB2618. ISO §8.4.3.9.3 SR4: "If the object property is used as a receiving item, a set property
      *> method shall exist for property-name-1 in the object referenced by identifier-1". A STRING stores only the
      *> positions it references (§14.9.43.4 GR7), yet NM is still its RECEIVING item (§14.9.43.3 SR10), and the
      *> property it stores through here, WITH NO SET, has no set property method -- so the statement is refused by
      *> SR4, however few positions it would store. (A WITH NO GET property is the opposite case and is legal:
      *> SR3 asks for a get method of a SENDING use only.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2618N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2618NC
           PROPERTY NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE PB2618NC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2618NC "NEW" RETURNING D.
           STRING "Q" DELIMITED BY SIZE INTO NM OF D.
           STOP RUN.
       END PROGRAM PB2618N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2618NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY WITH NO SET.
       END OBJECT.
       END CLASS PB2618NC.
