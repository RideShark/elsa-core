using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Elsa.Extensions
{

    public static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }
    public static class ConcurrentDictionaryExtensions
    {
        public static void Remove<T, V>(this ConcurrentDictionary<T, V> source, T key)
        {
            source.TryRemove(key, out _);
        }
        public static void Remove<T, V>(this ConcurrentDictionary<T, V> source, T key, out V removedValue)
        {
            source.TryRemove(key, out removedValue);
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }


    public static class StreamExtensions
    {
        public static Task WriteAsync(this Stream stream, byte[] bytes)
        {
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset UnixEpoch => new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static class FileExtensions
    {
        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            using var stream = File.OpenRead(path);
            var result = new byte[stream.Length];
            await stream.ReadAsync(result, 0, (int)stream.Length);
            return result;
        }
    }
    public static class ProcessStartInfoExtensions
    {
        public static ProcessStartInfo AddArgument(this ProcessStartInfo psi, string argument)
        {
            if (string.IsNullOrEmpty(psi.Arguments))
                psi.Arguments = argument;
            else
                psi.Arguments += " " + argument;

            return psi;
        }

        public static ProcessStartInfo AddArgument(this ProcessStartInfo psi, params string[] arguments)
        {
            foreach (var arg in arguments)
            {
                psi.AddArgument(arg);
            }
            return psi;
        }
    }
}

namespace Elsa.Services
{
    public interface IBookmark
    {
        /// <summary>
        /// Compares this bookmark instance with another to check, if the values are equal for the function of the bookmark.
        /// </summary>
        /// <returns><see langword="null"/> if default and no specific compare is done, false if not equal and true otherwise.</returns>
        bool? Compare(IBookmark bookmark);
    }

    public abstract class BaseIBookmark : IBookmark
    {
        /// <summary>
        /// Compares this bookmark instance with another to check, if the values are equal for the function of the bookmark.
        /// </summary>
        /// <returns><see langword="null"/> if default and no specific compare is done, false if not equal and true otherwise.</returns>
        public virtual bool? Compare(IBookmark bookmark) => null;

    }

    // Custom MatchCasing enum
    public enum MatchCasing
    {
        CaseSensitive,
        CaseInsensitive
    }

    // Custom MatchType enum
    public enum MatchType
    {
        Simple,
        CultureAware
    }

    // Custom EnumerationOptions
    public class EnumerationOptions
    {
        public MatchCasing MatchCasing { get; set; }
        public MatchType MatchType { get; set; }
        public bool RecurseSubdirectories { get; set; }
        public bool IgnoreInaccessible { get; set; }
        public int BufferSize { get; set; } // We can define it but won't be used
    }

    // DirectoryExtensions to use custom EnumerationOptions
    public static class DirectoryExtensions
    {

        public static IEnumerable<string> EnumerateFiles(
            this string directoryPath,
            string searchPattern,
            EnumerationOptions options)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

            SearchOption searchOption = options.RecurseSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            IEnumerable<string> files = directoryInfo.EnumerateFiles(searchPattern, searchOption)
                .Where(file => !options.IgnoreInaccessible || CanAccessFile(file))
                .Select(file => file.FullName);

            if (options.MatchCasing == MatchCasing.CaseInsensitive)
            {
                searchPattern = searchPattern.ToLowerInvariant();
                files = files.Where(file => file.ToLowerInvariant().Contains(searchPattern));
            }

            // Note: options.MatchType and options.BufferSize are not used as they can't be handled at this level of abstraction

            return files;
        }

        public static IEnumerable<string> EnumerateFiles(
            this DirectoryInfo directoryInfo,
            string searchPattern,
            EnumerationOptions options)
        {
            SearchOption searchOption = options.RecurseSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            IEnumerable<string> files = directoryInfo.EnumerateFiles(searchPattern, searchOption)
                .Where(file => !options.IgnoreInaccessible || CanAccessFile(file))
                .Select(file => file.FullName);

            if (options.MatchCasing == MatchCasing.CaseInsensitive)
            {
                searchPattern = searchPattern.ToLowerInvariant();
                files = files.Where(file => file.ToLowerInvariant().Contains(searchPattern));
            }

            // Note: options.MatchType and options.BufferSize are not used as they can't be handled at this level of abstraction

            return files;
        }

        private static bool CanAccessFile(FileInfo fileInfo)
        {
            try
            {
                using (fileInfo.OpenRead())
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }




}