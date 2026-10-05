      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.4.3 SR2: "Identifier-1 shall be of class object; neither the predefined
      *>   object reference NULL nor a universal object reference shall be specified." A function-identifier
      *>   is a legal identifier-1 of an inline method invocation (8.4.3.1.3 SR1), and SR2 asks the item it
      *>   references: PB1425N3F returns a UNIVERSAL object reference, so `FUNCTION PB1425N3F (1) :: "SPEAK"`
      *>   is refused by SR2 (COBOLNET2138) - an INVOKE statement is the form that takes a universal receiver.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425N3F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425N3K.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9.
       01 L-OBJ USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1425N3K "NEW" RETURNING L-OBJ
           GOBACK.
       END FUNCTION PB1425N3F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425N3K
           FUNCTION PB1425N3F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(6).
       PROCEDURE DIVISION.
           MOVE FUNCTION PB1425N3F (1) :: "NAME" TO W
           STOP RUN.
       END PROGRAM PB1425N3.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425N3K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. NAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC X(6).
       PROCEDURE DIVISION RETURNING LN.
           MOVE "K-NAME" TO LN.
       END METHOD NAME.
       END OBJECT.
       END CLASS PB1425N3K.
