using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Aurora.Core.IO
{
    public static class Persistence
    {
        /// <summary>
        /// Writes an object to a XML file.
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="filePath">The file path</param>
        /// <param name="obj">The object to be saved</param>
        public static void WriteXml<T>(string filePath, T obj)
        {
            TextWriter writer = null;
            try
            {
                var serialiser = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath);
                serialiser.Serialize(writer, obj);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads a XML file and outputs an object
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="filePath">The file path</param>
        /// <returns>The deserialised object</returns>
        public static T ReadXml<T>(string filePath)
        {
            TextReader reader = null;
            try
            {
                var serialiser = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serialiser.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
        }
    }
}
