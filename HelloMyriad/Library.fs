﻿namespace HelloMyriad

open Myriad.Plugins

[<Generator.Fields>]
type Test1 = { one: int; two: string; three: float; four: float32 }
type Test2 = { one: Test1; two: string }