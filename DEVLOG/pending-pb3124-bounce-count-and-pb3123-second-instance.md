## PB3124 filed (landing_measure counts a stacked group's failure as a bounce); PB3123 second instance

kb/Work/PB3124 records that `landing_measure.py complete` counts every failed merge-group run naming a PR as that PR's bounce, although a stacked group also fails for a PR ahead of it (PR #29 showed 3 bounces and was never removed for failed checks). kb/Work/PB3123 gains a second measured instance: PR #31 was removed from the queue at 07:19:53Z because #29 and #30 merged ahead with higher entry numbers, and cost one extra merge-group cycle (about 28 minutes).
