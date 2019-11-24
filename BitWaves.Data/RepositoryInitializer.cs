using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BitWaves.Data
{
    using Entities;

    /// <summary>
    /// 提供 BitWaves 数据库的初始化逻辑。
    /// </summary>
    public sealed class RepositoryInitializer
    {
        private readonly ILogger<RepositoryInitializer> _logger;
        private readonly Repository _repo;

        /// <summary>
        /// 初始化 <see cref="RepositoryInitializer"/> 的新实例。
        /// </summary>
        /// <param name="repo">要初始化的数据库。</param>
        /// <param name="logger">日志组件。</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="repo"/> 为 null。
        /// </exception>
        public RepositoryInitializer(Repository repo, ILogger<RepositoryInitializer> logger = null)
        {
            Contract.NotNull(repo, nameof(repo));
            Contract.NotNull(logger, nameof(logger));

            _logger = logger;
            _repo = repo;
        }

        /// <summary>
        /// 初始化静态内容数据集。
        /// </summary>
        private void InitializeContentCollection()
        {
            _logger?.LogTrace("初始化静态内容数据集...");

            var indexesList = new List<CreateIndexModel<Content>>
            {
                // CreationTime 上的递减索引
                new CreateIndexModel<Content>(Builders<Content>.IndexKeys.Descending(content => content.CreationTime)),
                // Name 上的哈希索引
                new CreateIndexModel<Content>(Builders<Content>.IndexKeys.Hashed(content => content.Name)),
                // MimeType 上的哈希索引
                new CreateIndexModel<Content>(Builders<Content>.IndexKeys.Hashed(content => content.MimeType))
            };

            _repo.Contents.Indexes.CreateMany(indexesList);
            _logger?.LogDebug("在静态内容数据集上创建了{0}个索引", indexesList.Count);
        }

        /// <summary>
        /// 初始化用户数据集。
        /// </summary>
        private void InitializeUserCollection()
        {
            _logger?.LogTrace("初始化用户数据集...");

            var indexesList = new List<CreateIndexModel<User>>
            {
                // Username 上的唯一升序索引
                // 注意：MongoDB 哈希索引不能保证唯一性，因此使用普通索引
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(user => user.Username),
                                           new CreateIndexOptions { Unique = true }),
                // TotalSubmissions 上的递减索引
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Descending(user => user.TotalSubmissions)),
                // TotalAcceptedSubmissions 上的递减索引
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Descending(user => user.TotalAcceptedSubmissions)),
                // TotalProblemsAttempted 上的递减索引
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Descending(user => user.TotalProblemsAttempted)),
                // TotalProblemsAccepted 上的递减索引
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Descending(user => user.TotalProblemsAccepted))
            };

            _repo.Users.Indexes.CreateMany(indexesList);
            _logger?.LogDebug("在用户数据集上创建了{0}个索引。", indexesList.Count);
        }

        /// <summary>
        /// 初始化全站公告数据集。
        /// </summary>
        private void InitializeAnnouncementCollection()
        {
            _logger?.LogTrace("初始化全站公告数据集...");

            var indexesList = new List<CreateIndexModel<Announcement>>
            {
                new CreateIndexModel<Announcement>(
                    Builders<Announcement>.IndexKeys.Descending(announcement => announcement.LastUpdateTime))
            };

            _repo.Announcements.Indexes.CreateMany(indexesList);
            _logger?.LogDebug("在全站公告数据集上创建了{0}个索引。", indexesList.Count);
        }

        /// <summary>
        /// 初始化题目数据集。
        /// </summary>
        private void InitializeProblemCollection()
        {
            _logger?.LogTrace("初始化题目数据集...");

            var indexesList = new List<CreateIndexModel<Problem>>
            {
                // ArchiveId 上的递增索引
                // 注意：不能在 ArchiveId 上创建唯一性索引，因为在 ArchiveId 上可能有多个实体值为 null
                // ArchiveId 上的唯一性由应用端保证
                new CreateIndexModel<Problem>(Builders<Problem>.IndexKeys.Ascending(problem => problem.ArchiveId)),
                // LastUpdateTime 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.LastUpdateTime)),
                // Difficulty 上的递增索引
                new CreateIndexModel<Problem>(Builders<Problem>.IndexKeys.Ascending(problem => problem.Difficulty)),
                // TotalSubmissions 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.TotalSubmissions)),
                // AcceptedSubmissions 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.AcceptedSubmissions)),
                // TotalAttemptedUsers 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.TotalAttemptedUsers)),
                // TotalSolvedUsers 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.TotalSolvedUsers)),
                // LastSubmissionTime 上的递减索引
                new CreateIndexModel<Problem>(
                    Builders<Problem>.IndexKeys.Descending(problem => problem.LastSubmissionTime))
            };

            _repo.Problems.Indexes.CreateMany(indexesList);
            _logger?.LogDebug("在题目数据集上创建了{0}个索引。", indexesList.Count);
        }

        /// <summary>
        /// 初始化题目标签数据字典。
        /// </summary>
        private void InitializeProblemTagsCollection()
        {
            _logger?.LogTrace("初始化题目标签数据字典...");

            var indexesList = new List<CreateIndexModel<ProblemTag>>
            {
                // Name 上的唯一哈希索引
                new CreateIndexModel<ProblemTag>(Builders<ProblemTag>.IndexKeys.Ascending(tag => tag.Name),
                    new CreateIndexOptions { Unique = true })
            };

            _repo.ProblemTags.Indexes.CreateMany(indexesList);
            _logger?.LogDebug("在题目标签数据字典上创建了 {0} 个索引。", indexesList.Count);
        }

        /// <summary>
        /// 初始化语言数据集。
        /// </summary>
        private void InitializeLanguageCollection()
        {
            _logger?.LogTrace("初始化语言数据集...");
        }

        /// <summary>
        /// 初始化数据库。
        /// </summary>
        public void Initialize()
        {
            _logger?.LogTrace("初始化 BitWaves 数据库...");

            InitializeContentCollection();
            InitializeUserCollection();
            InitializeAnnouncementCollection();
            InitializeProblemCollection();
            InitializeProblemTagsCollection();
            InitializeLanguageCollection();
        }

        /// <summary>
        /// 向用户数据集添加种子数据。
        /// </summary>
        private void SeedUserCollection()
        {
            _logger?.LogTrace("向用户数据集添加种子数据...");

            var adminUser = new User("admin", PasswordUtils.GetPasswordHash("bitwaves2019")) { IsAdmin = true };

            try
            {
                _repo.Users.InsertOne(adminUser);
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category != ServerErrorCategory.DuplicateKey)
                {
                    throw;
                }

                // 管理员用户已经存在。不执行任何操作。
            }

            _logger?.LogDebug("已经向用户数据集中插入了管理员用户。");
        }

        /// <summary>
        /// 向数据库添加种子数据。
        /// </summary>
        public void Seed()
        {
            _logger?.LogTrace("向 BitWaves 数据集中添加种子数据...");

            SeedUserCollection();
        }
    }
}
