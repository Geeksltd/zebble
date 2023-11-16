using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Olive;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace Zebble.Mvvm
{
    public class TestFailedException : Exception
    {
        public TestFailedException(string message) : base(message)
        {
        }
    }

    public abstract class TestScript
    {
        public static void Try(Action action, int attempts = 5)
        {
            try
            {
                Task.Factory.RunSync(doTry);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("=== TEST FAILED ===\n");
                System.Console.WriteLine(ex.Message);
                System.Console.ResetColor();
                throw;
            }

            async Task doTry()
            {
                for (var attempt = attempts; attempt >= 0; attempt--)
                {
                    try
                    {
                        action();
                        return;
                    }
                    catch
                    {
                        if (attempt <= 0) throw;
                        else await Task.Delay((1 + attempt - attempt) * 200);
                    }
                }
            }
        }

        public static async Task AwaitBackgroundNavigationTo<TExpectedCurrentPage>(int timeoutSeconds = 30)
        {
            var start = LocalTime.UtcNow;
            while (true)
            {
                if (ViewModel.ActiveScreen is TExpectedCurrentPage) return;

                else if (LocalTime.UtcNow.Subtract(start).TotalSeconds >= timeoutSeconds)
                    throw new TestFailedException("Current page is not " + typeof(TExpectedCurrentPage).Name);

                else await Task.Delay(100);
            }
        }

        /// <summary>
        /// If the current active screen is not of the specified type, it throws.
        /// </summary>
        [DebuggerStepThrough]
        public static TExpectedCurrentPage On<TExpectedCurrentPage>()
            where TExpectedCurrentPage : ViewModel
        {
            if (ViewModel.ActiveScreen is TExpectedCurrentPage result)
                return result;

            throw new TestFailedException("Current page is " + ViewModel.ActiveScreen.GetType().GetProgrammingName() + " not " + typeof(TExpectedCurrentPage).GetProgrammingName());
        }

        [DebuggerStepThrough]
        public static TExpectedCurrentPage On<TExpectedCurrentPage>(Action<TExpectedCurrentPage> action)
          where TExpectedCurrentPage : ViewModel
        {
            var result = On<TExpectedCurrentPage>();
            action(result);
            return result;
        }

        [DebuggerStepThrough]
        public static void Expect(bool condition)
        {
            Try(() =>
            {
                if (!condition)
                    throw new TestFailedException("Expected condition is not met, an exception is thrown.");
            });
        }

        /// <summary>
        /// Invokes the back button on the nav bar for redirecting to the previous page in the Stack.
        /// </summary>
        [DebuggerStepThrough]
        public static void NavBarBack()
        {
            if (ViewModel.Stack.None())
                throw new TestFailedException("There is no Back button in nav bar.");

            ViewModel.Back();
        }

        /// <summary>
        /// If the specified text does not exist on the bindable properties of the active screen (or its children) an exception is thrown.
        /// </summary>
        [DebuggerStepThrough]
        public static void Expect(string textOnScreen, bool caseSensitive = false)
        {
            Try(() =>
            {
                var screen = ViewModel.ActiveScreen;

                var text = screen.GetType().Name.ToLiteralFromPascalCase() + " | " +
                    new AutoUI.ViewModelNode("", screen, null)
                    .WithAllChildren()
                    .OfType<AutoUI.ValueNode>()
                    .Select(v => v.ValueString.Unless(v.ValueString.OrEmpty().StartsWith("ERR: ")))
                    .Trim().ToString(" | ");

                text += Dialogs.LastToast;

                if (text.Lacks(textOnScreen, caseSensitive))
                    throw new TestFailedException($"Failed to find the text '{textOnScreen}'\nCurrent text: '{text}'");
            });
        }

        /// <summary>
        /// If the specified text does not exist on the bindable properties of the active screen (or its children) an exception is thrown.
        /// </summary>
        [DebuggerStepThrough]
        public static void ExpectToast(string textOnScreen, bool caseSensitive = false)
        {
            Try(() =>
            {
                var contains = Dialogs.LastToast.OrEmpty().Contains(textOnScreen, caseSensitive);
                if (!contains)
                    throw new TestFailedException($"'{textOnScreen}' Not found in the last toast.");
            });
        }

        /// <summary>
        /// Creates an instance of a specified test and runs it.
        /// </summary>
        public static Task Run<TTest>(int timeOutSeconds = 30) where TTest : TestScript, new() => new TTest().Run(timeOutSeconds);

        /// <summary>
        /// Runs all tests in the project.
        /// </summary>
        public static async Task RunAll(int perTestTimeOutSeconds = 30)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => new { x.GetName().Name, Assembly = x })
                .ToArray();

            var assembly = assemblies.FirstOrDefault(x => x.Name.ToLower().IsAnyOf("vm", "~vm"))?.Assembly;

            if (assembly is null)
                throw new Exception("VM assembly is not found. Currently loaded:\n" + assemblies.Select(v => v.Name).ToLinesString());

            var toRun = assembly.GetTypes()
                .Where(t => t.IsA<TestScript>() && !t.IsAbstract)
                .Select(t => t.GetConstructor(new Type[0]))
                .ExceptNull()
                .ToArray();

            foreach (var ctor in toRun)
            {
                var test = (TestScript)ctor.Invoke(new object[0]);
                await test.Run(perTestTimeOutSeconds);
            }
        }

        /// <summary>
        /// Runs SetUp() then Execute() and then TearDown().
        /// </summary>
        public async Task Run(int timeOutSeconds = 30)
        {
            await SetUp().WithTimeout(timeOutSeconds.Seconds());
            await Execute().WithTimeout(timeOutSeconds.Seconds());
            await TearDown().WithTimeout(timeOutSeconds.Seconds());
        }

        protected virtual Task SetUp() => Task.CompletedTask;
        protected abstract Task Execute();
        protected virtual Task TearDown() => Task.CompletedTask;

        /// <summary>
        /// Returns the test file from the TestFiles directory.
        /// The file build type must be set as Content, with 'Copy to output' set to always.
        /// </summary>
        protected FileInfo TestFile(string name)
        {
            var result = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", name).AsFile();

            if (!result.Exists())
                throw new Exception("File not found: " + result.FullName + ".\nIs the file 'build action' set as 'Content' with 'Copy to output' set as 'always'?");

            return result;
        }

        /// <summary>
        /// Invokes the standard back navigation, similar to pressing the device back button, or swiping the screen left on iOS.
        /// </summary>
        protected void Back() => ViewModel.Back();

        protected static DialogsViewModel Dialogs => DialogsViewModel.Current;
    }
}