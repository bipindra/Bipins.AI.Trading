using System;
using System.Reflection;
using Bipins.AI.Vectors.Pinecone;
using Bipins.AI.Vectors.Milvus;
using Bipins.AI.Vectors.Weaviate;

static void Dump(Type t)
{
    Console.WriteLine($"\n== {t.FullName} ==");
    Console.WriteLine("Properties:");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        Console.WriteLine("  " + p.PropertyType.Name + " " + p.Name);
}

Dump(typeof(PineconeOptions));
Dump(typeof(MilvusOptions));
Dump(typeof(WeaviateOptions));
