      *> reject-at: 2002 2014 2023
      *> kb/Work PB1408. ISO 8.4.3.11.4 GR2: "If identifier-1 is a strongly-typed group item or a restricted
      *> data-pointer, the data-address-identifier is a restricted data-pointer that is restricted to the type of
      *> identifier-1." RP is a restricted data-pointer declared TYPE PT, so it is of type PT and ADDRESS OF RP is
      *> restricted to PT -- never to T-REC, the type of the item RP's value addresses. 14.9.39.3 SR19: "If data-name-1
      *> is a strongly-typed group item ..., identifier-6 shall reference a data-pointer restricted to the type of
      *> data-name-1." BS is a strongly-typed group of type T-REC, so a pointer cell (ADDRESS OF RP, restricted to PT)
      *> shall not become the address of BS: refused COBOLNET0869. The conforming spellings are the positive golden
      *> 2002/pb1408_address_of_restricted_pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1408N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 RP TYPE PT.
       01 BS TYPE T-REC BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET ADDRESS OF BS TO ADDRESS OF RP
           STOP RUN.
