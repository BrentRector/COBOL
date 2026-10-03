      *> reject-at: 2002 2014 2023
      *> kb/Work PB1063, the program-pointer arm of the same SET question.
      *> ISO 14.8.2.3.3 2) makes a BY CONTENT argument into a pointer
      *> formal "the same as if a SET statement were performed", and
      *> 14.9.39.3 SR22: "If identifier-7 references a restricted
      *> program-pointer, identifier-8 shall be the predefined address
      *> NULL or shall reference a program-pointer and the
      *> program-prototypes associated with identifier-7 and identifier-8
      *> shall have the same signature." LQ is restricted to N1063T; PU is
      *> unrestricted, associated with no program-prototype, so the INVOKE
      *> is COBOLNET0828 (before the fix only the category was compared).
       IDENTIFICATION DIVISION.
       CLASS-ID. N1063Q INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           PROGRAM N1063T.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO N1063T.
       PROCEDURE DIVISION.
       METHOD-ID. MQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ TYPE PPT.
       PROCEDURE DIVISION USING BY REFERENCE LQ.
           DISPLAY "IN-MQ"
           GOBACK.
       END METHOD MQ.
       END OBJECT.
       END CLASS N1063Q.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1063P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS N1063Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PU USAGE PROGRAM-POINTER.
       01 O USAGE OBJECT REFERENCE N1063Q.
       PROCEDURE DIVISION.
           INVOKE N1063Q "NEW" RETURNING O
           INVOKE O "MQ" USING BY CONTENT PU
           STOP RUN.
       END PROGRAM N1063P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1063T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           GOBACK.
       END PROGRAM N1063T.
