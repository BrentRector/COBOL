      *> reject-at: 2002 2014 2023
      *> kb/Work PB2292. A class-name is a user-defined word (ISO 8.3.2.2)
      *> and 8.3.2.1 1) says "Reserved words shall not be used as
      *> user-defined words or system-names"; NUMERIC is reserved at
      *> every edition. The grammar defined the rule className twice -
      *> the class-condition operand (NUMERIC, ALPHABETIC, ... or a
      *> word) and the OO class-name (a word) - and ANTLR kept the first,
      *> so CLASS-ID, END CLASS and the REPOSITORY CLASS entry took the
      *> class-condition keywords and this class compiled clean. Now the
      *> two are two rules and NUMERIC cannot name a class.
       IDENTIFICATION DIVISION.
       CLASS-ID. NUMERIC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS NUMERIC.
       FACTORY.
       PROCEDURE DIVISION.
       END FACTORY.
       END CLASS NUMERIC.
