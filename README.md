# SkyEditor.Utilities.AsyncFor
Run the body of a `for` or a `foreach` loop asynchronously.

Features:

* Asynchronous `for`-style loops, with optional custom step count
* Asynchronous `foreach`-style loops
* Monitor progress through events or progress report token
* Optionally run all tasks one at a time, useful in scenarios where race conditions are present but you still want to monitor progress
* Set maximum number of concurrent tasks

## Examples
### `for`

```
// Basic Example: print numbers 0 through 10 (inclusive)
await AsyncFor.For(0, 10, i =>
{
    Console.WriteLine(i);
});

// Custom step count: print only the even numbers from 0 through 10 (inclusive)
await AsyncFor.For(0, 10, i =>
{
    Console.WriteLine(i);
}, stepCount: 2);
```

### `foreach`

```
var sampleData = new[] { 0, 1, 2, 3 };

// Basic usage: print all numbers in the array
await AsyncFor.ForEach(sampleData, data => {
	Console.WriteLine(data);
});

// Extension method: available for all IEnumerable's and IEnumerable<T>'s
await sampleData.RunAsyncForEach(data => {
	Console.WriteLine(data);
});
```

### Concurrency options
```
// Prevent more than 3 tasks from running concurrently
await AsyncFor.For(0, 10, i =>
{
    Console.WriteLine(i);
}, batchSize: 3);

// Run tasks one at a time
await AsyncFor.For(0, 10, i =>
{
    Console.WriteLine(i);
}, runSynchronously: true);
```

### See progress
```
var progressToken = new ProgressReportToken();
progressToken.ProgressChanged += (object sender, ProgressReportedEventArgs e) =>
{
    Console.WriteLine($"Progress: {e.Progress * 100} %");
};
progressToken.Completed += (object sender, EventArgs e) =>
{
    Console.WriteLine("Completed!");
};

await AsyncFor.For(0, 10, i =>
{
    Console.WriteLine(i);
}, progressReportToken: progressToken);
```