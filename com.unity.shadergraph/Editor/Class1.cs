using System;
using UnityEngine;
using Utf8Json;
using Utf8Json.Resolvers;
using Utf8Json.Unity;

namespace UnityEditor
{
    class Person
    {
        public int Age;
        public string Name;
    }

    [InitializeOnLoad]
    public static class Class1
    {
        static Class1()
        {
            CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatter[]
            {
            }, new[]
            {
                UnityResolver.Instance,
                StandardResolver.Default
            });
            var p = new Person { Age = 99, Name = "foobar" };
            var vec3 = new Vector3(1, 2, 3);
            var json = JsonSerializer.PrettyPrint(JsonSerializer.Serialize(vec3));
//            Debug.Log(json);
        }
    }
}
