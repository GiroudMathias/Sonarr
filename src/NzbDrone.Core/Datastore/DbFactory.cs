using System;
using System.Data.SQLite;
using Marr.Data;
using Marr.Data.Reflection;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Datastore.Migration.Framework;


namespace NzbDrone.Core.Datastore
{
    public interface IDbFactory
    {
        IDatabase Create(MigrationType migrationType = MigrationType.Main);
        IDatabase Create(MigrationContext migrationContext);
    }

    public class DbFactory : IDbFactory
    {
        private readonly IMigrationController _migrationController;
        private readonly IConnectionStringFactory _connectionStringFactory;
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DbFactory));


        static DbFactory()
        {
            MapRepository.Instance.ReflectionStrategy = new SimpleReflectionStrategy();
            TableMapping.Map();
        }

        public static void RegisterDatabase(IContainer container)
        {
            var mainDb = new MainDatabase(container.Resolve<IDbFactory>().Create());

            container.Register<IMainDatabase>(mainDb);

            var logDb = new LogDatabase(container.Resolve<IDbFactory>().Create(MigrationType.Log));

            container.Register<ILogDatabase>(logDb);
        }

        public DbFactory(IMigrationController migrationController,
                         IConnectionStringFactory connectionStringFactory,
                         IDiskProvider diskProvider,
                         IAppFolderInfo appFolderInfo)
        {
            _migrationController = migrationController;
            _connectionStringFactory = connectionStringFactory;
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
        }

        public IDatabase Create(MigrationType migrationType = MigrationType.Main)
        {
            return Create(new MigrationContext(migrationType));
        }

        public IDatabase Create(MigrationContext migrationContext)
        {
            string connectionString;


            switch (migrationContext.MigrationType)
            {
                case MigrationType.Main:
                    {
                        connectionString = _connectionStringFactory.MainDbConnectionString;
                        break;
                    }
                case MigrationType.Log:
                    {
                        connectionString = _connectionStringFactory.LogDbConnectionString;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Invalid MigrationType");
                    }
            }

            try
            {
                _migrationController.Migrate(connectionString, migrationContext);
            }
            catch (SQLiteException ex)
            {
                var fileName = _connectionStringFactory.GetDatabasePath(connectionString);

                if (migrationContext.MigrationType == MigrationType.Log)
                {
                    Logger.Warn(ex, "Logging database is corrupt, attempting to recreate it automatically");

                    _diskProvider.DeleteFile(fileName);
                    _diskProvider.DeleteFile(fileName + "-shm");
                    _diskProvider.DeleteFile(fileName + "-wal");

                    _migrationController.Migrate(connectionString, migrationContext);
                }

                else
                {
                    throw new CorruptDatabaseException("Database file: {0} is corrupt, restore from backup if available", ex, fileName);
                }
            }

            var db = new Database(migrationContext.MigrationType.ToString(), () =>
                {
                    var dataMapper = new DataMapper(SQLiteFactory.Instance, connectionString)
                    {
                        SqlMode = SqlModes.Text,
                    };

                    return dataMapper;
                });

            return db;
        }
    }
}
