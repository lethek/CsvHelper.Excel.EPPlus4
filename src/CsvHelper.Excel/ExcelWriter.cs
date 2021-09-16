using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CsvHelper.Configuration;

using OfficeOpenXml;


namespace CsvHelper.Excel
{
    /// <summary>
    /// Defines methods used to serialize data into an Excel (2007+) file.
    /// </summary>
    public class ExcelWriter : CsvWriter
    {
        /// <summary>
        /// Creates a new serializer using a new <see cref="ExcelPackage"/> saved to the given <paramref name="stream"/>.
        /// <remarks>
        /// The package will not be saved until the serializer is disposed.
        /// </remarks>
        /// </summary>
        /// <param name="stream">The stream to which to save the package.</param>
        /// <param name="configuration">The configuration</param>
        public ExcelWriter(Stream stream, CsvConfiguration configuration = null)
            : this(new ExcelPackage(), configuration) {
            _stream = stream;
            _disposePackage = true;
        }


        /// <summary>
        /// Creates a new serializer using a new <see cref="ExcelPackage"/> saved to the given <paramref name="stream"/>.
        /// <remarks>
        /// The package will not be saved until the serializer is disposed.
        /// </remarks>
        /// </summary>
        /// <param name="stream">The stream to which to save the package.</param>
        /// <param name="sheetName">The name of the sheet to which to save</param>
        public ExcelWriter(Stream stream, string sheetName)
            : this(new ExcelPackage(), sheetName) {
            _stream = stream;
            _disposePackage = true;
        }



        /// <summary>
        /// Creates a new serializer using a new <see cref="ExcelPackage"/> saved to the given <paramref name="path"/>.
        /// <remarks>
        /// The package will not be saved until the serializer is disposed.
        /// </remarks>
        /// </summary>
        /// <param name="path">The path to which to save the package.</param>
        /// <param name="configuration">The configuration</param>
        public ExcelWriter(string path, CsvConfiguration configuration = null)
            : this(new ExcelPackage(new FileInfo(path)), configuration) {
            _disposePackage = true;
        }


        /// <summary>
        /// Creates a new serializer using a new <see cref="ExcelPackage"/> saved to the given <paramref name="path"/>.
        /// <remarks>
        /// The package will not be saved until the serializer is disposed.
        /// </remarks>
        /// </summary>
        /// <param name="path">The path to which to save the package.</param>
        /// <param name="sheetName">The name of the sheet to which to save</param>
        public ExcelWriter(string path, string sheetName)
            : this(new ExcelPackage(new FileInfo(path)), sheetName) {
            _disposePackage = true;
        }


        /// <summary>
        /// Creates a new serializer using the given <see cref="ExcelPackage"/> and <see cref="Configuration"/>.
        /// <remarks>
        /// The <paramref name="package"/> will <b><i>not</i></b> be disposed of when the serializer is disposed.
        /// The package will <b><i>not</i></b> be saved by the serializer.
        /// A new worksheet will be added to the package.
        /// </remarks>
        /// </summary>
        /// <param name="package">The package to write the data to.</param>
        /// <param name="configuration">The configuration.</param>
        public ExcelWriter(ExcelPackage package, CsvConfiguration configuration = null)
            : this(package, "Export", configuration) { }


        /// <summary>
        /// Creates a new serializer using the given <see cref="ExcelPackage"/> and <see cref="Configuration"/>.
        /// <remarks>
        /// The <paramref name="package"/> will <b><i>not</i></b> be disposed of when the serializer is disposed.
        /// The package will <b><i>not</i></b> be saved by the serializer.
        /// A new worksheet will be added to the package.
        /// </remarks>
        /// </summary>
        /// <param name="package">The package to write the data to.</param>
        /// <param name="sheetName">The name of the sheet to write to.</param>
        /// <param name="configuration">The configuration.</param>
        public ExcelWriter(ExcelPackage package, string sheetName, CsvConfiguration configuration = null)
            : this(package, package.GetOrAddWorksheet(sheetName), configuration) { }


        /// <summary>
        /// Creates a new serializer using the given <see cref="ExcelPackage"/> and <see cref="ExcelWorksheet"/>.
        /// <remarks>
        /// The <paramref name="worksheet"/> will <b><i>not</i></b> be disposed of when the serializer is disposed.
        /// The package will <b><i>not</i></b> be saved by the serializer.
        /// </remarks>
        /// </summary>
        /// <param name="package">The package to write the data to.</param>
        /// <param name="worksheet">The worksheet to write the data to.</param>
        /// <param name="configuration">The configuration</param>
        public ExcelWriter(ExcelPackage package, ExcelWorksheet worksheet, CsvConfiguration configuration = null)
            : this(package, (ExcelRangeBase)worksheet.Cells, configuration) { }


