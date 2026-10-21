using Xunit;

// Keep SQL container memory bounded on CI runners. Concurrency scenarios issue parallel requests explicitly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
