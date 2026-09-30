      *> reject-at: 2014 2023
      *> kb/Work PB1264 - ISO 13.18.38.3 SR29: "The implementor shall
      *>   specify a maximum permissible value for integer-4 and
      *>   integer-5. Their values shall not exceed this maximum value."
      *>   This implementation's maximum is 1,073,741,823 (docs/
      *>   CONFORMANCE.md DOC-A.1-60), so FROM 1073741824 is refused
      *>   COBOLNET2647; it used to compile and give the table capacity 0.
      *> cite.py --check 13.18.38.3 "The implementor shall specify a
      *>   maximum permissible value for integer-4 and integer-5" -> OK
      *>   13.18.38.3 29)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1264A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 T PIC X OCCURS DYNAMIC CAPACITY IN C FROM 1073741824.
       PROCEDURE DIVISION.
           STOP RUN.
