#if INTERACTIVE
#I __SOURCE_DIRECTORY__
#load @"..\Optics\Common.fs"
#load @"..\Optics\Optics.fs"
module HelloMyriad =
    type Species = Cat | Dog | Lizard | Cow
    type Name = Name of string
    type Animal = { name: Name; kind: Species; age: int }

    type Barn = {
        sign: Name
        pets: Animal list
        }
#load @"Generated.fs"
#else
module Main
#endif
open Optics
open Optics.Operations
open HelloMyriad
open Gen

let barn = {
    sign = Name "Fox Family Farm"
    pets = [
        { name = Name "Tim"; kind = Dog; age = 7 }
        { name = Name "Clara"; kind = Cat; age = 3 }
        { name = Name "Ricki"; kind = Lizard; age = 1 }
        { name = Name "Tim Sr"; kind = Cow; age = 17 }
        ]
    }

let age barn =
    barn |> over Barn.pets_ (List.map (over Animal.age_ ((+)1)))

barn
age barn
barn |> age |> age