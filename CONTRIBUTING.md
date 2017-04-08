# Instructions for Logging Issues

## 1. Search for Duplicates

[Search the existing issues](https://github.com/hedgehogqa/fsharp-hedgehog/issues?utf8=%E2%9C%93&q=is%3Aissue) before logging a new one.

## 2. Do you have a question?

It is preferred to use the issue tracker just for **issues**, in other words, bugs and suggestions.
If you have a *question*, please use [Stack Overflow](http://stackoverflow.com/questions/tagged/hedgehogqa), your favorite search engine, or other resources.

## 3. Did you find a bug?

When logging a bug, please be sure to include the following:
 * What version of Hedgehog you're using
 * If at all possible, an *isolated* way to reproduce the behavior
 * The behavior you expect to see, and the actual behavior

## 4. Do you have a suggestion?

We also accept suggestions in the issue tracker.
Be sure to [search](https://github.com/hedgehogqa/fsharp-hedgehog/issues?utf8=%E2%9C%93&q=is%3Aissue) first.

In general, things we find useful when reviewing suggestions are:
* A description of the problem you're trying to solve
* An overview of the suggested solution
* Examples of how the suggestion would work in various places
  * Code examples showing e.g. "this would be an error, this wouldn't"

# Instructions for Contributing Code

## Contributing bug fixes

Hedgehog is currently accepting contributions in the form of bug fixes. A bug must have an issue tracking it in the issue tracker. Your pull request should include a link to the bug that you are fixing. If you've submitted a PR for a bug, please post a comment in the bug to avoid duplication of effort.

## Contributing features

Features (things that add new or improved functionality to Hedgehog) may be accepted, but will need to first be approved by a Hedgehog coordinator in the suggestion issue.

Design changes will not be accepted at this time. If you have a design change proposal, please log a suggestion issue.

## Housekeeping

Your pull request should: 

* Include a description of what your change intends to do
* Be a child commit of a reasonably recent commit in the **master** branch 
    * Requests need not be a single commit, but should be a linear sequence of commits (i.e. no merge commits in your PR)
* It is desirable, but not necessary, for the tests to pass at each commit
* Have clear commit messages 
    * e.g. "Refactor feature", "Fix issue", "Add tests for issue"
* Include adequate tests 
    * At least one test should fail in the absence of your non-test code changes. If your PR does not match this criteria, please specify why
    * Tests should include reasonable permutations of the target fix/change
    * Include baseline changes with your change
* Respect the coding style already present. Look around in the source code to get a feel for it.
* To avoid line ending issues, set `autocrlf = input` and `whitespace = cr-at-eol` in your git configuration

## Running the Tests

To run all tests, invoke the `Tests` target in the build script:

```Shell
./build.sh Test
```

This run will all tests.
