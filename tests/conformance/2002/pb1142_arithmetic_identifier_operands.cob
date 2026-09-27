      *> kb/Work PB1142 - the arithmetic verbs' operands over every identifier format the grammar writes:
      *> a data reference, a function-identifier (ISO 8.4.3.1.2 Format 1) and
      *> an inline method invocation (Format 4, introduced by COBOL 2002 - hence this edition).
      *> The receiver screen of the arithmetic verbs' first format and the
      *> 8.8.1.1 class screen are the negatives' business; this pins that every LEGAL arm still binds and
      *> computes. Every expected value is DERIVED from the standard, not captured:
      *>   1 MULTIPLY F2  14.9.26.4 GR2: A (2.5) x GETX (4.0) = 10.0 stored in C (PIC 9(3)V9) -> 0100.
      *>   2 ADD F2       14.9.2.4 GR2: A + GETX = 2.5 + 4.0 = 6.5 -> 0065.
      *>   3 SUBTRACT F2  14.9.44.4 GR2: GETX - A = 4.0 - 2.5 = 1.5 -> 0015.
      *>   4 DIVIDE F2    14.9.12.4 GR2 a): GETX / A = 4.0 / 2.5 = 1.6 -> 0016.
      *>   5 MULTIPLY F1  identifier-1 an inline invocation: B (3.3) x 4.0 = 13.2 -> 0132.
      *>   6 ADD F1       identifier-1 a numeric function (15.44.1: INTEGER(4.5) = 4, "the greatest
      *>                  integer value that is less than or equal to the argument"): B + 4 = 13.2 + 4
      *>                  = 17.2 -> 0172.
      *>   7 DIVIDE F1    two DATA receivers, identifier-1 a numeric function (INTEGER(2.9) = 2):
      *>                  B 17.2 / 2 = 8.6 -> 0086; C 1.6 / 2 = 0.8 -> 0008.
      *>   8 EVALUATE     a SOLE alphanumeric inline invocation is a legal comparand (8.4.3.4.4 GR1: the
      *>                  temporary has the method's RETURNING class) - "12" = "12" -> YES.
      *>   9 relation     the same in an IF (8.8.4.2.1) -> YES.
      *>  10 COMPUTE      8.8.1.1: a NUMERIC inline invocation is an arithmetic operand: 4.0 x 2 = 8.0 -> 0080.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1142AO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1142AC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3)V9 VALUE 2.5.
       01 B PIC 9(3)V9 VALUE 3.3.
       01 C PIC 9(3)V9 VALUE 0.
       01 OB USAGE OBJECT REFERENCE PB1142AC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1142AC "NEW" RETURNING OB.
           MULTIPLY A BY OB :: "GETX" GIVING C.
           DISPLAY "1=" C.
           ADD A TO OB :: "GETX" GIVING C.
           DISPLAY "2=" C.
           SUBTRACT A FROM OB :: "GETX" GIVING C.
           DISPLAY "3=" C.
           DIVIDE A INTO OB :: "GETX" GIVING C.
           DISPLAY "4=" C.
           MULTIPLY OB :: "GETX" BY B.
           DISPLAY "5=" B.
           ADD FUNCTION INTEGER(4.5) TO B.
           DISPLAY "6=" B.
           DIVIDE FUNCTION INTEGER(2.9) INTO B C.
           DISPLAY "7=" B " " C.
           EVALUATE OB :: "GETNAME"
               WHEN "12" DISPLAY "8=YES"
               WHEN OTHER DISPLAY "8=NO"
           END-EVALUATE.
           IF OB :: "GETNAME" = "12"
               DISPLAY "9=YES"
           ELSE
               DISPLAY "9=NO"
           END-IF.
           COMPUTE C = OB :: "GETX" * 2.
           DISPLAY "10=" C.
           STOP RUN.
       END PROGRAM PB1142AO.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1142AC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC 9(3)V9.
       PROCEDURE DIVISION RETURNING LK-X.
       MAIN.
           MOVE 4 TO LK-X.
       END METHOD GETX.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC X(2).
       PROCEDURE DIVISION RETURNING LK-N.
       MAIN.
           MOVE "12" TO LK-N.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB1142AC.
