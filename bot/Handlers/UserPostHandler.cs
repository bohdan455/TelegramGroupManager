using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot.Handlers
{
    public class UserPostHandler
    {
        private readonly Update _update;
        private readonly List<(MessageType,Func<Task>)> _types = new List<(MessageType,Func<Task>)>();

        public UserPostHandler(Update update)
        {
            _update = update;
        }
        public void AddMessageType(MessageType type, Func<Task> func)
        {
            // TODO add check on unique

            _types.Add((type, func));
        }
        public async Task SendMessage()
        {
            MessageType currrentType = _update.Message.Type;
            foreach ((var type,var func) in _types)
            {
                if(type == currrentType)
                {
                    await func();
                    return;
                }
            }
        }
    }
}
