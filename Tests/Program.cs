var failures = RefactorRegressionTests.RunAll(args);
if (failures > 0)
{
    Console.Error.WriteLine($"Regression suite failed with {failures} assertion errors.");
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("Regression suite passed.");
}
