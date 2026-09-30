      *> reject-at: 2002 2014 2023
      *> kb/Work PB1531 - the >>TURN form of the EC-IMP-suffix refusal.
      *> §7.3.25.3 SR2: "Exception-name-1 shall be one of the exception
      *> names listed in 14.6.13.1, Exception conditions."
      *>   OK  §7.3.25.3 2)
      *> §14.6.13.1.1 makes an EC-IMP-suffix name the IMPLEMENTOR's to
      *> define, and this implementation defines none (DOC-A.1-99), so
      *> no exception-name EC-IMP-WIBBLE is listed and the directive is
      *> refused with COBOLNET0711, positioned at the directive's line.
       >>TURN EC-IMP-WIBBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1531N2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI"
           STOP RUN.
