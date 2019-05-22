﻿using Natasha.Engine.Builder;
using Natasha.Engine.Builder.Reverser;
using Natasha.Engine.Template;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Natasha
{
    public class MethodBuilder : BuilderStandard<MethodBuilder>
    {
        private static Regex _get_class;
        private string _method_name;
        static MethodBuilder()
        {
            _get_class = new Regex(@"\sclass.*?(?<result>[a-zA-Z0-9]*?)[\s]*{", RegexOptions.Compiled | RegexOptions.Singleline);
        }
        private List<KeyValuePair<Type, string>> _parameters;
        private List<Type> _parameters_types;
        private Type _return_type;
        private Type _delegate_type;
        private MethodInfo _info;
        private string _method;
        public bool _useFileComplie;
        public static Action<string> SingleError;

        public MethodBuilder() : base()
        {
            _link = this;
            _parameters = new List<KeyValuePair<Type, string>>();
            _parameters_types = new List<Type>();
            _return_type = null;
            _method = null;
            _method_name = "DynimacMethod";
        }

        public static MethodBuilder NewMethod
        {
            get { return new MethodBuilder(); }
        }


        public MethodBuilder UseFileComplie(bool shut=true)
        {
            _useFileComplie = shut;
            return this;
        }

        /// <summary>
        /// 设置函数名
        /// </summary>
        /// <param name="name">函数名</param>
        /// <returns></returns>
        public MethodBuilder MethodName(string name)
        {
            _method_name = name;
            return this;
        }


        /// <summary>
        /// 根据已经存在的函数来设置内容
        /// </summary>
        /// <param name="info">函数成员</param>
        /// <returns></returns>
        public MethodBuilder From(MethodInfo info)
        {
            Method(info);
            _info = info;
            return this;
        }
        public MethodBuilder From<T>(string name)
        {
            var info = typeof(T).GetMethod(name);
            return From(info);

        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名字</param>
        /// <returns></returns>
        public MethodBuilder Param<T>(string key)
        {
            return Param(typeof(T), key);
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="type">参数类型</param>
        /// <param name="key">参数名字</param>
        /// <returns></returns>
        public MethodBuilder Param(Type type, string key)
        {
            Using(type);
            _parameters_types.Add(type);
            _parameters.Add(new KeyValuePair<Type, string>(type, key));
            return this;
        }


        /// <summary>
        /// 设置返回类型
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <returns></returns>
        public MethodBuilder Return<T>()
        {
            return Return(typeof(T));
        }
        /// <summary>
        /// 设置返回类型,并生成运行时委托
        /// </summary>
        /// <param name="type">返回类型</param>
        /// <returns></returns>
        public MethodBuilder Return(Type type = null)
        {
            _return_type = type;
            Using(type);
            //根据参数，生成动态委托类型
            _delegate_type = DelegateBuilder.GetDelegate(_parameters_types.ToArray(), type);
            return this;
        }

        /// <summary>
        /// 返回动态委托
        /// </summary>
        /// <returns></returns>
        public Delegate Create()
        {
            //返回运行时委托
            return GetRuntimeMethodDelegate(_delegate_type);
        }
        public T Create<T>() where T : Delegate
        {
            //返回运行时委托
            return (T)GetRuntimeMethodDelegate(typeof(T));
        }
     
        /// <summary>
        /// 创建函数委托
        /// </summary>
        /// <param name="delegateType">委托类型</param>
        /// <returns></returns>
        public Delegate GetRuntimeMethodDelegate(Type delegateType)
        {
            string body = Static().Body(GetScript()).Builder();
            Assembly assembly = null;
            if (!_useFileComplie)
            {
                assembly = ScriptComplier.StreamComplier(body, _class_name, SingleError);
            }
            else
            {
                assembly = ScriptComplier.FileComplier(body, _class_name, SingleError);
            }
           

            if (assembly == null)
            {
                return null;
            }

            return AssemblyOperator
                .Loader(assembly)[_class_name]
                .GetMethod(_method_name)
                .CreateDelegate(delegateType);
        }

        /// <summary>
        /// 获取动态方法体
        /// </summary>
        /// <returns></returns>
        public string GetScript(bool isStatic = true)
        {
            if (_info != null)
            {
                MethodTemplate template = new MethodTemplate(_info);
                return template.Body(_text).Builder();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"public ");
                if (isStatic)
                {
                    sb.Append($"static ");
                }
                if (_return_type == null)
                {
                    sb.Append("void");
                }
                else
                {
                    sb.Append(_return_type.Name);
                }
                if (_method == null)
                {
                    sb.Append($" {_method_name}");
                }
                else
                {
                    sb.Append(" " + _method);
                }
                sb.Append("(");
                if (_parameters.Count > 0)
                {
                    sb.Append($"{TypeReverser.Get(_parameters[0].Key)} {_parameters[0].Value}");
                    for (int i = 1; i < _parameters.Count; i++)
                    {
                        sb.Append($",{TypeReverser.Get(_parameters[i].Key)} {_parameters[i].Value}");
                    }
                }
                sb.Append("){");
                sb.Append(_text);
                sb.Append("}");
                return sb.ToString();
            }
        }


        /// <summary>
        /// 静态调用，编译传进来的代码
        /// </summary>
        /// <param name="script">完全的脚本</param>
        /// <param name="type">委托类型</param>
        /// <returns></returns>
        public static Delegate Create(string script, Type type)
        {
            //返回运行时委托
            return GetRuntimeMethodDelegate(script, type);
        }
        /// <summary>
        /// 静态调用，编译传进来的代码
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="script">完全的脚本</param>
        /// <returns></returns>
        public static T Create<T>(string script) where T : Delegate
        {
            //返回运行时委托
            return (T)GetRuntimeMethodDelegate(script, typeof(T));
        }
        /// <summary>
        /// 创建函数委托
        /// </summary>
        /// <param name="delegateType">委托类型</param>
        /// <returns></returns>
        public static Delegate GetRuntimeMethodDelegate(string content, Type delegateType)
        {
            string className = _get_class.Match(content).Groups["result"].Value;
            Assembly assembly = ScriptComplier.StreamComplier(content, className, SingleError);

            if (assembly == null)
            {
                return null;
            }

            return AssemblyOperator
                .Loader(assembly)[className]
                .GetMethod("DynamicMethod")
                .CreateDelegate(delegateType);
        }
    }
}
