## Filed PB3152: the wave planner keys a report's finisher on any open note its header mentions

Wave 1076's drain plan made `plan-wave-drain`'s review finisher a group named for PB3101, an open note its report only
mentions, and never planned it under its own lead PB3150. The wave unit dropped that group and filed the mechanism
as PB3152. No code changed.

Review R1-1 and R1-2 (answered by the operator): the planner reads ids from the report HEADER only, not its whole text, so the regression test must put the mention there; and two more consumers, `attempts` (the Opus escalation) and `held_by_dispatch` (the dispatch-ledger release), read the same ids and are in the fix direction.
