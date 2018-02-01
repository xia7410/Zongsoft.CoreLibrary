﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2013-2018 Zongsoft Corporation <http://www.zongsoft.com>
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

namespace Zongsoft.Collections
{
	public class NamedCollectionEx<T> : NamedCollectionBaseEx<T>
	{
		#region 成员字段
		private Func<T, string> _getKey;
		#endregion

		#region 构造函数
		public NamedCollectionEx(Func<T, string> getKey, StringComparer comparer = null) : base(comparer)
		{
			_getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(T item)
		{
			return _getKey(item);
		}
		#endregion
	}
}
