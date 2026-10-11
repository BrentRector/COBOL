## Entry 1732 — 2026-09-27 04:44 PDT — An unqualified "SR15" I wrote in OoBinder failed the citation audit; qualified

The re-run battery #87's phase −1 (and CI's `audits` job on the PB1628 landing run) found one code-citation defect,
and it was mine, from PB1631 (DEVLOG 1728). My SR16 comment in `OoBinder` said NULL is identifier-5 "(§8.4.3.1.3 SR7)
and whose class SR15 admits". The bare "SR15" binds to the last clause named, §8.4.3.1.3, which has 12 syntax
rules. The rule meant is §14.9.23.3 SR15 ("identifier-5 shall be of class message-tag, numeric, object or pointer").
The original comment named §14.9.23.3 explicitly, and my rewrite dropped it. I ran `cite.py --check` on the rules I
reasoned about, but not on a clause reference I edited in passing. That is rule 1's inherited-citation failure, in a
variant: the citation was right before I touched it. Fixed; `audit_code_citations --check` = 0 findings. The PB1628
landing run was stopped, and the stack lands again with this fix on top.

