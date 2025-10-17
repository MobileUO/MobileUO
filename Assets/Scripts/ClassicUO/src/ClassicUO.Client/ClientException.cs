// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO
{
    public class InvalidClientVersion : Exception
    {
        public InvalidClientVersion(string msg) : base(msg)
        {
        }
    }

    public class InvalidClientDirectory : Exception
    {
        public InvalidClientDirectory(string msg) : base(msg)
        {
        }
    }
}