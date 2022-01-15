# NiceStateMachineGenerator

Describe state machines declaratively, validate and visualize them, then export to use in your code.


## Background

Sometimes you need to implement a complex behavior in your program.
For example — you may need to to synchronize events from different sources and assure that no 'unexpected' situations happen.

In this situations you may find that you are implementing a state machine.
Some protocols explicitly describe their behavior in terms of state machines (see for example [here](https://datatracker.ietf.org/doc/html/rfc3261#section-17.1.4)).

But with straightforward implementations it's easy to sink in a spaghetti code with all those "if event E1 comes in state S1 and no event E2 came before, but ..".
And even if you write code with a great caution,how could you be sure that you haven't missed another "what-if" situation?

## An Approach

To handle the aforementioned concerns and simplify our lives, we propose the following approach — let's separate the state machine logics and transitions from the rest of the code.
Let's describe the state machine declaratively in a simple DSL, with all the states, events an callbacks needed.
Let's pass this declaration to a formal validation procedure to ensure all the corner cases are handled, and all the transitions are valid.
And then, let's generate a simple code class to wrap all this logic, to use in actual program.

The DSL we propose is language-agnostic enough to support any target language (at least we hope so). For now we have built-in support for C# and C++(20) targets. 
We also support exporting to [Graphviz](https://graphviz.org/) [DOT](https://graphviz.org/doc/info/lang.html) format which allows making nice visualizations like this:
![call_handler sample](samples/call_handler/sm.json.dot.png)

# The DSL