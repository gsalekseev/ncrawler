﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NCrawler.Extensions
{
	public static class StreamExtensions
	{
		#region Class Methods

		/// <summary>
		/// 	Copies any stream into a local MemoryStream
		/// </summary>
		/// <param name = "stream">The source stream.</param>
		/// <returns>The copied memory stream.</returns>
		public static MemoryStream CopyToMemory(this Stream stream)
		{
			var memoryStream = new MemoryStream((int) stream.Length);
			stream.CopyTo(memoryStream);
			return memoryStream;
		}

        public static async Task CopyToAsync(this Stream source, Stream destination,
            Action<Stream, Stream, Exception> completed, Action<uint> progress,
            uint bufferSize, uint? maximumDownloadSize, TimeSpan? timeout)
        {
            var buffer = new byte[bufferSize];

            Action<Exception> done = exception =>
            {
                completed?.Invoke(source, destination, exception);
            };

            var maxDownloadSize = maximumDownloadSize.HasValue
                ? (int)maximumDownloadSize.Value
                : int.MaxValue;
            var bytesDownloaded = 0;
            try
            {
            repeat:
                var bytesRead = await source.ReadAsync(buffer, 0, new[] { maxDownloadSize, buffer.Length }.Min()).WithTimeout(timeout).ConfigureAwait(false);
                var bytesToWrite = new[] { maxDownloadSize - bytesDownloaded, buffer.Length, bytesRead }.Min();
                destination.Write(buffer, 0, bytesToWrite);
                bytesDownloaded += bytesToWrite;
                if (!progress.IsNull() && bytesToWrite > 0)
                {
                    progress((uint)bytesDownloaded);
                }

                if (bytesToWrite == bytesRead && bytesToWrite > 0)
                {
                    goto repeat;
                }
                else
                {
                    done(null);
                }
            }
            catch (Exception e)
            {
                done(e);
            }
        }

        /// <summary>
        /// 	Opens a StreamReader using the specified encoding.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>The stream reader</returns>
        public static StreamReader GetReader(this Stream stream, Encoding encoding)
		{
			if (!stream.CanRead)
			{
				throw new InvalidOperationException("Stream does not support reading.");
			}

			return encoding.IsNull()
				? new StreamReader(stream, true)
				: new StreamReader(stream, encoding);
		}

		/// <summary>
		/// 	Reads all text from the stream using the default encoding.
		/// </summary>
		/// <param name = "stream">The stream.</param>
		/// <returns>The result string.</returns>
		public static string ReadToEnd(this Stream stream)
		{
			return stream.ReadToEnd(null);
        }

        /// <summary>
        /// 	Reads all text from the stream using the default encoding.
        /// </summary>
        /// <param name = "stream">The stream.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the TResult
        /// parameter contains a string with the characters from the current position to
        /// the end of the stream.
        /// </returns>
        public static async Task<string> ReadToEndAsync(this Stream stream)
        {
            return await stream.ReadToEndAsync(null).ConfigureAwait(false);
        }

        /// <summary>
        /// 	Reads all text from the stream using a specified encoding.
        /// </summary>
        /// <param name = "stream">The stream.</param>
        /// <param name = "encoding">The encoding.</param>
        /// <returns>The result string.</returns>
        public static string ReadToEnd(this Stream stream, Encoding encoding)
		{
			using (var reader = stream.GetReader(encoding))
			{
				return reader.ReadToEnd();
			}
        }

        /// <summary>
        /// 	Reads all text from the stream using a specified encoding.
        /// </summary>
        /// <param name = "stream">The stream.</param>
        /// <param name = "encoding">The encoding.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the TResult
        /// parameter contains a string with the characters from the current position to
        /// the end of the stream.
        /// </returns>
        public static async Task<string> ReadToEndAsync(this Stream stream, Encoding encoding)
        {
            using (var reader = stream.GetReader(encoding))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        #endregion
    }
}