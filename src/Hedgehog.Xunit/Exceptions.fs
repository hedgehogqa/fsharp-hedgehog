namespace Hedgehog.Xunit

open Xunit.Sdk

// This exists to make it clear to users that the exception is in the return of their test.
// Raising System.Exception isn't descriptive enough.
// Using Xunit.Assert.True could be confusing since it may resemble a user's assertion.
type internal TestReturnedFalseException() =
  inherit System.Exception("Test returned `false`.")

/// Exception for property test failures that produces clean output
type PropertyFailedException(message: string) =
    inherit XunitException(message)
    
    override this.ToString() = message
    
    // Override StackTrace to return empty string to hide xUnit's "Stack Trace:" section
    override this.StackTrace = "<xUnit stack trace hidden>"

    interface IAssertionException
