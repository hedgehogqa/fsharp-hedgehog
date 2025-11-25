namespace Hedgehog.Xunit

// This exists to make it clear to users that the exception is in the return of their test.
// Raising System.Exception isn't descriptive enough.
// Using Xunit.Assert.True could be confusing since it may resemble a user's assertion.
type internal TestReturnedFalseException() =
  inherit System.Exception("Test returned `false`.")
