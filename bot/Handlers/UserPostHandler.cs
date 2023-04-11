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
        private readonly List<(MessageType,Func<Task>)> _messageTypes = new List<(MessageType,Func<Task>)>();
        private Func<Task> _UndefinedTypeHandler;
        private Func<Task> _ResponceToUser;
        public UserPostHandler(Update update)
        {
            _update = update;
        }
        public void AddMessageType(MessageType type, Func<Task> action)
        {
            // TODO add check on unique

            _messageTypes.Add((type, action));
        }
        public void AddUndefinedTypeHandler(Func<Task> UndefinedTypeHandler)
        {
            _UndefinedTypeHandler = UndefinedTypeHandler;
        }
        public void AddResponceToUser(Func<Task> ResponceToUser)
        {
            _ResponceToUser = ResponceToUser;
        }
        public async Task SendMessage()
        {
            MessageType currrentType = _update.Message.Type;
            foreach ((var type,var action) in _messageTypes)
            {
                if(type == currrentType)
                {
                    if (_ResponceToUser is not null)
                        await _ResponceToUser();

                    await action();
                    return;
                }
            }
            if (_UndefinedTypeHandler is not null)
                await _UndefinedTypeHandler();
        }
    }
}
