/// Reflection utilities for type checking
module internal ReflectionHelpers

open System
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
    t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Result<_, _>>
