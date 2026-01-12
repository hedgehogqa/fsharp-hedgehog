/// Reflection utilities for type checking and method invocation
module internal ReflectionHelpers

open System
open System.Reflection
open System.Threading
open System.Threading.Tasks

// ========================================
// Type Checking
// ========================================

let isGenericTask (t: Type) =
    t.IsGenericType && typeof<Task>.IsAssignableFrom(t)

let isGenericValueTask (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<ValueTask<_>>

let isAsync (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Async<_>>

let isResult (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Result<_,_>>

// ========================================
// Method Invocation
// ========================================

let invokeAwaitTask (taskObj: obj) =
    let taskType = taskObj.GetType()
    let awaitTaskMethod =
        typeof<Async>.GetMethods()
        |> Array.find (fun m -> m.Name = "AwaitTask" && m.IsGenericMethod)

    awaitTaskMethod
        .MakeGenericMethod(taskType.GetGenericArguments().[0])
        .Invoke(null, [|taskObj|])

let invokeAsyncRunSynchronously (asyncObj: obj) =
    typeof<Async>
        .GetMethod("RunSynchronously")
        .MakeGenericMethod(asyncObj.GetType().GetGenericArguments())
        .Invoke(null, [| asyncObj; None; Some CancellationToken.None |])

let assertResultOk (resultObj: obj) (markerType: Type) (resultIsOkMethodName: string) =
    markerType
        .GetTypeInfo()
        .GetDeclaredMethod(resultIsOkMethodName)
        .MakeGenericMethod(resultObj.GetType().GetGenericArguments())
        .Invoke(null, [|resultObj|])
