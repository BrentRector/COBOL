      *> reject-at: 2002 2014 2023
      *> kb/Work PB1551 - ISO 8.4.3.11.3 SR5: "This identifier format shall not
      *> be specified as a receiving operand." (cite.py: OK 8.4.3.11.3 5))
      *> The data-address-identifier ADDRESS OF B is the legal SENDER of the
      *> first SET; as INITIALIZE's identifier-1 it is a receiving operand
      *> (COBOLNET3979). SET Format 7's own receiver ADDRESS OF data-name-1 is
      *> not a data-address-identifier (Annex D.9.2.1) and stays legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551DA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 B PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET P TO ADDRESS OF B
           INITIALIZE ADDRESS OF B
           STOP RUN.
       END PROGRAM PB1551DA.
