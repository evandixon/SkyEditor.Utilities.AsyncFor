using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static SkyEditor.Utilities.AsyncFor.AsyncFor;

namespace SkyEditor.Utilities.AsyncFor
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Asynchronously runs <paramref name="delegateFunction"/> for every item in the given collection
        /// </summary>
        /// <typeparam name="T">Type of the collection item</typeparam>
        /// <param name="collection">The collection to be enumerated</param>
        /// <param name="delegateFunction">The function to asynchronously run</param>
        /// <param name="runSynchronously">Whether or not to allow running multiple tasks at once. Defaults to false.</param>
        /// <param name="batchSize">The maximum number of tasks to run at once, or 0 or negative for no limit.</param>
        /// <param name="progressReportToken">Optional token to receive progress updates</param>
        /// <exception cref="InvalidOperationException">Thrown if execution starts before the end of another operation</exception>
        public static async Task RunAsyncForEach<T>(this IEnumerable<T> collection, ForEachItemAsync<T> delegateFunction, bool runSynchronously = false, int batchSize = 0, ProgressReportToken progressReportToken = null)
        {
            await AsyncFor.ForEach(collection, delegateFunction, runSynchronously, batchSize, progressReportToken);
        }

        /// <summary>
        /// Asynchronously runs <paramref name="delegateFunction"/> for every item in the given collection
        /// </summary>
        /// <typeparam name="T">Type of the collection item</typeparam>
        /// <param name="collection">The collection to be enumerated</param>
        /// <param name="delegateFunction">The function to asynchronously run</param>
        /// <param name="runSynchronously">Whether or not to allow running multiple tasks at once. Defaults to false.</param>
        /// <param name="batchSize">The maximum number of tasks to run at once, or 0 or negative for no limit.</param>
        /// <param name="progressReportToken">Optional token to receive progress updates</param>
        /// <exception cref="InvalidOperationException">Thrown if execution starts before the end of another operation</exception>
        public static async Task RunAsyncForEach<T>(this IEnumerable<T> collection, ForEachItem<T> delegateFunction, bool runSynchronously = false, int batchSize = 0, ProgressReportToken progressReportToken = null)
        {
            await AsyncFor.ForEach(collection, delegateFunction, runSynchronously, batchSize, progressReportToken);
        }
    }
}
