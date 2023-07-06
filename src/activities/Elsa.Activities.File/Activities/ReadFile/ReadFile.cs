using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Output input value to specified location",
        Outcomes = new[] { OutcomeNames.Done })]
    public class ReadFile : Activity
    {
        [Required]
        [ActivityInput(Hint = "Path to read content from.", UIHint = ActivityInputUIHints.SingleLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; } = default!;

        [ActivityOutput(Hint = "Bytes of the file read.")]
        public byte[]? Bytes { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async ValueTask<IActivityExecutionResult> Execute(ActivityExecutionContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Bytes = System.IO.File.ReadAllBytes(Path);
            return Done(Bytes);
        }
    }
}
