module FableExtensions

#if FABLE_COMPILER
let using (resource : 'T when 'T :> System.IDisposable) action = 
    try action(resource)
    finally match (box resource) with null -> () | _ -> resource.Dispose()
#endif