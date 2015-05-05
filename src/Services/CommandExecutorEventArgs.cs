﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2013 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.CoreLibrary.
 *
 * Zongsoft.CoreLibrary is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.CoreLibrary is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.CoreLibrary; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;

namespace Zongsoft.Services
{
	[Serializable]
	public class CommandExecutorEventArgs : EventArgs
	{
		#region 成员字段
		private CommandExecutorContextBase _context;
		#endregion

		#region 构造函数
		public CommandExecutorEventArgs(CommandExecutorContextBase context)
		{
			if(context == null)
				throw new ArgumentNullException("context");

			_context = context;
		}
		#endregion

		#region 公共属性
		public CommandExecutorContextBase Context
		{
			get
			{
				return _context;
			}
		}

		public ICommandExecutor CommandExecutor
		{
			get
			{
				return _context.Executor;
			}
		}

		public string CommandText
		{
			get
			{
				return _context.CommandText;
			}
		}

		public object Parameter
		{
			get
			{
				return _context.Parameter;
			}
		}

		public CommandTreeNode CommandNode
		{
			get
			{
				return _context.CommandNode;
			}
		}

		public ICommand Command
		{
			get
			{
				return _context.Command;
			}
		}

		public object Result
		{
			get
			{
				return _context.Result;
			}
			set
			{
				_context.Result = value;
			}
		}
		#endregion
	}
}