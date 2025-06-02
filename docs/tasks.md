`jot` includes a task management system that allows users to create, list, and update the status of tasks, or "todos".

Tasks are stored like other things, and can therefore be manipulated with general commands like `thing set`.  However, there are a variety of task-specific commands that provide convenience features managing tasks, loosely adhering to the principals of [todo.txt](https://github.com/todotxt/todo.txt), the [Getting Things Done (TM)](https://gettingthingsdone.com/what-is-gtd/) productivity methodology, and the style of [Ultralist](https://github.com/gammons/ultralist), a nice but defunct CLI for managing tasks.  `jot` includes substantially all the functionality of Ultralist as part of its overarching functionality.

# Concepts
## Contexts
`jot` knows about _contexts_, which answers the question of “what do I need in order to accomplish this?”. Contexts are denoted with an `@` symbol.

**Examples of contexts**
- `chat with @sean`
- `@call dealership about car repair`
- `@email my question about the project update`

## Projects
_Projects_ are defined as outcomes that will require more than one action step to complete. So, a project may have multiple tasks.

**Examples of projects**
- `+smallTasks ping @sean about the security bulletin`
- `+mobileDev ask @ringler what is next on the roadmap`

## Prioritization
You can prioritize a task. This will make it show as bold in the output, and denotes that this task is more important than others.

## Archiving
Once tasks are completed, you can additionally archive them. Archived tasks do not show up anymore when listing tasks, unless specifically requested with a flag.

# Showing Tasks
You can lists all tasks using this command.

`jot task list`

By default, this excludes archived tasks, but using filters (documented below), you can hide and show tasks based on their data.  "`ls`" and "`l`" are additional aliases for "`list`".

### Filtering tasks
Filter criteria allow specific tasks to be shown or hidden based on their data. Filter criteria is in the form of `attribute:value`, much like the syntax for filtering Github issues.

Here are the following filters available:
- `due`
- `duebefore`
- `dueafter`
- `completed`
- `priority`
- `archived`
- `status`
- `project`
- `context`

### Filtering by date

`due:(tod|today|tom|tomorrow|thisweek|nextweek|lastweek|mon|tue|wed|thu|fri|sat|sun|none|<specific date>)`

**Examples**

- `jot task l due:tod` - show tasks due today
- `jot task l duebefore:tom` - show tasks due before tomorrow (today and earlier)
- `jot task l dueafter:tod` - show tasks due after today

Only one `due` filter is allowed.

### Filtering tasks by completion or priority

* `completed:true`
- `completed:false`
- `priority:true`
- `priority:false`

**Examples**

- `jot task l completed:true` - show only completed tasks
- `jot task l completed:false` - show only incomplete tasks
- `jot task l priority:true` - show only prioritized tasks
- `jot task l priority:false` - show only non-prioritized tasks

### Filtering archived tasks

* `archived:true`
- `archived:false` - note that this option is **implicitly added**. jot defaults to not showing archived tasks.

**Examples**

- `jot task l archived:true` - show archived tasks

### Filtering by completion date

- `completed:(tod|today)`
- `completed:thisweek`

**Examples**

- `jot task l completed:tod` - show tasks that were completed today
- `jot task l completed:thisweek` - show tasks that were completed this week

### Filtering by a project, context, or status

- `jot task l project:mobile` - Show all tasks with a project of `mobile`
- `jot task l project:mobile,devops` - Show all tasks with a project of `mobile` or `devops`

**Negation filters**

Adding a minus (`-`) to a project, context or status will remove those matching tasks from the list.

- `jot task l project:-devops` - Show all tasks **without** a `devops` project.
- `jot task l project:mobile,-devops` - Show only tasks with a project of `mobile` but exclude tasks with a `devops` project.

**Combining things**

- `jot task l project:mobile status:next due:tod` - Show all tasks with a project of `mobile`, a status of `next`, and is due today.

**Combining with grep or fzf**

`jot` is a \*nix tool, just like any other. You can use `grep` to combine a complex listing with a filter.

Example: `jot task l due:tom | grep @bob`

### Grouping

- `group:project` 
- `group:context`
- `group:status`

**Examples**
- `jot task l group:project` or `jot task l group:p` - List all tasks, grouped by project.
- `jot task l group:context` or `jot task l group:c` - List all tasks, grouped by context.
- `jot task l group:status` or `jot task l group:s` - List all tasks, grouped by status.

### Showing tasks with notes

Use the `--notes` flag to show notes on tasks when listing.

- `jot task l --notes duebefore:tom group:p`

### Examples combining groups and listing filters
- `jot task l duebefore:tom status:now` - Show all tasks due today or earlier, with the status of `now`
- `jot task l group:context due:tom` - Show all tasks due tomorrow, and group them by context:
- `jot task l completed:tod` - Look back at all the tasks you completed today, and feel good about yourself:

# Technical Details

Tasks are system schemas, and to view their definition, one can `select task` and then print the detail of the `task` system schema with the print command shortcut `?`, like so:

```
> s task
Done: Schema task selected.

(task)> ?

Schema       task                                                                
Description  System schema for tasks                                             
Plural       tasks                                                               
Properties   ╭───────────────┬───────────────────────┬──────────────┬───────────╮
             │ Property Name │ Data Type             │ Display Name │ Required? │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ complete      │ date                  │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ due           │ date                  │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ assignee      │ person                │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ notes         │ array of string       │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ archived      │ bool                  │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ id            │ increment             │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ priority      │ bool                  │ ❌           │ ❌        │
             ├───────────────┼───────────────────────┼──────────────┼───────────┤
             │ status        │ enum [now,next,later] │ ❌           │ ❌        │
             ╰───────────────┴───────────────────────┴──────────────┴───────────╯

```

Because tasks use the standard mechanisms of schemas and things in Figment, you can review tasks like any other things with its schema Plural keyword to see it with its schema-defined [[Views|view]], like so:

```
> tasks
┌───────────────────────────────────────┬──────────┬───────────┬───────────┐
│ Name                                  │ Assignee │ complete  │ due       │
├───────────────────────────────────────┼──────────┼───────────┼───────────┤
│ Don't forget the milk                 │ <UNSET>  │ 5/30/2025 │ 5/27/2025 │
│ Call @mom and @dad about the +project │ <UNSET>  │ <UNSET>   │ 5/29/2025 │
└───────────────────────────────────────┴──────────┴───────────┴───────────┘
```

However, using the specialized `task list` command displays tasks in a more intuitive format useful for task management, like so:

```
> task list
all
1      [✔]      Tue May 27     Don't forget the milk                 
2      [ ]      Thu May 29     Call @mom and @dad about the +project 
```

You may edit the enum for `status` to use custom statuses for tasks of your choosing.  The `task` command of `jot` expects certain properties to be present on the task schema (GUID `00000000-0000-0000-0000-000000000004`), in particular:

* id
* complete
* due
* priority
* archived
* status
* notes

Deleting or changing the data types of these fields may inhibit the built-in `task` commands from working correctly.  However, changing them back with the built-in commands of `jot` will restore their functionality.

# Adding Tasks

## Creating a new task
Add a task by running `jot task add` or `jot task a`, and then filling out the details of the task.

A `+project` or `@context` can be inserted into the task name, or body. They can only be one word long. `jot` expects the due date at the _end_, if there is a due date.

### Due date format

For things due today or tomorrow, you can use `due:today` and `due:tomorrow`. You can also use `due:tod` or `due:tom`.

For things due this week, you can use the first 3 letters of the day name. For instance, `due:mon` or `due:thu`. **`jot` will always look forward**. If today is a Wednesday and you specify `due:mon`, the due date will be for the _next Monday_.

For specific dates, you can use either `due:may2` or `due:2may`. The month is always lowercase and 3 letters.

**Examples**
* `jot task add chat with @bob about +specialProject due:tom`
* `jot task a "+lunch make turkey sandwich" --priority`
* `jot task a "+todo respond to @shelly about +mobile due:may3" --status next`

Of note, this format is slightly different than `Ultralist`, since specifying priority or status is done with command line arguments (`--priority` or `--status`) rather than parsing of these values from the text body (such as `priority:true` or `status:next` in that utility).  Contexts and projects, however, are inferred from mentions in task bodies, such as "+lunch" for a project named 'lunch' and "@shelly" for a context involving 'shelly'.  Zero or many contexts and projects can be added in any given task body.

# Managing Tasks
## Completing/Uncompleting tasks

- `jot task complete [id]` or `jot task c [id]` - complete a task
- `jot task uncomplete [id]` or `jot task uc [id]` - un-complete a task

**Examples**
```
jot task complete 35
jot task c 35
jot task uc 35
```

## Archiving/Unarchiving Tasks

- `jot task archive [id]` or `jot task ar [id]` - archive a task
- `jot task unarchive [id]` or `jot task uar [id]` - unarchive a task
- `jot task ar c` - archive all completed tasks (_a great command to run at the end of the day!_)
- `jot task ar gc` - Garbage collect.  This renumbers all tasks so any holes in the numbering are closed by updating every task sequentially starting from 1.

**Examples**
```
jot task archive 35
jot task ar 35
jot task uar 35
jot task ar c
```