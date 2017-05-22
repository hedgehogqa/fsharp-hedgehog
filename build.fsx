#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing

Target "Build" <| fun _ ->
    !! "Hedgehog.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

Target "Doctest" (fun _ ->
    CreateDir "Doctest"
    CopyFiles "Doctest" [
        "packages/testing/Argu/lib/net40/Argu.dll"
        "packages/testing/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
        "packages/testing/FSharp.Core/lib/net45/FSharp.Core.dll"
        "packages/testing/FSharp.Data/lib/net40/FSharp.Data.dll"
        "packages/testing/Unquote/lib/net40/Unquote.dll"
        "packages/testing/Doctest/lib/net452/Doctest.exe"
        "packages/testing/Doctest/lib/net452/Doctest.exe.config"
        ]
    let cmd =
        "Doctest/Doctest.exe"
    let arg =
        System.IO.Path.Combine
            (__SOURCE_DIRECTORY__, "src/Hedgehog/bin/Release/Hedgehog.dll")

    let exitCode =
        Shell.Exec (cmd, arg)

    if  exitCode = 0 then
        ()
    else
        failwithf "The command %s %s exited with code %i" cmd arg exitCode)

Target "Test" (fun _ ->
    !! "**/bin/Release/*Hedgehog.*Tests.dll"
    |> xUnit2 (fun p -> { p with Parallel = ParallelMode.All }))

Target "NuGet" (fun _ ->
    Paket.Pack (fun p -> { p with OutputPath = ".nuget" }))

"Build"
==> "Doctest"
==> "Test"
==> "NuGet"

RunTargetOrDefault "Test"
