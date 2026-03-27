using System;
using System.IO;
using UnityEngine;

namespace Project.Infrastructure.Persistence
{
    public sealed class JsonSaveService
    {
        public void Save<T>(string fileName, T data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must not be empty.", nameof(fileName));
            }

            var path = BuildPath(fileName);
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
        }

        public bool TryLoad<T>(string fileName, out T data)
        {
            data = default;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var path = BuildPath(fileName);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string BuildPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
