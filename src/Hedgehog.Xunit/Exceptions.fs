namespace Hedgehog.Xunit

open Xunit.Sdk

/// Exception for property test failures that produces clean output
type PropertyFailedException(message: string) =
    inherit XunitException(message)
    
    override this.ToString() = message
    
    // Override StackTrace to return empty string to hide xUnit's "Stack Trace:" section
    override this.StackTrace = "<xUnit stack trace hidden>"

    interface IAssertionException
