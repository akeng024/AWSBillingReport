
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace BillingReportLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async void TestToUpperFunction()
        {

            // Invoke the lambda function and confirm the string was upper cased.
            var function = new Function();
            var context = new TestLambdaContext();
            var result = await function.FunctionHandler(context);

            Assert.True(result);
        }
    }
}
