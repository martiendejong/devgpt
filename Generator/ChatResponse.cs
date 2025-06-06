﻿using backend.Models;
using System.Text.Json.Serialization;

namespace DevGPT.NewAPI
{
    public abstract class ChatResponse<T> : Serializer<T> where T : ChatResponse<T>, new()
    {
        [JsonIgnore]
        public abstract T _example { get; }
        public static T Example => new T()._example;

        [JsonIgnore]
        public abstract string _signature { get; }
        public static string Signature => new T()._signature;

        protected ChatResponse() { }
    }
}