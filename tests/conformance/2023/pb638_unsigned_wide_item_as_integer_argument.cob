      *> kb/Work PB638 - 15.3 type 6 admits "an integer data item", and the compiler's carrier for the
      *>   item is its own business.  An UNSIGNED 16-byte item (USAGE COMP-5 over a PICTURE of 19 or
      *>   more digits) is carried as a UInt128; the bounded integer-argument intake (INTEGER-OF-DATE,
      *>   DATE-OF-INTEGER, CHAR, ...) had no arm for that carrier and the generated C# did not
      *>   compile (CS1503) - a compile-time crash on conforming source.  Every value below is an
      *>   ordinary in-range integer, so the expected results are the functions' own:
      *>   - INTEGER-OF-DATE(20240229): 15.5.2 makes integer date 1 = 1601-01-01, so 2024-02-29 is
      *>     (days from 1601-01-01 to 2024-02-29) + 1 = 154557 (and 1995-02-15 is 143951, the value the
      *>     sibling conformance programs already use) - computed independently with Python datetime;
      *>   - DATE-OF-INTEGER(154557) is the inverse, 20240229;
      *>   - CHAR(66) is the character at ordinal position 66, "A" in the ASCII collating sequence.
      *>   An unsigned-wide value past the long range is an incorrect argument (15.3), which checking
      *>   turns into the condition the declarative observes (the returned value is the implementor's
      *>   and is not asserted).
      *>   cite.py --check 15.3 "An arithmetic expression that will always result in an integer
      *>     value or an integer data item shall be specified" -> OK 15.3 6)
      *>   cite.py --check 15.3 "the EC-ARGUMENT-FUNCTION exception condition is set to exist" -> OK 15.3
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB638UWI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U19 PIC 9(19) COMP-5 VALUE 20240229.
       01 U31 PIC 9(31) COMP-5 VALUE 20240229.
       01 UDT PIC 9(19) COMP-5 VALUE 154557.
       01 UCH PIC 9(19) COMP-5 VALUE 66.
       01 UBG PIC 9(31) COMP-5 VALUE 9999999999999999999999.
       01 R   PIC 9(9).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-INTEGER-OF-DATE-19"
           DISPLAY "  " FUNCTION INTEGER-OF-DATE(U19)
           DISPLAY "2-INTEGER-OF-DATE-31"
           DISPLAY "  " FUNCTION INTEGER-OF-DATE(U31)
           DISPLAY "3-DATE-OF-INTEGER"
           DISPLAY "  " FUNCTION DATE-OF-INTEGER(UDT)
           DISPLAY "4-CHAR"
           DISPLAY "  " FUNCTION CHAR(UCH)
           DISPLAY "5-PAST-THE-LONG-RANGE"
           COMPUTE R = FUNCTION INTEGER-OF-DATE(UBG)
           STOP RUN.
