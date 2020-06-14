namespace HelloMyriad

open Myriad.Plugins

[<Generator.Fields; Generator.DuCases>]
type Test1 = { one: int; two: string; three: float; four: float32 }
type Test2 = { one: Test1; two: string }

[<Generator.DuCases>]
type Animal = Cat | Dog