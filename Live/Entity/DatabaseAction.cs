using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public enum DatabaseActionType
    {
        Unchanged,
        Insert,
        Update,
        Delete,
    }

    public static partial class Extensions
    {
        public static DatabaseActionType Add(this DatabaseActionType oldAction, DatabaseActionType newAction)
        {
            switch (oldAction)
            {
                case DatabaseActionType.Unchanged:
                    return newAction;
                case DatabaseActionType.Insert:
                    switch (newAction)
                    {
                        case DatabaseActionType.Insert:
                        case DatabaseActionType.Update:
                            return DatabaseActionType.Insert;
                        case DatabaseActionType.Delete:
                            return DatabaseActionType.Unchanged;
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
                            return DatabaseActionType.Unchanged;
                    }
                    break;
            }
            throw new InvalidOperationException("Invalid action");
        }
    }
}