using System;

namespace Vertigo.Live
{
    public enum DatabaseActionType
    {
        None,
        Insert,
        Update,
        Delete,
    }

    public interface IDatabaseAction
    {
        object Entity { get; }
        DatabaseActionType Type { get; }
    }

    public class DatabaseAction<TEntity> : IDatabaseAction
    {
        public DatabaseAction(TEntity entity, DatabaseActionType type)
        {
            Entity = entity;
            Type = type;
        }

        public TEntity Entity { get;  private set; }
        object IDatabaseAction.Entity { get { return Entity; } }
        public DatabaseActionType Type { get; private set; }
    }

    public static partial class Extensions
    {
        public static DatabaseActionType Merge(this DatabaseActionType oldAction, DatabaseActionType newAction)
        {
            switch (oldAction)
            {
                case DatabaseActionType.None:
                    return newAction;
                case DatabaseActionType.Insert:
                    switch (newAction)
                    {
                        case DatabaseActionType.Update:
                            return DatabaseActionType.Insert;
                        case DatabaseActionType.Delete:
                            return DatabaseActionType.None;
                    }
                    break;
                case DatabaseActionType.Update:
                    switch (newAction)
                    {
                        case DatabaseActionType.Update:
                            return DatabaseActionType.Update;
                        case DatabaseActionType.Delete:
                            return DatabaseActionType.Delete;
                    }
                    break;
                case DatabaseActionType.Delete:
                    switch (newAction)
                    {
                        case DatabaseActionType.Insert:
                            return DatabaseActionType.None;
                    }
                    break;
            }
            throw new InvalidProgramException("Invalid action");
        }
    }
}