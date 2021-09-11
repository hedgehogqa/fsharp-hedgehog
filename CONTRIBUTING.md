Thank you for your interest in contributing to Hedgehog! We are very open to newcomers, and have established the following (very simple) process to get you up and running.

1. If the change is significant, open a discussion for it and we can provide guidance.
2. Once the concept is approved, make the changes and issue a PR.
3. Once the PR is approved, we will merge it and include the PR in our changelog.

# Looking for work

We try to tag issues with the `good first issue` label when we feel they are good for newcomers. Any of these issues don't require a discussion before PR, but if the details aren't clear don't hesitate to start a discussion on the issue itself.

# Testing

There are a few ways to make sure your changes are working. The easiest is to just run the tests in `tests/Hedgehog.Tests`, these tests will report passes/failures as usual. The second step is to run the tests in `src/Hedgehog/Script.fsx`, these tests are less clear since they include properties that fail on purpose.

# Performance

The repo also contains benchmarks in `tests/Hedgehog.Benchmarks`, if your changes include potential performance impact, we will review the output of these benchmarks and provide feedback.
