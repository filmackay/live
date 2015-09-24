namespace Vertigo
{
    public class Logger
    {
        protected readonly ILog Log;

        public Logger()
        {
            Log = LogManager.GetLogger(GetType());
        }
    }
}