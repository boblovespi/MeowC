# Examples

## Hello, world

Needs strings to be defined
```meow
let main: proc := [] {
	print "meow :3\n";
}
```

## Factorial 

```meow
let factorial: i32 -> i32 := n |-> {
	1; if n = 0,
	n * factorial(n - 1), otherwise.
};
```

## Basic types

```meow
let unit: 1 := 1;
let boolean: bool := false;
let enum: 9 := 5;
let integer: i32 := 10000;
let char: i8 := 'a';

let main: proc := [] {
	print unit; // prints '()'
	if (~boolean)
		print enum; // prints '5'
	print integer; // prints '10000'
	print char; // prints 'a'
}
```

## Functions

```meow
let add: i32 * i32 -> i32 := (x, y) |-> x + y;
let curriedAdd: i32 -> i32 -> i32 := x -> y -> x + y;
let empty: () -> () := x |-> ();
```