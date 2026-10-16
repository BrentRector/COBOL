      *> kb/Work PB780 - literal-1 of the OPTIONS INITIALIZE clause
      *>   written as a constant-name, in every source element that
      *>   carries an OPTIONS paragraph.
      *> 13.10.3 SR2: "constant-name-1 may be used anywhere that a
      *>   format specifies a literal of the class and category of
      *>   constant-name-1"; 11.9.10.2 prints literal-1 in the clause.
      *> 13.10.4 GR1: the effect is "as if literal-1 ... were written
      *>   where constant-name-1 is written", so 11.9.10.3 SR1 (a
      *>   one-byte hexadecimal-alphanumeric literal) holds for each
      *>   constant below, written X"nn".
      *> 11.9.10.4 GR5 c) fills with literal-1's byte; no rule makes a
      *>   constant-name defined later in the element unusable here.
      *> Derivation (VALUE-less alphanumeric items):
      *>   S=[FFF]  PB780OF's WORKING-STORAGE, filled with KF = X"46",
      *>            an entry written after the OPTIONS paragraph.
      *>   T=[GG]   contained PB780OI fills with its container's GLOBAL
      *>            constant KG = X"47" (8.4.6.2.2 scope).
      *>   FW=[XX]  the FACTORY paragraph's own constant KX = X"58".
      *>   OW=[OOO] the OBJECT paragraph's own constant KO = X"4F".
      *>   M1=[MMM] method M1's LOCAL-STORAGE, its own constant KM.
      *>   M2=[OOO] method M2 names its object's constant KO.
      *>   U=[JJ]   contained PB780OJ fills with KK, written AS KJ, that
      *>            is AS X"4A": KK is as if X"4A" were written
      *>            (13.10.4 GR1), so SR1 holds of the literal KJ
      *>            stands for, never of the word KJ (review R1-1).
      *>   V=[HHHH] contained PB780OK fills with KH, FROM compilation
      *>            variable PB780VH defined AS X"48": GR1, "as if
      *>            ... the text represented by compilation-variable-
      *>            name-1 were written", and 7.3.11.1 names it "a
      *>            particular literal value".
       >>DEFINE PB780VH AS X"48"
       IDENTIFICATION DIVISION.
       CLASS-ID. PB780OC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO KX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FW PIC X(2).
       01 KX CONSTANT AS X"58".
       PROCEDURE DIVISION.
       METHOD-ID. FM.
       PROCEDURE DIVISION.
           DISPLAY "FW=[" FW "]".
       END METHOD FM.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO KO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OW PIC X(3).
       01 KO CONSTANT AS X"4F".
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO KM.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       01 KM CONSTANT AS X"4D".
       PROCEDURE DIVISION.
           DISPLAY "OW=[" OW "] M1=[" F "]".
       END METHOD M1.
       METHOD-ID. M2.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE TO KO.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "M2=[" F "]".
       END METHOD M2.
       END OBJECT.
       END CLASS PB780OC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780OF.
       OPTIONS.
           INITIALIZE WORKING-STORAGE SECTION TO KF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB780OC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(3).
       01 KF CONSTANT AS X"46".
       01 KG CONSTANT GLOBAL AS X"47".
       01 O1 USAGE OBJECT REFERENCE PB780OC.
       PROCEDURE DIVISION.
           DISPLAY "S=[" S "]"
           CALL "PB780OI"
           CALL "PB780OJ"
           CALL "PB780OK"
           INVOKE PB780OC "FM"
           INVOKE PB780OC "NEW" RETURNING O1
           INVOKE O1 "M1"
           INVOKE O1 "M2"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780OI.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO KG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "T=[" T "]"
           GOBACK.
       END PROGRAM PB780OI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780OJ.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO KK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U PIC X(2).
       01 KJ CONSTANT AS X"4A".
       01 KK CONSTANT AS KJ.
       PROCEDURE DIVISION.
           DISPLAY "U=[" U "]"
           GOBACK.
       END PROGRAM PB780OJ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780OK.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO KH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC X(4).
       01 KH CONSTANT FROM PB780VH.
       PROCEDURE DIVISION.
           DISPLAY "V=[" V "]"
           GOBACK.
       END PROGRAM PB780OK.
       END PROGRAM PB780OF.
