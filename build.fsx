#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing

Target "Build" <| fun _ ->
    !! "Hedgehog.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

Target "Test" (fun _ ->
    !! "*/bin/Release/*Hedgehog.*Tests.dll"
    |> xUnit2 (fun p -> { p with Parallel = ParallelMode.All }))

"Build"
==> "Test"

RunTargetOrDefault "Test"
