*>--- conformance-case
*> id: pb548-set-restricted-into-unrestricted
*> rules: [GR-13.18.60.4-23, SR-14.9.39.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB548 - the CONVERSE direction over the same third arm of SET Format 7: `SET R TO P`
      *> with a plain pointer-item sender (setToValueStatement -> SetBinder.BindSetPointer). P is
      *> restricted to type TT; R is an unrestricted USAGE POINTER.
      *> ISO 14.9.39.3 SR19, its unnumbered continuation paragraph: "If identifier-6 references a
      *> restricted data-pointer, either identifier-5 shall reference a data-pointer restricted to the
      *> same type or data-name-1 shall be a typed item of the type to which identifier-6 is restricted."
      *> (python scripts/spec/cite.py --check 14.9.39.3 "If identifier-6 references a restricted
      *> data-pointer, either identifier-5 shall reference a data-pointer restricted to the same type"
      *> -> OK 14.9.39.3 19))
      *> The SAME statement written with an ADDRESS OF receiver beside it was already refused
      *> (negative/pb450-set-format7-restricted-sender-second-receiver); this plain spelling was not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB548NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT IS TYPEDEF STRONG.
          05 F1 PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO TT.
       01 V TYPE TT.
       01 P TYPE PT.
       01 R USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET P TO ADDRESS OF V
           SET R TO P
           STOP RUN.
