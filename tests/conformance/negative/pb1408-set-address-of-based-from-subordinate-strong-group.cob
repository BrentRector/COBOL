      *> reject-at: 2002 2014 2023
      *> kb/Work PB1408. ISO 8.4.3.11.4 GR2 over a strongly-typed group NESTED in a strong record: T-G is a group
      *> subordinate to the type declaration T-REC (Annex D.8.3: strongly-typed group items "are subordinate to a type
      *> declaration with the STRONG phrase"), so ADDRESS OF T-G OF S is a restricted data-pointer restricted to "the
      *> type of identifier-1", and by 8.5.3.1's second alternative the type of a subordinate item is the declaration
      *> PLUS its relative position and length in it, which T-REC itself (whole record) does not have. 14.9.39.3 SR19:
      *> "If data-name-1 is a strongly-typed group item ..., identifier-6 shall reference a data-pointer restricted
      *> to the type of data-name-1." BS is of type T-REC, so the address of the subordinate group shall not become
      *> the address of BS (it used to compile and abort at run time with EC-BOUND-PTR): refused COBOLNET0869.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1408N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 S TYPE T-REC.
       01 BS TYPE T-REC BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET ADDRESS OF BS TO ADDRESS OF T-G OF S
           STOP RUN.