        /// <summary>
        /// Creates a new serializer using the given <see cref="ExcelPackage"/> and <see cref="ExcelRange"/>.
        /// </summary>
        /// <param name="package">The package to write the data to.</param>
        /// <param name="range">The range to write the data to.</param>
        /// <param name="configuration">The configuration</param>
        public ExcelWriter(ExcelPackage package, ExcelRange range, CsvConfiguration configuration = null)
            : this(package, (ExcelRangeBase)range, configuration) { }


        private ExcelWriter(ExcelPackage package, ExcelRangeBase range, CsvConfiguration configuration)
            : base(TextWriter.Null, configuration)
        {
            configuration.Validate();

            Package = package;
            _range = range;
            //Configuration = configuration ?? new CsvConfiguration(CultureInfo.InvariantCulture);
            //Configuration.ShouldQuote = (s, context) => false;
            //Context = new WritingContext(TextWriter.Null, Configuration, false);
        }

        /// <summary>
        /// Gets the package to which the data is being written.
        /// </summary>
        /// <value>
        /// The package.
        /// </value>
        public ExcelPackage Package { get; }

        /// <summary>
        /// Gets and sets the number of rows to offset the start position from.
        /// </summary>
        public int RowOffset { get; set; }

        /// <summary>
        /// Gets and sets the number of columns to offset the start position from.
        /// </summary>
        public int ColumnOffset { get; set; }


        /// <summary>
        /// Writes a record to the Excel file.
        /// </summary>
        /// <param name="record">The record to write.</param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown is the serializer has been disposed.
        /// </exception>
        public virtual void Write(string[] record) {
            for (var i = 0; i < record.Length; i++) {
                var row = _range.Start.Row + _currentRow + RowOffset - 1;
                var column = _range.Start.Column + ColumnOffset + i;
                _range.Worksheet.SetValue(row, column, ReplaceHexadecimalSymbols(record[i]));
            }

            _currentRow++;
        }


        /// <summary>
        /// Writes asynchronously a record to the Excel file.
        /// </summary>
        /// <param name="record">The record to write.</param>
        /// <returns></returns>
        public Task WriteAsync(string[] record) {
            Write(record);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Implementation forced by CsvHelper : <see cref="IParser"/>.
        /// </summary>
        public void WriteLine() { }


        /// <summary>
        /// Implementation forced by CsvHelper : <see cref="IParser"/>
        /// </summary>
        public Task WriteLineAsync() {
            WriteLine();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Replaces the hexadecimal symbols.
        /// </summary>
        /// <param name="text">The text to replace.</param>
        /// <returns>The input</returns>
        protected static string ReplaceHexadecimalSymbols(string text)
            => !String.IsNullOrEmpty(text)
                ? Regex.Replace(text, "[\x00-\x08\x0B\x0C\x0E-\x1F]", String.Empty, RegexOptions.Compiled)
                : text;


        /// <summary>
        /// Finalizes an instance of the <see cref="ExcelWriter"/> class.
        /// </summary>
        ~ExcelWriter() {
            Dispose(false);
        }


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            Flush();
            if (_stream != null) {
                Package.SaveAs(_stream);
                _stream.Flush();
            } else {
                Package.Save();
            }

            if (disposing) {
                if (_disposePackage) {
                    Package.Dispose();
                }
            }
            _disposed = true;
        }


        #if !NET45 && !NET47 && !NETSTANDARD2_0
		/// <inheritdoc/>
		protected override async ValueTask DisposeAsync(bool disposing)
		{
			if (_disposed) {
				return;
			}

			await FlushAsync().ConfigureAwait(false);
            if (_stream != null) {
                Package.SaveAs(_stream);
                await _stream.FlushAsync().ConfigureAwait(false);
            } else {
                Package.Save();
            }

			if (disposing) {
                //Dispose managed state (managed objects)
                if (_disposePackage) {
                    Package.Dispose();
                }
				/*if (!_leaveOpen) {
					await _stream.DisposeAsync().ConfigureAwait(false);
				}*/
			}

			// Free unmanaged resources (unmanaged objects) and override finalizer
			// Set large fields to null
			_disposed = true;
		}
        #endif


        private readonly Stream _stream;
        private readonly bool _disposePackage;
        private readonly ExcelRangeBase _range;
        private int _currentRow = 1;
        private bool _disposed;
    }
}