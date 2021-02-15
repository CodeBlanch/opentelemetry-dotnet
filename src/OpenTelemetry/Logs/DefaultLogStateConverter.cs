// <copyright file="DefaultLogStateConverter.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NET461 || NETSTANDARD2_0
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenTelemetry.Logs
{
    internal static class DefaultLogStateConverter
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyGetter>> TypePropertyCache = new ConcurrentDictionary<Type, List<PropertyGetter>>();

        public static void ConvertState(ActivityTagsCollection tags, object state)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object>> stateList)
            {
                for (int i = 0; i < stateList.Count; i++)
                {
                    AddStateItemToMessage(tags, stateList[i]);
                }
            }
            else if (state is IEnumerable<KeyValuePair<string, object>> stateValues)
            {
                foreach (KeyValuePair<string, object> item in stateValues)
                {
                    AddStateItemToMessage(tags, item);
                }
            }
            else if (!(state is string))
            {
                AddObjectPropertiesToMessageData(tags, state);
            }
        }

        private static void AddStateItemToMessage(ActivityTagsCollection tags, KeyValuePair<string, object> item)
        {
            if (item.Key != "{OriginalFormat}")
            {
                tags[item.Key] = item.Value;
            }
        }

        private static void AddObjectPropertiesToMessageData(ActivityTagsCollection tags, object item)
        {
            Type type = item.GetType();
            if (!TypePropertyCache.TryGetValue(type, out List<PropertyGetter> propertyGetters))
            {
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyGetters = new List<PropertyGetter>(properties.Length);

                foreach (PropertyInfo propertyInfo in properties)
                {
                    if (propertyInfo.CanRead)
                    {
                        propertyGetters.Add(new PropertyGetter(type, propertyInfo));
                    }
                }

                TypePropertyCache.TryAdd(type, propertyGetters);
            }

            foreach (PropertyGetter propertyGetter in propertyGetters)
            {
                tags[propertyGetter.PropertyName] = propertyGetter.GetPropertyFunc(item);
            }
        }

        private class PropertyGetter
        {
            public PropertyGetter(Type type, PropertyInfo propertyInfo)
            {
                this.PropertyName = propertyInfo.Name;

                this.GetPropertyFunc = BuildGetPropertyFunc(propertyInfo, type);
            }

            public string PropertyName { get; }

            public Func<object, object> GetPropertyFunc { get; }

            private static Func<object, object> BuildGetPropertyFunc(PropertyInfo propertyInfo, Type runtimePropertyType)
            {
                MethodInfo realMethod = propertyInfo.GetMethod;

                Type declaringType = propertyInfo.DeclaringType;

                Type declaredPropertyType = propertyInfo.PropertyType;

                DynamicMethod dynamicMethod = new DynamicMethod(
                    nameof(PropertyGetter),
                    typeof(object),
                    new[] { typeof(object) },
                    typeof(PropertyGetter).Module,
                    skipVisibility: true);
                ILGenerator generator = dynamicMethod.GetILGenerator();

                generator.Emit(OpCodes.Ldarg_0);

                if (declaringType.IsValueType)
                {
                    generator.Emit(OpCodes.Unbox, declaringType);
                    generator.Emit(OpCodes.Call, realMethod);
                }
                else
                {
                    generator.Emit(OpCodes.Castclass, declaringType);
                    generator.Emit(OpCodes.Callvirt, realMethod);
                }

                if (declaredPropertyType != runtimePropertyType && declaredPropertyType.IsValueType)
                {
                    generator.Emit(OpCodes.Box, declaredPropertyType);
                }

                generator.Emit(OpCodes.Ret);

                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }
        }
    }
}
#endif
