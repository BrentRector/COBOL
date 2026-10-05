      *> reject-at: 2002 2014 2023
      *> kb/Work PB1212 - ISO 13.18.22.3 SR4: "The EXTERNAL clause shall not be specified for a data item of class object or pointer."   cite.py: OK  13.18.22.3 4)  (Syntax rules)
      *> A's description is that of PT (13.18.57.4 GR1), a USAGE POINTER type, so A is a data item of class pointer with the EXTERNAL clause specified.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1212EXTTYPEPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PT TYPEDEF USAGE POINTER.
       01  A TYPE PT EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
