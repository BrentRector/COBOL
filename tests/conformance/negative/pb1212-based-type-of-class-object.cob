      *> reject-at: 2002 2014 2023
      *> kb/Work PB1212 - ISO 13.18.5.3 SR1: "The subject of the entry shall not be of class object."   cite.py: OK  13.18.5.3 1)  (Syntax rules)
      *> O's description is that of T (13.18.57.4 GR1), a USAGE OBJECT REFERENCE type, so O is of class object and the BASED clause written at the site violates SR1 -
      *> exactly as the direct spelling `01 O USAGE OBJECT REFERENCE BASED.` does.  The screen used to run over the WRITTEN entries only and never saw a TYPE entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1212BASEDTYPEOBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T USAGE OBJECT REFERENCE TYPEDEF.
       01  O TYPE T BASED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
