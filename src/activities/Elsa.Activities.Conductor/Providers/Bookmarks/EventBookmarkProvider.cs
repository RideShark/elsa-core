using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.Conductor.Providers.Bookmarks
{
    public record EventBookmark(string EventName) : IBookmark
    {
        /// <summary>
        /// Compares this bookmark instance with another to check, if the values are equal for the function of the bookmark.
        /// </summary>
        /// <returns><see langword="null"/> if default and no specific compare is done, false if not equal and true otherwise.</returns>
        public bool? Compare(IBookmark bookmark) => null;
    }

    public class EventBookmarkProvider : BookmarkProvider<EventBookmark, EventReceived>
    {
        public override bool SupportsActivity(BookmarkProviderContext<EventReceived> context) => context.ActivityType.Type == typeof(EventReceived);
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<EventReceived> context, CancellationToken cancellationToken) => await GetBookmarksInternalAsync(context, cancellationToken).ToListAsync(cancellationToken);

        private async IAsyncEnumerable<BookmarkResult> GetBookmarksInternalAsync(BookmarkProviderContext<EventReceived> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var eventName = ToLower(await context.ReadActivityPropertyAsync(x => x.EventName, cancellationToken))!;
            yield return Result(new EventBookmark(eventName), nameof(EventReceived));
        }

        private static string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}