namespace HelloMyriad

open Myriad.Plugins
open MyMyriad

[<Generator.LensesAttribute("Optics.lens")>]
type Test1 = { one: int; two: string; three: float; four: float32 }
[<Generator.LensesAttribute("Optics.lens")>]
type Test2 = { one: Test1; two: string }

[<Generator.DuCases>]
type Animal = Cat | Dog