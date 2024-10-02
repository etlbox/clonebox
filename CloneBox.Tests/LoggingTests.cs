using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using static CloneBox.Tests.ComplexClassTests;

namespace CloneBox.Tests {
    public class LoggingTests {

        ILogger _output;
        public LoggingTests(ITestOutputHelper output) {
            // Pass the ITestOutputHelper object to the TestOutput sink
            _output = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output, Serilog.Events.LogEventLevel.Verbose)                
                .CreateLogger();
        }
        [Fact]
        public void CheckLoggingOutput() {

            var orig = ComplexClass.CreateTestObject();

            var clone = orig.CloneX(new CloneSettings() {
                Logger = new SerilogLoggerFactory(_output).CreateLogger("Default")
            });

        }
    }
}
