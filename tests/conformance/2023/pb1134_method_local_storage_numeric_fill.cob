      *> kb/Work PB1134 - ISO 13.6.4 GR2 (LOCAL-STORAGE is initialized as 11.9.10 says) with 14.6.2.3.2 action 1 and the owner decision kb/Work R53: the OPTIONS INITIALIZE
      *> fill reaches EVERY storage position of the sections it names, numeric items included, in an OO METHOD's LOCAL-STORAGE too (the method's own OPTIONS paragraph is
      *> the clause in force, 11.9.4 GR1).  INITIALIZE LOCAL-STORAGE TO X"41" ("A", ordinal 66 by FUNCTION ORD):
      *>   N   PIC 9(4)           -> DISPLAY storage holds the fill characters -> AAAA
      *>   G   group of GA PIC 9(2) (2 bytes) and GB PIC 9(4) COMP (2 bytes) -> every byte of the group is X"41":  ORD(G(1:1)) = 66, ORD(G(4:1)) = 66 (GB's second byte)
      *>   GA  PIC 9(2)           -> AA
      *> BEFORE: a method's LOCAL-STORAGE numeric items kept their zeros (the bind-time promotion walked only the program's roots), and a method-local GROUP's members were
      *> answered from the physical-field memo the class-level emission built first, so even the elementary root N was filled while G was not.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1134CLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO X"41".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N PIC 9(4).
       01 G.
          05 GA PIC 9(2).
          05 GB PIC 9(4) COMP.
       PROCEDURE DIVISION.
           DISPLAY "N=[" N "]".
           DISPLAY "G=" FUNCTION ORD(G(1:1)) " " FUNCTION ORD(G(4:1)).
           DISPLAY "GA=[" GA "]".
       END METHOD M1.
       END OBJECT.
       END CLASS PB1134CLS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1134M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1134CLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE PB1134CLS.
       PROCEDURE DIVISION.
           INVOKE PB1134CLS "NEW" RETURNING O1
           INVOKE O1 "M1"
           STOP RUN.
