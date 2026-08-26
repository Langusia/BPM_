using Xunit;

// BProcessGraphConfiguration holds process graphs in static state; test classes
// that register graphs must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
