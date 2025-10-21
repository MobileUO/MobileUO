// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    public enum ConsolePrompt
    {
        None,
        ASCII,
        Unicode
    }

    public struct PromptData
    {
        public ConsolePrompt Prompt;
        public ulong Data;
    }
}