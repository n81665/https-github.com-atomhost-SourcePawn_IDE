using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourcePawn_IDE
{
    public enum CMDTYPE
    {
        Console = 0,
        Server = 1,
        Admin = 2,
        CommandListener = 3
    }

    public struct Command
    {
        public string command;
        public string Description;
        public string Callback;
        public string Group;
        public string Flags;
        public CMDTYPE CommandType;

        public Command(string Command, string Description, string Callback, string Group, string Flags, CMDTYPE CommandType)
        {
            this.command = Command;
            this.Description = Description;
            this.Callback = Callback;
            this.Group = Group;
            this.Flags = Flags;
            this.CommandType = CommandType;
        }
    }
}
