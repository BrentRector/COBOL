      *> reject-at: 2002 2014 2023
      *> kb/Work PB665. ISO 14.9.39.3 SR19, last sentence: "If identifier-6 references a restricted data-pointer, either
      *> identifier-5 shall reference a data-pointer restricted to the same type or data-name-1 shall be a typed item of the
      *> type to which identifier-6 is restricted." SET ADDRESS OF names no identifier-5, so data-name-1 shall be that typed
      *> item: the untyped window W (from the pointer PS and from ADDRESS OF HOLDER, the other spelling of identifier-6)
      *> and R3, typed to a DIFFERENT, weakly-typed type, are each refused COBOLNET0869 ("third sentence"). The
      *> conforming spelling (R2, TYPE T-REC) and a plain pointer sender (PL) are legal and are the positive golden
      *> 2002/pb665_restricted_pointer_conforming.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB665N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-A PIC X(4).
          05 T-B PIC X(8).
       01 O-REC IS TYPEDEF.
          05 O-A PIC X(12).
       01 T-RP IS TYPEDEF USAGE POINTER TO T-REC.
       01 HOLDER TYPE T-REC.
       01 PS TYPE T-RP.
       01 PL USAGE POINTER.
       01 W PIC X(12) BASED.
       01 R3 TYPE O-REC BASED.
       01 R2 TYPE T-REC BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET PS TO ADDRESS OF HOLDER
           SET ADDRESS OF W TO PS
           SET ADDRESS OF R3 TO PS
           SET ADDRESS OF W TO ADDRESS OF HOLDER
           SET ADDRESS OF R2 TO PS
           SET ADDRESS OF W TO PL
           STOP RUN.
