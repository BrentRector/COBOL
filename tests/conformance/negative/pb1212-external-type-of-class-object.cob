      *> reject-at: 2002 2014 2023
      *> kb/Work PB1212 - ISO 13.18.22.3 SR4 (cite.py OK): the object half of "class object or pointer", acquired through TYPE - the sibling of
      *> pb1212-external-type-of-class-pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1212EXTTYPEOBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OT TYPEDEF USAGE OBJECT REFERENCE.
       01  A TYPE OT EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
