using System;
using System.Runtime.Serialization;
using MongoDB.Driver;

namespace BitWaves.Data.Repositories
{
    /// <summary>
    /// 当数据源发生异常时抛出。
    /// </summary>
    [Serializable]
    public class RepositoryException : Exception
    {
        /// <summary>
        /// 当底层 MongoDB 组件抛出异常时，包装其异常的 <see cref="RepositoryException"/> 对象的异常消息。
        /// </summary>
        private const string RepositoryExceptionMessage = "Failed to complete the required operation";

        /// <summary>
        /// 获取错误码。
        /// </summary>
        public RepositoryErrorCode ErrorCode { get; }

        /// <summary>
        /// 获取该异常是否由操作超时而引起。
        /// </summary>
        public bool IsTimeout => ErrorCode == RepositoryErrorCode.Timeout;

        /// <summary>
        /// 获取该异常是否由引入了重复的键而引起。
        /// </summary>
        public bool IsDuplicateKey => ErrorCode == RepositoryErrorCode.DuplicateKey;

        /// <summary>
        /// 获取引发当前异常的 <see cref="MongoException"/> 异常。
        /// </summary>
        public MongoException MongoException => (MongoException) InnerException;

        /// <summary>
        /// 初始化 <see cref="RepositoryException"/> 类的新实例。
        /// </summary>
        /// <param name="ex">引发当前异常的 <see cref="MongoException"/> 异常。</param>
        public RepositoryException(MongoException ex)
            : base(RepositoryExceptionMessage, ex)
        {
            if (ex is MongoWriteException writeException)
            {
                ErrorCode = RepositoryErrorCodeHelper.FromMongoServerErrorCategory(writeException.WriteError.Category);
            }
            else
            {
                ErrorCode = RepositoryErrorCode.Unknown;
            }
        }

        /// <summary>
        /// 初始化 <see cref="RepositoryException"/> 类的新实例。
        /// </summary>
        /// <param name="errorCode">错误码。</param>
        public RepositoryException(RepositoryErrorCode errorCode)
            : base(RepositoryExceptionMessage)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 从给定的序列化上下文中反序列化 <see cref="RepositoryException"/> 类的新实例。
        /// </summary>
        /// <param name="info">序列化信息。</param>
        /// <param name="context">序列化环境的流上下文。</param>
        protected RepositoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), ErrorCode);

            base.GetObjectData(info, context);
        }
    }
}
