*>--- conformance-case
*> id: pb457_capacity_register_qualified
*> rules: [SR-14.9.39.3-29]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB457 - a QUALIFIED reference to an OCCURS DYNAMIC CAPACITY register.
      *> ISO 13.18.38.3 SR30: "Data-name-3 shall not be defined elsewhere in the source element. If qualifiers
      *> are required for uniqueness, it shall be treated as though implicitly defined at the same level as the
      *> entry containing the OCCURS clause" - so WS-CAP stands beside WS-E and WS-TABLE qualifies it.
      *> ISO 8.4.2.2.3 SR2: "A name may be qualified even though it does not need qualification" - both the OF
      *> and the IN spelling (8.4.2.2.2 Format 1) are legal on a name that is already unique.
      *> Expected values are DERIVED, not measured:
      *>   INIT  - 8.5.1.9.1 "may be initialized explicitly in the FROM phrase" + 13.18.38.4 GR16
      *>           ("integer-4 is the minimum capacity") => 2.
      *>   TO    - 14.9.39.4 GR30a: the new capacity is arithmetic-expression-4 => 7.
      *>   UP    - GR30b: 7 + 2 => 9.
      *>   DOWN  - GR30c: 9 - 8 => 1, and "If the new capacity of the table is less than the minimum capacity
      *>           defined in the corresponding OCCURS clause, the new capacity of the table shall be the
      *>           minimum capacity" => 2.
      *>   SUM   - the register is a sending arithmetic operand (13.18.38.4 GR15, "a numeric data item that
      *>           contains the current capacity of the associated table"): 2 + 1 => 3.
      *>   COND  - the same value in a relation condition => YES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB457-CAP-QUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC ZZ9.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-CAP OF WS-TABLE TO ED.
           DISPLAY "INIT=[" ED "]".
           SET WS-CAP IN WS-TABLE TO 7.
           MOVE WS-CAP IN WS-TABLE TO ED.
           DISPLAY "TO=[" ED "]".
           SET WS-CAP OF WS-TABLE UP BY 2.
           MOVE WS-CAP OF WS-TABLE TO ED.
           DISPLAY "UP=[" ED "]".
           SET WS-CAP OF WS-TABLE DOWN BY 8.
           MOVE WS-CAP OF WS-TABLE TO ED.
           DISPLAY "DOWN=[" ED "]".
           COMPUTE ED = WS-CAP OF WS-TABLE + 1.
           DISPLAY "SUM=[" ED "]".
           IF WS-CAP OF WS-TABLE = 2
              DISPLAY "COND=YES"
           ELSE
              DISPLAY "COND=NO"
           END-IF.
           STOP RUN.
