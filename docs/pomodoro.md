# Pomodoro Timers
`jot` includes a simple [Pomodoro Technique](https://en.wikipedia.org/wiki/Pomodoro_Technique) timer.  This timer lets a user start successive 25 minute work intervals, which can be interrupted with breaks (using the `s` or `l` keys for short or long breaks, respectively).  When the timer is finished, the console will beep (unless the `-q` switch is provided to keep the console quiet) and will exit with a report of the time spent (unless the `-a` switch is provided to keep the timer automatically renewing).

By default, porodori timers are 25 minutes each, and breaks are 5 and 15 minutes for short and long breaks, respectively.  These can be specified with different intervals using command line arguments for the `pomodoro start` command.

# Usage
The following output shows `pomo start`, which was interrupted with an early 'd' command to mark the effort complete.  `pomo` is a supported alias for the `pomodoro` command.

```
> pomo start
You can type a letter to E[x]it the timer, take a [s]hort break, a [l]ong break, [r]esume working 
when on break, or mark this task fully [d]one.
                                                                                
🍅 Pomodoro timer ticking ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100% 00:02:27
                                                                                
Done: Completed 2 minutes (02:27) of work.

```

# Help Documentation
```
DESCRIPTION:
Starts a pomodoro timer

USAGE:
    jot pomodoro start [POMODORI_TIME] [SHORT_BREAK_TIME] [LONG_BREAK_TIME] [OPTIONS]

ARGUMENTS:
    [POMODORI_TIME]       Number of seconds for each work interval
    [SHORT_BREAK_TIME]    Number of seconds for each short break  
    [LONG_BREAK_TIME]     Number of seconds for each long break   

OPTIONS:
    -h, --help     Prints help information                                    
    -a, --auto     Automatically resume working when the current timer expires
    -q, --quiet    Do not beep when any timer events complete    
```