//------------------------------------------------------------------------------
//        This code was generated by myriad.
//        Changes to this file will be lost when the code is regenerated.
//------------------------------------------------------------------------------
namespace rec Gen


namespace rec Gen

module Animal =
    open HelloMyriad

    let toString (x: Animal) =
        match x with
        | Cat -> "Cat"
        | Dog -> "Dog"
        | Lizard -> "Lizard"
        | Pig -> "Pig"

    let fromString (x: string) =
        match x with
        | "Cat" -> Some Cat
        | "Dog" -> Some Dog
        | "Lizard" -> Some Lizard
        | "Pig" -> Some Pig
        | _ -> None

    let toTag (x: Animal) =
        match x with
        | Cat -> 0
        | Dog -> 1
        | Lizard -> 2
        | Pig -> 3

    let isCat (x: Animal) =
        match x with
        | Cat -> true
        | _ -> false

    let isDog (x: Animal) =
        match x with
        | Dog -> true
        | _ -> false

    let isLizard (x: Animal) =
        match x with
        | Lizard -> true
        | _ -> false

    let isPig (x: Animal) =
        match x with
        | Pig -> true
        | _ -> false
namespace rec Gen

module Test1 =
    open HelloMyriad

    let one_ =
        Optics.lens (fun (data: Test1) -> data.one) (fun (value: int) (data: Test1) -> { data with one = value })

    let two_ =
        Optics.lens (fun (data: Test1) -> data.two) (fun (value: string) (data: Test1) -> { data with two = value })

    let three_ =
        Optics.lens (fun (data: Test1) -> data.three) (fun (value: float) (data: Test1) -> { data with three = value })

    let four_ =
        Optics.lens (fun (data: Test1) -> data.four) (fun (value: float32) (data: Test1) -> { data with four = value })

module Test2 =
    open HelloMyriad

    let one_ =
        Optics.lens (fun (data: Test2) -> data.one) (fun (value: Test1) (data: Test2) -> { data with one = value })

    let two_ =
        Optics.lens (fun (data: Test2) -> data.two) (fun (value: string) (data: Test2) -> { data with two = value })

module Foo =
    open HelloMyriad

    let Lens' =
        Optics.lens (function Foo x -> x) (fun (value: int) (_: Foo) -> Foo value)
