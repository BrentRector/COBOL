      *> reject-at: 2002 2014 2023
      *> kb/Work PB1531 - an EC-IMP-suffix names no exception condition.
      *> §14.6.13.1.1: "The first type of level-3 exception-names for
      *> implementor-defined exceptions are defined by the implementor by
      *> creating a level-3 exception-name starting with the characters
      *> 'EC-IMP-'" and "The implementor defines the action to be taken,
      *> the fatality, and when any of these exceptions are raised."
      *>   OK  §14.6.13.1.1  (General)
      *> This implementation defines none (docs/CONFORMANCE.md
      *> DOC-A.1-99), so RAISE EXCEPTION EC-IMP-WIBBLE names nothing
      *> §14.6.13.1 lists: §14.9.29.3 SR1 "Exception-name-1 shall be a
      *> level-3 exception-name" is unsatisfiable, and the program is
      *> refused with COBOLNET0711. (It used to compile and terminate
      *> the run unit with 'EC-IMP-WIBBLE (fatal)'.) The complement -
      *> the level-2 name EC-IMP stays valid - is golden
      *> 2002/l1_dns_a1_99_ec_imp_level2_valid.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1531N1.
       PROCEDURE DIVISION.
       MAIN.
           RAISE EXCEPTION EC-IMP-WIBBLE
           STOP RUN.
