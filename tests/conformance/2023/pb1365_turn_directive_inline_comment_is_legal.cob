      *> PB1365 - ISO 7.3.3 SR3: a directive may be followed by an optional
      *>   inline comment; only OTHER words after CHECKING ON|OFF [WITH
      *>   LOCATION] are refused (the negative pb1365-turn-directive-trailing-
      *>   word). This is the control: the same directive with a trailing
      *>   inline comment compiles and runs.
      *> cite.py --check 7.3.3 "may be followed only by space characters
      *>   and an optional inline comment" -> OK  7.3.3 3)
       >>TURN EC-ALL CHECKING OFF *> nothing else follows the phrase
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365P.
       PROCEDURE DIVISION.
           DISPLAY "TURN-OK"
           STOP RUN.
