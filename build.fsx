#r @"packages/FAKE/tools/FakeLib.dll"

open Fake

Target "Build" <| fun _ ->
    !! "Jack.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

RunTargetOrDefault "Build"
