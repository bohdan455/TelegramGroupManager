using Telegram.Bot.Types;

namespace bot.Middleware
{
    public class CommandsHandler
    {
        private readonly Update _update;
        private readonly List<(string, Func<Task>)> _commands = new List<(string, Func<Task>)>();

        public CommandsHandler(Update update)
        {
            _update = update;
        }
        public void Add(string command, Func<Task> action)
        {
            _commands.Add((command, action));
        }
        public async Task SendMessage()
        {
            string messageCommand = _update?.Message?.Text?.Split(" ")[0];

            foreach ((string command, Func<Task> Func) in _commands)
            {
                if (messageCommand == command)
                {
                    await Func();
                    return;
                }
            }
        }
    }
}
