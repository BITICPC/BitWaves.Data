using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace BitWaves.Data
{
    /// <summary>
    /// 为 <see cref="TestDataArchiveMetadata"/> 提供构造逻辑。
    /// </summary>
    internal sealed class TestDataArchiveMetadataBuilder
    {
        /// <summary>
        /// 为原始的 ZIP 压缩包中的每一个 entry 提供其在测试数据包中的类型。
        /// </summary>
        private enum ArchiveEntryKind
        {
            /// <summary>
            /// Entry 对应于一个输入文件。
            /// </summary>
            InputFile,

            /// <summary>
            /// Entry 对应于一个答案文件。
            /// </summary>
            AnswerFile,

            /// <summary>
            /// Entry 对应于一个答案检查器源文件。
            /// </summary>
            Checker,

            /// <summary>
            /// Entry 对应于一个交互器源文件。
            /// </summary>
            Interactor,

            /// <summary>
            /// 无法得知 entry 的类型。
            /// </summary>
            Unknown
        }

        /// <summary>
        /// 输入文件的扩展名。
        /// </summary>
        private const string InputFileExtension = "in";

        /// <summary>
        /// 答案文件的扩展名。
        /// </summary>
        private const string AnswerFileExtension = "ans";

        /// <summary>
        /// 答案检查器源文件的文件名，不包含扩展名。
        /// </summary>
        private const string CheckerFileName = "checker";

        /// <summary>
        /// 交互器源文件的文件名，不包含扩展名。
        /// </summary>
        private const string InteractorFileName = "interactor";

        /// <summary>
        /// 获取给定 <see cref="ZipArchiveEntry"/> 在测试数据集中的类型。
        /// </summary>
        /// <param name="entry">要检查的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>给定的 <see cref="ZipArchiveEntry"/> 在测试数据集中的类型。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        private static ArchiveEntryKind GetEntryKind(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            var extension = Path.GetExtension(entry.Name);
            if (string.Compare(extension, InputFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ArchiveEntryKind.InputFile;
            }

            if (string.Compare(extension, AnswerFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ArchiveEntryKind.AnswerFile;
            }

            var name = Path.GetFileNameWithoutExtension(entry.Name);
            if (string.Compare(name, CheckerFileName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ArchiveEntryKind.Checker;
            }

            if (string.Compare(name, InteractorFileName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return ArchiveEntryKind.Interactor;
            }

            return ArchiveEntryKind.Unknown;
        }

        private readonly TestDataArchiveMetadata _metadata;
        private readonly Dictionary<string, ZipArchiveEntry> _testCaseEntries;

        /// <summary>
        /// 初始化 <see cref="TestDataArchiveMetadataBuilder"/> 类的新实例。
        /// </summary>
        public TestDataArchiveMetadataBuilder()
        {
            _metadata = new TestDataArchiveMetadata();
            _testCaseEntries = new Dictionary<string, ZipArchiveEntry>();
        }

        /// <summary>
        /// 检查是否所有的构造工作均已成功完成。若存在尚未完成的构造工作，抛出相应的异常。
        /// </summary>
        /// <exception cref="InvalidOperationException">某些构造工作尚未成功完成。</exception>
        private void FinishBuild()
        {
            if (_testCaseEntries.Count > 0)
            {
                throw new InvalidOperationException("存在尚未配对的测试用例。");
            }
        }

        /// <summary>
        /// 获取构造的 <see cref="TestDataArchiveMetadata"/> 对象。
        /// </summary>
        /// <returns>构造的 <see cref="TestDataArchiveMetadata"/> 对象。</returns>
        /// <exception cref="InvalidOperationException">某些构造工作尚未成功完成。</exception>
        public TestDataArchiveMetadata GetMetadata()
        {
            FinishBuild();
            return _metadata;
        }

        /// <summary>
        /// 向测试数据集中添加一个原始的 <see cref="ZipArchiveEntry"/> 对象。
        /// </summary>
        /// <param name="entry">原始 ZIP 压缩包中的一个数据项的 <see cref="ZipArchiveEntry"/> 封装。</param>
        /// <returns>返回当前的 <see cref="TestDataArchiveMetadataBuilder"/> 对象。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        /// <exception cref="ArgumentException">无法为 <paramref name="entry"/> 确定其在测试数据集中的类型。</exception>
        public TestDataArchiveMetadataBuilder AddZipArchiveEntry(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            switch (GetEntryKind(entry))
            {
                case ArchiveEntryKind.InputFile:
                    return AddInputFileEntry(entry);

                case ArchiveEntryKind.AnswerFile:
                    return AddAnswerFileEntry(entry);

                case ArchiveEntryKind.Checker:
                    return AddCheckerEntry(entry);

                case ArchiveEntryKind.Interactor:
                    return AddInteractorEntry(entry);

                default:
                    throw new ArgumentException($"存在无法归类的归档条目：\"{entry.FullName}\"");
            }
        }

        /// <summary>
        /// 检查与给定的 <see cref="ZipArchiveEntry"/> 相关联的测试用例构造情况。若已经有一个同名的测试用例，返回该测试用例中
        /// 另一个 <see cref="ZipArchiveEntry"/> 并将其从 <see cref="_testCaseEntries"/> 中移除；否则将给定的
        /// <see cref="ZipArchiveEntry"/> 对象添加到 <see cref="_testCaseEntries"/> 中。
        /// </summary>
        /// <param name="currentEntry">要检查的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>
        /// 若已经有一个同名的测试用例，返回该测试用例中的另一个 <see cref="ZipArchiveEntry"/> 对象；否则返回 null。
        /// </returns>
        private ZipArchiveEntry ExtractOrAddTestCasePairEntry(ZipArchiveEntry currentEntry)
        {
            Contract.NotNull(currentEntry, nameof(currentEntry));

            var testCaseName = PathUtils.RemoveExtension(currentEntry.FullName);
            if (_testCaseEntries.ContainsKey(testCaseName))
            {
                var partner = _testCaseEntries[testCaseName];
                _testCaseEntries.Remove(testCaseName);
                return partner;
            }

            _testCaseEntries.Add(testCaseName, currentEntry);
            return null;
        }

        /// <summary>
        /// 将给定的 <see cref="ZipArchiveEntry"/> 作为输入文件的 entry 添加到测试数据集中。
        /// </summary>
        /// <param name="entry">要添加的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>返回当前的 <see cref="TestDataArchiveMetadataBuilder"/> 对象。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        private TestDataArchiveMetadataBuilder AddInputFileEntry(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            var partner = ExtractOrAddTestCasePairEntry(entry);
            if (partner != null)
            {
                _metadata.TestCases.Add(new TestCaseEntry(new TestDataArchiveEntry(entry),
                                                          new TestDataArchiveEntry(partner)));
            }

            return this;
        }

        /// <summary>
        /// 将给定的 <see cref="ZipArchiveEntry"/> 作为答案文件的 entry 添加到测试数据集中。
        /// </summary>
        /// <param name="entry">要添加的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>返回当前的 <see cref="TestDataArchiveMetadataBuilder"/> 对象。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        private TestDataArchiveMetadataBuilder AddAnswerFileEntry(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            var partner = ExtractOrAddTestCasePairEntry(entry);
            if (partner != null)
            {
                _metadata.TestCases.Add(new TestCaseEntry(new TestDataArchiveEntry(partner),
                                                          new TestDataArchiveEntry(entry)));
            }

            return this;
        }

        /// <summary>
        /// 将给定的 <see cref="ZipArchiveEntry"/> 作为答案检查器源文件的 entry 添加到测试数据集中。
        /// </summary>
        /// <param name="entry">要添加的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>返回当前的 <see cref="TestDataArchiveMetadataBuilder"/> 对象。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        /// <exception cref="InvalidOperationException">已经存在一个答案检查器或交互器的 entry。</exception>
        private TestDataArchiveMetadataBuilder AddCheckerEntry(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            if (_metadata.Checker != null)
            {
                throw new InvalidOperationException("在尝试添加新的答案检查器时发现已经存在一个答案检查器。");
            }

            if (_metadata.Interactor != null)
            {
                throw new InvalidOperationException("在尝试添加新的答案检查器时发现已经存在一个交互器。");
            }

            _metadata.Checker = new TestDataArchiveEntry(entry);
            return this;
        }

        /// <summary>
        /// 将给定的 <see cref="ZipArchiveEntry"/> 作为交互器源文件的 entry 添加到测试数据集中。
        /// </summary>
        /// <param name="entry">要添加的 <see cref="ZipArchiveEntry"/> 对象。</param>
        /// <returns>返回当前的 <see cref="TestDataArchiveMetadataBuilder"/> 对象。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 null。</exception>
        /// <exception cref="InvalidOperationException">已经存在一个答案检查器或交互器的 entry。</exception>
        private TestDataArchiveMetadataBuilder AddInteractorEntry(ZipArchiveEntry entry)
        {
            Contract.NotNull(entry, nameof(entry));

            if (_metadata.Checker != null)
            {
                throw new InvalidOperationException("在尝试添加新的交互器时发现已经存在一个答案检查器。");
            }

            if (_metadata.Interactor != null)
            {
                throw new InvalidOperationException("在尝试添加新的交互器时发现已经存在一个交互器。");
            }

            _metadata.Interactor = new TestDataArchiveEntry(entry);
            return this;
        }
    }
}