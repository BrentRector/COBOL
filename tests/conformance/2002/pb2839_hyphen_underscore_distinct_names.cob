      *> kb/Work PB2839 - A-B and A_B are two COBOL words, so two methods,
      *> two properties and two formals named by them are two of each.
      *> RULE: ISO 8.3.2.1, "Each character of a COBOL word that is not a
      *> special character word shall be selected from the set of basic
      *> letters, basic digits, extended letters, and the basic special
      *> characters hyphen and underscore" (cite.py OK): the hyphen and the
      *> underscore are two characters, so A-B and A_B are two words, and
      *> INVOKE names a method by its externalized name, compared by the
      *> Annex C fold (DOC-A.1-68), which never equates them.
      *> Before PB2839 each pair compiled to ONE C# identifier and Roslyn
      *> refused the program (CS0111, CS0102): a legal program rejected.
      *> DERIVATION (every value is set by this program):
      *>   INVOKE "M-X" USING "11" "12": method M-X displays its formals
      *>   F-A then F_A in that order -> "M-X 11 12".
      *>   INVOKE "M_X" USING "21" "22": method M_X displays F_A then F-A
      *>   -> "M_X 22 21".
      *>   MOVE "PH" TO P-Q OF the factory, MOVE "PU" TO P_Q: the two set
      *>   property methods store into two items, and the get property
      *>   methods read them back -> "PH PU".
      *> EDITION: CLASS-ID and the underscore in a word are 2002; placed at
      *> 2002 (identical at 2014/2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2839MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2839C
           PROPERTY P-Q
           PROPERTY P_Q.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB2839C "M-X" USING "11" "12"
           INVOKE PB2839C "M_X" USING "21" "22"
           MOVE "PH" TO P-Q OF PB2839C
           MOVE "PU" TO P_Q OF PB2839C
           DISPLAY P-Q OF PB2839C " " P_Q OF PB2839C
           STOP RUN.
       END PROGRAM PB2839MAIN.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2839C.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P-Q PIC X(2) VALUE SPACES PROPERTY.
       01 P_Q PIC X(2) VALUE SPACES PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. M-X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F-A PIC X(2).
       01 F_A PIC X(2).
       PROCEDURE DIVISION USING F-A F_A.
       MAIN-PARA.
           DISPLAY "M-X " F-A " " F_A.
       END METHOD M-X.
       METHOD-ID. M_X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F-A PIC X(2).
       01 F_A PIC X(2).
       PROCEDURE DIVISION USING F-A F_A.
       MAIN-PARA.
           DISPLAY "M_X " F_A " " F-A.
       END METHOD M_X.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS PB2839C.
