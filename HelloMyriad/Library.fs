namespace HelloMyriad

open Myriad.Plugins
open MyMyriad

[<Generator.DuCases>]
type Species = Cat | Dog | Lizard | Cow
[<Generator.LensesAttribute>]
type Name = Name of string
[<Generator.LensesAttribute>]
type Animal = { name: Name; kind: Species; age: int }

[<Generator.LensesAttribute>]
type Barn = {
    sign: Name
    pets: Animal list
    }
