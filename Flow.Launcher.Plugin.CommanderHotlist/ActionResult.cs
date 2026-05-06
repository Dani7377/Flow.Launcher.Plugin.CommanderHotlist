namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal class ActionResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        private ActionResult(bool success, string userMessage, Exception? ex)
        {
            IsSuccess = success;
            Message = userMessage;
            Exception = ex;
        }

        public static ActionResult Success() => new ActionResult(true, string.Empty, null);

        public static ActionResult Fail(string userMessage, Exception? ex)
            => new ActionResult(false, userMessage, ex);

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
                context.API.LogException(nameof(Main), result.Message, result.Exception);
            }

            return result.IsSuccess;
        }
    }
}
