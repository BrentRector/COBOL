       >>LEAP-SECOND ON
      *> kb/Work PB1379 (row GR-7.3.17.4-4) - 7.3.17.4 GR4: with LEAP-SECOND ON "a standard numeric time
      *>   form value shall be greater than or equal to zero and less than 86,401".  The floor is the
      *>   same under ON as under OFF: a COMP-2 / COMP-1 argument of -1.0E-10 is below zero and takes the
      *>   out-of-range path (EC-ARGUMENT-FUNCTION, observed by the declarative) instead of being
      *>   truncated to 0 and formatted as midnight.  The ceiling is 86,401 EXCLUSIVE: 86400 (the leap
      *>   second) is in the form and 86401 is the first value out of it.
      *>   Also 15.54.4 r2: LOCALE-TIME-FROM-SECONDS returns "a character-string containing hours,
      *>   minutes, and seconds of the time specified by argument-1" - for the leap second that time is 23:59:60 (15.3.3.3: the
      *>   seconds subfield may be 60 under ON), the same reading FORMATTED-TIME gives 86400 (235960),
      *>   not the 24:00:00 a day does not have.  The locale is NAMED (de-DE, time pattern HH:mm:ss) so the
      *>   expected text does not depend on the host's current locale.
      *>   cite.py --check 7.3.17.4 "a standard numeric time form value shall be greater than or equal
      *>     to zero and less than 86,401" -> OK 7.3.17.4 4)
      *>   cite.py --check 15.54.4 "The returned value is a character-string containing hours,
      *>     minutes, and seconds of the time specified by argument-1" -> OK 15.54.4 2)
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1379FLN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE GER IS "de-DE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SECS2 COMP-2 VALUE -1.0E-10.
       01 SECS1 COMP-1 VALUE -1.0E-10.
       01 LEAP COMP-2 VALUE 86400.
       01 OVER COMP-2 VALUE 86401.
       01 D    PIC 9(7) VALUE 143951.
       01 R    PIC X(40).
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
           DISPLAY "1-FORMATTED-TIME-COMP-2-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" SECS2) TO R
           DISPLAY "2-FORMATTED-TIME-COMP-1-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" SECS1) TO R
           DISPLAY "3-FORMATTED-DATETIME-COMP-2-BELOW-ZERO"
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D SECS2)
               TO R
           DISPLAY "4-LOCALE-TIME-FROM-SECONDS-COMP-2-BELOW-ZERO"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(SECS2) TO R
           DISPLAY "5-THE-LEAP-SECOND-IS-IN-THE-FORM"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" LEAP) TO R
           DISPLAY "  " R
           DISPLAY "6-86401-IS-THE-FIRST-VALUE-OUT-OF-IT"
           MOVE FUNCTION FORMATTED-TIME("hhmmss" OVER) TO R
           DISPLAY "7-LOCALE-TIME-OF-THE-LEAP-SECOND"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(86400 GER) TO R
           DISPLAY "  " FUNCTION TRIM(R)
           DISPLAY "8-LOCALE-TIME-OF-THE-LAST-ORDINARY-SECOND"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(86399 GER) TO R
           DISPLAY "  " FUNCTION TRIM(R)
           STOP RUN.
