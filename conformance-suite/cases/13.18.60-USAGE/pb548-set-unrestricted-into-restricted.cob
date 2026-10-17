*>--- conformance-case
*> id: pb548-set-unrestricted-into-restricted
*> rules: [GR-13.18.60.4-23, SR-14.9.39.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB548 - THE THIRD ARM of SET Format 7. `SET P TO R`, a plain pointer-item sender, reaches
      *> the binder through setToValueStatement (SetBinder.BindSetPointer), not setAddressStatement
      *> (PtrBinder.BindSetAddress), and it used to check only the operands' CATEGORY: the unrestricted
      *> address in R was stored into the restricted P, and `SET ADDRESS OF L TO P` then described the
      *> first four characters of the PIC X(8) item C as type TT.
      *> ISO 14.9.39.3 SR19: "If identifier-5 references a restricted data-pointer, identifier-6 shall be
      *> the predefined address NULL or shall reference a data-pointer restricted to the same type."
      *> (python scripts/spec/cite.py --check 14.9.39.3 "If identifier-5 references a restricted
      *> data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
      *> restricted to the same type" -> OK 14.9.39.3 19))
      *> The rule it protects, 13.18.60.4 GR23: "A restricted data-pointer shall contain only the
      *> predefined address NULL or the address of a data item of the specified type." (cite.py --check
      *> 13.18.60.4 -> OK 23)). R is USAGE POINTER with no TO phrase - unrestricted - so it is neither.
      *> Below 2002 the whole family draws the TYPEDEF introduction gate instead, hence reject-at 2002 up.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB548NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT IS TYPEDEF STRONG.
          05 F1 PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO TT.
       01 P TYPE PT.
       01 R USAGE POINTER.
       01 C PIC X(8) VALUE "12345678".
       01 L TYPE TT BASED.
       PROCEDURE DIVISION.
       MAIN-P.
           SET R TO ADDRESS OF C
           SET P TO R
           SET ADDRESS OF L TO P
           DISPLAY "L=[" F1 OF L "]"
           STOP RUN.
