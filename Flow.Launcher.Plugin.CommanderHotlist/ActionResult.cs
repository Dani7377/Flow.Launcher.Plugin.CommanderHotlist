namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal class ActionResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public string? ClassName { get; }

        private ActionResult(bool success, string userMessage, Exception? ex, string? className)
        {
            IsSuccess = success;
            Message = userMessage;
            Exception = ex;
            ClassName = className;
        }

        public static ActionResult Success() => new ActionResult(true, string.Empty, null, null);

        public static ActionResult Fail(string userMessage, Exception? ex, string? className)
            => new ActionResult(false, userMessage, ex, className);

        /// <summary>
        /// Handles an ActionResult:
        ///   * success - just returns true, doesn't show any messages
        ///   * fail - returns false, prints Flow Launcher error and logs the exception if it exists
        /// </summary>
        public static bool HandleActionResult(ActionResult result, PluginInitContext context)
        {
            if (!result.IsSuccess)
            {
                context.API.ShowMsgError(result.Message);
            }

            if (result.Exception != null)
            {
                string className;
                if(result.ClassName != null)
                {
                    className = result.ClassName;
                }
                else // I don't even think it's possible to reach this 'else'
                {
                    className = nameof(Main);
                }
                context.API.LogException(className, result.Message, result.Exception);
            }

            return result.IsSuccess;
        }
    }
}
