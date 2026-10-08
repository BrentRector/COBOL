      *> kb/Work PB2587 - ISO 14.2.3 GR9, second regime: for "a program
      *> for which there is a program-specifier in the REPOSITORY
      *> paragraph of the activating runtime element", "a program and
      *> the NESTED phrase", "a method" and "a function", a BY CONTENT
      *> argument is moved into a record of the FORMAL's description:
      *> "if the formal parameter is numeric, a COMPUTE statement ...
      *> otherwise, a MOVE statement". Before the fix every CALL lane
      *> adopted the argument's image unconverted for an identifier.
      *> EXPECTED VALUES, DERIVED (14.9.25.4 GR4/GR6; 14.6.8):
      *>  J1 A4 "ABCD" into X(6) JUSTIFIED RIGHT: GR6 a) alignment per
      *>     14.6.8, a justified receiver right-justifies  => [  ABCD]
      *>  J2 literal "AB" into the same formal           => [    AB]
      *>  J3 group GA ("AB" + 9(2) 12) into the same formal: GR4's
      *>     group move "as if ... alphanumeric to alphanumeric
      *>     elementary", no conversion, still justified  => [  AB12]
      *>  X1 S9(3) -12 into X(4): GR6 a) "the operational sign is not
      *>     moved", the three digit positions           => [012 ]
      *>  X2 9(4) COMP 1234 into X(4): the usage is converted to the
      *>     receiver's (GR6 a)), never the binary bytes  => [1234]
      *>  E1 9(4) 1234 into ZZ,ZZ9: edited into the mask  => [ 1,234]
      *>  E2 X(5) "12345" into ZZ,ZZ9: an alphanumeric sender into a
      *>     numeric-edited receiver is an unsigned integer, edited
      *>                                                 => [12,345]
      *>  E3 group GA into ZZ,ZZ9: GR4 - no editing      => [AB12  ]
      *>  A1 A4 into XX/XX: simple insertion editing     => [AB/CD]
      *>  A2 SPACE into XX/XX: the fill is edited too (C: and I:)
      *>                                                 => [  /  ]
      *>  B1 PIC 1(2) B"11" into PIC 1(4): 14.6.8 boolean zero fill
      *>                                                 => [1100]
      *>  F1 "AB" into a function's X(6) JUSTIFIED RIGHT formal
      *>                                                 => [    AB]
      *>  U1 S9(3) -12 into the X(3) formal of P2587U, a program with
      *>     NO program-specifier: GR9's first regime, "moved to this
      *>     allocated record without conversion" - the storage image,
      *>     its trailing operational sign overpunched (DOC-A.1 sign
      *>     representation, --sign-encoding ibm)         => [01K]
      *> C: lines are the CALL lane - each program is defined after
      *> P2587M, so 12.3.8.4 GR10 c) takes its details from the
      *> external repository and the record is filled at the
      *> activation boundary; U: the same boundary for a program with
      *> no program-specifier; F: the function, whose definition
      *> precedes the activation; I: the INVOKE lane.
       IDENTIFICATION DIVISION.
       CLASS-ID. P2587K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS P2587K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MJ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6) JUSTIFIED RIGHT.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:J=[" L "]"
           GOBACK.
       END METHOD MJ.
       IDENTIFICATION DIVISION.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ,ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:E=[" L "]"
           GOBACK.
       END METHOD ME.
       IDENTIFICATION DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC XX/XX.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:A=[" L "]"
           GOBACK.
       END METHOD MA.
       END OBJECT.
       END CLASS P2587K.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P2587F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6) JUSTIFIED RIGHT.
       01 R PIC X(6).
       PROCEDURE DIVISION USING L RETURNING R.
           MOVE L TO R
           GOBACK.
       END FUNCTION P2587F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2587K
           FUNCTION P2587F
           PROGRAM P2587J
           PROGRAM P2587X
           PROGRAM P2587E
           PROGRAM P2587A
           PROGRAM P2587B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE P2587K.
       01 A4 PIC X(4) VALUE "ABCD".
       01 A5 PIC X(5) VALUE "12345".
       01 SN PIC S9(3) VALUE -12.
       01 NC PIC 9(4) COMP VALUE 1234.
       01 N4 PIC 9(4) VALUE 1234.
       01 B2 PIC 1(2) VALUE B"11".
       01 GA.
          05 GA1 PIC X(2) VALUE "AB".
          05 GA2 PIC 9(2) VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P2587K "NEW" RETURNING O
           CALL P2587J USING BY CONTENT A4
           INVOKE O "MJ" USING BY CONTENT A4
           CALL P2587J USING BY CONTENT "AB"
           INVOKE O "MJ" USING BY CONTENT "AB"
           CALL P2587J USING BY CONTENT GA
           INVOKE O "MJ" USING BY CONTENT GA
           CALL P2587X USING BY CONTENT SN
           CALL P2587X USING BY CONTENT NC
           CALL P2587E USING BY CONTENT N4
           INVOKE O "ME" USING BY CONTENT N4
           CALL P2587E USING BY CONTENT A5
           CALL P2587E USING BY CONTENT GA
           INVOKE O "ME" USING BY CONTENT GA
           CALL P2587A USING BY CONTENT A4
           CALL P2587A USING BY CONTENT SPACE
           CALL P2587B USING BY CONTENT B2
           INVOKE O "MA" USING BY CONTENT SPACE
           CALL "P2587U" USING BY CONTENT SN
           DISPLAY "F:J=[" FUNCTION P2587F("AB") "]"
           STOP RUN.
       END PROGRAM P2587M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587J.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6) JUSTIFIED RIGHT.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:J=[" L "]"
           GOBACK.
       END PROGRAM P2587J.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:X=[" L "]"
           GOBACK.
       END PROGRAM P2587X.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587E.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ,ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:E=[" L "]"
           GOBACK.
       END PROGRAM P2587E.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC XX/XX.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:A=[" L "]"
           GOBACK.
       END PROGRAM P2587A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:B=[" L "]"
           GOBACK.
       END PROGRAM P2587B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2587U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION USING L.
           DISPLAY "U:X=[" L "]"
           GOBACK.
       END PROGRAM P2587U.
