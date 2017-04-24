﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2016 Zongsoft Corporation <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Zongsoft.Data
{
	public class DataService<TEntity> : IDataService<TEntity>
	{
		#region 事件定义
		public event EventHandler<DataCountedEventArgs> Counted;
		public event EventHandler<DataCountingEventArgs> Counting;
		public event EventHandler<DataExecutedEventArgs> Executed;
		public event EventHandler<DataExecutingEventArgs> Executing;
		public event EventHandler<DataExistedEventArgs> Existed;
		public event EventHandler<DataExistingEventArgs> Existing;
		public event EventHandler<DataIncrementedEventArgs> Incremented;
		public event EventHandler<DataIncrementingEventArgs> Incrementing;
		public event EventHandler<DataDecrementedEventArgs> Decremented;
		public event EventHandler<DataDecrementingEventArgs> Decrementing;
		public event EventHandler<DataGettedEventArgs> Getted;
		public event EventHandler<DataGettingEventArgs> Getting;
		public event EventHandler<DataSelectedEventArgs> Selected;
		public event EventHandler<DataSelectingEventArgs> Selecting;
		public event EventHandler<DataDeletedEventArgs> Deleted;
		public event EventHandler<DataDeletingEventArgs> Deleting;
		public event EventHandler<DataInsertedEventArgs> Inserted;
		public event EventHandler<DataInsertingEventArgs> Inserting;
		public event EventHandler<DataUpdatedEventArgs> Updated;
		public event EventHandler<DataUpdatingEventArgs> Updating;
		#endregion

		#region 成员字段
		private string _name;
		private IDataAccess _dataAccess;
		private Zongsoft.Services.IServiceProvider _serviceProvider;
		private DataSearchAttribute.DataSearchKey[] _keys;
		#endregion

		#region 构造函数
		public DataService(Zongsoft.Services.IServiceProvider serviceProvider)
		{
			if(serviceProvider == null)
				throw new ArgumentNullException("serviceProvider");

			_serviceProvider = serviceProvider;
			_dataAccess = serviceProvider.ResolveRequired<IDataAccess>();

			//获取当前数据搜索键
			_keys = ((DataSearchAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(DataSearchAttribute), true))?.Keys;

			//注册数据递增键序列号
			DataSequence.Register(this);
		}

		public DataService(string name, Zongsoft.Services.IServiceProvider serviceProvider)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");
			if(serviceProvider == null)
				throw new ArgumentNullException("serviceProvider");

			_name = name.Trim();
			_serviceProvider = serviceProvider;
			_dataAccess = serviceProvider.ResolveRequired<IDataAccess>();

			//获取当前数据搜索键
			_keys = ((DataSearchAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(DataSearchAttribute), true))?.Keys;

			//注册数据递增键序列号
			DataSequence.Register(this);
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				if(string.IsNullOrWhiteSpace(_name))
				{
					var dataAccess = this.DataAccess;

					if(dataAccess != null && dataAccess.Mapper != null)
						_name = dataAccess.Mapper.Get<TEntity>();
				}

				return _name;
			}
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public IDataAccess DataAccess
		{
			get
			{
				return _dataAccess;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_dataAccess = value;
			}
		}

		public Zongsoft.Services.IServiceProvider ServiceProvider
		{
			get
			{
				return _serviceProvider;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_serviceProvider = value;
			}
		}
		#endregion

		#region 保护属性
		protected virtual Zongsoft.Security.CredentialPrincipal Principal
		{
			get
			{
				return Zongsoft.ComponentModel.ApplicationContextBase.Current.Principal as Zongsoft.Security.CredentialPrincipal;
			}
		}
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(IDictionary<string, object> inParameters)
		{
			IDictionary<string, object> outParameters;
			return this.Execute<T>(inParameters, out outParameters);
		}

		public virtual IEnumerable<T> Execute<T>(IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters)
		{
			//激发“Executing”事件
			var args = this.OnExecuting(typeof(T), inParameters, out outParameters);

			if(args.Cancel)
				return args.Result as IEnumerable<T>;

			//执行数据操作方法
			args.Result = this.DataAccess.Execute<T>(this.Name, args.InParameters, out outParameters);

			//激发“Executed”事件
			return this.OnExecuted(typeof(T), args.InParameters, ref outParameters, args.Result) as IEnumerable<T>;
		}

		public object ExecuteScalar(IDictionary<string, object> inParameters)
		{
			IDictionary<string, object> outParameters;
			return this.ExecuteScalar(inParameters, out outParameters);
		}

		public virtual object ExecuteScalar(IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters)
		{
			//激发“Executing”事件
			var args = this.OnExecuting(typeof(object), inParameters, out outParameters);

			if(args.Cancel)
				return args.Result;

			//执行数据操作方法
			args.Result = this.DataAccess.ExecuteScalar(this.Name, args.InParameters, out outParameters);

			//激发“Executed”事件
			return this.OnExecuted(typeof(object), args.InParameters, ref outParameters, args.Result);
		}
		#endregion

		#region 存在方法
		public virtual bool Exists(ICondition condition)
		{
			//激发“Existing”事件
			var args = this.OnExisting(condition);

			if(args.Cancel)
				return args.Result;

			//执行存在操作方法
			args.Result = this.DataAccess.Exists(this.Name, condition);

			//激发“Existed”事件
			return this.OnExisted(args.Condition, args.Result);
		}

		public virtual bool Exists<TKey>(TKey key)
		{
			return this.Exists(this.ConvertKey(key));
		}

		public virtual bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2)
		{
			return this.Exists(this.ConvertKey(key1, key2));
		}

		public virtual bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return this.Exists(this.ConvertKey(key1, key2, key3));
		}
		#endregion

		#region 计数方法
		public virtual int Count(ICondition condition, string includes = null)
		{
			//激发“Counting”事件
			var args = this.OnCounting(condition, includes);

			if(args.Cancel)
				return args.Result;

			//执行数据计数操作方法
			args.Result = this.DataAccess.Count(this.Name, args.Condition, args.Includes);

			//激发“Counted”事件
			return this.OnCounted(args.Condition, args.Includes, args.Result);
		}
		#endregion

		#region 递增方法
		public virtual long Increment(string member, ICondition condition, int interval = 1)
		{
			if(string.IsNullOrWhiteSpace(member))
				throw new ArgumentNullException(nameof(member));

			//激发“Incrementing”事件
			var args = this.OnIncrementing(member, condition, interval);

			if(args.Cancel)
				return args.Result;

			//执行递增操作方法
			args.Result = this.DataAccess.Increment(this.Name, member, condition, interval);

			//激发“Incremented”事件
			return this.OnIncremented(args.Member, args.Condition, args.Interval, args.Result);
		}

		public long Decrement(string member, ICondition condition, int interval = 1)
		{
			if(string.IsNullOrWhiteSpace(member))
				throw new ArgumentNullException(nameof(member));

			//激发“Decrementing”事件
			var args = this.OnDecrementing(member, condition, interval);

			if(args.Cancel)
				return args.Result;

			//执行递减操作方法
			args.Result = this.DataAccess.Decrement(this.Name, member, condition, interval);

			//激发“Decremented”事件
			return this.OnDecremented(args.Member, args.Condition, args.Interval, args.Result);
		}
		#endregion

		#region 查询方法
		public object Get<TKey>(TKey key, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, null, sortings);
		}

		public virtual object Get<TKey>(TKey key, string scope, Paging paging = null, params Sorting[] sortings)
		{
			return this.Get(this.ConvertKey(key), scope, paging, sortings, items => this.GetResult(items, new object[] { key }));
		}

		public object Get<TKey>(TKey key, Paging paging, string scope = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, scope, paging, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, null, sortings);
		}

		public virtual object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string scope, Paging paging = null, params Sorting[] sortings)
		{
			return this.Get(this.ConvertKey(key1, key2), scope, paging, sortings, items => this.GetResult(items, new object[] { key1, key2 }));
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, string scope = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, scope, paging, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, sortings);
		}

		public virtual object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string scope, Paging paging = null, params Sorting[] sortings)
		{
			return this.Get(this.ConvertKey(key1, key2, key3), scope, paging, sortings, items => this.GetResult(items, new object[] { key1, key2, key3 }));
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, string scope = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, scope, paging, sortings);
		}

		private object Get(ICondition condition, string scope, Paging paging, Sorting[] sortings, Func<IEnumerable<TEntity>, object> resultThunk)
		{
			//激发“Getting”事件
			var args = this.OnGetting(condition, scope, paging, sortings);

			if(args.Cancel)
				return args.Result;

			//执行数据获取操作方法
			var items = this.OnGet(args.Condition, args.Scope, args.Paging, args.Sortings);

			//进一步处理数据结果
			args.Result = resultThunk != null ? resultThunk(items) : items;

			//激发“Getted”事件
			return this.OnGetted(args.Condition, args.Scope, args.Paging, args.Sortings, args.Result);
		}

		protected virtual IEnumerable<TEntity> OnGet(ICondition condition, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.DataAccess.Select<TEntity>(this.Name, condition, scope, paging, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition = null, params Sorting[] sortings)
		{
			return this.Select(condition, null, string.Empty, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string scope, params Sorting[] sortings)
		{
			return this.Select(condition, null, scope, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, null, scope, paging, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, null, null, paging, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select(condition, null, scope, paging, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Grouping grouping, params Sorting[] sortings)
		{
			return this.Select(condition, grouping, string.Empty, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Grouping grouping, string scope, params Sorting[] sortings)
		{
			return this.Select(condition, grouping, scope, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Grouping grouping, string scope, Paging paging, params Sorting[] sortings)
		{
			//激发“Selecting”事件
			var args = this.OnSelecting(typeof(TEntity), condition, grouping, scope, paging, sortings);

			if(args.Cancel)
				return args.Result as IEnumerable<TEntity>;

			//执行数据查询操作
			args.Result = this.OnSelect(args.Condition, args.Grouping, args.Scope, args.Paging, args.Sortings);

			//激发“Selected”事件
			return this.OnSelected(typeof(TEntity), args.Condition, args.Grouping, args.Scope, args.Paging, args.Sortings, (IEnumerable<TEntity>)args.Result);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Grouping grouping, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, grouping, null, paging, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Grouping grouping, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select(condition, grouping, scope, paging, sortings);
		}

		protected virtual IEnumerable<TEntity> OnSelect(ICondition condition, Grouping grouping, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.DataAccess.Select<TEntity>(this.Name, condition, grouping, scope, paging, sortings);
		}
		#endregion

		#region 删除方法
		public virtual int Delete<TKey>(TKey key, params string[] cascades)
		{
			return this.Delete(this.ConvertKey(key), cascades);
		}

		public virtual int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, params string[] cascades)
		{
			return this.Delete(this.ConvertKey(key1, key2), cascades);
		}

		public virtual int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params string[] cascades)
		{
			return this.Delete(this.ConvertKey(key1, key2, key3), cascades);
		}

		public int Delete(ICondition condition, params string[] cascades)
		{
			//激发“Deleting”事件
			var args = this.OnDeleting(condition, cascades);

			if(args.Cancel)
				return args.Result;

			//执行数据删除操作
			args.Result = this.OnDelete(args.Condition, args.Cascades);

			//激发“Deleted”事件
			return this.OnDeleted(args.Condition, args.Cascades, args.Result);
		}

		protected virtual int OnDelete(ICondition condition, string[] cascades)
		{
			if(condition == null)
				throw new NotSupportedException("The condition cann't is null on delete operation.");

			return this.DataAccess.Delete(this.Name, condition, cascades);
		}
		#endregion

		#region 插入方法
		public int Insert(object data, string scope = null)
		{
			//激发“Inserting”事件
			var args = this.OnInserting(data, scope);

			if(args.Cancel)
				return args.Result;

			//执行数据插入操作
			args.Result = this.OnInsert(DataDictionary<TEntity>.GetDataDictionary(args.Data), args.Scope);

			//激发“Inserted”事件
			return this.OnInserted(args.Data, args.Scope, args.Result);
		}

		public int InsertMany(IEnumerable data, string scope = null)
		{
			//激发“Inserting”事件
			var args = this.OnInserting(data, scope);

			if(args.Cancel)
				return args.Result;

			//执行数据插入操作
			args.Result = this.OnInsertMany(DataDictionary<TEntity>.GetDataDictionaries(args.Data), args.Scope);

			//激发“Inserted”事件
			return this.OnInserted(args.Data, args.Scope, args.Result);
		}

		protected virtual int OnInsert(DataDictionary<TEntity> data, string scope)
		{
			if(data == null || data.Data == null)
				return 0;

			//尝试递增注册的递增键值
			DataSequence.Increments(this, data);

			//执行数据引擎的插入操作
			return this.DataAccess.Insert(this.Name, data, scope);
		}

		protected virtual int OnInsertMany(IEnumerable<DataDictionary<TEntity>> items, string scope)
		{
			if(items == null)
				return 0;

			int count = 0;

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				foreach(var item in items)
				{
					count += this.OnInsert(item, scope);
				}
			}

			return count;
		}
		#endregion

		#region 更新方法
		public virtual int Update<TKey>(object data, TKey key, string scope = null)
		{
			return this.Update(data, this.ConvertKey(key), scope);
		}

		public virtual int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, string scope = null)
		{
			return this.Update(data, this.ConvertKey(key1, key2), scope);
		}

		public virtual int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, string scope = null)
		{
			return this.Update(data, this.ConvertKey(key1, key2, key3), scope);
		}

		public virtual int UpdateMany<TKey>(IEnumerable data, TKey key, string scope = null)
		{
			return this.UpdateMany(data, this.ConvertKey(key), scope);
		}

		public virtual int UpdateMany<TKey1, TKey2>(IEnumerable data, TKey1 key1, TKey2 key2, string scope = null)
		{
			return this.UpdateMany(data, this.ConvertKey(key1, key2), scope);
		}

		public virtual int UpdateMany<TKey1, TKey2, TKey3>(IEnumerable data, TKey1 key1, TKey2 key2, TKey3 key3, string scope = null)
		{
			return this.UpdateMany(data, this.ConvertKey(key1, key2, key3), scope);
		}

		public int Update(object data, ICondition condition = null, string scope = null)
		{
			//激发“Updating”事件
			var args = this.OnUpdating(data, condition, scope);

			if(args.Cancel)
				return args.Result;

			//执行数据更新操作
			args.Result = this.OnUpdate(DataDictionary<TEntity>.GetDataDictionary(args.Data), args.Condition, args.Scope);

			//激发“Updated”事件
			return this.OnUpdated(args.Data, args.Condition, args.Scope, args.Result);
		}

		public int Update(object data, string scope, ICondition condition = null)
		{
			return this.Update(data, condition, scope);
		}

		public int UpdateMany(IEnumerable data, ICondition condition = null, string scope = null)
		{
			//激发“Updating”事件
			var args = this.OnUpdating(data, condition, scope);

			if(args.Cancel)
				return args.Result;

			//执行数据更新操作
			args.Result = this.OnUpdateMany(DataDictionary<TEntity>.GetDataDictionaries(args.Data), args.Condition, args.Scope);

			//激发“Updated”事件
			return this.OnUpdated(args.Data, args.Condition, args.Scope, args.Result);
		}

		public int UpdateMany(IEnumerable data, string scope, ICondition condition = null)
		{
			return this.UpdateMany(data, condition, scope);
		}

		protected virtual int OnUpdate(DataDictionary<TEntity> data, ICondition condition, string scope)
		{
			if(data == null || data.Data == null)
				return 0;

			return this.DataAccess.Update(this.Name, data, condition, scope);
		}

		protected virtual int OnUpdateMany(IEnumerable<DataDictionary<TEntity>> items, ICondition condition, string scope)
		{
			if(items == null)
				return 0;

			int count = 0;

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				foreach(var item in items)
				{
					count += this.OnUpdate(item, condition, scope);
				}
			}

			return count;
		}
		#endregion

		#region 激发事件
		protected int OnCounted(ICondition condition, string includes, int result)
		{
			var args = new DataCountedEventArgs(this.Name, condition, includes, result);
			this.OnCounted(args);
			return args.Result;
		}

		protected DataCountingEventArgs OnCounting(ICondition condition, string includes)
		{
			var args = new DataCountingEventArgs(this.Name, condition, includes);
			this.OnCounting(args);
			return args;
		}

		protected object OnExecuted(Type resultType, IDictionary<string, object> inParameters, ref IDictionary<string, object> outParameters, object result)
		{
			var args = new DataExecutedEventArgs(this.Name, resultType, inParameters, null, result);
			this.OnExecuted(args);
			outParameters = args.OutParameters;
			return args.Result;
		}

		protected DataExecutingEventArgs OnExecuting(Type resultType, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters)
		{
			var args = new DataExecutingEventArgs(this.Name, resultType, inParameters);
			this.OnExecuting(args);
			outParameters = args.OutParameters;
			return args;
		}

		protected bool OnExisted(ICondition condition, bool result)
		{
			var args = new DataExistedEventArgs(this.Name, condition, result);
			this.OnExisted(args);
			return args.Result;
		}

		protected DataExistingEventArgs OnExisting(ICondition condition, bool cancel = false)
		{
			var args = new DataExistingEventArgs(this.Name, condition, cancel);
			this.OnExisting(args);
			return args;
		}

		protected long OnIncremented(string member, ICondition condition, int interval, long result)
		{
			var args = new DataIncrementedEventArgs(this.Name, member, condition, interval, result);
			this.OnIncremented(args);
			return args.Result;
		}

		protected DataIncrementingEventArgs OnIncrementing(string member, ICondition condition, int interval = 1, bool cancel = false)
		{
			var args = new DataIncrementingEventArgs(this.Name, member, condition, interval, cancel);
			this.OnIncrementing(args);
			return args;
		}

		protected long OnDecremented(string member, ICondition condition, int interval, long result)
		{
			var args = new DataDecrementedEventArgs(this.Name, member, condition, interval, result);
			this.OnDecremented(args);
			return args.Result;
		}

		protected DataDecrementingEventArgs OnDecrementing(string member, ICondition condition, int interval = 1, bool cancel = false)
		{
			var args = new DataDecrementingEventArgs(this.Name, member, condition, interval, cancel);
			this.OnDecrementing(args);
			return args;
		}

		protected object OnGetted(ICondition condition, string scope, Paging paging, Sorting[] sortings, object result)
		{
			var args = new DataGettedEventArgs(this.Name, condition, scope, paging, sortings, result);
			this.OnGetted(args);
			return args.Result;
		}

		protected DataGettingEventArgs OnGetting(ICondition condition, string scope, Paging paging, Sorting[] sortings)
		{
			var args = new DataGettingEventArgs(this.Name, condition, scope, paging, sortings);
			this.OnGetting(args);
			return args;
		}

		protected IEnumerable<TEntity> OnSelected(Type entityType, ICondition condition, Grouping grouping, string scope, Paging paging, Sorting[] sortings, IEnumerable<TEntity> result)
		{
			var args = new DataSelectedEventArgs(this.Name, entityType, condition, grouping, scope, paging, sortings, result);
			this.OnSelected(args);
			return args.Result as IEnumerable<TEntity>;
		}

		protected DataSelectingEventArgs OnSelecting(Type entityType, ICondition condition, Grouping grouping, string scope, Paging paging, Sorting[] sortings)
		{
			var args = new DataSelectingEventArgs(this.Name, entityType, condition, grouping, scope, paging, sortings);
			this.OnSelecting(args);
			return args;
		}

		protected int OnDeleted(ICondition condition, string[] cascades, int result)
		{
			var args = new DataDeletedEventArgs(this.Name, condition, cascades, result);
			this.OnDeleted(args);
			return args.Result;
		}

		protected DataDeletingEventArgs OnDeleting(ICondition condition, string[] cascades)
		{
			var args = new DataDeletingEventArgs(this.Name, condition, cascades);
			this.OnDeleting(args);
			return args;
		}

		protected int OnInserted(object data, string scope, int result)
		{
			var args = new DataInsertedEventArgs(this.Name, data, scope, result);
			this.OnInserted(args);
			return args.Result;
		}

		protected DataInsertingEventArgs OnInserting(object data, string scope)
		{
			var args = new DataInsertingEventArgs(this.Name, data, scope);
			this.OnInserting(args);
			return args;
		}

		protected int OnUpdated(object data, ICondition condition, string scope, int result)
		{
			var args = new DataUpdatedEventArgs(this.Name, data, condition, scope, result);
			this.OnUpdated(args);
			return args.Result;
		}

		protected DataUpdatingEventArgs OnUpdating(object data, ICondition condition, string scope)
		{
			var args = new DataUpdatingEventArgs(this.Name, data, condition, scope);
			this.OnUpdating(args);
			return args;
		}

		protected virtual void OnCounted(DataCountedEventArgs args)
		{
			var e = this.Counted;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnCounting(DataCountingEventArgs args)
		{
			var e = this.Counting;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnExecuted(DataExecutedEventArgs args)
		{
			var e = this.Executed;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnExecuting(DataExecutingEventArgs args)
		{
			var e = this.Executing;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnExisted(DataExistedEventArgs args)
		{
			var e = this.Existed;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnExisting(DataExistingEventArgs args)
		{
			var e = this.Existing;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnIncremented(DataIncrementedEventArgs args)
		{
			var e = this.Incremented;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnIncrementing(DataIncrementingEventArgs args)
		{
			var e = this.Incrementing;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnDecremented(DataDecrementedEventArgs args)
		{
			var e = this.Decremented;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnDecrementing(DataDecrementingEventArgs args)
		{
			var e = this.Decrementing;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnGetted(DataGettedEventArgs args)
		{
			var e = this.Getted;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnGetting(DataGettingEventArgs args)
		{
			var e = this.Getting;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnSelected(DataSelectedEventArgs args)
		{
			var e = this.Selected;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnSelecting(DataSelectingEventArgs args)
		{
			var e = this.Selecting;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnDeleted(DataDeletedEventArgs args)
		{
			var e = this.Deleted;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnDeleting(DataDeletingEventArgs args)
		{
			var e = this.Deleting;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnInserted(DataInsertedEventArgs args)
		{
			var e = this.Inserted;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnInserting(DataInsertingEventArgs args)
		{
			var e = this.Inserting;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnUpdated(DataUpdatedEventArgs args)
		{
			var e = this.Updated;

			if(e != null)
				e(this, args);
		}

		protected virtual void OnUpdating(DataUpdatingEventArgs args)
		{
			var e = this.Updating;

			if(e != null)
				e(this, args);
		}
		#endregion

		#region 键值操作
		/// <summary>
		/// 根据指定的查询参数值获取对应的查询键值对数组或<see cref="ICondition"/>条件。
		/// </summary>
		/// <param name="values">传入的查询值数组。</param>
		/// <returns>返回对应的键值对数组或者<see cref="ICondition"/>条件。</returns>
		/// <remarks>
		///		<para>基类的实现始终返回当前数据服务对应的主键的键值对数组。</para>
		///		<para>对于重载者的提示：如果<paramref name="values"/>参数值为空(null)或空数组(零长度)，则应返回当前实体的主键的键值对数组（调用基类的<see cref="GetKey(object[])"/>即可）。</para>
		/// </remarks>
		protected virtual object GetKey(object[] values)
		{
			if(values == null || values.Length == 0)
				return null;

			//如果查询参数只有一个，并且当前数据服务启用了数据搜索特性
			if(values.Length == 1 && _keys != null && _keys.Length > 0)
			{
				//根据查询参数获取对应的搜索条件
				var condition = this.GetSearchCondition(values[0] as string);

				if(condition != null)
					return condition;
			}

			var primaryKey = this.DataAccess.GetKey(this.Name);

			if(primaryKey == null || primaryKey.Length == 0)
				return null;

			var result = new object[Math.Min(primaryKey.Length, values.Length)];

			for(int i = 0; i < result.Length; i++)
			{
				result[i] = new KeyValuePair<string, object>(primaryKey[i], values[i]);
			}

			return result;
		}

		protected virtual ICondition ConvertKey<TKey>(TKey key)
		{
			return this.EnsureInquiryKey(new object[] { key });
		}

		protected virtual ICondition ConvertKey<TKey1, TKey2>(TKey1 key1, TKey2 key2)
		{
			return this.EnsureInquiryKey(new object[] { key1, key2 });
		}

		protected virtual ICondition ConvertKey<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return this.EnsureInquiryKey(new object[] { key1, key2, key3 });
		}
		#endregion

		#region 私有方法
		private ICondition GetSearchCondition(string argument)
		{
			if(_keys == null || _keys.Length == 0)
				return null;

			if(string.IsNullOrWhiteSpace(argument))
				return null;

			var index = argument.IndexOf(':');

			if(index < 1)
				return null;

			var tag = argument.Substring(0, index);
			var value = index < argument.Length - 1 ? argument.Substring(index + 1) : null;

			foreach(var key in _keys)
			{
				if(key.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
				{
					var conditions = new ConditionCollection(ConditionCombination.Or);

					foreach(var field in key.Fields)
					{
						conditions.Add(new Condition(field, value));
					}

					if(conditions.Count == 1)
						return conditions[0];
					else
						return conditions;
				}
			}

			return null;
		}

		private ICondition EnsureInquiryKey(object[] values)
		{
			if(values != null && values.Length > 3)
				throw new NotSupportedException("Too many the keys.");

			//获取查询键值对数组
			var inquiryKey = this.GetKey(values ?? new object[0]);

			if(inquiryKey == null)
				return null;

			//如果查询键可别转换成条件，则直接返回转换后的条件
			var condition = this.GetCondition(inquiryKey);

			if(condition != null)
				return condition;

			//如果最后查询键不可遍历，则抛出异常
			var items = inquiryKey as IEnumerable;

			if(items == null)
				throw new InvalidOperationException($"Invalid inquiry key: {inquiryKey}");

			var conditions = new ConditionCollection(ConditionCombination.And);

			foreach(var item in items)
			{
				condition = this.GetCondition(item);

				if(condition != null)
					conditions.Add(condition);
			}

			if(conditions.Count > 1)
				return conditions;

			return conditions.FirstOrDefault();
		}

		private ICondition GetCondition(object item)
		{
			var condition = item as ICondition;

			if(condition != null)
				return condition;

			if(item is DictionaryEntry && ((DictionaryEntry)item).Key != null)
				return Condition.Equal(((DictionaryEntry)item).Key.ToString(), ((DictionaryEntry)item).Value);
			if(item is KeyValuePair<string, object> && ((KeyValuePair<string, object>)item).Key != null)
				return Condition.Equal(((KeyValuePair<string, object>)item).Key, ((KeyValuePair<string, object>)item).Value);

			return null;
		}

		private object GetResult(IEnumerable<TEntity> result, object[] values)
		{
			//获取当前数据服务对应的主键
			var primaryKey = this.DataAccess.GetKey(this.Name);

			//获取当前查询对应的查询键名称数组
			var inquiryKey = this.GetKey(values);

			if(inquiryKey == null)
				return result;

			string[] keys = null;

			if(inquiryKey is Condition)
				keys = new string[] { ((Condition)inquiryKey).Name };
			else if(inquiryKey is IEnumerable<ICondition>)
				keys = ((IEnumerable<ICondition>)inquiryKey).Where(p => p is Condition).Select(p => ((Condition)p).Name).ToArray();
			if(inquiryKey is IEnumerable<KeyValuePair<string, object>>)
				keys = ((IEnumerable<KeyValuePair<string, object>>)inquiryKey).Select(p => p.Key).ToArray();
			else if(inquiryKey is IEnumerable<object>)
				keys = ((IEnumerable<object>)inquiryKey).Where(p => p is KeyValuePair<string, object>).Select(p => ((KeyValuePair<string, object>)p).Key).ToArray();
			else if(inquiryKey is KeyValuePair<string, object>)
				keys = new string[] { ((KeyValuePair<string, object>)inquiryKey).Key };

			//如果查询键与主键完全一致，则返回单数据（主键查询）
			if(primaryKey != null && keys != null && this.CompareStringArray(primaryKey, keys))
				return result.FirstOrDefault();

			return result;
		}

		private bool CompareStringArray(string[] a, string[] b)
		{
			if((a == null || a.Length == 0) && (b == null || b.Length == 0))
				return true;

			if((a == null && b != null) || (a != null && b == null) || a.Length != b.Length)
				return false;

			for(int i = 0; i < a.Length; i++)
			{
				if(!Array.Exists(b, item => string.Equals(a[i], item, StringComparison.OrdinalIgnoreCase)))
					return false;
			}

			return true;
		}
		#endregion

		#region 嵌套子类
		private static class DataSequence
		{
			#region 常量定义
			private const string SEQUENCE_KEY_PREFIX = "Zongsoft.Data.Sequence";
			#endregion

			#region 静态缓存
			private static readonly ConcurrentDictionary<DataService<TEntity>, DataSequenceToken[]> _cache = new ConcurrentDictionary<DataService<TEntity>, DataSequenceToken[]>();
			#endregion

			#region 公共方法
			public static bool Register(DataService<TEntity> dataService)
			{
				var attributes = (DataSequenceAttribute[])Attribute.GetCustomAttributes(dataService.GetType(), typeof(DataSequenceAttribute), true);

				if(attributes == null || attributes.Length == 0)
					return false;

				var tokens = new DataSequenceToken[attributes.Length];

				for(int i = 0; i < attributes.Length; i++)
				{
					var attribute = attributes[i];
					var sequence = string.IsNullOrWhiteSpace(attribute.SequenceName) ? dataService.ServiceProvider.ResolveRequired<Common.ISequence>() : dataService.ServiceProvider.ResolveRequired(attribute.SequenceName) as Common.ISequence;

					if(sequence == null)
						throw new InvalidOperationException($"Not found '{attribute.SequenceName}' sequence for the '{dataService.Name}' data service.");

					var property = typeof(TEntity).GetProperty(attribute.Keys[attribute.Keys.Length - 1], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

					if(property == null)
						throw new InvalidOperationException($"The '{attribute.Keys[attribute.Keys.Length - 1]}' sequence property is not existed.");

					if(!Zongsoft.Common.TypeExtension.IsNumeric(property.PropertyType))
						throw new InvalidOperationException($"The '{property.Name}' sequence property type not is numeric.");

					tokens[i] = new DataSequenceToken(sequence, attribute, property.PropertyType);
				}

				return _cache.TryAdd(dataService, tokens);
			}

			public static void Increments(DataService<TEntity> dataService, DataDictionary<TEntity> data)
			{
				DataSequenceToken[] tokens;

				if(_cache.TryGetValue(dataService, out tokens))
				{
					foreach(var token in tokens)
					{
						var sequenceKey = GetSequenceKey(data, token.Attribute);

						if(token.Attribute.Keys.Length == 1)
							data.Set(token.Attribute.Keys[0], () => token.Sequence.Increment(sequenceKey, 1, token.Attribute.Seed), value => (long)System.Convert.ChangeType(value, typeof(long)) == 0);
					}
				}
			}
			#endregion

			#region 私有方法
			private static string GetSequenceKey(DataDictionary<TEntity> data, DataSequenceAttribute attribute)
			{
				var result = SEQUENCE_KEY_PREFIX;

				for(int i = 0; i < attribute.Keys.Length - 1; i++)
				{
					var value = data.Get(attribute.Keys[i]);

					if(value != null)
						result += ":" + value.ToString().ToLowerInvariant();
				}

				return result += ":" + attribute.Keys[attribute.Keys.Length - 1].ToLowerInvariant();
			}
			#endregion

			#region 嵌套子类
			private class DataSequenceToken
			{
				public readonly Common.ISequence Sequence;
				public readonly DataSequenceAttribute Attribute;
				public readonly Type Type;

				public DataSequenceToken(Common.ISequence sequence, DataSequenceAttribute attribute, Type type)
				{
					this.Sequence = sequence;
					this.Attribute = attribute;
					this.Type = type;
				}
			}
			#endregion
		}
		#endregion
	}
}
