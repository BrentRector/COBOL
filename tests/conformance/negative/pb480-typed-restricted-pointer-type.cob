      *> reject-at: 2002 2014 2023
      *> kb/Work PB480 (train 1021 review D finding 2, typed-lane sibling) - a restricted data-pointer ITEM
      *> passed BY REFERENCE to a method through a CLASS-typed object reference. 14.8.2.3.2: "If either is a
      *> restricted pointer, both shall be restricted and of the same type" (cite.py --check 14.8.2.3.2 ->
      *> OK 14.8.2.3.2 4)); 8.5.3.1: two type declarations of one type-name are the same type only when
      *> equivalent (OK 8.5.3.1). The caller's T-REC (X(2) + X(4)) and the class's T-REC (one 9(6)) are not,
      *> so RP (POINTER TO the caller's T-REC) does not conform to LP (POINTER TO the class's T-REC). Before
      *> the fix the typed lane compared the restriction's type-NAME only and compiled the INVOKE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480TP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480TP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 S TYPE T-REC.
       01 RP TYPE PT.
       01 U USAGE OBJECT REFERENCE C480TP.
       PROCEDURE DIVISION.
           SET RP TO ADDRESS OF S
           INVOKE C480TP "NEW" RETURNING U
           INVOKE U "TR" USING RP
           STOP RUN.
       END PROGRAM PB480TP.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480TP INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-N PIC 9(6).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR:REACHED".
       END METHOD TR.
       END OBJECT.
       END CLASS C480TP.
