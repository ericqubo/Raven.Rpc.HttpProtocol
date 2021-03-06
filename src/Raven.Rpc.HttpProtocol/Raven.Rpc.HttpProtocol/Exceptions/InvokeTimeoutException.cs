﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Rpc.HttpProtocol.Exceptions
{
    /// <summary>
    /// 远程调用超时
    /// </summary>
    public class InvokeTimeoutException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public const string ExceptionMessage = "远程调用超时";

        /// <summary>
        /// 
        /// </summary>
        public InvokeTimeoutException() : this(ExceptionMessage)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public InvokeTimeoutException(string message) : base(message)
        { }
    }
}
