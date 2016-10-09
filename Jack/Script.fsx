#r "../packages/FSharpx.Collections/lib/net40/FSharpx.Collections.dll"
#r "../packages/FsControl/lib/net40/FsControl.dll"

#load "Seed.fs"
#load "Tree.fs"
#load "Shrink.fs"
#load "Random.fs"
#load "Gen.fs"

open System
open Jack
open FSharpx.Collections

Gen.printSample <| gen {
    let! x = Gen.choose 0 10
    let! y = Gen.elements [ "x"; "y"; "z"; "w" ]
    return sprintf "%A + %s" x y
}

